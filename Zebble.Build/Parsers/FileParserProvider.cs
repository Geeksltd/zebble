namespace Zebble.Build
{
    static class FileParserProvider
    {
        public static ZblFileParser CreateZebble(string filePath) => new ZblFileParser(filePath);
        public static CssFileParser CreateStyle(string filePath) => new CssFileParser(filePath);

        public static CsFileParser CreateCSharp(string filePath) => new CsFileParser(filePath);
    }
}
