using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Zebble.Common.Extensions;

namespace Zebble.Common.Serialization
{
    public class ZebbleWriter : XmlWriter
    {
        readonly XmlWriterSettings _settings;
        readonly XmlWriter _writer;

        int _indentLevel;
        int _elementNameLength;

        public ZebbleWriter(TextWriter output, XmlWriterSettings settings)
        {
            _settings = settings;
            _writer = Create(output, settings);
        }

        public override WriteState WriteState => _writer.WriteState;

        public override void Close() => _writer.Close();

        public override void Flush() => _writer.Flush();

        public override string LookupPrefix(string ns) => _writer.LookupPrefix(ns);

        public override void WriteBase64(byte[] buffer, int index, int count) =>
            _writer.WriteBase64(buffer, index, count);

        public override void WriteCData(string text) => _writer.WriteCData(text);

        public override void WriteCharEntity(char ch) => _writer.WriteCharEntity(ch);

        public override void WriteChars(char[] buffer, int index, int count) =>
            _writer.WriteChars(buffer, index, count);

        public override void WriteComment(string text) => _writer.WriteComment(text);

        public override void WriteDocType(string name, string pubid, string sysid, string subset) =>
            _writer.WriteDocType(name, pubid, sysid, subset);

        public override void WriteEntityRef(string name) => _writer.WriteEntityRef(name);

        public override void WriteProcessingInstruction(string name, string text) =>
            _writer.WriteProcessingInstruction(name, text);

        public override void WriteRaw(char[] buffer, int index, int count) => _writer.WriteRaw(buffer, index, count);

        public override void WriteRaw(string data) => _writer.WriteRaw(data);

        public override void WriteSurrogateCharEntity(char lowChar, char highChar) =>
            _writer.WriteSurrogateCharEntity(lowChar, highChar);

        public override void WriteWhitespace(string ws) => _writer.WriteWhitespace(ws);

        public override void WriteStartDocument() => _writer.WriteStartDocument();

        public override void WriteStartDocument(bool standalone) => _writer.WriteStartDocument(standalone);

        public override void WriteEndDocument() => _writer.WriteEndDocument();

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            _indentLevel += _elementNameLength == default ? 0 : 1;
            _writer.WriteStartElement(prefix, localName, ns);
            _elementNameLength = GetLength(prefix, localName) + 1;
        }

        public override void WriteEndElement()
        {
            _writer.WriteEndElement();
            _indentLevel--;
        }

        public override void WriteFullEndElement() => _writer.WriteFullEndElement();

        public void WriteAttributeString(string localName, string ns, string value, bool chop)
        {
            if (chop)
            {
                WriteNewLine();
                WriteChars(" ", _elementNameLength + (ns.Contains("xmlns") ? 0 : 1));
            }

            _writer.WriteAttributeString(localName, ns, value);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns) => _writer.WriteStartAttribute(prefix, localName, ns);

        public override void WriteString(string text) => _writer.WriteString(text);

        public override void WriteEndAttribute() => _writer.WriteEndAttribute();

        void WriteNewLine()
        {
            RawText(_settings.NewLineChars);
            WriteChars(_settings.IndentChars, _indentLevel);
        }

        void WriteChars(string chars, int count) => RawText(Enumerable.Repeat(chars, count).Join());

        static int GetLength(string prefix, string localName)
        {
            return (prefix?.Length > 1 ? prefix.Length + 1 : 0) + localName.Length;
        }

        void RawText(string s) => InvokeMethod(RawWriter, "RawText", s);

        XmlWriter RawWriter => GetField<XmlWriter>(_writer, "_rawWriter");

        static TValue GetField<TValue>(object source, string name)
        {
            return (TValue)source.GetType()
                                 .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                                 .GetValue(source);
        }

        static void InvokeMethod(object source, string name, params object[] parameters)
        {
            source.GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(x => x.Name == name && x.GetParameters().Length == parameters.Length)
                .Invoke(source, parameters);
        }
    }
}