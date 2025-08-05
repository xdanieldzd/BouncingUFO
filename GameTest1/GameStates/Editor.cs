using Foster.Framework;
using GameTest1.Editors;
using ImGuiNET;

namespace GameTest1.GameStates
{
    public class Editor(Manager manager) : GameStateBase(manager), IGameState
    {
        private readonly TilesetEditor tilesetEditor = new(manager);
        private readonly MapEditor mapEditor = new(manager);

        private IEditor[]? editors;

        public override void UpdateApp() { }

        public override void UpdateUI()
        {
            if (editors == null)
            {
                editors = [tilesetEditor, mapEditor];
                foreach (var editor in editors) editor.Setup();
            }

            if (ImGui.Begin("Editors", ImGuiWindowFlags.AlwaysAutoResize))
            {
                foreach (var editor in editors)
                {
                    if (ImGui.Button(editor.Name))
                    {
                        editor.IsOpen = true;
                        ImGui.SetWindowFocus(editor.Name);
                        ImGui.SetWindowCollapsed(editor.Name, false);
                    }
                    editor.Run();
                }
            }
            ImGui.End();
        }

        public override void Render()
        {
            manager.Screen.Clear(Color.DarkGray);

            manager.MapRenderer.Render(mapEditor.CurrentMapAndTileset.Map, mapEditor.CurrentMapAndTileset.Tileset);
        }
    }
}
