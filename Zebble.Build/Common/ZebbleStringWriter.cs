namespace Zebble.Build
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    sealed class ZebbleStringWriter : StringWriter
    {
        static readonly IDictionary<string, string> _allowedEntities = new Dictionary<string, string> {
            { " encoding=\"utf-8\"", "" },
            { "=&gt;", "=>" }
        };

        readonly Encoding _encoding;

        public ZebbleStringWriter(Encoding encoding = null) => _encoding = encoding;

        public override Encoding Encoding => _encoding ?? base.Encoding;

        public override string ToString() => _allowedEntities.Aggregate(base.ToString(), (acc, x) => acc.Replace(x.Key, x.Value));
    }
}