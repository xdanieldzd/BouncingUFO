using System.Text.Json;

namespace BouncingUFO.Editors
{
    public abstract class EditorBase(Manager manager) : IEditor
    {
        protected readonly Manager manager = manager;

        protected static readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true, IncludeFields = true };

        protected bool isOpen;
        public bool IsOpen { get => isOpen; set => isOpen = value; }

        protected bool isCollapsed;
        public bool IsCollapsed => isCollapsed;

        protected bool isFocused;
        public bool IsFocused => isOpen && !isCollapsed && isFocused;

        public string CurrentFilePath { get; set; } = string.Empty;

        public abstract string Name { get; }

        public virtual void Setup() { }
        public abstract void Run(float delta);
    }
}
