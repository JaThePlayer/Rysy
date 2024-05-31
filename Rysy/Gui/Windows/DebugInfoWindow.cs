using ImGuiNET;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Rysy.Extensions;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Scenes;
using Rysy.Stylegrounds;
using System.Diagnostics;
using System.Linq;

namespace Rysy.Gui.Windows;

public class DebugInfoWindow : Window {
    public static DebugInfoWindow Instance { get; } = new();

    public static bool Enabled { get; set; } = false;

    public DebugInfoWindow() : base("Debug", new(480, 480)) {
        SetRemoveAction((w) => Enabled = false);
    }

    private static string HistoryFromText = "";

    protected override void Render() {
        ImGui.Text($"FPS: {RysyState.CurrentFPS}");

        if (ImGui.CollapsingHeader("Memory")) {
            ImGui.Text($"RAM: {Process.GetCurrentProcess().WorkingSet64 / 1024m}KB");
        }

#if !FNA
        if (ImGui.CollapsingHeader("Metrics")) {
            var metrics = RysyEngine.GDM.GraphicsDevice.Metrics;
            ImGui.Text(metrics.ToJson());
        }
#endif

        HistoryTab();

        if (ImGui.CollapsingHeader("GC")) {
            ImGui.Text($"Pinned: {GC.GetGCMemoryInfo().PinnedObjectsCount}");
        }

        if (ImGui.CollapsingHeader("Markdown Test")) {
            var str = """
                # Header
                ***Hello***, this is **bold**!!
                And ~~strikethrough, too~~
                <"hello">
                **~~Bold and strikethrough~~, a bit more**

                ## Table
                | Tables | Exist  | Now       |
                |--------|--------|-----------|
                | Isn't  | that   | *cool*    |
                | ***Yea*** | **it** | is~~n't~~ |
                | ![Image Link](tilesets/subfolder/betterTemplate)  |[Github](https://github.com/JaThePlayer/Rysy)| https://github.com/JaThePlayer/Rysy |
                
                # Entity.sprite(room, entity)
                
                This function is used when you want your entity rendering to be more complicated than just a rectangle or single sprite.
                
                ## DrawableSprite
                
                To draw celeste sprites in the `sprite` function, you can use the `drawableSprite` struct.
                First, import it at the top of your plugin file:
                `local drawableSprite = require("structs.drawable_sprite")`
                
                Now, you can use it in the `sprite` function:
                ```
                function YOUR_ENTITY.sprite(room, entity)
                    -- creates a sprite instance. First arg is the texture path, second is a table containing the position, color, etc.
                    -- In most cases, you just want to pass the `entity` to make the sprite appear the entity's position.
                    local computerSprite = drawableSprite.fromTexture("objects/kevinspc/pc", entity)
                    -- you can also edit the sprite after creating it
                    -- equivalent to `Celeste.Sprite.JustifyOrigin(x, y)` in C#:
                    -- By default, justification is 0.5, 0.5 (centered)
                    computerSprite:setJustification(0.5, 1.0)
                
                    -- you can also create multiple sprites in the same function:
                    local spectogramSprite = drawableSprite.fromTexture("objects/kevinspc/spectogram", entity)
                    spectogramSprite:setJustification(0.0, 0.0)
                    -- This moves the sprite:
                    spectogramSprite:addPosition(-16, -39)
                    -- This is equivalent to MTexture.CreateSubtexture - allows you to render only parts of the sprite.
                    spectogramSprite:useRelativeQuad(0, 0, 32, 18)
                
                    -- You need to return all the sprites you created, or else they won't be rendered.
                    -- If you have more than 1 sprite, you need to create a table containing the sprites, like this:
                    return {
                        computerSprite,
                        spectogramSprite
                    }
                end
                ```
                """;
            if (Doc is null || DocStr != str) {
                var doc = Markdig.Markdown.Parse(str, ImGuiMarkdown.MarkdownPipeline);
                //foreach (var item in doc) {
                //    Print(item, "");
                //}
                Doc = doc;
                DocStr = str;
            }
            ImGuiMarkdown.RenderMarkdown(Doc);

            void Print(MarkdownObject item, string indent) {
                Console.WriteLine((indent, item.GetType(), item.ToString()));
                //if (item is Block or ParagraphBlock) {
                    foreach (var obj in item.Descendants()) {
                        Print(obj, indent + "  ");
                    }
                //}
            }
        }

        if (RysyEngine.Scene is EditorScene editor) {
            if (ImGui.CollapsingHeader("Lonn Entity stats:")) {
                var lonnEntities = editor.CurrentRoom?.Entities.OfType<LonnEntity>().ToList() ?? new();
                var cachedAmt = lonnEntities.Count(e => e.CachedSprites is { });
                var uncachedSids = lonnEntities.Where(e => e.CachedSprites is not { }).Select(e => e.Name).Distinct();

                ImGui.TextUnformatted($"Cached: {cachedAmt}/{lonnEntities.Count} [{cachedAmt / (float)lonnEntities.Count * 100}%]");
                
                if (ImGui.BeginListBox("Uncached SID's", new(Size!.Value.X, 300))) {
                    foreach (var sid in uncachedSids) {
                        ImGui.TextUnformatted(sid);
                    }

                    ImGui.EndListBox();
                }
                
            }

            if (ImGui.CollapsingHeader("Camera Info:")) {
                var cam = editor.Camera;

                ImGui.Text($"Pos: {cam.Pos}");
                ImGui.Text($"Scale: {cam.Scale}");
                ImGui.Text($"Room: {EditorState.CurrentRoom?.Pos ?? default}");
                ImGui.Text($"Viewport: {cam.Viewport.Bounds.Size()}");
                ImGui.Text($"{Parallax.CalcCamPos(cam)}");

                var s = cam.Scale;
                if (ImGui.InputFloat("Scale", ref s)) {
                    cam.Scale = s;
                }

                var p = cam.Pos.ToNumerics();
                if (ImGui.InputFloat2("Pos", ref p)) {
                    cam.Move(p.ToXna() - cam.Pos);
                }
            }

            const int times = 100;
            var room = EditorState.CurrentRoom;
            if (room is { } && ImGui.Button("Benchmark current room")) {
                Benchmark(room, false, times);
            }
            
            if (room is { } && ImGui.Button("Benchmark current room (Aggressively Clear Caches)")) {
                Benchmark(room, true, times);
            }
            
            if (room is { } && ImGui.Button("Benchmark current room's entities")) {
                BenchmarkEntities(room, false, times);
            }
            
            if (room is { } && ImGui.Button("Benchmark current room's entities (Aggressively Clear Caches)")) {
                BenchmarkEntities(room, true, times);
            }
            
            if (room is { } && ImGui.Button("Benchmark entire map (Aggressively Clear Caches)")) {
                var watch = Stopwatch.GetTimestamp();
                foreach (var innerRoom in room.Map.Rooms) {
                    Benchmark(innerRoom, true, 1);
                }
                var elapsed = Stopwatch.GetElapsedTime(watch);
                
                Logger.Write("Benchmark", LogLevel.Info, $"Benchmark: {room.Map.Package ?? room.Map.Filepath}: {elapsed}");
            }
        }

        ImGui.Checkbox("Imgui Demo", ref imguiDemo);
        if (imguiDemo)
            ImGui.ShowDemoWindow();
    }

