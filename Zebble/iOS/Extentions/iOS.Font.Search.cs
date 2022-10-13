namespace Zebble
{
    using System;
    using System.Linq;
    using UIKit;
    using Olive;
    using System.Collections.Concurrent;
    using System.Diagnostics;

    partial class Font
    {
        static string[] availableFonts;
        static readonly ConcurrentDictionary<string, string> FontNameCache = new();

        static string GetFontName(string name) => FontNameCache.GetOrAdd(name, DiscoverFontName);

        static string DiscoverFontName(string name)
        {
            var result = DoDiscoverFontName(name);
            if (result != name)
                Log.For<Font>().Warning("Best match found for font (" + name + ") --> " + result);
            return result;
        }

        static bool Exists(string name)
        {
            if (name.IsEmpty()) return false;
            return UIFont.FromName(name, 10) != null;
        }

        static string[] AvailableFonts => availableFonts ??= UIFont.FamilyNames.SelectMany(family => UIFont.FontNamesForFamilyName(family)).Distinct().ToArray();

        static string DoDiscoverFontName(string name)
        {
            if (Exists(name)) return name;

            var fileName = name;

            if (name.Contains("#"))
            {
                fileName = name.Split('#').Trim().First().TrimEnd(".ttf").TrimEnd(".otf");
                name = name.Split('#').Trim().Last();
            }
            else if (name.ContainsAny(new[] { ".ttf", ".otf" }))
            {
                fileName = name.Split(new[] { ".ttf", ".otf" }, StringSplitOptions.RemoveEmptyEntries).First();
            }
            else return DefaultSystemFont;

            var parts = new[] { fileName, name }.Distinct().OrderByDescending(x => x.Length).ToArray();

            var attempts = AvailableFonts
                 .OrderByDescending(v => v.Equals(fileName + "-" + name, caseSensitive: false))
                 .ThenByDescending(v => v.Equals(name + "-" + fileName, caseSensitive: false))
                 .ThenByDescending(v => v.StartsWith(fileName + "-" + name, caseSensitive: false))
                 .ThenByDescending(v => v.StartsWith(name + "-" + fileName, caseSensitive: false))
                 .ThenByDescending(v => v.Equals(parts.First(), caseSensitive: false))
                 .ThenByDescending(v => v.Equals(parts.Last(), caseSensitive: false))
                 .ThenBy(v => v.Length)
                 .ToArray();

            var result = attempts.FirstOrDefault(Exists);

            if(!result.Contains(name) && AvailableFonts.Any(x=>x.StartsWith(name)))
            {
                var similarFonts = AvailableFonts.Where(x => x.StartsWith(name)).Select(x => new[] { x.Replace("-", ""), x });
                var exFont = similarFonts.FirstOrDefault(x => x[0].Equals(fileName));
                if (exFont != null) result = exFont[1];
            }

            if (result is null)
            {
                result = DefaultSystemFont;
                Log.For<Font>().Warning("Failed to find any match for font: " + name);
            }

            return result;
        }
    }
}
