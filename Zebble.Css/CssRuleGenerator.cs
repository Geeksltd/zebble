namespace Zebble.Css
{
    using Olive;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    class CssRuleGenerator
    {
        public string File, Selector, Platform, Body, ClassBaseName, SelectorSource;
        public int RepeatIndex;

        public CssRuleGenerator(string file, string body, string selector, string selectorSource)
        {
            File = file;
            Body = body;
            Selector = selector;
            SelectorSource = selectorSource;
            Platform = GetPlatform(selector);

            if (Platform.HasValue()) Selector = Selector.Split(' ').ExceptFirst().ToString(" ");

            ClassBaseName = Selector.Replace(".", " ").Replace("-", " ").Replace(">", " ")
                .Replace("_", " ").Replace("#", " ").Replace(":", " ")
                     .KeepReplacing("  ", " ").ToPascalCaseId();
        }

        string GetPlatform(string selector)
        {
            var firstPart = selector.Split(' ').Trim().FirstOrDefault();
            if (!firstPart.OrEmpty().EndsWith("-only")) return null;
            firstPart = firstPart.TrimEnd("-only").TrimStart(".").ToLower();

            if (firstPart.IsAnyOf("android", "windows")) return firstPart.ToPascalCaseId();
            else if (firstPart == "ios") return "IOS";
            else return null;
        }

        public bool Equals(CssRuleGenerator other) => File == other.File && Selector == other.Selector && Body == other.Body;

        string Namespace()
        {
            return "File_" + File.TrimEnd(".scss", caseSensitive: false).Replace(Path.DirectorySeparatorChar.ToString(), ".").KeepReplacing("..", ".")
                .Split('.').Trim().Select(x => x.ToPascalCaseId()).ToString(".");
        }

        public string ClassFullName() => Namespace() + "." + GetClassName();

        public string GetClassName() => ClassBaseName + RepeatIndex.ToString().Unless("0").WithPrefix("_") + "CssRule";

        internal string GenerateClass()
        {
            var r = new StringBuilder();
            r.AppendLine("namespace " + Namespace());
            r.AppendLine("{");
            r.AppendLine("[EscapeGCop(\"Auto-generated\")]");
            r.Append($"[CssSelector(");
            r.Append(Platform.WithWrappers("DevicePlatform.", ", "));
            r.AppendLine($"\"{SelectorSource}\", \"{Selector}\")]");
            r.AppendLine("[CssBody(\"" + Body.Remove("\r").Replace("\n", " ").KeepReplacing("  ", " ").Replace("\"", "\\\"") + "\")]");
            r.AppendLine($"class {GetClassName()} : CssRule");
            r.AppendLine("{");
            r.AppendLine();

            r.AppendLine("public override bool Matches(View view)");
            r.AppendLine("{");
            r.AppendLine(GenerateMatches());
            r.AppendLine("}");
            r.AppendLine();

            r.Append("public override ");

            var tag = GetTag(Selector.Split(' ').Last());

            var methodBody = new StringBuilder();

            if (tag.HasValue() && tag != "*")
            {
                if (tag.Contains("..")) tag = tag.RemoveFrom("..") + "<" + tag.RemoveBeforeAndIncluding("..") + ">";
                methodBody.AppendLine("var view = (" + tag.Replace("-", ".") + ")untypedView;");
            }

            methodBody.AppendLine(new CssBodyProcessor(Body).GenerateCode());

            if (methodBody.ToString().ContainsWholeWord("await"))
                r.Append("async ");

            r.Append("Task Apply(View ");

            if (tag.HasValue() && tag != "*") r.AppendLine("untypedView)");
            else r.AppendLine("view)");

            r.AppendLine("{");
            r.AppendLine(methodBody.ToString());

            if (!methodBody.ToString().ContainsWholeWord("await"))
                r.AppendLine("return Task.CompletedTask;");

            r.AppendLine("}");
            r.AppendLine("}");
            r.AppendLine("}");

            r.AppendLine();
            return r.ToString();
        }

        string GenerateMatches()
        {
            var r = new StringBuilder();

            var parts = Selector.Split(' ').Trim().Reverse().ToArray();

            var directCondition = GetMatchCondition(parts.First(), isDirect: true);

            if (directCondition.HasValue())
                r.AppendLine($"if (!({directCondition})) return false;");
            else if (parts.First() != "*") r.AppendLine("// CssEngine will only call me if a view matches: " + parts.First());

            r.AppendLine();

            var shouldRecurseUp = true;

            foreach (var parent in parts.ExceptFirst())
            {
                if (parent == ">") { shouldRecurseUp = false; continue; }

                r.AppendLine();

                if (shouldRecurseUp)
                {
                    var tag = GetTag(parent);

                    if (parent.StartsWith("#") && parent.Substring(1).LacksAll(":", "."))
                    {
                        r.AppendLine($"view = CssEngine.FindParentById(view, \"{parent.Substring(1)}\");");
                        r.AppendLine($"if (view is null) return false;");
                    }
                    else if (parent.StartsWith(".") && parent.Substring(1).LacksAll(":", ".", "#"))
                    {
                        r.AppendLine($"view = CssEngine.FindParentByCssClass(view, \"{parent.Substring(1)}\");");
                        r.AppendLine($"if (view is null) return false;");
                    }
                    else if (tag.HasValue() && tag != "*" && parent.Substring(1).LacksAll(":", ".", "#"))
                    {
                        r.AppendLine($"view = CssEngine.FindParentByType<{tag}>(view);");
                        r.AppendLine($"if (view is null) return false;");
                    }
                    else
                    {
                        r.AppendLine($"view = view.parent;");

                        r.AppendLine("while (true)");
                        r.AppendLine("{");

                        r.AppendLine("if (view == null) return false;");
                        r.AppendLine($"else if ({GetMatchCondition(parent, isDirect: false) }) break;");

                        r.AppendLine("view = view.parent;");
                        r.AppendLine("}");
                    }
                }
                else
                {
                    r.AppendLine($"view = view.parent;");
                    r.AppendLine("if (view == null) return false;");
                    r.AppendLine($"else if (!({GetMatchCondition(parent, isDirect: false)})) return false;");
                    shouldRecurseUp = true;
                }

                r.AppendLine();
            }

            r.AppendLine();
            r.AppendLine("return true;");
            return r.ToString();
        }

        static string GetTag(string direct) => direct.RemoveFrom(".").RemoveFrom("#").RemoveFrom(":").Replace("-", ".");

        static string GetMatchCondition(string selectorPart, bool isDirect)
        {
            var conditions = new List<string>();
            var tag = GetTag(selectorPart);

            if (tag.HasValue() && tag != "*")
            {
                conditions.Add("view is " + tag);
                selectorPart = selectorPart.Substring(tag.Length).Trim();
            }

            if (selectorPart.HasValue())
                while (true)
                {
                    if (selectorPart == "*") break;
                    else if (selectorPart.StartsWithAny(".", "#", ":"))
                    {
                        var until = selectorPart.Substring(1).IndexOfAny(new[] { '.', '#', ':' });

                        if (until == -1) until = selectorPart.Length;
                        else until++;

                        var part = selectorPart.Substring(1, until - 1);

                        if (selectorPart.StartsWith(".")) conditions.Add("HasClass(view, \"" + part + "\")");
                        else if (selectorPart.StartsWith(":")) conditions.Add("view.PseudoCssState.ContainsWholeWord(\"" + part + "\")");
                        else conditions.Add("view.Id == \"" + part + "\"");

                        selectorPart = selectorPart.Substring(1).Substring(part.Length).Trim();

                        if (selectorPart.IsEmpty()) break;
                    }
                    else
                    {
                        Console.WriteLine("CSS PARSE ERROR: " + selectorPart);
                        break;
                    }
                }

            if (tag != "*")
                if (isDirect && conditions.IsSingle()) return null;

            return conditions.ToString(" && ");
        }
    }
}