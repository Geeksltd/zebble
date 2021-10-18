namespace Zebble.Css.DataType
{
    public class SourceMapResult
    {
        public SourceMapResult(int cssLineNumber, string source) => (CssLineNumber, Source) = (cssLineNumber, source);

        public int CssLineNumber { get; set; }

        public string Source { get; set; }
    }
}