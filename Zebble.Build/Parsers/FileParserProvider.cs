namespace Zebble.Build
{
    static class FileParserProvider
    {
        public static ZblFileParser CreateZebble(string filePath) => new(filePath);
        public static CssFileParser CreateStyle(string filePath) => new(filePath);

        public static CsFileParser CreateCSharp(string filePath) => new(filePath);
    }
}
