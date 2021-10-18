namespace Zebble.Schema
{
    using System.Xml;
    using System.Xml.Schema;

    public class AttributeInfo
    {
        public string Name { set; get; }
        public string Type { set; get; } = "xs:string";
        public bool IsMandatory;

        public AttributeInfo() { }
        public AttributeInfo(string name) => Name = name;
        public AttributeInfo(string name, string type) : this(name) => Type = type;

        public override string ToString() => Type + " " + Name;

        public XmlSchemaAttribute ToSchema()
        {
            var result = new XmlSchemaAttribute
            {
                Name = Name,
                SchemaTypeName = new XmlQualifiedName(Type)
            };

            if (IsMandatory) result.Use = XmlSchemaUse.Required;

            return result;
        }
    }
}