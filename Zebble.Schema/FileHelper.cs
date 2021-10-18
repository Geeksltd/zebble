namespace Zebble.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Olive;
    using Zebble.Tooling;

    public static class FileHelper
    {
        static readonly string[] ImageExtensions = { "png", "jpg", "jpeg", "bmp", "gif", "tiff", "tif" };

        public static List<string> FindAllImages()
        {
            return FindAllFiles(ImageExtensions)
                .Where(x => x.Contains("\\Resources\\"))
                    .Select(f => f.RemoveBefore("\\Resources\\", caseSensitive: false))
                    .Select(f => f.Replace("\\", "/"))
                    .ToList();
        }

        public static string[] FindAllCssStyle()
        {
            var styles = new List<string>();

            foreach (var file in FindAllFiles(new[] { "scss" }))
            {
                var text = File.ReadAllText(file);
                var expressions = new Regex(@"\.[a-zA-z0-9\-]*[\s]*\{").Matches(text);

                foreach (Match expression in expressions)
                    styles.Add(expression.Value.Remove("{").Trim().Remove("."));
            }

            return styles.ToArray();
        }

        public static string[] FindAllZebblePages()
        {
            var pages = new List<string>();
            var fileList = FindAllFiles(new string[] { "zbl" }).Where(f => f.Contains("\\Views\\Pages"));

            foreach (var page in fileList)
            {
                var text = File.ReadAllText(page);
                var xDocument = new XmlDocument();
                xDocument.LoadXml(text);

                var attributes = xDocument.DocumentElement.Attributes;
                var zType = attributes["z-type"]?.Value;
                var zNameSpace = attributes["z-namespace"]?.Value.OrEmpty().TrimStart("UI.");

                if (xDocument.DocumentElement.Name == "zbl")
                {
                    foreach (XmlNode item in xDocument.DocumentElement.ChildNodes)
                    {
                        attributes = item.Attributes;
                        zType = (attributes["type"]).Value;
                        zNameSpace = (attributes["namespace"]).Value.OrEmpty().TrimStart("UI.");

                        pages.Add($"{zNameSpace}.{zType}");
                    }

                    continue;
                }

                pages.Add($"{zNameSpace}.{zType}");
            }

            return pages.ToArray();
        }

        static List<string> FindAllFiles(string[] extensions)
        {
            return DirectoryContext.RootFolder.GetFiles(includeSubDirectories: true)
                    .Where(f => extensions.Any(s => f.ToLower().EndsWith("." + s)))
                    .ToList();
        }
    }
}