using Rysy.Components;
using Rysy.Graphics;
using Rysy.History;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Selections;
using Rysy.Signals;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Rysy.Helpers;

public sealed class PrefabHelper : IHasComponentRegistry, ISignalEmitter {
    private ListenableDictionary<string, Prefab>? _currentPrefabsMutable;

    public PrefabLayer EditorLayer => field ??= new PrefabLayer(this);
    
    public ListenableDictionary<string, Prefab> CurrentPrefabs => _currentPrefabsMutable ??= Load();

    public IReadOnlyList<IEditorLayer> ValidLayers => Registry?.GetAll<IEditorLayer>() ?? [];

    private ListenableDictionary<string, Prefab> Load() {
        _currentPrefabsMutable = new(StringComparer.Ordinal);

        var fs = SettingsHelper.GetFilesystem(perProfile: true);

        foreach (var file in fs.FindFilesInDirectoryRecursive("prefabs", "json")) {
            LoadFromFile(file);
        }
        
        _currentPrefabsMutable.OnChanged += OnChanged;

        return _currentPrefabsMutable;
    }

    private void OnChanged() {
        this.Emit(new PrefabsChanged(this));
    }

    private Prefab? LoadFromFile(string path) {
        var fs = SettingsHelper.GetFilesystem(perProfile: true);

        if (fs.OpenFile(path, stream => { 
            // ReSharper disable once VariableHidesOuterVariable
            if (!JsonExtensions.TryDeserialize<Prefab>(stream.ReadAllText(), out var prefab))
                return null;

            prefab.Filename = path;

            return prefab;
        }) is {} prefab) {
            _currentPrefabsMutable![prefab.Name] = prefab;
            return prefab;
        }

        return null;
    }

    public static bool SelectionsLegal(IReadOnlyList<IEditorLayer> layers, List<CopypasteHelper.CopiedSelection> selections) 
        => selections.All(s => s.ResolveLayer(layers) is not RoomLayer);

    public void RegisterPrefab(string name, List<CopypasteHelper.CopiedSelection> selections) {
        if (!SelectionsLegal(ValidLayers, selections))
            return;
        if (_currentPrefabsMutable is null)
            return;

        var prefab = new Prefab {
            Name = name,
            Objects = selections,
        };

        lock (_currentPrefabsMutable) {
            _currentPrefabsMutable[prefab.Name] = prefab;
        }

        var path = GetPrefabPath(prefab);
        var fs = SettingsHelper.GetFilesystem(perProfile: true);
        prefab.Filename = path;
        
        fs.TryWriteToFile(path, prefab.ToJsonUtf8());
    }

    public void Remove(string name) {
        var prefabs = _currentPrefabsMutable;
        if (prefabs is null)
            return;
        
        if (!prefabs.Remove(name, out var prefab))
            return;

        var path = prefab.Filename;
        var fs = SettingsHelper.GetFilesystem(perProfile: true);
        fs.TryDeleteFile(path);
    }

    public Placement? PlacementFromName(string name) {
        if (!CurrentPrefabs.TryGetValue(name, out var prefab)) {
            return null;
        }

        var placement = new Placement(name);
        placement.PlacementHandler = new PrefabPlacementHandler(this, prefab);

        return placement;
    }

    private static string GetPrefabPath(Prefab prefab) {
        var fs = SettingsHelper.GetFilesystem(perProfile: true);
        
        var filename = prefab.Name.ToValidFilename();

        filename = filename.GetDeduplicatedIn(fs.FindFilesInDirectory("prefabs", "")
            .Select(f => Path.GetFileNameWithoutExtension(f)));

        return $"prefabs/{filename}.json";
    }

    public class Prefab {
        public Prefab() { }

        public string Name { get; set; }

        public List<CopypasteHelper.CopiedSelection> Objects { get; set; }

        [JsonIgnore]
        public string Filename { get; internal set; }
    }

    private sealed record class PrefabPlacementHandler(PrefabHelper Helper, Prefab Prefab) : IPlacementHandler {
        public ISelectionHandler CreateSelection(EditorState editorState, Placement placement, Vector2 pos, Room room) {
            var selections = CopypasteHelper.PasteSelections(Helper.ValidLayers,
                EditorState.Current ?? throw new UnreachableException("EditorState.Current is null"), 
                Prefab.Objects, history: null, map: null, room, pos, out var pastedRooms);
            if (pastedRooms) {
                throw new NotImplementedException("Pasting rooms in prefabs is not supported yet!");
            }

            return new MergedSelectionHandler(selections ?? new());
        }

