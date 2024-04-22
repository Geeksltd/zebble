namespace Zebble.Css
{
    using DartSassHost;
    using JavaScriptEngineSwitcher.ChakraCore;
    using Olive;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Zebble.Css.DataType;
    using Zebble.Tooling;

    class CssManager
    {
        readonly List<CssRuleGenerator> Rules = new();
        public bool HasErrors;
        List<SourceMapResult> SourceMapResults;
        int CurrentLine;

        public static FileInfo[] FindScssFiles(bool watch)
        {
            var resources = DirectoryContext.AppUIResourcesFolder.FullName.ToUpper() + Path.DirectorySeparatorChar;

            return DirectoryContext.AppUIFolder.GetFiles($"{(watch ? "*" : "Common")}.scss", SearchOption.AllDirectories)
                      .Except(f => f.FullName.ToUpper().StartsWith(resources))
                      .ToArray();
        }

        internal void Load(FileInfo file)
        {
            var scss = file.ReadAllText(Encoding.UTF8);
            var (text, map) = CompileToCss(file.DirectoryName, scss);
            text = ToLines(text);
            text = RemoveComments(text.Trim());
            SourceMapResults = SourceMapDecoder.Decode(map);

            try
            {
                CurrentLine = 0;
                DetectRules(text, GetFileKey(file));
            }
            catch (Exception ex)
            {
                Thread.Sleep(12000);
                HasErrors = true;
                ConsoleHelpers.Error("Failed to parse " + file.FullName + "\r\n" + ex.Message);
                return;
            }
        }

        (string, string) CompileToCss(string path, string text)
        {
            var options = new CompilationOptions { SourceMap = true, IncludePaths = new[] { path } };
            var compiler = new SassCompiler(new ChakraCoreJsEngineFactory());
            var result = compiler.Compile(
                text, "input.scss", "output.css", "output.css.map", options
            );
            return (result.CompiledContent, result.SourceMap);
        }

        string GetFileKey(FileInfo file) => file.FullName.TrimStart(DirectoryContext.AppUIFolder.Parent.FullName).TrimStart(Path.DirectorySeparatorChar);

        string RemoveComments(string text)
        {
            var from = text.IndexOf("/*");
            var until = text.IndexOf("*/");

            if (from > -1 && until > from)
            {
                var comment = text.Substring(from, 2 + until - from).Split("\r\n");
                text = text.Remove(from, 2 + until - from).Trim();

                for (int i = 0; i < comment.Length - 1; i++)
                {
                    if (from < text.Length)
                        text = text.Insert(from, Environment.NewLine);
                }

                return RemoveComments(text);
            }

            return text;
        }

        void DetectRules(string remaining, string file)
        {
            if (remaining.IsEmpty()) return;

            if (remaining.StartsWith("\r\n"))
            {
                CurrentLine += 1;
                remaining = remaining.Remove(0, 2);
                DetectRules(remaining, file);
                return;
            }

            var selector = remaining.RemoveFrom("{").Trim().KeepReplacing("  ", " ");
            if (selector.IsEmpty()) throw new FormatException("Failed to find css rules from: " + remaining);

            remaining = remaining.TrimStart(selector).Trim();

            var until = remaining.IndexOf("}");
            if (until == -1) throw new FormatException("Failed to find css rules from: " + remaining);

            var body = remaining.Substring("{", "}", inclusive: false).Trim();

            if (body.HasValue())
            {
                foreach (var item in selector.Split(',').Trim())
                {
                    Rules.Add(new CssRuleGenerator(file, body, item, SourceMapResults.FirstOrDefault(x => x.CssLineNumber == CurrentLine)?.Source));
                }
            }

            remaining = remaining.Substring(until + 1);

            if (remaining.StartsWith("\r\n"))
                remaining = remaining.Remove(0, 2);

            CurrentLine += 1;
            DetectRules(remaining, file);
        }

        internal CssRuleGenerator[] ExtractRules()
        {
            FindScssFiles(watch: false).Do(Load);

            foreach (var group in Rules.GroupBy(x => x.ClassFullName() + "." + x.ClassBaseName))
            {
                var index = 0;

                foreach (var item in group)
                {
                    item.RepeatIndex = index;
                    index++;
                }
            }

            return Rules.ToArray();
        }

        string ToLines(string text)
        {
            return text.Replace("\n", "\r\n").Replace("\r\n\n", "\r\n").Split("\r\n").Except(x => x.StartsWith("@charset ")).ToLinesString();
        }
    }
}