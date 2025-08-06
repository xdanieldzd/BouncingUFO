using Foster.Framework;
using GameTest1.Game.Sprites;
using GameTest1.Utilities;
using ImGuiNET;
using System.Text.Json;

namespace GameTest1.Editors
{
    public class SpriteEditor(Manager manager) : EditorBase(manager), IEditor
    {
        public override string Name => "Sprite Editor";

        const float zoom = 5f;

        private Sprite? sprite;

        private string currentSpritePath = string.Empty;
        private bool isFrameEditorOpen = false, isFrameEditorFocused = false;
        private int selectedFrame = -1;
        //

        public override void Setup()
        {
            // TEST TEST TEST
            isOpen = true;
            isFrameEditorOpen = true;
            currentSpritePath = @"D:\Programming\UFO\Sprites\PlayerTest.json";
            sprite = JsonSerializer.Deserialize<Sprite>(File.ReadAllText(currentSpritePath), Assets.SerializerOptions);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) || isFrameEditorFocused;

                ImGui.BeginGroup();
                if (ImGui.Button("New Sprite"))
                {
                    if (sprite != null) ImGui.OpenPopup("New");
                    else sprite = new();
                }
                ImGui.SameLine();

                var center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f));
                if (ImGui.BeginPopupModal("New", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("A sprite is currently open; overwrite?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes")) { sprite = new(); currentSpritePath = string.Empty; ImGui.CloseCurrentPopup(); }
                    ImGui.SameLine();
                    ImGui.SetItemDefaultFocus();
                    if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                }

                if (ImGui.Button("Load Sprite"))
                {
                    manager.FileSystem.OpenFileDialog(new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                        {
                            currentSpritePath = s[0];
                            sprite = JsonSerializer.Deserialize<Sprite>(File.ReadAllText(currentSpritePath), Assets.SerializerOptions);
                        }
                    }), [new("JSON files (*.json)", "json")], currentSpritePath);
                }
                ImGui.SameLine();

                if (sprite == null) ImGui.BeginDisabled();
                if (ImGui.Button("Save Sprite") && sprite != null)
                {
                    manager.FileSystem.SaveFileDialog(new FileSystem.DialogCallbackSingleFile((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success)
                            File.WriteAllText(s, JsonSerializer.Serialize(sprite, Assets.SerializerOptions));
                    }), [new("JSON files (*.json)", "json")], currentSpritePath);
                }
                if (sprite == null) ImGui.EndDisabled();
                ImGui.SameLine();

                if (sprite == null) ImGui.BeginDisabled();
                if (ImGui.Button("Export as Asset") && sprite != null)
                {
                    var spritesheetFullPath = new string(sprite.SpritesheetFile);
                    var jsonFilename = Path.ChangeExtension(Path.GetFileName(sprite.SpritesheetFile), "json");
                    var relativeSpritesheetFilePath = Path.Join(Assets.AssetsFolderName, Assets.SpriteFolderName, Path.GetFileName(sprite.SpritesheetFile));
                    manager.FileSystem.OpenFolderDialog(new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                        {
                            //
                        }
                    }), null);
                }
                if (sprite == null) ImGui.EndDisabled();
                ImGui.EndGroup();

                if (sprite != null)
                {
                    if (sprite.SpritesheetTexture == null)
                        sprite.LoadTexture(manager.GraphicsDevice);

                    ImGui.Separator();

                    ImGui.BeginGroup();
                    ImGui.Text($"Current sprite: {(string.IsNullOrWhiteSpace(currentSpritePath) ? "unsaved" : currentSpritePath)}");
                    ImGui.EndGroup();

                    ImGui.BeginGroup();
                    ImGui.InputText("Name", ref sprite.Name, 128);
                    ImGuiUtilities.InputFileBrowser("Spritesheet File", ref sprite.SpritesheetFile, manager.FileSystem, [new("Image files (*.png;*.bmp;*.jpg)", "png;bmp;jpg")], new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success)
                            sprite.SpritesheetFile = s.Length > 0 && s[0] != null ? s[0] : string.Empty;
                    }));
                    ImGui.SliderFloat2("Origin", ref sprite.Origin, 0f, 1f);
                    ImGui.EndGroup();

                    //

                    if (ImGui.Button("Frame Editor"))
                        isFrameEditorOpen = true;

                    RunFrameEditor();
                }
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }

        private void RunFrameEditor()
        {
            if (!isFrameEditorOpen || sprite == null) return;

            ImGui.ShowDemoWindow();

            if (ImGui.Begin("Frame Editor", ref isFrameEditorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFrameEditorFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                if (ImGui.BeginTable("framestable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendY, new(0f, 100f)))
                {
                    ImGui.TableSetupColumn("Frame #");
                    ImGui.TableSetupColumn("Source X");
                    ImGui.TableSetupColumn("Source Y");
                    ImGui.TableSetupColumn("Source Width");
                    ImGui.TableSetupColumn("Source Height");
                    ImGui.TableSetupColumn("Duration");

                    ImGui.TableHeadersRow();

                    if (sprite.Frames.Count != 0)
                    {
                        unsafe
                        {
                            var maxX = (float)(sprite.SpritesheetTexture?.Width ?? 0f);
                            var maxY = (float)(sprite.SpritesheetTexture?.Height ?? 0f);

                            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                            clipper.Begin(sprite.Frames.Count);
                            while (clipper.Step())
                            {
                                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                                {
                                    var frame = sprite.Frames[i];

                                    ImGui.TableNextRow();

                                    ImGui.PushID(i);

                                    ImGui.TableSetColumnIndex(0);
                                    ImGui.BeginDisabled();
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    var dummy = $"{i}";
                                    ImGui.InputText($"##frame-id-{i}", ref dummy, 32, ImGuiInputTextFlags.ReadOnly);
                                    ImGui.EndDisabled();

                                    ImGui.TableSetColumnIndex(1);
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.DragFloat($"##frame-x-{i}", ref frame.Rectangle.X, 0.25f, 0f, maxX)) frame.Rectangle.X = Math.Clamp(frame.Rectangle.X, 0f, maxX);

                                    ImGui.TableSetColumnIndex(2);
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.DragFloat($"##frame-y-{i}", ref frame.Rectangle.Y, 0.25f, 0f, maxY)) frame.Rectangle.Y = Math.Clamp(frame.Rectangle.Y, 0f, maxY);

                                    ImGui.TableSetColumnIndex(3);
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.DragFloat($"##frame-width-{i}", ref frame.Rectangle.Width, 0.25f, 0f, maxX)) frame.Rectangle.Width = Math.Clamp(frame.Rectangle.Width, 0f, maxX);

                                    ImGui.TableSetColumnIndex(4);
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.DragFloat($"##frame-height-{i}", ref frame.Rectangle.Height, 0.25f, 0f, maxY)) frame.Rectangle.Height = Math.Clamp(frame.Rectangle.Height, 0f, maxY);

                                    ImGui.TableSetColumnIndex(5);
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.DragFloat($"##frame-duration-{i}", ref frame.Duration, 0.25f, 0f, 100f)) frame.Duration = Math.Clamp(frame.Duration, 0f, 100f);
                                    /*
                                    if (ImGui.IsAnyItemHovered())
                                    {
                                        if (ImGui.BeginPopupContextItem())
                                        {
                                            selectedFrame = i;
                                            ImGui.Text($"Popup for frame {selectedFrame}!");
                                            if (ImGui.Button("Close"))
                                                ImGui.CloseCurrentPopup();
                                            ImGui.EndPopup();
                                        }
                                    }*/

                                    ImGui.PopID();
                                }
                            }
                            clipper.Destroy();
                        }
                    }
                }
                ImGui.EndTable();
            }
            ImGui.End();
        }
    }
}
