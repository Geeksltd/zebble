namespace Zebble.Build
{
    using System.IO;
    using Zebble.Tooling;

    partial class ProjectUpgrader : Builder
    {
        readonly SolutionFixer SolutionFixer;

        public ProjectUpgrader()
        {
            var projectName = Path.GetFileNameWithoutExtension(DirectoryContext.RootFolder.GetFiles("*.sln")[0].Name);
            SolutionFixer = new SolutionFixer(DirectoryContext.RootFolder, projectName);
        }

        protected override void AddTasks()
        {
            Add(() => UpdateFileNames());
            Add(() => ReplaceFileContents());
            Add(() => UpdateSolution());
            Add(() => AddGitIgnore());
            Add(() => DownloadAndSaveDefaultTargetFile());
            Add(() => FindAndUpdateBuildTargets());
            Add(() => FindAndRemoveZebbleExe());
            Add(() => FindAndRemoveReadMeNowTxt());
            Add(() => FindAndRemoveRunMeNowBat());
        }

        void UpdateFileNames() => SolutionFixer.UpdateFileNames();
        void ReplaceFileContents() => SolutionFixer.ReplaceFileContents();
        void UpdateSolution() => SolutionFixer.UpdateSolution();
        void AddGitIgnore() => SolutionFixer.AddGitIgnore();
    }
}