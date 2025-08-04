namespace GameTest1.Editors
{
    internal interface IEditor
    {
        bool IsOpen { get; set; }

        abstract string Name { get; }

        virtual void Setup() { }
        abstract void Run();
    }
}
