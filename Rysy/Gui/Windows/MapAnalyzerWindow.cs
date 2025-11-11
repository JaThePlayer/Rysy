using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.MapAnalyzers;

namespace Rysy.Gui.Windows;

internal sealed class MapAnalyzerWindow : Window {
    private AnalyzerCtx? _ctx;
    private List<IAnalyzerResult>? _results;

    public Action? SaveAnyway;

    public new static string Name => "rysy.analyzers.window.name".Translate();

    public MapAnalyzerWindow() : base(Name, new(600, 500)) {
        NoSaveData = false;

        EditorState.History!.OnApply += Update;
        EditorState.History!.OnUndo += Update;
        EditorState.OnMapChanged += Update;

        Update();
    }

    public override void RemoveSelf() {
        base.RemoveSelf();

        EditorState.History!.OnApply -= Update;
        EditorState.History!.OnUndo -= Update;
        EditorState.OnMapChanged -= Update;
    }


    private void Update() {
        _ctx = null;
        _results = null;

        if (EditorState.Map is not { } map) {
            _ctx = null;
            _results = null;

            return;
        }
    }

    private void SetCtx() {
        if (EditorState.Map is not { } map) {
            return;
        }

        var ctx = MapAnalyzerRegistry.Global.Analyze(map);
        _ctx = ctx;

        _results = ctx.Results.OrderByDescending(r => r.Level).ToList();

        ForceSetSize(new(Size!.Value.X, _results!.Count.AtLeast(1) * ImGui.GetTextLineHeightWithSpacing() + ImGui.GetFrameHeightWithSpacing() * 3
            + ImGui.GetTextLineHeight() * 2));
    }

    protected override void Render() {
        base.Render();

        if (_ctx is null) {
            SetCtx();
            if (_ctx is null)
                return;
        }

        ImGui.BeginChild("List");

        if (!ImGui.BeginTable("Results", 3, ImGuiManager.TableFlags)) {
            ImGui.EndChild();
            return;
        }

        var textBaseWidth = ImGui.CalcTextSize("A").X;

        ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 6f);
        ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Fix", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 3f);
        ImGui.TableHeadersRow();

        var i = 0;
        if (_results is { }) {
            foreach (var res in _results) {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.TextColored(res.Level.ToColorNumVec(), res.Level.FastToString());

                ImGui.TableNextColumn();

                var open = ImGui.TreeNodeEx(res.Message, ImGuiTreeNodeFlags.SpanFullWidth);

                if (open) {
                    res.RenderDetailImgui();

                    ImGui.TreePop();
                }

                if (res.AutoFixable) {
                    ImGuiManager.PushEditedStyle();
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable($"Fix##{i++}")) {
                        RysyState.OnEndOfThisFrame += () => {
                            res.Fix();
                            Update();
                        };
                    }
                    ImGuiManager.PopEditedStyle();
                }
            }
        }


        ImGui.EndTable();
        ImGui.EndChild();
    }
}