        public IEnumerable<ISprite> GetPreviewSprites(EditorState editorState, ISelectionHandler handler, Vector2 pos, Room room) {
            List<ISprite> sprites = new();

            if (handler is MergedSelectionHandler h) {
                var entitySelections = h.Selections.Select(s => s.Handler).OfType<EntitySelectionHandler>();
                var tileSelections = h.Selections.Select(s => s.Handler).OfType<TileSelectionHandler>();

                var firstEntitySelection = entitySelections.FirstOrDefault();

                var prevPos = firstEntitySelection?.Entity.Pos;
                var delta = pos - prevPos;
                if (firstEntitySelection is { })
                    AddEntitySprites(pos, sprites, entitySelections);


                foreach (var item in tileSelections) {
                    item.Rect = item.Rect.MovedBy(delta ?? (pos - item.Rect.Location.ToVector2()));
                    sprites.AddRange(item.GetSprites(Color.Red).Cast<ISprite>());
                }
            }

            return sprites.OrderByDescending(x => x.Depth);
        }

        private static void AddEntitySprites(Vector2 pos, List<ISprite> sprites, IEnumerable<EntitySelectionHandler> entitySelections) {
            bool isFirst = true;
            Vector2 prevPos = default;
            Vector2 delta = default;

            foreach (var selection in entitySelections) {
                if (isFirst) {
                    prevPos = selection.Entity.Pos;
                    delta = pos - prevPos;
                    isFirst = false;
                }

                var e = selection.Entity;

                e.Pos += delta;
                if (e.Nodes is { } nodes)
                    foreach (var item in nodes) {
                        item.Pos += delta;
                    }

                // todo: hacky!!!
                e.Selected = true;
                sprites.AddRange(e.GetSpritesWithNodes());
                e.Selected = false;
            }
        }

        public IHistoryAction Place(EditorState editorState, ISelectionHandler handler, Room room) {
            return handler.PlaceClone(room);
        }

        private sealed class MergedSelectionHandler : ISelectionHandler {
            public List<Selection> Selections;

            public MergedSelectionHandler(List<Selection> selections) {
                Selections = selections;
            }

            public object Parent => this;

            public IEditorLayer Layer => EditorLayers.All;

            public Rectangle Rect => RectangleExt.Merge(Selections.Select(s => s.Handler.Rect));

            public void ClearCollideCache() {
                foreach (var s in Selections) {
                    s.Handler.ClearCollideCache();
                }
            }

            public IHistoryAction DeleteSelf() {
                return Selections.Select(s => s.Handler.DeleteSelf()).MergeActions();
            }

            public bool IsWithinRectangle(Rectangle roomPos) {
                return Selections.Any(s => s.Handler.IsWithinRectangle(roomPos));
            }

            public IHistoryAction MoveBy(Vector2 offset) {
                return Selections.Select(s => s.Handler.MoveBy(offset)).MergeActions();
            }

            public void OnRightClicked(EditorState editorState, IEnumerable<Selection> selections) {

            }

            public BinaryPacker.Element? PackParent() {
                throw new NotImplementedException();
            }

            public IHistoryAction PlaceClone(Room room) {
                return Selections.Select(s => s.Handler.PlaceClone(room)).MergeActions();
                //.Select(s => s.Handler is Tilegrid.RectSelectionHandler tile ? tile.PlaceCloneAt(room, pos) : s.Handler.PlaceClone(room)).MergeActions();
            }

            public void RenderSelection(Color c) {
                foreach (var s in Selections) {
                    s.Render(c);
                }
            }
            
            public void RenderSelectionHollow(Color c) {
                foreach (var s in Selections) {
                    s.RenderHollow(c);
                }
            }

            public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos = null) {
                return null;
            }

            public IHistoryAction? TryResize(Point delta) {
                // dont allow resizing prefabs
                //var act = Selections.Select(s => s.Handler.TryResize(delta)).MergeActions();

                //if (act.Any())
                //    return act;

                return null;
            }

            public bool ResizableX => false;

            public bool ResizableY => false;
        }
    }

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
    public IComponentRegistry? Registry { get; set; }
}