    private void Benchmark(Room room, bool aggressive, int times) {
        var watch = Stopwatch.GetTimestamp();
        for (int i = 0; i < times; i++) {
            if (aggressive)
                room.ClearRenderCacheAggressively();
            else
                room.ClearRenderCache();
            
            room.CacheSpritesIfNeeded();
        }

        var elapsed = Stopwatch.GetElapsedTime(watch);
        Logger.Write("Benchmark", LogLevel.Info, $"Benchmark: {room.Name}: {(elapsed / times).TotalMilliseconds}ms");
    }

    private void BenchmarkEntities(Room room, bool aggressive, int howManyTimes) {
        var entities = room.Entities;
        var times = entities.Select(e => e.Name).SafeToDictionary(t => (t, TimeSpan.Zero));
        
        foreach (var e in entities) {
            var start = Stopwatch.GetTimestamp();
            
            if (aggressive) {
                for (int i = 0; i < howManyTimes; i++) {
                    e.GetSprites().Enumerate();
                    e.ClearInnerCaches();
                }
            } else {
                for (int i = 0; i < howManyTimes; i++) {
                    e.GetSprites().Enumerate();
                }
            }
            
            var elapsed = Stopwatch.GetElapsedTime(start);
            times[e.Name] += elapsed;
        }

        var summary = string.Join('\n', times
            .OrderByDescending(t => t.Value)
            .Select(t => $"{t.Key}: {(t.Value / howManyTimes).TotalMilliseconds}ms"));
        Logger.Write("Benchmark", LogLevel.Info, $"Benchmark: {room.Name}:\n{summary}");
    }

    private bool imguiDemo;

    string DocStr;
    Markdig.Syntax.MarkdownDocument Doc;

    private static void HistoryTab() {
        if (RysyEngine.Scene is EditorScene editor && ImGui.CollapsingHeader("History")) {
            ImGui.Text($"Count: {editor.HistoryHandler.Actions.Count}");
            if (ImGui.BeginListBox("")) {
                ImGui.TextWrapped(string.Join('\n', editor.HistoryHandler.Actions.Select(act => act.ToString())));
                //ImGui.TextWrapped(editor.HistoryHandler.Serialize());
                ImGui.EndListBox();
            }

            if (ImGui.InputText("From Text", ref HistoryFromText, 10_000, ImGuiInputTextFlags.EnterReturnsTrue)) {
                foreach (var item in HistoryHandler.Deserialize(HistoryFromText)) {
                    editor.HistoryHandler.ApplyNewAction(item);
                }
            }
        }
    }
}
