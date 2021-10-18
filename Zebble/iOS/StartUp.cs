using System;
using System.IO;

namespace Zebble
{
    partial class StartUp
    {
        static string GetIOTempRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "..", "Library", "Caches");
        }

        string GetIORoot() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }
}