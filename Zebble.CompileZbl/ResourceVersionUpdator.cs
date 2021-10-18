namespace Zebble.CompileZbl
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Olive;
    using Zebble.Tooling;

    class ResourceVersionUpdater
    {
        public ResourceVersionUpdater()
        {
            if (!DirectoryContext.AppUIResourcesFolder.Exists())
            { ConsoleHelpers.Error("Resources directory not found:: " + DirectoryContext.AppUIResourcesFolder.FullName); return; }
        }

        internal string FindNewHash()
        {
            var items = DirectoryContext.AppUIResourcesFolder.GetFiles("*.*", SearchOption.AllDirectories)
                .OrderBy(x => x.FullName)
                           .Select(x => Hash(Convert.ToBase64String(File.ReadAllBytes(x.FullName)) + "-" + x.FullName))
                           .ToList();

            return Hash(items.ToString("|"));
        }

        static string Hash(string text)
        {
            var result = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(text))).TrimEnd('=');

            return string.Join("", result.Where(char.IsLetterOrDigit));
        }
    }
}