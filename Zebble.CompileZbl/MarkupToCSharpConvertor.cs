namespace Zebble.CompileZbl
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Olive;
    using Zebble.Tooling;

    class MarkupToCSharpConvertor : BaseGenerator
    {
        protected override string GenerateCode()
        {
            var result = DirectoryContext.AppUIFolder.GetFiles("*.zbl", SearchOption.AllDirectories)
                .Select(f => Generate(f)).Trim().ToList()
                .ToString($"{Environment.NewLine}{Environment.NewLine}");

            var resourceHash = new ResourceVersionUpdater().FindNewHash();
            result += $"{Environment.NewLine}{Environment.NewLine}";
            result += $"namespace UI{Environment.NewLine}";
            result += $"{{{Environment.NewLine}";
            result += $"    partial class StartUp{Environment.NewLine}";
            result += $"    {{{Environment.NewLine}";
            result += $"        // Hashed content of all resources{Environment.NewLine}";
            result += $"        public override string GetResourcesVersion() => \"{resourceHash}\";{Environment.NewLine}";
            result += $"    }}{Environment.NewLine}";
            result += $"}}{Environment.NewLine}";

            return result;
        }

        protected override string GetFileName() => ".zebble-generated.cs";

        string Generate(FileInfo markupFile)
        {
            // Program.Progress("Converting ZBL file to C# " + markupFile.FullName);

            var path = markupFile.FullName.TrimStart(DirectoryContext.AppUIFolder.FullName).TrimStart(Path.DirectorySeparatorChar);

            var markup = markupFile.ReadAllText();

            if (markup.Contains("z-Component"))
                markup = ZblFormat.XmlHelpers.CleanupZebbleFile(markupFile.FullName, write: false);

            if (markup.IsEmpty()) return null;

            // var result = RazorToXml(markup);
            var result = markup;
            XElement type;

            try { type = result.To<XElement>(); }
            catch (Exception ex)
            { return Error("Failed to parse " + path + ": " + ex.Message); }

            try
            {
                return type.Elements()
                    .Where(v => v.Name == "class")
                    .Select(v => new ZblRootClassGenerator(v).Generate(path))
                    .ToLinesString();
            }
            catch (Exception ex)
            { return Error("Failed to generate C# from " + path + Environment.NewLine + ex); }
        }
    }
}