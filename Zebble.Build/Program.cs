namespace Zebble.Build
{
    using System;
    using System.Diagnostics;
    using Zebble.Tooling;

    class Program
    {
        static int Main(string[] args)
        {
            Builder.ShouldLog = args.GetBoolOption("log");

            switch (args.GetCommand())
            {
                case "new":
                    {
                        var templateRepo = args.GetStringOption("template-repo", @"https://github.com/Geeksltd/Zebble.Template");
                        var templateName = args.GetStringOption("template-name", "Template");
                        var projectName = args.GetStringOption("name") ?? throw new ArgumentNullException("name");

                        return new ProjectCreator(templateRepo, templateName, projectName).Execute();
                    }

                case "upgrade":
                    return new ProjectUpgrader().Execute();

                case "build":
                    return new SolutionCompiler().Execute();

                case "nav-xml":
                    {
                        var block = args.GetBoolOption("block");
                        if (block)
                            return new NavXmlGenerator().Execute();

                        Process.Start(new ProcessStartInfo("zebble-build")
                        {
                            Arguments = "nav-xml --block",
                            WindowStyle = ProcessWindowStyle.Minimized,
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                        return 0;
                    }

                case "update-plugin":
                    {
                        var increaseVersion = args.GetBoolOption("increase-version");
                        var publish = args.GetBoolOption("publish");
                        var configuration = args.GetStringOption("configuration", @"Release");
                        var source = args.GetStringOption("source", @"https://api.nuget.org/v3/index.json");
                        var apiKey = args.GetStringOption("api-key") ?? (publish ? throw new ArgumentNullException("api-key") : (string)null);
                        var commit = args.GetBoolOption("commit");
                        
                        return new PluginUpdater(increaseVersion, configuration, publish, source, apiKey, commit).Execute();
                    }

                case "convert-plugin":
                    {
                        var templateRepo = args.GetStringOption("template-repo", @"https://github.com/Geeksltd/Zebble.Template");
                        var templateName = args.GetStringOption("template-name", "Plugin");

                        return new PluginConverter(templateRepo, templateName).Execute();
                    }

                default:
                    return new Unknown().Execute();
            }
        }
    }
}