using System;
using System.IO;
using System.Linq;
using ZblFormat.Constants;
using ZblFormat.Extensions;

namespace ZblFormat.Helpers
{
    static class PathHelpers
    {
        public static string GetRelativeSchemaPath(string itemPath)
        {
            var isInViewsFolder = itemPath.IndexOf($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}", StringComparison.InvariantCultureIgnoreCase) >= 0;

            var numberOfAboveParents = GetParentDirectoriesCount(itemPath, isInViewsFolder ? "Views" : ZebbleSolutionItemNames.AppUi);

            if (numberOfAboveParents == 0)
            {
                if (isInViewsFolder)
                    return ZebbleSolutionItemNames.ZebbleSchemaXml;

                return $"Views/{ZebbleSolutionItemNames.ZebbleSchemaXml}";
            }

            var pathForReplace = Enumerable.Repeat("../", numberOfAboveParents).Join("");

            if (!isInViewsFolder)
                pathForReplace = $"./{pathForReplace}Views/";

            return $"{pathForReplace}{ZebbleSolutionItemNames.ZebbleSchemaXml}";
        }

        static int GetParentDirectoriesCount(string path, string parentName)
        {
            if (path is null) return 0;

            if (parentName is null) return 0;

            var parentDirectory = new DirectoryInfo(path)?.Parent;

            if (parentDirectory is null) return 0;

            var numberOfAboveParents = 0;

            while (parentDirectory != null && !parentDirectory.Name.Equals(parentName, StringComparison.InvariantCultureIgnoreCase))
            {
                numberOfAboveParents++;
                parentDirectory = parentDirectory.Parent;
            }

            return numberOfAboveParents;
        }
    }
}