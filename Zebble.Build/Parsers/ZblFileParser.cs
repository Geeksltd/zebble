namespace Zebble.Build
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using Olive;

    class ZblFileParser : XmlParser
    {
        public ZblFileParser(string filePath) : base(filePath) { }

        public XElement Component { get; private set; }

        public string Namespace { get; private set; }
        public string Type { get; private set; }
        public string FullName => $"{Namespace}.{Type}";
        public string Base { get; private set; }
        public string ViewModel { get; private set; }
        public string NoNamespaceSchemaLocation { get; private set; }

        protected override void Parse()
        {
            Component = Document.GetElement("z-Component");
            Namespace = Component.GetValue<string>("@z-namespace");
            Type = Component.GetValue<string>("@z-type");
            Base = Component.GetValue<string>("@z-base");
            ViewModel = Component.GetValue<string>("@z-viewmodel");
            NoNamespaceSchemaLocation = Component.GetValue<string>("@noNamespaceSchemaLocation");
        }

        public override string ToString() => FullName;
    }
}
