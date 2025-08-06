using Foster.Framework;
using GameTest1.Game.Sprites;
using GameTest1.Utilities;
using ImGuiNET;
using System.Numerics;
using System.Text.Json;

namespace GameTest1.Editors
{
    public class SpriteEditor(Manager manager) : EditorBase(manager), IEditor
    {
        public override string Name => "Sprite Editor";

        const float zoom = 4f;

        private Sprite? sprite;

        private string currentSpritePath = string.Empty;

        private string frameEditorName = "Frame Editor";
        private bool isFrameEditorOpen = false, isFrameEditorFocused = false;
        private int selectedFrame = 0;

        private string animEditorName = "Animation Editor";
        private bool isAnimEditorOpen = false, isAnimEditorFocused = false;
        private int selectedAnim = 0;
        //

        public override void Setup()
        {
            // TEST TEST TEST
            isOpen = true;
            isFrameEditorOpen = true;
            isAnimEditorOpen = true;
            currentSpritePath = @"D:\Programming\UFO\Sprites\PlayerTest.json";
            sprite = JsonSerializer.Deserialize<Sprite>(File.ReadAllText(currentSpritePath), Assets.SerializerOptions);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) || isFrameEditorFocused || isAnimEditorFocused;

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

                    ImGui.Separator();

                    if (ImGui.Button(frameEditorName)) { isFrameEditorOpen = true; ImGui.SetWindowFocus(frameEditorName); }
                    ImGui.SameLine();
                    if (ImGui.Button(animEditorName)) { isAnimEditorOpen = true; ImGui.SetWindowFocus(animEditorName); }

                    RunFrameEditor();
                    RunAnimEditor();
                }
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }

        private void RunFrameEditor()
        {
            if (!isFrameEditorOpen || sprite == null) return;

            if (ImGui.Begin(frameEditorName, ref isFrameEditorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFrameEditorFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                ImGui.BeginGroup();
                var currentFrameCount = sprite.Frames.Count;
                if (currentFrameCount == 0) ImGui.BeginDisabled();
                ImGui.SliderInt("Selected frame", ref selectedFrame, 0, sprite.Frames.Count == 0 ? 0 : sprite.Frames.Count - 1);
                if (currentFrameCount == 0) ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.Dummy(new(10f, 0f));
                ImGui.SameLine();
                if (ImGui.Button("Add new frame")) sprite.Frames.Add(new());
                if (currentFrameCount <= 0) ImGui.BeginDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Remove selected frame"))
                {
                    sprite.Frames.RemoveAt(selectedFrame);
                    selectedFrame = Math.Clamp(selectedFrame - 1, 0, sprite.Frames.Count);
                }
                if (currentFrameCount <= 0) ImGui.EndDisabled();
                ImGui.EndGroup();

                if (sprite.Frames.Count != 0)
                {
                    var maxX = sprite.SpritesheetTexture?.Width ?? 0;
                    var maxY = sprite.SpritesheetTexture?.Height ?? 0;

                    var frame = sprite.Frames[selectedFrame];

                    ImGui.Separator();

                    ImGui.BeginGroup();
                    ImGui.SliderInt("Source X", ref frame.SourceCoord.X, 0, maxX, null, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.SliderInt("Source Y", ref frame.SourceCoord.Y, 0, maxY, null, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.SliderInt("Source Width", ref frame.SourceSize.X, 0, maxX, null, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.SliderInt("Source Height", ref frame.SourceSize.Y, 0, maxY, null, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.SliderFloat("Duration", ref frame.Duration, 0.1f, 15f, null, ImGuiSliderFlags.AlwaysClamp);
                    ImGui.EndGroup();

                    ImGui.SameLine();

                    if (sprite.SpritesheetTexture != null)
                    {
                        ImGui.BeginGroup();
                        if (ImGui.BeginChild("spritesheet", Vector2.Zero, ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                        {
                            if (manager.ImGuiRenderer.BeginBatch(new(sprite.SpritesheetTexture.Width * zoom, sprite.SpritesheetTexture.Height * zoom), out var batcher, out var bounds))
                            {
                                batcher.CheckeredPattern(bounds, 8f, 8f, Color.Gray, Color.LightGray);
                                batcher.Image(sprite.SpritesheetTexture, Vector2.Zero, Vector2.Zero, Vector2.One * zoom, 0f, Color.White);

                                batcher.RectLine(frame.SourceRectangle * zoom, 2f, Color.Red);
                            }
                            manager.ImGuiRenderer.EndBatch();
                        }
                        ImGui.EndChild();
                        ImGui.EndGroup();
                    }
                }
            }
            ImGui.End();
        }

        private void RunAnimEditor()
        {
            if (!isAnimEditorOpen || sprite == null) return;

            if (ImGui.Begin(animEditorName, ref isAnimEditorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isAnimEditorFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                ImGui.BeginGroup();
                var currentAnimCount = sprite.Animations.Count;
                if (currentAnimCount == 0) ImGui.BeginDisabled();
                ImGui.SliderInt("Selected animation", ref selectedAnim, 0, sprite.Animations.Count == 0 ? 0 : sprite.Animations.Count - 1);
                if (currentAnimCount == 0) ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.Dummy(new(10f, 0f));
                ImGui.SameLine();
                if (ImGui.Button("Add new anim")) sprite.Animations.Add(new());
                if (currentAnimCount <= 0) ImGui.BeginDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Remove selected anim"))
                {
                    sprite.Animations.RemoveAt(selectedAnim);
                    selectedAnim = Math.Clamp(selectedAnim - 1, 0, sprite.Animations.Count);
                }
                if (currentAnimCount <= 0) ImGui.EndDisabled();
                ImGui.EndGroup();

                //
            }
            ImGui.End();
        }
    }
}
