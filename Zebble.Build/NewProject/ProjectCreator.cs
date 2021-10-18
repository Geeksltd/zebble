namespace Zebble.Build
{
    using System;
    using System.IO;
    using Zebble.Tooling;
    using Olive;

    class ProjectCreator : Builder
    {
        readonly DirectoryInfo Root;
        readonly Uri TemplateRepo;
        readonly string TemplateName;
        readonly string ProjectName;
        readonly DirectoryInfo ProjectDir;

        public ProjectCreator(string templateRepo, string templateName, string projectName)
        {
            Root = DirectoryContext.RootFolder;

            TemplateRepo = templateRepo.AsUri();
            TemplateName = templateName;
            ProjectName = projectName;

            if (Root.GetSubDirectory(projectName).Exists())
                throw new Exception($"A directory with name {projectName} already exists. Please try again with another name.");

            ProjectDir = Root.CreateSubdirectory(projectName);

            Log($"A new project will be created in {ProjectDir.FullName}");
            Log($"Download started from {TemplateRepo} ({TemplateName})");
        }

        protected override void AddTasks()
        {
            Add(() => PrepareGitRepo());
            Add(() => MoveUpTemplateDirContents());
            Add(() => FixFileNamesAndContents());
        }

        void PrepareGitRepo()
        {
            var cloner = new GitRepoCloner(TemplateRepo, TemplateName, ProjectDir);

            cloner.CloneGitRepo();
            cloner.SparseCheckoutGitRepo();
            cloner.CheckoutGitRepo();
            cloner.PurgeGitAssociations();
        }

        void FixFileNamesAndContents()
        {
            var solutionFixer = new SolutionFixer(ProjectDir, ProjectName);

            solutionFixer.UpdateFileNames();
            solutionFixer.ReplaceFileContents();
            solutionFixer.UpdateSolution();
            solutionFixer.AddGitIgnore();
        }

        void MoveUpTemplateDirContents()
        {
            ProjectDir.GetSubDirectory(TemplateName).MoveUpContents();
        }
    }
}