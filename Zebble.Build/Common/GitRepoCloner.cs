namespace Zebble.Build
{
    using System;
    using System.IO;
    using Zebble.Tooling;
    using Olive;

    class GitRepoCloner
    {
        readonly Uri TemplateRepo;
        readonly string TemplateName;
        readonly DirectoryInfo ProjectDir;

        public GitRepoCloner(Uri templateRepo, string templateName, DirectoryInfo projectDir)
        {
            TemplateRepo = templateRepo;
            TemplateName = templateName;
            ProjectDir = projectDir;
        }

        public void CloneGitRepo()
        {
            RunGitCommand($"clone --filter=blob:none --no-checkout --depth 1 --sparse {TemplateRepo} {ProjectDir.Name}", ProjectDir.Parent.FullName);
        }

        public void SparseCheckoutGitRepo() => RunGitCommand($"sparse-checkout set {TemplateName}", ProjectDir.FullName);

        public void CheckoutGitRepo() => RunGitCommand("checkout", ProjectDir.FullName);

        public void PurgeGitAssociations()
        {
            var gitDire = ProjectDir.GetSubDirectory(".git");

            gitDire.SetAttributes(FileAttributes.Normal);
            gitDire.Delete(true);
        }

        void RunGitCommand(string command, string workingDir)
        {
            Commands.Git.Execute(command, configuration: x => x.StartInfo.WorkingDirectory = workingDir);
        }
    }
}