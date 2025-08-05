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

        //

        public override void Setup()
        {
            //
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

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

                    ImGui.Text("more stuffs here");
                }
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }
    }
}
