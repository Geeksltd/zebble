namespace Zebble.Image
{
    using SkiaSharp;
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Zebble.Tooling;
    using Olive;

    class MetaXmlGenerator
    {
        static string[] ImageExtensions = "jpg jpeg gif png webp".Split(' ').Trim().ToArray();

        public void Generate()
        {
            Console.WriteLine("Updating Zebble-meta.xml...");

            var code = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + Environment.NewLine +
                GenerateRoot();

            DirectoryContext.AppUIResourcesFolder.GetFile("Zebble-Meta.xml").WriteAllText(code);
        }

        XElement GenerateRoot()
        {
            var result = new XElement("data");

            foreach (var file in DirectoryContext.AppUIResourcesFolder.GetFiles(includeSubDirectories: true))
            {
                var extension = Path.GetExtension(file).OrEmpty().ToLower().TrimStart(".");

                if (extension.IsNoneOf(ImageExtensions)) continue;

                var path = file.TrimStart(DirectoryContext.AppUIResourcesFolder.FullName).KeepReplacing("\\", "/").TrimStart("/");

                using (var image = SKBitmap.Decode(file))
                {
                    try
                    {
                        result.Add(new XElement("image",
                            new XAttribute("path", path),
                            new XAttribute("width", image.Width),
                            new XAttribute("height", image.Height)));
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelpers.Error("Failed to load image: " + file + Environment.NewLine +
                               ex.Message);
                    }
                }
            }

            return result;
        }
    }
}