namespace Zebble.Build
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Zebble.Tooling;
    using Olive;

    partial class ProjectUpgrader
    {
        void FindAndRemoveZebbleExe()
        {
            EnsureAndroidFolderExists();
            FindAndRemoveFile(DirectoryContext.AndroidFolder.GetFile("Zebble.exe"));
        }

        void FindAndRemoveReadMeNowTxt()
        {
            EnsureAndroidFolderExists();
            FindAndRemoveFile(DirectoryContext.AndroidFolder.GetFile("-READ-ME-NOW!!!!!!!!!!!!.txt"));
        }

        void FindAndRemoveRunMeNowBat()
        {
            EnsureAndroidFolderExists();
            FindAndRemoveFile(DirectoryContext.AndroidFolder.GetFile("-RUN-ME-NOW!!!!!!!!!!!!!.bat"));
        }

        void FindAndRemoveFile(FileInfo file)
        {
            Console.WriteLine($"Finding {file.Name}...");

            if (!file.Exists())
            {
                Console.WriteLine($"We're unable to find {file.Name}.");
                return;
            }

            file.Attributes = FileAttributes.Normal;
            file.Delete();

            Console.WriteLine($"{file.Name} Deleted");

            var projectFile = file.Directory.GetFiles("*.csproj")[0];
            var document = XDocument.Load(projectFile.FullName);

            var project = document.GetElement("Project");

            var fileRefs = project.Descendants()
                .Where(x => x.GetValue<string>("@Include") == file.Name)
                .ToList();

            if (fileRefs.Any())
            {
                fileRefs.Remove();
                projectFile.WriteAllText(document.ToString());
            }

            Console.WriteLine($"{projectFile.Name} updated.");
        }

        static void EnsureAndroidFolderExists()
        {
            if (DirectoryContext.AndroidFolder == null)
                throw new Exception("We're unable to find Android folder. You need to run this tool in the root of your Zebble project.");
        }
    }
}