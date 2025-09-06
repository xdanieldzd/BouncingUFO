using BouncingUFO.Editors;
using Foster.Framework;
using ImGuiNET;

namespace BouncingUFO.Game.States
{
    public class Editor(Manager manager, params object[] args) : GameStateBase(manager, args)
    {
        public override Color ClearColor => Color.Black;
        public override float FadeDuration => 0f;

        private readonly TilesetEditor tilesetEditor = new(manager);
        private readonly MapEditor mapEditor = new(manager);
        private readonly SpriteEditor spriteEditor = new(manager);
        private readonly JsonEditor jsonEditor = new(manager);

        private IEditor[]? editors;

        private float deltaTime = 0f;

        public void RenderImGui()
        {
            if (editors == null)
            {
                editors = [tilesetEditor, mapEditor, spriteEditor, jsonEditor];
                foreach (var editor in editors)
                {
                    editor.CurrentFilePath = @"D:\Programming\UFO\";
                    editor.Setup();
                }
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
                    editor.Run(deltaTime);
                }
                ImGui.Separator();
                if (ImGui.Button("Exit", new(150f, 0f)))
                    LeaveState();
            }
            ImGui.End();
        }

        public override void OnEnterState() { }

        public override void OnFadeIn() { }

        public override void OnFadeInComplete() { }

        public override void OnUpdate() => deltaTime = manager.Time.Delta;

        public override void OnRender() { }

        public override void OnBeginFadeOut() { }

        public override void OnFadeOut() { }

        public override void OnLeaveState()
        {
            manager.GameStates.Pop();
            if (manager.GameStates.Count == 0) manager.GameStates.Push(new TitleScreen(manager));
        }
    }
}
