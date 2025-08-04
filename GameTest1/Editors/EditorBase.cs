namespace GameTest1.Editors
{
    public abstract class EditorBase(Manager manager) : IEditor
    {
        protected readonly Manager manager = manager;

        protected bool isOpen;
        public bool IsOpen { get => isOpen; set => isOpen = value; }

        public abstract string Name { get; }

        public virtual void Setup() { }
        public abstract void Run();
    }
}
