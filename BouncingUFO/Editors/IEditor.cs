namespace BouncingUFO.Editors
{
    internal interface IEditor
    {
        bool IsOpen { get; set; }
        bool IsCollapsed { get; }
        bool IsFocused { get; }

        string CurrentFilePath { get; set; }

        abstract string Name { get; }

        virtual void Setup() { }
        abstract void Run(float delta);
    }
}
