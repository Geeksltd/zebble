namespace System
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.Concurrent;
    using Olive;

    [EscapeGCop("Hard coded values are acceptable here")]
    public static partial class ZebbleExtensions
    {
        static readonly ConcurrentDictionary<string, object> StringLocks = new();

        public static object GetLockReference(this string text) => StringLocks.GetOrAdd(text, x => new object());
    }
}