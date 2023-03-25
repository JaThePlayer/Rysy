using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;
using System.Text.Json.Serialization;

namespace Rysy.Helpers;

public static class PrefabHelper {
    private static ListenableDictionary<string, Prefab> _CurrentPrefabs;
    public static ListenableDictionary<string, Prefab> CurrentPrefabs => _CurrentPrefabs ??= Load();

    private static ListenableDictionary<string, Prefab> Load() {
        _CurrentPrefabs = new(StringComparer.Ordinal);

        var prefabPath = GetPrefabDir();

        if (Directory.Exists(prefabPath)) {
            var actions = Directory.EnumerateFiles(prefabPath, "*.json", SearchOption.AllDirectories).Select(LoadFromFileAsync);
            Task.WhenAll(actions).Wait();
        }

        return _CurrentPrefabs;
    }

    public static async Task LoadFromFileAsync(string path) {
        if (!File.Exists(path))
            return;

        if (await JsonHelper.TryDeserializeAsync<Prefab>(File.Open(path, FileMode.Open)) is not { } prefab)
            return;

        lock (_CurrentPrefabs) {
            _CurrentPrefabs[prefab.Name] = prefab;
        }

        prefab.Filename = path;
    }

    public static bool SelectionsLegal(List<CopypasteHelper.CopiedSelection> selections) => !selections.Any(s => s.Layer == SelectionLayer.Rooms);

    public static void RegisterPrefab(string name, List<CopypasteHelper.CopiedSelection> selections) {
        if (!SelectionsLegal(selections))
            return;

        var prefab = new Prefab {
            Name = name,
            Objects = selections,
        };

        lock (_CurrentPrefabs) {
            _CurrentPrefabs[prefab.Name] = prefab;
        }

        var path = GetPrefabPath(prefab);
        prefab.Filename = path;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, prefab.ToJsonUTF8());
    }

    public static void Remove(string name) {
        if (!CurrentPrefabs.TryGetValue(name, out var prefab))
            return;

        CurrentPrefabs.Remove(name);

        var path = prefab.Filename;
        if (File.Exists(path))
            File.Delete(path);
    }

    public static Placement? PlacementFromName(string name) {
        if (!CurrentPrefabs.TryGetValue(name, out var prefab)) {
            return null;
        }

        var placement = new Placement(name);
        placement.PlacementHandler = new PrefabPlacementHandler(prefab);

        return placement;
    }

    private static string GetPrefabDir(bool perProfile = true) => SettingsHelper.GetFullPath("prefabs", perProfile: perProfile);
    private static string GetPrefabPath(Prefab prefab) {
        var prefabDir = GetPrefabDir();
        var filename = prefab.Name.ToValidFilename();

        if (Directory.Exists(prefabDir)) {
            filename = filename.GetDeduplicatedIn(Directory.GetFiles(prefabDir).Select(f => Path.GetFileNameWithoutExtension(f)));
        }

        return $"{prefabDir}/{filename}.json";
    }

    public class Prefab {
        public Prefab() { }

        public string Name { get; set; }

        public List<CopypasteHelper.CopiedSelection> Objects { get; set; }

        [JsonIgnore]
        public string Filename { get; internal set; }
    }

    private record class PrefabPlacementHandler(Prefab prefab) : IPlacementHandler {
        public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room room) {
            var selections = CopypasteHelper.PasteSelections(prefab.Objects, history: null, map: null, room, pos, out var pastedRooms);
            if (pastedRooms) {
                throw new NotImplementedException("Pasting rooms in prefabs is not supported yet!");
            }

            return new MergedSelectionHandler(selections ?? new());
        }

        public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room room) {
            List<ISprite> sprites = new();

            if (handler is MergedSelectionHandler h) {
                var entitySelections = h.Selections.Select(s => s.Handler).OfType<EntitySelectionHandler>();
                var prevPos = entitySelections.First().Entity.Pos;
                var delta = pos - prevPos;

                foreach (var selection in entitySelections) {
                    var e = selection.Entity;

                    e.Pos += delta;
                    if (e.Nodes is { } nodes)
                        foreach (var item in nodes) {
                            item.Pos += delta;
                        }
                    sprites.AddRange(e.GetSpritesWithNodes());
                }
            }

            return sprites;
        }

        public IHistoryAction Place(ISelectionHandler handler, Room room) {
            return handler.PlaceClone(room);
        }

        private class MergedSelectionHandler : ISelectionHandler {
            public List<Selection> Selections;

            public MergedSelectionHandler(List<Selection> selections) {
                Selections = selections;
            }

            public object Parent => throw new NotImplementedException();

            public SelectionLayer Layer => throw new NotImplementedException();

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

            public void OnRightClicked(IEnumerable<Selection> selections) {

            }

            public BinaryPacker.Element? PackParent() {
                throw new NotImplementedException();
            }

            public IHistoryAction PlaceClone(Room room) {
                return Selections.Select(s => s.Handler.PlaceClone(room)).MergeActions();
            }

            public void RenderSelection(Color c) {
                foreach (var s in Selections) {
                    s.Render(c);
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
        }
    }
}
