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

        private IEditor[]? editors;

        public override void UpdateApp() { }

        public override void UpdateUI()
        {
            if (editors == null)
            {
                editors = [tilesetEditor, mapEditor];
                foreach (var editor in editors) editor.Setup();
            }


            mapEditor.IsOpen = true;



            if (ImGui.Begin("Level Editor", ImGuiWindowFlags.AlwaysAutoResize))
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

            manager.Batcher.Text(manager.Assets.Font, "This is a test! This is the 'Editor' GameState!", Vector2.Zero, Color.White);
            manager.Batcher.Text(manager.Assets.Font, $"Current FPS:{manager.FrameCounter.CurrentFps:00.00}, average FPS:{manager.FrameCounter.AverageFps:00.00}", new(0f, 20f), Color.White);
        }
    }
}
