using Foster.Framework;
using GameTest1.Editors;
using ImGuiNET;
using System.Numerics;

namespace GameTest1.GameStates
{
    public class Editor(Manager manager) : GameStateBase(manager), IGameState
    {
        private readonly TilesetEditor tilesetEditor = new(manager);
        private readonly MapEditor mapEditor = new(manager);
        private readonly SpriteEditor spriteEditor = new(manager);
        private readonly JsonEditor jsonEditor = new(manager);

        private IEditor[]? editors;

        public override void UpdateApp() { }

        public override void UpdateUI()
        {
            if (editors == null)
            {
                editors = [tilesetEditor, mapEditor, spriteEditor, jsonEditor];
                foreach (var editor in editors) editor.Setup();
            }

            if (ImGui.Begin("Editors", ImGuiWindowFlags.AlwaysAutoResize))
            {
                foreach (var editor in editors)
                {
                    if (ImGui.Button(editor.Name, new(150f, 0f)))
                    {
                        editor.IsOpen = true;
                        ImGui.SetWindowFocus(editor.Name);
                        ImGui.SetWindowCollapsed(editor.Name, false);
                    }
                    editor.Run();
                }
                ImGui.Separator();
                if (ImGui.Button("Exit", new(150f, 0f)))
                {
                    manager.GameStates.Pop();
                    if (manager.GameStates.Count == 0) manager.GameStates.Push(new Intro(manager));
                }
            }
            ImGui.End();
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            var focusedEditor = editors?.FirstOrDefault(x => x.IsFocused) ?? null;
            if (focusedEditor == tilesetEditor)
            {
                manager.Batcher.Text(manager.Assets.SmallFont, "Tileset editor preview here", Vector2.Zero, Color.White);
            }
            else if (focusedEditor == mapEditor)
            {
                manager.MapRenderer.Render(mapEditor.CurrentMapAndTileset.Map, mapEditor.CurrentMapAndTileset.Tileset);
            }
            else if (focusedEditor == spriteEditor)
            {
                manager.Batcher.Text(manager.Assets.SmallFont, "Sprite editor preview here", Vector2.Zero, Color.White);
            }
            else if (focusedEditor == jsonEditor)
            {
                manager.Batcher.Text(manager.Assets.SmallFont, "No preview for JSON editor.", Vector2.Zero, Color.White);
            }
            else
                manager.Batcher.Text(manager.Assets.SmallFont, "No editor selected.", 1024f, new(0f, manager.Screen.Height), new(0f, 1.5f), Color.White);
        }
    }
}
