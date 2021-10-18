namespace Zebble.Build
{
    abstract class FileParserBase
    {
        protected string FilePath { get; private set; }

        protected FileParserBase(string filePath) => FilePath = filePath;

        protected abstract void Parse();
        public abstract void Save();
    }
}
