namespace Zebble.Build
{
    using System;
    using System.Linq;

    class CssFileParser : LinearParser
    {
        public CssFileParser(string filePath) : base(filePath) { }

        public bool HasMatchingSelector(string selector)
        {
            var searchItems = new[] { selector, $".{selector}", $"#{selector}" };

            return ContentLines.Any(x => searchItems.Any(p => x.Contains(p, StringComparison.OrdinalIgnoreCase)));
        }

        protected override void Parse()
        {
        }
    }
}
