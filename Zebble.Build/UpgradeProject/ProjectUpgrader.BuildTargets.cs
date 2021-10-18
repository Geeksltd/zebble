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
        void FindAndUpdateBuildTargets()
        {
            if (DirectoryContext.RunFolder == null)
            {
                Log("We're unable to find Run folder. You need to run this tool in the root of your Zebble project.");
                return;
            }

            Log("Replacing file contents...");

            var projectFiles = GetCsProjFiles();

            foreach (var projectFile in projectFiles)
            {
                var document = XDocument.Load(projectFile.FullName);
                var changed = false;

                var isVm = projectFile.FullName.EndsWith("VM.csproj");

                var project = document.GetElement("Project");
                if (project == null)
                    continue;

                var elements = project.Elements().ToList();
                var legacyTargets = elements.Where(x => x.Name.LocalName == "Target")
                    .Where(IsLegacyZebbleTarget)
                    .ToList();

                if (legacyTargets.Any())
                {
                    legacyTargets.Remove();
                    changed = true;
                }

                if (isVm)
                {
                    var existingTargets = elements.Where(c => c.Name.LocalName == "Import")
                        .Where(IsNewZebbleTargetsImport)
                        .ToList();

                    if (existingTargets.Any())
                    {
                        existingTargets.Remove();
                        changed = true;
                    }

                    if (elements.Where(x => x.Name.LocalName == "Target").None(IsUpdateNavXmlFileTarget))
                    {
                        var target = new XElement("Target");
                        target.SetAttributeValue("Name", "UpdateNavXmlFile");
                        target.SetAttributeValue("AfterTargets", "AfterBuild");

                        var exec = new XElement("Exec");
                        exec.SetAttributeValue("WorkingDirectory", "$(SolutionDir)");
                        exec.SetAttributeValue("Command", "start zebble-build nav-xml");

                        exec.AddTo(target);

                        target.AddTo(project);

                        changed = true;
                    }
                }
                else if (elements.Where(x => x.Name.LocalName == "Import").None(IsNewZebbleTargetsImport))
                {
                    var import = new XElement("Import");
                    import.SetAttributeValue("Project", "$(SolutionDir)\\Zebble.targets");

                    import.AddTo(project);

                    changed = true;
                }

                if (changed)
                    projectFile.WriteAllText(document.ToString());
            }

            Console.WriteLine("Done.");
        }

        static FileInfo[] GetCsProjFiles()
        {
            return DirectoryContext.RunFolder.GetFiles(includeSubDirectories: true)
                 .Select(OliveExtensions.AsFile)
                 .Where(f => f.Extension.ToLowerOrEmpty().TrimStart(".").IsAnyOf("csproj")).ToArray();
        }

        static bool IsLegacyZebbleTarget(XElement target)
        {
            var exec = target.GetElement("Exec");

            if (exec == null)
                return false;

            var command = exec.GetValue<string>("@Command");

            return command?.ContainsAny(new[] { "\\Zebble", "Zebble.exe" }, caseSensitive: false) ?? false;
        }

        static bool IsNewZebbleTargetsImport(XElement import)
        {
            var project = import.GetValue<string>("@Project");

            return project?.EndsWith("Zebble.targets", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        static bool IsUpdateNavXmlFileTarget(XElement target)
        {
            var name = target.GetValue<string>("@Name");

            return name?.Equals("UpdateNavXmlFile", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}