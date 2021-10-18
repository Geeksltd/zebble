namespace Zebble.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Olive;

    partial class CssRule
    {
        class InheritanceDepth
        {
            static InheritanceDepth()
            {
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            }

            static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
            {
                if (All is null) return;
                var ass = args.LoadedAssembly;
                if (ass is null) return;
                if (ass.GetName().Name.StartsWith("System.")) return;
                if (!ass.References(ZebbleAssembly)) return;

                foreach (var type in ExtractTypes(ass).GroupBy(v => v.Name))
                    if (All.LacksKey(type.Key)) All[type.Key] = type.Min(GetDepth);
            }

            static Dictionary<string, int> All;

            static int GetDepth(Type type) => type.WithAllParents().TakeWhile(v => v != typeof(View)).Count();

            static Assembly ZebbleAssembly => typeof(CssRule).Assembly;

            static void Reload()
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                       .Where(c => c.References(ZebbleAssembly) || c.GetName().Name.Contains("Zebble"))
                       .ToArray();

                All = assemblies.SelectMany(ExtractTypes).GroupBy(v => v.Name).ToDictionary(x => x.Key, x => x.Min(GetDepth));
            }

            static Type[] ExtractTypes(Assembly ass)
            {
                try
                {
                    return ass.GetTypes().Where(v => v.IsA<View>())
                      .Except(v => v.IsAbstract)
                      .Except(v => v.ContainsGenericParameters)
                      .ToArray();
                }
                catch (Exception ex)
                {
                    Log.For<InheritanceDepth>().Error(ex, "Failed to load the types from " + ass.FullName);
                    return new Type[0];
                }
            }

            internal static int Of(string type)
            {
                if (All is null) Reload();

                if (All.TryGetValue(type, out var result)) return result;
                return 0;
            }
        }
    }
}