namespace Zebble.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;
    using Olive;
    using Zebble.Tooling;

    public class TypeDetector
    {
        static DirectoryInfo AssembliesFolder => DirectoryContext.UWPBinFolder.GetSubDirectory("x86").GetSubDirectory("Debug");
        static DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver();

        static TypeDetector() => AssemblyResolver.AddSearchDirectory(AssembliesFolder.FullName);

        static AssemblyDefinition DetectAssembly(string assemblyAddress)
        {
            return AssemblyDefinition.ReadAssembly(assemblyAddress,
                new ReaderParameters { AssemblyResolver = AssemblyResolver });
        }

        static IEnumerable<FileInfo> FindAllUsedAssemblies()
        {
            if (!AssembliesFolder.Exists()) return Enumerable.Empty<FileInfo>();

            return AssembliesFolder.GetFiles()
                .Where(x => x.Extension.ToLowerOrEmpty().IsAnyOf(".dll", ".exe"))
                .Where(x => x.Extension.ToLowerOrEmpty() == ".exe" || x.Name.ToLower().StartsWith("zebble."))
                .Except(x => x.Name.ToLower().StartsWithAny(new[] { "system.", "sni.", "clr", "sqlite" }))
                .Distinct(x => x.Name.ToLower())
                .OrderBy(x => x.Name);
        }

        internal static TypeInfo[] DetectViewTypes()
        {
            return FindAllUsedAssemblies()
                   .SelectMany(x => DoDetectViewTypes(x.FullName))
                   .ToArray();
        }

        internal static EnumInfo[] DetectEnums()
        {
            return FindAllUsedAssemblies()
                   .SelectMany(x => DoDetectEnums(x.FullName))
                   .Except(x => x.Name.Contains("`"))
                   .ToArray();
        }

        static TypeInfo[] DoDetectViewTypes(string assemblyAddress)
        {
            return DetectAssembly(assemblyAddress).Modules
                .SelectMany(m => m.Types.Where(x => IsAZebbleView(x) || IsAMarkupAddable(x)))
                .Select(t => new TypeInfo(t) { Attributes = GetAttributes(t) })
                .GroupBy(x => x.Name)
                .Select(x => x.Merge())
                .ToArray();
        }

        static IEnumerable<EnumInfo> DoDetectEnums(string assemblyAddress)
        {
            foreach (var module in DetectAssembly(assemblyAddress).Modules)
                foreach (var type in module.Types.Where(x => x.IsEnum))
                    yield return new EnumInfo
                    {
                        Name = type.GetName(),
                        Options = type.Fields.Select(x => x.Name).Where(x => char.IsUpper(x[0])).ToArray()
                    };
        }

        static bool IsAZebbleView(TypeDefinition type)
        {
            try
            {
                if (type.FullName == "Zebble.View") return true;
                else if (type.BaseType != null && IsAZebbleView(type.BaseType.Resolve())) return true;
            }
            catch (Exception exception)
            {
                if (!exception.Message.StartsWith("Failed to resolve assembly"))
                    throw exception;
            }

            return false;
        }

        static bool IsAMarkupAddable(TypeDefinition type) => type.CustomAttributes.Any(x => x.AttributeType.Name == "MarkupAddableAttribute");

        static List<AttributeInfo> GetAttributes(TypeDefinition type)
        {
            var result = new List<AttributeInfo>();

            if (type.GenericParameters.Any())
                result.Add(new AttributeInfo("of"));

            foreach (var property in type.Properties)
            {
                if (property.SetMethod is null) continue;
                if (property.SetMethod.IsPrivate || property.SetMethod.IsStatic || property.PropertyType.Name.Contains("Func")) continue;

                if (property.CustomAttributes.Any(x => x.AttributeType.Name == "EditorBrowsableAttribute")) continue;

                result.Add(new AttributeInfo
                {
                    Name = property.Name,
                    Type = GetTypeName(property.PropertyType),
                });
            }

            foreach (var field in type.Fields.Where(f => f.IsPublic))
            {
                if (!char.IsUpper(field.Name[0])) continue;

                if (field.CustomAttributes.Any(x => x.AttributeType.Name == "EditorBrowsableAttribute")) continue;

                if (field.IsStatic || field.Name.IsEmpty() || field.FieldType.Name.Contains("Func")) continue;

                var isEvent = field.FieldType.Name.Contains("AsyncEvent");
                if (field.IsInitOnly && !isEvent) continue;

                if (DeclaresAttribute(type.BaseType?.Resolve(), field.Name)) continue;

                result.Add(new AttributeInfo
                {
                    Name = isEvent ? "on-" + field.Name : field.Name,
                    Type = "xs:string"
                });
            }

            if (type.Name == "View")
                result.Add(new AttributeInfo
                {
                    Name = "Style",
                    Type = "xs:string"
                });

            return result;
        }

        static bool DeclaresAttribute(TypeReference type, string name)
        {
            if (type is null) return false;
            if (type.Resolve().Fields.Any(f => f.Name == name && f.IsPublic)) return true;

            if (type.Resolve().Properties.Any(f => f.Name == name && f.SetMethod != null &&
            f.SetMethod.IsPublic)) return true;

            return DeclaresAttribute(type.Resolve().BaseType, name);
        }

        static string GetTypeName(TypeReference typeRef)
        {
            if (typeRef?.Resolve()?.IsEnum == true || typeRef?.Name == "Boolean")
                if (!typeRef.IsArray) return typeRef.Name;

            return "xs:string";
        }
    }
}