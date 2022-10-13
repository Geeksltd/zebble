using System;
using System.Xml.Linq;
using Olive;

namespace Zebble.Services
{
    public class XmlProperties
    {
        readonly XDocument Data = new();

        const string DOCUMENT_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8"" ?><Root>{0}</Root>";

        /// <summary>
        /// Creates a new XmlProperties instance.
        /// </summary>
        public XmlProperties(string xmlContent)
        {
            Data = XDocument.Parse(string.Format(DOCUMENT_TEMPLATE, xmlContent));
        }

        /// <summary>
        /// Creates a new XmlProperties instance.
        /// </summary>
        public XmlProperties() : this("")
        {
        }

        public T Get<T>(string key)
        {
            if (Data.Root.GetElement(key) is null)
            {
                return default(T);
            }
            else
            {
                return Data.Root.GetValue<T>(key);
            }
        }

        public string Set(string key, object value)
        {
            var element = Data.Root.GetElement(key);

            if (value is null) element?.Remove();
            else if (element is null)
                Data.Root.Add(new XElement(key, value));
            else element.Value = $"{value}";

            return ToString();
        }

        /// <summary>
        /// Returns the XML representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return Data.Root.ToString().TrimStart("<Root>").TrimEnd("</Root>");
        }
    }
}