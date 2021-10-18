namespace Zebble.Build
{
    using System;
    using System.IO;

    abstract class LinearParser : FileParserBase
    {
        protected string[] ContentLines { get; }

        protected LinearParser(string filePath) : base(filePath)
        {
            ContentLines = File.ReadAllLines(filePath);
            Parse();
        }

        public override void Save() => throw new NotSupportedException();
    }
}
