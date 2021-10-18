using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Zebble.Common.Constants;
using Zebble.Common.Helpers;
using Zebble.Common.Serialization;

namespace Zebble.FormatZbl.Helpers
{
    static class XmlHelpers
    {
        public static void CleanupZebbleFile(string filePath)
        {
            using var output = new ZebbleStringWriter(Encoding.UTF8);

            using var writer = new ZebbleWriter(output, new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "   "
            });

            var document = XDocument.Load(filePath);
            var element = document.Elements().First();

            var attributes = new[] {
                new XAttribute(
                    XName.Get(ZebbleSolutionItemNames.Xsi, "http://www.w3.org/2000/xmlns/"), "http://www.w3.org/2001/XMLSchema-instance"
                ),
                new XAttribute(
                    XName.Get(ZebbleSolutionItemNames.NoNamespaceSchemaLocation, "http://www.w3.org/2001/XMLSchema-instance"), PathHelpers.GetRelativeSchemaPath(filePath)
                )
            };

            WriteElement(writer, element, attributes);

            writer.Close();

            File.WriteAllText(filePath, output.ToString());
        }

        static string ConvertAttributeToNewFormat(string name) => name.Replace("z-", "");

        static void WriteElement(ZebbleWriter writer, XElement element, IList<XAttribute> updatedAttributes = null)
        {
            var lineLength = 0;

            if (element.Name == "z-Component" && element.Parent == null)
            {
                lineLength = GetElementLength(element) + GetAttributesLength(updatedAttributes);
                writer.WriteStartElement("zbl", "");

                if (updatedAttributes != null)
                    for (int i = 0; i < updatedAttributes.Count(); i++)
                    {
                        var attribute = updatedAttributes[i];

                        writer.WriteAttributeString(attribute.Name.LocalName,
                        attribute.Name.NamespaceName,
                        attribute.Value,
                        IsChopRequired(lineLength) && i != 0);
                    }
            }

            if (element.Name == "z-Component")
                element.Name = "class";

            writer.WriteStartElement(
                element.Name.LocalName,
                element.Name.NamespaceName
            );

            var attributes = element.Attributes()
                .Where(x => string.IsNullOrEmpty(x.Name.NamespaceName) && x.Name.LocalName != "xsi" && x.Name.LocalName != "xsi")
                .ToList();

            lineLength = GetElementLength(element) + GetAttributesLength(attributes);

            for (var index = 0; index < attributes.Count; index++)
            {
                var attribute = attributes[index];

                writer.WriteAttributeString(
                    ConvertAttributeToNewFormat(attribute.Name.LocalName),
                    attribute.Name.NamespaceName,
                    attribute.Value,
                    IsChopRequired(lineLength) && index != 0
                );
            }

            if (element.HasElements)
                foreach (var innerElement in element.Elements())
                    WriteElement(writer, innerElement);

            if (element.Name == "z-Component" && element.Parent == null)
                writer.WriteEndElement();

            writer.WriteEndElement();
        }

        static bool IsChopRequired(int lineLength) => lineLength > 120;

        static int GetElementLength(XElement element)
        {
            var length = 1 + element.Name.LocalName.Length;

            if (!string.IsNullOrEmpty(element.Name.NamespaceName))
                length += element.Name.NamespaceName.Length + 1;

            if (element.HasElements)
                length += 1;
            else
                length += 3;

            return length;
        }

        static int GetAttributesLength(IEnumerable<XAttribute> attributes)
        {
            return attributes.Sum(x =>
            {
                var length = 4 + x.Name.LocalName.Length + x.Value.Length;

                if (!string.IsNullOrEmpty(x.Name.NamespaceName))
                    length += x.Name.NamespaceName.Length + 1;

                return length;
            });
        }
    }
}