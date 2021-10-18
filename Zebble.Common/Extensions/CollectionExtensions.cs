using System.Collections.Generic;

namespace Zebble.Common.Extensions
{
    static class CollectionExtensions
    {
        public static string Join(this IEnumerable<string> source, string separator = "")
        {
            return string.Join(separator, source);
        }
    }
}