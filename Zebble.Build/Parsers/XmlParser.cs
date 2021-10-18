namespace Zebble.Build
{
    using System.Xml.Linq;

    abstract class XmlParser : FileParserBase
    {
        protected XDocument Document { get; }

        protected XmlParser(string filePath) : base(filePath)
        {
            Document = XDocument.Load(filePath);
            Parse();
        }

        public override void Save() => Document.Save(FilePath);
    }
}
