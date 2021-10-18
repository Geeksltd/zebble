namespace Zebble.Css
{
    using Newtonsoft.Json;
    using Olive;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Zebble.Css.DataType;
    using Zebble.Tooling;

    class CssCompiler : BaseGenerator
    {
        const int COMMENT_LINE_LENGTH = 70;

        List<string> Result = new List<string>();

        protected override string GetFileName() => ".zebble-generated-css.cs";

        protected override string GenerateCode()
        {
            EnsureSorceMap();
            Result.Add("using System;");
            Result.Add("using Zebble;");
            Result.Add("using Zebble.Services;");
            Result.Add("using Zebble.Plugin;");
            Result.Add("using Zebble.Device;");
            Result.Add("using UI.Modules;");
            Result.Add("using UI.Pages;");
            Result.Add("using UI.Templates;");
            Result.Add("using System.Threading.Tasks;");
            Result.Add("using Olive;");
            Result.Add(string.Empty);

            Result.Add("namespace UI");
            Result.Add("{");
            Result.Add(Environment.NewLine);
            Result.Add("[EscapeGCop(\"Auto-generated\")]");
            Result.Add("public class CssStyles");
            Result.Add("{");

            var rules = new CssManager().ExtractRules();

            Result.Add("public static void LoadAll()");
            Result.Add("{");

            foreach (var group in rules.GroupBy(x => x.File))
            {
                Result.Add(Environment.NewLine);
                Result.Add("// =".PadRight(COMMENT_LINE_LENGTH + 3, '='));
                Result.Add("// " + group.Key.PadRight(COMMENT_LINE_LENGTH, '-'));

                foreach (var byPlatform in group.GroupBy(x => x.Platform))
                {
                    if (byPlatform.Key.HasValue())
                    {
                        Result.Add($"if (CssEngine.Platform == DevicePlatform.{byPlatform.Key})");
                        Result.Add("{");
                    }

                    foreach (var item in byPlatform)
                        Result.Add($"CssEngine.Add(new {item.ClassFullName()}());");

                    if (byPlatform.Key.HasValue())
                    {
                        Result.Add("}");
                        Result.Add("");
                    }
                }

                Result.Add("");
            }

            Result.Add("}");
            Result.Add(string.Empty);

            Result.Add("}");
            Result.Add("}");

            Result.Add("");
            Result.Add("// Ensure auto-generated namespaces exist:");
            Result.Add("namespace UI.Modules { }");
            Result.Add("namespace UI.Pages { }");
            Result.Add("namespace UI.Templates { }");
            Result.Add("namespace Zebble.Plugin { }");
            Result.Add("namespace Zebble.Data { }");

            foreach (var rule in rules)
            {
                Result.Add(string.Empty);
                Result.Add(rule.GenerateClass());
            }

            return new CSharpFormatter(Result.ToLinesString()).Format();
        }

        void EnsureSorceMap()
        {
            var file = DirectoryContext.AppUIFolder.GetFile("compilerconfig.json.defaults");
            if (!file.Exists()) return;

            var current = file.ReadAllText();
            const string bad = "\"sourceMap\": false";
            const string good = "\"sourceMap\": true";

            if (current.Contains(bad))
                file.WriteAllText(current.Replace(bad, good));
        }
    }
}