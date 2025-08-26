using Foster.Framework;
using GameTest1.Editors;
using ImGuiNET;

namespace GameTest1.Game.States
{
    public class Editor(Manager manager, params object[] args) : GameStateBase(manager, args), IGameState
    {
        public override Color ClearColor => Color.Black;
        public override float ScreenFadeDuration => 0f;

        private readonly TilesetEditor tilesetEditor = new(manager);
        private readonly MapEditor mapEditor = new(manager);
        private readonly SpriteEditor spriteEditor = new(manager);
        private readonly JsonEditor jsonEditor = new(manager);

        private IEditor[]? editors;

        public void RenderImGui()
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
                    LeaveState();
            }
            ImGui.End();
        }

        public override void OnEnter() { }

        public override void OnUpdateMain() { }

        public override void OnRenderMain() { }

        public override void OnExit()
        {
            manager.GameStates.Pop();
            if (manager.GameStates.Count == 0) manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
