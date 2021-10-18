namespace Zebble.Build
{
    using System.Collections.Generic;
    using System.Linq;

    class CsFileParser : LinearParser
    {
        public CsFileParser(string filePath) : base(filePath) { }

        public string Namespace { get; private set; }
        public string TypeName { get; private set; }
        public string FullName => $"{Namespace}.{TypeName}";
        public string InheritedTypeName { get; private set; }
        public string InheritedGenericName { get; private set; }
        public string[] Tokens { get; private set; }

        protected override void Parse()
        {
            Namespace = FindLineTokens("namespace")[1];

            var classParts = FindLineTokens("class");
            if (!(classParts?.Any() ?? false))
                return;

            TypeName = classParts.ElementAtOrDefault(classParts.IndexOf("class") + 1);
            var inheritedParts = classParts.ElementAtOrDefault(classParts.IndexOf(":") + 1)?.Split('<', '>');
            InheritedTypeName = inheritedParts?.ElementAtOrDefault(0);
            InheritedGenericName = inheritedParts?.ElementAtOrDefault(1)?.Split('.').LastOrDefault();
            Tokens = ContentLines.SelectMany(x => x.Split(' ', '.', '<', '>')).ToArray();
        }

        IList<string> FindLineTokens(string keyword) => ContentLines.FirstOrDefault(x => x.Contains(keyword))?.Split(' ');

        public override string ToString() => FullName;
    }
}
