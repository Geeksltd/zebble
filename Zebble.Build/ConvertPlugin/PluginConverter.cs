namespace Zebble.Build
{
    using System;
    using System.IO;
    using Zebble.Tooling;
    using System.Xml.Linq;
    using System.Linq;
    using System.Collections.Generic;
    using Olive;

    class PluginConverter : Builder
    {
        readonly DirectoryInfo Root;
        readonly Uri TemplateRepo;
        readonly string TemplateName;
        readonly DirectoryInfo ProjectDir;
        string ProjectName;
        string Version;
        string Description;

        public PluginConverter(string templateRepo, string templateName)
        {
            Root = DirectoryContext.RootFolder;

            TemplateRepo = templateRepo.AsUri();
            TemplateName = templateName;

            ProjectDir = Root.CreateSubdirectory(Guid.NewGuid().ToString());

            Log("This plugin will be converted to the new TFM style");
            Log($"Download started from {TemplateRepo} ({TemplateName})");
        }

        protected override void AddTasks()
        {
            Add(() => PrepareGitRepo());
            Add(() => MoveUpTemplateDirContents());
            Add(() => FindNuspecFileAndFillProps());
            Add(() => CopyPluginFiles());
            Add(() => FixFileNamesAndContents());
            Add(() => RemoveOldFilesAndFolders());
            Add(() => MoveUpProjectDirContents());
            Add(() => RemovePlaceholderFiles());
        }

        void PrepareGitRepo()
        {
            var cloner = new GitRepoCloner(TemplateRepo, TemplateName, ProjectDir);

            cloner.CloneGitRepo();
            cloner.SparseCheckoutGitRepo();
            cloner.CheckoutGitRepo();
            cloner.PurgeGitAssociations();
        }

        void MoveUpTemplateDirContents()
        {
            ProjectDir.GetSubDirectory(TemplateName).MoveUpContents();
        }

        void FindNuspecFileAndFillProps()
        {
            var nuspecFile = Root.GetFiles("*.nuspec", SearchOption.AllDirectories)[0];
            if (nuspecFile == null)
                throw new IOException("Couldn't find any .nuspec file.");

            var metadata = XDocument.Parse(nuspecFile.ReadAllText()).GetElement("package/metadata");
            if (metadata == null)
                throw new FormatException("Couldn't find metadata tag");

            ProjectName = metadata.GetElement("id")?.Value.Remove("Zebble.").ToPascalCaseId();
            Version = metadata.GetElement("version")?.Value;
            Description = metadata.GetElement("description")?.Value;
        }

        void FixFileNamesAndContents()
        {
            var solutionFixer = new SolutionFixer(
                ProjectDir, ProjectName,
                new Dictionary<string, string> {
                    { "MyProjectVersion", Version },
                    { "MyProjectDescription",Description},
                    { "Shared/NuGet/Icon.png", "icon.png"}
                }
            );

            solutionFixer.UpdateFileNames();
            solutionFixer.ReplaceFileContents();
        }

        void CopyPluginFiles()
        {
            Root.GetFiles("README.md", SearchOption.AllDirectories)
                .ElementAtOrDefault(0)?
                .MoveTo(Path.Combine(ProjectDir.FullName, "README.md"), true);

            Root.GetSubDirectory("Shared").GetFiles("icon.png", SearchOption.AllDirectories)
                .ElementAtOrDefault(0)?
                .MoveTo(Path.Combine(ProjectDir.FullName, "icon.png"), true);

            void MoveTo(string name)
            {
                Root.GetSubDirectory(name).GetFiles("*.cs", SearchOption.AllDirectories)
                    .Do(f => f.MoveTo(Path.Combine(ProjectDir.FullName, name, f.Name))
                );
            }

            MoveTo("Shared");
            MoveTo("iOS");
            MoveTo("Android");
            MoveTo("WinUI");
        }

        void RemoveOldFilesAndFolders()
        {
            Root.GetDirectories().Except(d => d.Name.IsAnyOf(ProjectDir.Name, ".git"))
                                 .Do(d => d.Delete(recursive: true));

            Root.GetFiles("*.*").Do(f => f.Delete());
        }

        void MoveUpProjectDirContents()
        {
            ProjectDir.MoveUpContents();
        }

        void RemovePlaceholderFiles()
        {
            Root.GetFiles("Placeholder.temp", SearchOption.AllDirectories).Do(f => f.Delete());
        }
    }
}