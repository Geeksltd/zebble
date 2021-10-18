using System.IO;
using System.Linq;
using ZblFormat;
using Zebble.Tooling;

namespace Zebble.FormatZbl
{
    class FormatZblFiles : BaseFormatter
    {
        protected override string GetFileName() => "*.zbl";

        protected override void FormatFiles()
        {
            var result = DirectoryContext.AppUIFolder.GetFiles("*.zbl", SearchOption.AllDirectories).ToList();
            result.ForEach(x => XmlHelpers.CleanupZebbleFile(x.FullName, true));
        }
    }
}