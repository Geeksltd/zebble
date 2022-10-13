using Olive;
using SourcemapToolkit.SourcemapParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zebble.Css.DataType;

namespace Zebble.Css
{
    static class SourceMapDecoder
    {
        static internal List<SourceMapResult> Decode(string sourceMapString)
        {
            var tempFile = Path.GetTempFileName();

            File.WriteAllText(tempFile, sourceMapString);
            var sourceMapResult = new List<SourceMapResult>();

            try
            {
                var parser = new SourceMapParser();
                SourceMap sourceMap;

                using (FileStream stream = new(tempFile, FileMode.Open))
                    sourceMap = parser.ParseSourceMap(new StreamReader(stream));

                if (sourceMap != null)
                {
                    var grouped = sourceMap.ParsedMappings.GroupBy(x => x.GeneratedSourcePosition.ZeroBasedLineNumber)
                        .Select(x => new
                        {
                            Line = x.Key,
                            Detail = x.First()
                        });

                    var t = grouped.Where(x => x.Detail.OriginalFileName.Contains("undete"));

                    foreach (var item in grouped)
                    {
                        sourceMapResult.Add(
                            new SourceMapResult(
                                item.Line,
                                $"{item.Detail.OriginalFileName}:{item.Detail.OriginalSourcePosition.ZeroBasedLineNumber}")
                            );
                    }
                }
            }
            catch (Exception ex)
            {
                // ToDo Log Errors
            }

            return sourceMapResult;
        }
    }
}