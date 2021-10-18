namespace Zebble.Tooling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Olive;

    class CSharpFormatter
    {
        readonly List<string> _lines;
        int _indentLevel;

        /// <summary>
        /// Creates a new CSharpFormatter instance.
        /// </summary>
        public CSharpFormatter(string source)
        {
            _lines = source.Or("").Trim().Split('\n').Select(l => l.Trim()).ToList();
        }

        /// <summary>
        /// Formats the specified CSharp code.
        /// </summary>
        internal string Format()
        {
            InsertSpaceLines();
            RemoveRedundantEmptyLines();

            var indentNextLine = false;

            for (var i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];

                if (line.IsAnyOf("}", "};", "});") || line.StartsWith("}END_H____"))
                {
                    if (_indentLevel > 0) _indentLevel--;// Indentation shifts back:
                }

                if (indentNextLine)
                {
                    _indentLevel++;
                    FormatLine(i);
                    _indentLevel--;
                }
                else
                {
                    FormatLine(i);
                }

                #region Indent Block Forward

                if (line.StartsWith("{") && !(line.EndsWith("}") || line.EndsWith("},") || line.EndsWith("};")))
                {
                    // Its an open "{" line:
                    _indentLevel++;
                }

                #endregion

                indentNextLine = NeedsSingleLineIndent(line, i);
            }

            return _lines.Select(l => l.Trim().IsEmpty() ? string.Empty : l.TrimEnd(' ')).ToLinesString();
        }

        /// <summary>
        /// Determines if this line pushes the line after it to indent one step.
        /// </summary>
        bool NeedsSingleLineIndent(string originalLine, int lineIndex)
        {
            var line = originalLine;
            var nextLine = lineIndex < _lines.Count - 1 ? _lines[lineIndex + 1] : "";

            if (LineStartsWith(line, "//")) return false;

            if (line.EndsWith(")"))
            {
                if (LineStartsWith(line, "for") || LineStartsWith(line, "if") || LineStartsWith(line, "while"))
                    if (nextLine != "{") return true;
            }

            if (line.EndsWith("+") || line.EndsWith("-") || line.EndsWith("(") || line.EndsWith("["))
                return true;

            if (LineStartsWith(line, "case ") && line.EndsWith(":"))
                return true;

            if (LineStartsWith(line, "default:") && line.EndsWith("default:"))
                return true;

            return false;
        }

        bool LineStartsWith(string line, string phrase)
        {
            if (line.StartsWith(phrase)) return true;
            if (line.Contains("___END_SH_" + phrase)) return true;
            return false;
        }

        /// <summary>
        /// Formats the line at the specified index.
        /// </summary>
        void FormatLine(int lineIndex)
        {
            var line = new StringBuilder(_lines[lineIndex]);

            for (int i = 0; i < _indentLevel; i++)
                line.Insert(0, "    ");

            _lines[lineIndex] = line.ToString();
        }

        /// <summary>
        /// Inserts necessary space lines.
        /// </summary>
        void InsertSpaceLines()
        {
            var linesToAdd = new List<int>();

            for (var i = 0; i < _lines.Count - 1; i++)
            {
                var previousLine = i > 0 ? _lines[i - 1] : null;
                var line = _lines[i];
                var nextLine = _lines[i + 1];

                if (previousLine.IsEmpty() || previousLine == "{") continue;

                if (line == "try" || line == "scope.Complete();" || line.StartsWithAny("try {", "foreach (", "if (", "using ("))
                {
                    linesToAdd.Add(i);
                }

                if (previousLine == "}")
                {
                    if (line.StartsWithAny("return ", "return;"))
                    {
                        linesToAdd.Add(i);
                    }
                }
            }

            foreach (var l in linesToAdd.Reverse<int>())
                _lines.Insert(l, string.Empty);
        }

        /// <summary>
        /// Removes the redundant empty lines.
        /// </summary>
        void RemoveRedundantEmptyLines()
        {
            var linesToRemove = new List<int>();

            for (var i = 0; i < _lines.Count - 1; i++)
            {
                var previousLine = i > 0 ? _lines[i - 1] : null;
                var line = _lines[i];
                var nextLine = _lines[i + 1];

                if (line.IsEmpty())
                {
                    if (nextLine.IsAnyOf("else", "}", "{") || nextLine.StartsWith("else if (") || nextLine.IsEmpty())
                    {
                        linesToRemove.Add(i);
                    }
                    else if (previousLine?.EndsWithAny("{", "[", ",") == true)
                    {
                        linesToRemove.Add(i);
                    }
                }
            }

            foreach (var l in linesToRemove.Reverse<int>())
                _lines.RemoveAt(l);

            if (linesToRemove.Any())
            {
                // try again:
                RemoveRedundantEmptyLines();
            }
        }
    }
}