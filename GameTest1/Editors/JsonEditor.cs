using Foster.Framework;
using ImGuiNET;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GameTest1.Editors
{
    public class JsonEditor(Manager manager) : EditorBase(manager), IEditor
    {
        public override string Name => "JSON Editor";

        private string currentJsonPath = string.Empty;
        private JsonObject? jsonObject = null;

        public override void Setup()
        {
            isOpen = true;
            currentJsonPath = @"D:\Programming\UFO\\UI\DialogBox.json";
            jsonObject = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(currentJsonPath), Assets.SerializerOptions);
        }

        public override void Run()
        {
            if (!isOpen) return;

            ImGui.SetNextWindowSize(new(350f, 0f), ImGuiCond.Appearing);
            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                ImGui.BeginGroup();
                {
                    if (ImGui.Button("Load JSON"))
                    {
                        manager.FileSystem.OpenFileDialog(new FileSystem.DialogCallback((s, r) =>
                        {
                            if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                            {
                                currentJsonPath = s[0];
                                jsonObject = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(currentJsonPath), Assets.SerializerOptions);
                            }
                        }), [new("JSON files (*.json)", "json")], currentJsonPath);
                    }

                    ImGui.SameLine();
                    if (jsonObject == null) ImGui.BeginDisabled();
                    if (ImGui.Button("Save JSON") && jsonObject != null)
                    {
                        manager.FileSystem.SaveFileDialog(new FileSystem.DialogCallbackSingleFile((s, r) =>
                        {
                            if (r == FileSystem.DialogResult.Success)
                                File.WriteAllText(s, JsonSerializer.Serialize(jsonObject, Assets.SerializerOptions));
                        }), [new("JSON files (*.json)", "json")], currentJsonPath);
                    }
                    if (jsonObject == null) ImGui.EndDisabled();

                    var currentFileLabel = $"Current file: {(string.IsNullOrWhiteSpace(currentJsonPath) ? "none" : currentJsonPath)}";
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(currentFileLabel).X);
                    ImGui.Text(currentFileLabel);
                }
                ImGui.EndGroup();

                if (jsonObject != null)
                {
                    ImGui.BeginGroup();
                    {
                        if (ImGui.BeginChild("jsontree", new(600f, 450f), ImGuiChildFlags.Borders))
                        {
                            if (ImGui.BeginTable("##jsontable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX))
                            {
                                ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthFixed);
                                ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableHeadersRow();

                                processObject(jsonObject);

                                static void processObject(JsonObject? json)
                                {
                                    if (json == null) return;

                                    foreach (var (key, node) in json)
                                    {
                                        if (node == null) continue;

                                        static void generateTreeNode(string propName, JsonNode? node)
                                        {
                                            if (node == null) return;

                                            ImGui.TableNextColumn();
                                            ImGui.AlignTextToFramePadding();
                                            if (ImGui.TreeNodeEx($"{propName}##{node.GetPath()}", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAllColumns))
                                            {
                                                ImGui.TableNextRow();

                                                var valueKind = node.GetValueKind();
                                                if (valueKind == JsonValueKind.Array)
                                                {
                                                    var array = node.AsArray();
                                                    for (var i = 0; i < array.Count; i++)
                                                        generateTreeNode(i.ToString(), array[i]);
                                                }
                                                else if (valueKind == JsonValueKind.Object)
                                                    processObject(node.AsObject());
                                                else
                                                    generateEditor(propName, node);

                                                ImGui.TreePop();
                                            }
                                            ImGui.TableSetColumnIndex(0);
                                            ImGui.TableNextRow();
                                        }

                                        static void generateEditor(string propName, JsonNode? node)
                                        {
                                            if (node == null) return;

                                            var valueKind = node.GetValueKind();

                                            ImGui.TableNextColumn();
                                            ImGui.AlignTextToFramePadding();
                                            ImGui.Text(propName);

                                            ImGui.TableNextColumn();
                                            ImGui.Text(valueKind.ToString());

                                            ImGui.TableNextColumn();
                                            ImGui.PushID(node.GetPath());
                                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                            switch (valueKind)
                                            {
                                                case JsonValueKind.String:
                                                    {
                                                        var valueString = node.GetValue<string>();
                                                        if (ImGui.InputText(string.Empty, ref valueString, 1024))
                                                            node.ReplaceWith(valueString);
                                                    }
                                                    break;

                                                case JsonValueKind.Number:
                                                    {
                                                        var valueNumber = node.GetValue<float>();
                                                        if (ImGui.InputFloat(string.Empty, ref valueNumber))
                                                            node.ReplaceWith(valueNumber);
                                                    }
                                                    break;
                                            }
                                            ImGui.PopID();
                                        }

                                        var valueKind = node.GetValueKind();
                                        if (valueKind == JsonValueKind.Object || valueKind == JsonValueKind.Array)
                                            generateTreeNode(node.GetPropertyName(), node);
                                        else
                                            generateEditor(node.GetPropertyName(), node);
                                    }
                                }
                            }
                            ImGui.EndTable();
                        }
                        ImGui.EndChild();
                    }
                    ImGui.EndGroup();
                }
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }
    }
}
