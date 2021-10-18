namespace Zebble.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Olive;

    public class TypeInfo
    {
        internal TypeDefinition Type;


        public TypeInfo(TypeDefinition type)
        {
            Type = type;
            Interfaces = type.Interfaces.Select(x => x.InterfaceType.FullName).ToArray();
            Name = type.GetName();
            IsAbstract = type.IsAbstract;
            IsGeneric = type.IsGenericInstance;
            HasGenericArgs = type.GenericParameters.Any();

            Base = type.BaseType.GetName().Replace(".", "__");
            NamedAfterParent = Name == Base;
            Base += type.BaseType.GetBaseClassExtension();

            if (type.BaseType?.FullName == "System.Object")
                Base = string.Empty;

            try { IsBaseAbstract = type.BaseType?.Resolve().IsAbstract == true; }
            catch { Console.WriteLine("Failed to resolve: " + type.BaseType.FullName); }
        }

        public string Name, Base;
        public string[] Interfaces;
        public bool IsAbstract, IsBaseAbstract, IsGeneric, NamedAfterParent, HasGenericArgs;
        public List<AttributeInfo> Attributes { set; get; }
    }

    public static class TypeDefinitionExtensions
    {
        internal static string GetName(this TypeReference type)
        {
            return
            type.Namespace.Unless("Zebble").Unless("UI").Unless("Zebble.Plugin").WithSuffix(".")
            + type.Name.RemoveFrom("`");
        }

        internal static string GetBaseClassExtension(this TypeReference baseType)
        {
            var abstractClassNames = new string[] { "View", "Page", "NavBarTabsPage" };
            return abstractClassNames.Contains(GetName(baseType)) ? "-AbstractType" : "Type";
        }

        public static TypeInfo Merge(this IEnumerable<TypeInfo> sameName)
        {
            if (sameName.IsSingle()) return sameName.Single();

            var root = sameName.FirstOrDefault(x => !x.NamedAfterParent);
            if (root == null) return sameName.First(); // should not happen

            var existingAttributes = root.Attributes.Select(v => v.Name).ToArray();
            var extraAttributes = sameName.SelectMany(v => v.Attributes)
                .Where(v => v.Name.IsNoneOf(existingAttributes)).Distinct(x => x.Name).ToArray();
            root.Attributes.AddRange(extraAttributes);

            if (sameName.Except(x => x.IsAbstract).All(v => v.HasGenericArgs))
            {
                var of = root.Attributes.FirstOrDefault(x => x.Name == "of");
                if (of != null) of.IsMandatory = true;
            }

            if (sameName.Any(x => !x.IsAbstract))
                root.IsAbstract = false;

            return root;
        }
    }
}