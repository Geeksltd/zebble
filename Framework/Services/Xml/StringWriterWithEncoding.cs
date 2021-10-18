namespace System.IO
{
    using System.Text;

    public class StringWriterWithEncoding : StringWriter
    {
        public StringWriterWithEncoding(Encoding encoding) => Encoding = encoding;

        public override Encoding Encoding { get; }
    }
}