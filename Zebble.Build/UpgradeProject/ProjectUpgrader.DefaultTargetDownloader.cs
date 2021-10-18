namespace Zebble.Build
{
    using System;
    using System.Net.Http;
    using Zebble.Tooling;
    using Olive;

    partial class ProjectUpgrader
    {
        const string TARGET_FILE_URI = @"https://raw.githubusercontent.com/Geeksltd/Zebble.Template/main/Template/Zebble.targets";

        void DownloadAndSaveDefaultTargetFile()
        {
            var zebbleTargets = DirectoryContext.RootFolder.GetFile("Zebble.targets");

            if (zebbleTargets.Exists())
            {
                Console.WriteLine("Zebble.targets file already exists.");
                return;
            }

            using var client = new HttpClient();

            var fileContent = client.GetStringAsync(TARGET_FILE_URI).GetAwaiter().GetResult();

            zebbleTargets.WriteAllText(fileContent);

            Console.WriteLine("Zebble.targets created.");
        }
    }
}