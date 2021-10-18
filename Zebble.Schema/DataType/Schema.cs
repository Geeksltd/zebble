namespace Zebble.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using Olive;

    public class Schema
    {
        readonly XmlSchema Doc = new XmlSchema();

        public Schema() => AddConstantElements();

        bool IsFirstType => CountItems() == 0;

        void AddConstantElements()
        {
            AddZbl();
            AddClass();
            AddzPlace();
            AddzForeach();
            AddBoolean();
            AddStringPattern();
            AddImagePathEnumeration();
            AddNavigationPageEnumeration();
            AddStylesEnumeration();
        }

        void AddStringPattern()
        {
            var stringPattern = new XmlSchemaSimpleType { Name = "StringPattern" };
            Doc.Items.Add(stringPattern);

            var stringPatternRestriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName = Type("string", "http://www.w3.org/2001/XMLSchema")
            };

            stringPattern.Content = stringPatternRestriction;
        }

        void AddBoolean()
        {
            var booleanEnum = new XmlSchemaSimpleType { Name = "BooleanEnum" };
            Doc.Items.Add(booleanEnum);

            var booleanEnumRestriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName = Type("string", "http://www.w3.org/2001/XMLSchema")
            };

            booleanEnumRestriction.Facets.Add(new XmlSchemaEnumerationFacet { Value = "true" });
            booleanEnumRestriction.Facets.Add(new XmlSchemaEnumerationFacet { Value = "false" });
            booleanEnum.Content = booleanEnumRestriction;

            var boolean = new XmlSchemaSimpleType { Name = "Boolean" };
            Doc.Items.Add(boolean);

            var union = new XmlSchemaSimpleTypeUnion
            {
                MemberTypes = new XmlQualifiedName[2]
                    {Type("BooleanEnum"), Type("StringPattern")}
            };

            boolean.Content = union;
        }

        void AddzPlace()
        {
            var sequence = new XmlSchemaSequence();
            sequence.Items.Add(new XmlSchemaAny { MinOccurs = 0, MaxOccurs = decimal.MaxValue });

            var complexType = new XmlSchemaComplexType
            {
                Particle = sequence,
                AnyAttribute = new XmlSchemaAnyAttribute { ProcessContents = XmlSchemaContentProcessing.Skip }
            };

            complexType.Attributes
                .Add(new XmlSchemaAttribute
                {
                    Name = "inside",
                    SchemaTypeName = Type("xs:Name"),
                    Use = XmlSchemaUse.Required
                });

            Doc.Items.Add(new XmlSchemaElement { Name = "place", SchemaType = complexType });
        }

        void AddzForeach()
        {
            var sequence = new XmlSchemaSequence();
            sequence.Items.Add(new XmlSchemaAny { MinOccurs = 0, MaxOccurs = decimal.MaxValue });

            var complexType = new XmlSchemaComplexType
            {
                Particle = sequence,
                AnyAttribute = new XmlSchemaAnyAttribute { ProcessContents = XmlSchemaContentProcessing.Skip }
            };

            complexType.Attributes
                .Add(new XmlSchemaAttribute
                {
                    Name = "var",
                    SchemaTypeName = Type("xs:Name"),
                    Use = XmlSchemaUse.Required
                });

            complexType.Attributes
                .Add(new XmlSchemaAttribute
                {
                    Name = "in",
                    SchemaTypeName = Type("xs:string"),
                    Use = XmlSchemaUse.Required
                });

            Doc.Items.Add(new XmlSchemaElement { Name = "foreach", SchemaType = complexType });
        }

        void AddImagePathEnumeration()
        {
            AddEnumeration(new EnumInfo
            {
                Name = "ImagesPath",
                Options = FileHelper.FindAllImages().ToArray()
            });
        }

        void AddNavigationPageEnumeration()
        {
            AddEnumeration(new EnumInfo { Name = "ZNavType", Options = FileHelper.FindAllZebblePages() });
        }

        void AddStylesEnumeration()
        {
            var styles = FileHelper.FindAllCssStyle();

            var enumeration = new EnumInfo
            {
                Name = "CssStyle",
                Options = styles
            };

            AddEnumeration(enumeration);
        }

        static XmlQualifiedName Type(string name) => new XmlQualifiedName(name);

        static XmlQualifiedName Type(string name, string ns) => new XmlQualifiedName(name, ns);

        void AddZbl()
        {
            var zblElement = new XmlSchemaElement { Name = "zbl" };
            Doc.Items.Add(zblElement);

            var zblComplexType = new XmlSchemaComplexType();
            zblElement.SchemaType = zblComplexType;
            var zblSequence = new XmlSchemaSequence();
            zblComplexType.Particle = zblSequence;

            var zblAny = new XmlSchemaAny
            {
                MinOccurs = 0,
                MaxOccurs = decimal.MaxValue
            };

            zblSequence.Items.Add(zblAny);
        }

        void AddClass()
        {
            var classElement = new XmlSchemaElement { Name = "class" };
            Doc.Items.Add(classElement);

            var zblComplexType = new XmlSchemaComplexType();
            classElement.SchemaType = zblComplexType;
            var zblSequence = new XmlSchemaSequence();
            zblComplexType.Particle = zblSequence;

            var zComponentAny = new XmlSchemaAny
            {
                MinOccurs = 0,
                MaxOccurs = decimal.MaxValue
            };

            zblSequence.Items.Add(zComponentAny);

            var zComponentAttrType = new XmlSchemaAttribute
            {
                Name = "type",
                SchemaTypeName = Type("xs:Name"),
                Use = XmlSchemaUse.Required
            };

            zblComplexType.Attributes.Add(zComponentAttrType);

            var zComponentAttrBase = new XmlSchemaAttribute
            {
                Name = "base",
                SchemaTypeName = Type("xs:string"),
                Use = XmlSchemaUse.Required
            };

            zblComplexType.Attributes.Add(zComponentAttrBase);

            var zComponentAttrNamespace = new XmlSchemaAttribute
            {
                Name = "namespace",
                SchemaTypeName = Type("xs:Name")
            };

            zblComplexType.Attributes.Add(zComponentAttrNamespace);
            // <xs:attribute name="z-implements" type="xs:Name" use="required"/>
            var zComponentAttrImplements = new XmlSchemaAttribute
            {
                Name = "implements",
                SchemaTypeName = Type("xs:string")
            };

            zblComplexType.Attributes.Add(zComponentAttrImplements);

            zblComplexType.Attributes
                .Add(new XmlSchemaAttribute
                {
                    Name = "cache",
                    SchemaTypeName = Type("Boolean")
                });

            zblComplexType.Attributes
                .Add(new XmlSchemaAttribute
                {
                    Name = "viewmodel",
                    SchemaTypeName = Type("xs:string")
                });

            // <xs:attribute name="Title" type="xs:string"/>
            var zComponentAttrTitle = new XmlSchemaAttribute
            {
                Name = "Title",
                SchemaTypeName = Type("xs:string")
            };

            zblComplexType.Attributes.Add(zComponentAttrTitle);
            // <xs:anyAttribute processContents="skip"/>
            var zComponentAttrAny = new XmlSchemaAnyAttribute
            {
                ProcessContents = XmlSchemaContentProcessing.Skip
            };

            zblComplexType.AnyAttribute = zComponentAttrAny;
        }

        public void AddModule(ModuleInfo module)
        {
            // <xs:element name="Modules.LoginForm">
            var element = new XmlSchemaElement { Name = module.CompleteName };
            Doc.Items.Add(element);
            // <xs:complexType>
            var complex = new XmlSchemaComplexType();
            element.SchemaType = complex;
            // <xs:attribute name="Id" type="xs:string" use="required" />
            var attrId = new XmlSchemaAttribute
            {
                Name = "Id",
                SchemaTypeName = Type("xs:string")
            };

            complex.Attributes.Add(attrId);
            // <xs:attribute name="Path" type="xs:string" />
            var attrPath = new XmlSchemaAttribute
            {
                Name = "Path",
                SchemaTypeName = Type("ImagesPath"),
                Use = XmlSchemaUse.Optional
            };

            complex.Attributes.Add(attrPath);
            // <xs:attribute name="nav-go" type="xs:string" />
            var attrZNavgo = new XmlSchemaAttribute
            {
                Name = "nav-go",
                SchemaTypeName = Type("ZNavType"),
                Use = XmlSchemaUse.Optional
            };

            complex.Attributes.Add(attrZNavgo);
            // <xs:attribute name="nav-forward" type="xs:string" />
            var attrZNavforward = new XmlSchemaAttribute
            {
                Name = "nav-forward",
                SchemaTypeName = Type("ZNavType"),
                Use = XmlSchemaUse.Optional
            };

            complex.Attributes.Add(attrZNavforward);
            // <xs:anyAttribute processContents="skip"/>
            var attrAny = new XmlSchemaAnyAttribute
            {
                ProcessContents = XmlSchemaContentProcessing.Skip
            };

            complex.AnyAttribute = attrAny;
        }

        public void AddAllModules(IEnumerable<ModuleInfo> modules)
        {
            foreach (var module in modules)
                AddModule(module);
        }

        public void AddEnumeration(EnumInfo enumeration)
        {
            // <xs:simpleType name="*Enum">
            var type = new XmlSchemaSimpleType { Name = enumeration.Name + "Enum" };

            // <xs:restriction base="xs:string">
            var restriction = new XmlSchemaSimpleTypeRestriction
            {
                BaseTypeName = Type("string", "http://www.w3.org/2001/XMLSchema")
            };

            /*
             * <xs:enumeration value="option1" />
             * <xs:enumeration value="option2" />
             * <xs:enumeration value="option3" />
             */
            foreach (var option in enumeration.Options)
            {
                var enumerationFacet = new XmlSchemaEnumerationFacet { Value = option };
                restriction.Facets.Add(enumerationFacet);
            }

            // <xs:enumeration value = "@....." />
            restriction.Facets.Add(new XmlSchemaEnumerationFacet { Value = "@....." });
            type.Content = restriction;
            Doc.Items.Add(type);

            /*
             * this part union the *Enum with StringPattern to allow
             * assignment of dynamic values which can't represent
             * in the enumeration format
             * 
             */
            var enumerationNames = enumeration.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var name = enumeration.Name;

            if (enumerationNames.Length > 1)
                name = enumerationNames.Last();

            var type2 = new XmlSchemaSimpleType { Name = name };

            var union = new XmlSchemaSimpleTypeUnion
            {
                MemberTypes = new[]
                    {Type(type.Name), Type("StringPattern")}
            };

            type2.Content = union;
            Doc.Items.Add(type2);
        }

        public void AddAllEnumerations(IEnumerable<EnumInfo> enumerations)
        {
            foreach (var enumeration in enumerations)
                AddEnumeration(enumeration);
        }

        public HashSet<string> baseTypesTobeImplement = new HashSet<string>();
        public HashSet<string> baseTypesImplemented = new HashSet<string>();

        public void AddType(TypeInfo type)
        {
            if (IsFirstType || type.Base == "System.Object" || type.Name == "View")
            {
                AddFirstType(type);
                return;
            }

            var complex = new XmlSchemaComplexType { Name = GetNodeType(type.Name) };

            if (type.IsAbstract)
                complex.Name = complex.Name.TrimEnd("Type") + "-AbstractType";

            Doc.Items.Add(complex);

            var baseType = type.Base;

            if (baseType.Contains("`"))
                baseType = "View-AbstractType";

            else if (type.IsBaseAbstract && !baseType.EndsWith("-AbstractType"))
                baseType = baseType.TrimEnd("Type") + "-AbstractType";

            var extention = new XmlSchemaComplexContentExtension
            {
                BaseTypeName = Type(baseType),
                AnyAttribute = new XmlSchemaAnyAttribute { ProcessContents = XmlSchemaContentProcessing.Skip }
            };

            complex.ContentModel = new XmlSchemaComplexContent { Content = extention };

            foreach (var attribute in type.Attributes)
            {
                // frz: patched for overrided Text property in derived types (XML intellisense broken with duplication Text prop definition)
                if (complex.Name == "TextControl-AbstractType" && attribute.Name == "Text") continue;

                extention.Attributes.Add(attribute.ToSchema());
            }

            baseTypesImplemented.Add(complex.Name);
            baseTypesTobeImplement.Add(extention.BaseTypeName.Name);

            // <xs:element name="*" nillable="true" type="*Type" />
            /* the abstract types doesn't need an concrete element */
            if (type.IsAbstract) return;

            var element = new XmlSchemaElement
            {
                Name = type.Name,
                IsNillable = true,
                SchemaTypeName = Type(GetNodeType(type.Name))
            };

            Doc.Items.Add(element);
            baseTypesImplemented.Add(element.Name);
        }

        internal static string GetNodeType(string name) => name.Remove("Zebble.").Replace(".", "__") + "Type";

        void AddFirstType(TypeInfo type)
        {
            // <xs:complexType name="ViewType">
            var complex = new XmlSchemaComplexType { Name = GetNodeType(type.Name) };

            if (type.IsAbstract)
                complex.Name = complex.Name.TrimEnd("Type") + "-AbstractType";

            Doc.Items.Add(complex);

            // <xs:sequence>
            var sequence = new XmlSchemaSequence();
            complex.Particle = sequence;

            // <xs:any minOccurs="0" maxOccurs="unbounded" />
            var any = new XmlSchemaAny
            {
                MinOccurs = 0,
                MaxOccurs = decimal.MaxValue
            };

            sequence.Items.Add(any);

            // <xs:anyAttribute processContents="skip"/>
            complex.AnyAttribute = new XmlSchemaAnyAttribute
            {
                ProcessContents = XmlSchemaContentProcessing.Skip
            };

            // <xs:attribute name="*" type="*"/>
            foreach (var attribute in type.Attributes)
                complex.Attributes.Add(attribute.ToSchema());

            baseTypesImplemented.Add(complex.Name);

            // <xs:element name="*" nillable="true" type="*Type" />
            /* the abstract types doesn't need an concrete element */
            if (type.IsAbstract) return;

            var element = new XmlSchemaElement
            {
                Name = type.Name,
                IsNillable = true,
                SchemaTypeName = Type(GetNodeType(type.Name))
            };

            Doc.Items.Add(element);

            baseTypesImplemented.Add(element.Name);
        }

        public void WriteToFile(string path)
        {
            using (var outputFile = new StreamWriter(path))
                Doc.Write(outputFile);
        }

        int CountItems()
        {
            var count = 0;
            var enumarator = Doc.Items.GetEnumerator();

            while (true)
            {
                try
                {
                    if (enumarator.Current is XmlSchemaComplexType)
                        count++;
                }
                catch
                {
                    // No logging is needed
                }

                if (!enumarator.MoveNext()) return count;
            }
        }

        public void AddPsudoTypes(string[] notImplementedTypes)
        {
            foreach (var type in notImplementedTypes)
            {
                var complex = new XmlSchemaComplexType { Name = type };
                // <xs:sequence>
                var sequence = new XmlSchemaSequence();
                complex.Particle = sequence;

                // <xs:any minOccurs="0" maxOccurs="unbounded" />
                var any = new XmlSchemaAny
                {
                    MinOccurs = 0,
                    MaxOccurs = decimal.MaxValue
                };

                sequence.Items.Add(any);
                Doc.Items.Add(complex);
            }
        }
    }
}