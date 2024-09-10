namespace Zebble.Build
{
    using System;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Xml;
    using Zebble.Tooling;
    using Olive;

    class NavXmlGenerator : Builder
    {
        readonly FileInfo _navXmlFile;

        public NavXmlGenerator()
        {
            _navXmlFile = Path.Combine(DirectoryContext.WinUiObjFolder.FullName, "zebble-nav.xml").AsFile();
            Log($"Updated navigation info will be written into: {_navXmlFile.FullName}");
        }

        protected override void AddTasks()
        {
            Add(() => GenerateZebbleNavXml());
        }

        void GenerateZebbleNavXml()
        {
            using var output = new ZebbleStringWriter(Encoding.UTF8);
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    ",
                NewLineOnAttributes = false
            };
            using var writer = XmlWriter.Create(output, settings);

            writer.WriteStartElement("All");

            var zblItems = DirectoryContext.AppUIFolder.GetFiles("*.zbl", SearchOption.AllDirectories)
                .Select(x => new { x.FullName, Parser = FileParserProvider.CreateZebble(x.FullName) })
                .ToList();
            var scssItems = DirectoryContext.AppUIStylesFolder.GetFiles("*.scss", SearchOption.AllDirectories)
                .Select(x => new { x.FullName, Name = x.NameWithoutExtension().TrimStart("_") })
                .ToList();
            var cssItems = DirectoryContext.AppUIStylesFolder.GetFiles("*.css", SearchOption.AllDirectories)
                .Select(x => new { x.FullName, Name = x.NameWithoutExtension().TrimStart("_") })
                .ToList();
            var vmItems = DirectoryContext.ViewModelFolder.GetFiles("*.cs", SearchOption.AllDirectories)
                .Select(x => new { x.FullName, Parser = FileParserProvider.CreateCSharp(x.FullName) })
                .ToList();
            var domItems = DirectoryContext.AppDomainFolder.GetFiles("*.cs", SearchOption.AllDirectories)
                .Select(x => new { x.FullName, Parser = FileParserProvider.CreateCSharp(x.FullName) })
                .ToList();

            foreach (var zblItem in zblItems)
            {
                writer.WriteStartElement("Sisters");

                WriteButton(writer, "View", zblItem.FullName);

                var cssItem = scssItems.FirstOrDefault(x => x.Name.Equals(zblItem.Parser.Type)) ??
                              cssItems.FirstOrDefault(x => x.Name.Equals(zblItem.Parser.Type));
                if (cssItem != null)
                    WriteButton(writer, "Css", cssItem.FullName);

                if (zblItem.Parser.ViewModel.HasValue())
                {
                    var vmItem = vmItems.FirstOrDefault(x => x.Parser.FullName == zblItem.Parser.ViewModel);
                    if (vmItem != null)
                    {
                        WriteButton(writer, "ViewModel", vmItem.FullName);

                        foreach (var domItem in domItems.Where(domItem => vmItem.Parser.Tokens.Contains(domItem.Parser.TypeName, StringComparer.Ordinal)))
                            WriteButton(writer, "Domain", domItem.FullName);
                    }
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Close();

            _navXmlFile.WriteAllText(output.ToString());
        }

        static void WriteButton(XmlWriter writer, string text, string fullPath)
        {
            writer.WriteStartElement("Button");
            writer.WriteAttributeString("Text", text);
            writer.WriteAttributeString("Path", ToRelativePath(fullPath));
            writer.WriteEndElement();
        }

        static string ToRelativePath(string fullPath) => fullPath.Remove(DirectoryContext.RootFolder.FullName);
    }
}