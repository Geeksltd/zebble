namespace System
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections.Concurrent;
    using Olive;

    [EscapeGCop("Hard coded values are acceptable here")]
    public static partial class ZebbleExtensions
    {
        static Dictionary<string, string> IOSafeHashes = new Dictionary<string, string>();
        static ConcurrentDictionary<string, object> StringLocks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Gets a SHA1 hash of this text where all characters are alpha numeric.
        /// </summary>
        public static string ToIOSafeHash(this string clearText)
        {
            if (IOSafeHashes.TryGetValue(clearText, out string result)) return result;

            result = new string(clearText.CreateSHA1Hash().ToCharArray().Where(c => c.IsLetterOrDigit()).ToArray());
            lock (IOSafeHashes) IOSafeHashes[clearText] = result;
            return result;
        }

        public static object GetLockReference(this string text) => StringLocks.GetOrAdd(text, x => new object());
    }
}