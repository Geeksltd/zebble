namespace Zebble.Build
{
    using Olive;
    using System.IO;
    using Zebble.Tooling;

    class SolutionCompiler : Builder
    {
        readonly DirectoryInfo Root;

        public SolutionCompiler()
        {
            Root = DirectoryContext.RootFolder;

            Log($"Build started for: {Root.FullName}");
        }

        protected override void AddTasks()
        {
            Add(() => RestoreNuget());
            Add(() => BuildSolution());
        }

        void RestoreNuget()
        {
            Commands.DotNet.Execute("restore", configuration: x => x.StartInfo.WorkingDirectory = Root.FullName);
        }

        void BuildSolution()
        {
            Commands.DotNet.Execute("build", configuration: x => x.StartInfo.WorkingDirectory = Root.FullName);
        }
    }
}