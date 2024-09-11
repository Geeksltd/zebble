namespace Zebble.Schema
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Olive;
    using Zebble.Tooling;
    using Att = AttributeInfo;

    static class SchemaGenerator
    {
        static TypeInfo[] Types;

        public static void Run()
        {
            if (!DirectoryContext.WinUIBinFolder.Exists() || DirectoryContext.WinUIBinFolder.GetFiles("*.dll", SearchOption.AllDirectories).None())
            {
                Console.WriteLine("Skipped to update the xml schema as WinUI is not compiled yet.");
                return;
            }

            try { DoGenerate(); }
            catch (Exception ex)
            {
                ConsoleHelpers.Error("Failed to update Xml Schema: " + ex.Message);
            }
        }

        static void DoGenerate()
        {
            ConsoleHelpers.Progress("-------------------------------------");
            ConsoleHelpers.Progress("Updating the generated schema...");

            var schema = new Schema();

            ConsoleHelpers.Progress("Discovering view types...");
            GetTypes().Do(schema.AddType);

            PatchNotImplementedBaseTypes(schema);

            ConsoleHelpers.Progress("Discovering Enums...");

            schema.AddAllEnumerations(TypeDetector.DetectEnums().Concat(GetPseudoEnums()));

            var target = DirectoryContext.AppUIFolder.GetSubDirectory("Views").GetFile(".zebble-schema.xml");

            schema.WriteToFile(target.FullName);
            ConsoleHelpers.Progress("Successfully updated: " + target.FullName);
        }

        static void PatchNotImplementedBaseTypes(Schema schema)
        {
            schema.baseTypesTobeImplement.ExceptWith(schema.baseTypesImplemented.ToArray());
            var notImplementedTypes = schema.baseTypesTobeImplement.ToArray();
            schema.AddPsudoTypes(notImplementedTypes);
        }

        static TypeInfo[] GetTypes()
        {
            var result = TypeDetector.DetectViewTypes();

            var viewType = result.FirstOrDefault(x => x.Name == "View");
            if (viewType == null) return Types = result;

            viewType.Attributes.AddRange(GetPseudoAttributes());
            viewType.Attributes.Where(x => x.Name.Contains("CssClass")).Do(x => x.Type = "CssStyle");
            viewType.Attributes.Where(x => x.Name.Contains("Path")).Do(x => x.Type = "ImagesPath");

            return Types = result;
        }

        static IEnumerable<Att> GetPseudoAttributes()
        {
            yield return new Att("z-ctor");
            yield return new Att("z-navBar");
            yield return new Att("z-nav-go", "ZNavType");
            yield return new Att("z-nav-forward", "ZNavType");
            yield return new Att("z-nav-params");
            yield return new Att("z-nav-transition", "PageTransition");

            yield return new Att("z-nav-throws", "Boolean");

            yield return new Att("z-animate-to");
            yield return new Att("z-animate-duration");
            yield return new Att("z-animate-easing", "AnimationEasing");
            yield return new Att("z-animate-factor", "EasingFactor");
        }

        static IEnumerable<EnumInfo> GetPseudoEnums()
        {
            yield return new EnumInfo
            {
                Name = "FormField-Of",
                Options = Types.Where(x => x.Interfaces.Contains("FormField/IControl")).Select(x => x.Name).ToArray()
            };
        }
    }
}