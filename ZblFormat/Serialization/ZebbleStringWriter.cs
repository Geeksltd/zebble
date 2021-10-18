using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ZblFormat.Serialization
{
    public sealed class ZebbleStringWriter : StringWriter
    {
        static readonly IDictionary<string, string> _allowedEntities = new Dictionary<string, string> {
            { " encoding=\"utf-8\"", "" },
            { "=&gt;", "=>" }
        };

        readonly Encoding _encoding;

        public ZebbleStringWriter(Encoding encoding = null) => _encoding = encoding;

        public override Encoding Encoding => _encoding ?? base.Encoding;

        public override string ToString() => Enumerable.Aggregate(_allowedEntities, base.ToString(), (acc, x) => acc.Replace(x.Key, x.Value));
    }
}