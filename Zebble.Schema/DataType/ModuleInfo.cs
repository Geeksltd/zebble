namespace Zebble.Schema
{
    using Olive;

    public class ModuleInfo
    {
        public ModuleInfo(string name) => Name = name.TrimStart("Modules.");

        public string Name { get; }

        public string CompleteName => "Modules." + Name;
    }
}