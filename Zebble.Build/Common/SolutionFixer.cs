namespace Zebble.Build
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Olive;

    class SolutionFixer
    {
        readonly DirectoryInfo ProjectDir;
        readonly string ProjectName;
        readonly IDictionary<string, string> AdditionalReplacements;

        public SolutionFixer(DirectoryInfo projectDir, string projectName, IDictionary<string, string> additionalReplacements = null)
        {
            ProjectDir = projectDir;
            ProjectName = projectName;
            AdditionalReplacements = additionalReplacements;
        }

        public void UpdateFileNames()
        {
            Console.WriteLine("Renaming files...");

            var replacements = GetReplacements(ProjectName);

            var files = ProjectDir.GetFiles(includeSubDirectories: true)
                 .Select(f => f.AsFile());

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.Name);
                var changed = false;

                foreach (var item in replacements.Where(r => fileName.Contains(r.Key)))
                {
                    changed = true;
                    fileName = fileName.Replace(item.Key, item.Value);
                }

                if (changed)
                    File.Move(file.FullName, Path.Combine(file.DirectoryName, fileName));
            }
        }

        public void ReplaceFileContents()
        {
            Console.WriteLine("Replacing file contents...");

            var replacements = GetReplacements(ProjectName);

            var files = ProjectDir.GetFiles(includeSubDirectories: true)
                 .Select(f => f.AsFile())
                 .Where(f => f.Extension.ToLowerOrEmpty().TrimStart(".").IsAnyOf("sln", "cs", "xml", "msharp", "appxmanifest", "csproj", "plist", "md")).ToArray();

            foreach (var file in files)
            {
                var body = file.ReadAllText();
                var changed = false;

                foreach (var item in replacements.Where(r => body.Contains(r.Key)))
                {
                    changed = true;
                    body = body.Replace(item.Key, item.Value);
                }

                if (changed)
                    file.WriteAllText(body);
            }

            Console.WriteLine("Done.");
        }

        public void UpdateSolution()
        {
            var solutionFile = ProjectDir.GetFiles("*.sln")[0];

            FixSolutionSettings(solutionFile);
            RenameSpecificPlatformsProjects(solutionFile);
        }

        public void AddGitIgnore()
        {
            Console.Write("Adding .gitignore...");

            var file = ProjectDir.GetFile(".gitignore");
            if (file.Exists())
            {
                Console.WriteLine("Skipped, as there already is a .gitignore file.");
                return;
            }

            var content = Assembly.GetExecutingAssembly().ReadEmbeddedTextFile("Zebble.Build", "Resources/gitignore.txt");

            file.WriteAllText(content);

            Console.WriteLine("Done.");
        }

        void RenameSpecificPlatformsProjects(FileInfo solutionFile)
        {
            var runFolder = ProjectDir.GetSubDirectory("Run");
            if (runFolder != null)
                RenameProjectNames(runFolder);

            var solutionText = solutionFile.ReadAllText();
            var runSolutionFolder = solutionText.Substring(
                from: "Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Run\", \"Run\"",
                to: "EndProject", inclusive: true
            );

            if (runSolutionFolder.HasValue())
                solutionText = solutionText.Remove(runSolutionFolder);

            foreach (var token in new[] { "", $"{solutionFile.NameWithoutExtension()}\\" })
            {
                solutionText = solutionText.Replace(
                    oldValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"UWP\", \"{token}Run\\UWP\\UWP.csproj\"",
                    newValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"~UWP\", \"{token}Run\\UWP\\~UWP.csproj\""
                );

                solutionText = solutionText.Replace(
                    oldValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"Android\", \"{token}Run\\Android\\Android.csproj\"",
                    newValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"~Android\", \"{token}Run\\Android\\~Android.csproj\""
                );

                solutionText = solutionText.Replace(
                    oldValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"iOS\", \"{token}Run\\iOS\\iOS.csproj\"",
                    newValue: $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"~iOS\", \"{token}Run\\iOS\\~iOS.csproj\""
                );

                solutionText = solutionText.Replace(
                    oldValue: $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"VM\", \"{token}Run\\VM\\VM.csproj\"",
                    newValue: $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"~VM\", \"{token}Run\\VM\\~VM.csproj\""
                );
            }

            var runSolutionFolderSection = solutionText.Substring(
                from: "GlobalSection(NestedProjects) = preSolution",
                to: "EndGlobalSection", inclusive: true
            );

            if (runSolutionFolderSection.Contains("{51AEAFB7-E02B-4F51-8AB0-AA90E034E684} = "))
                solutionText = solutionText.Remove(runSolutionFolderSection);

            solutionFile.WriteAllText(solutionText);
        }

        static void RenameProjectNames(DirectoryInfo root)
        {
            if (!root.Exists())
                return;

            var folders = root.GetDirectories();

            foreach (var folder in folders)
            {
                var projFile = folder.GetFile(folder.Name + ".csproj");

                if (projFile.Exists())
                {
                    projFile.CopyTo($"{folder.FullName}\\~{folder.Name}.csproj");
                    projFile.Delete();
                }
            }
        }

        static void FixSolutionSettings(FileInfo solution)
        {
            Console.Write("Fixing solution: " + solution.FullName + "...");

            var text = solution.ReadAllText();

            var buildSettings = new List<string>();

            var uwpGUID = "{51AEAFB7-E02B-4F51-8AB0-AA90E034E684}";
            foreach (var platform in new[] { "Any CPU", "iPhone", "iPhoneSimulator" })
            {
                buildSettings.Add($"{uwpGUID}.Debug|{platform}.Build.0 = Debug|x86");
                buildSettings.Add($"{uwpGUID}.Debug|{platform}.Deploy.0 = Debug|x86");
            }

            var iosGUID = "{3B21A769-F17C-4219-A595-27A34955228F}";
            foreach (var platform in new[] { "Any CPU", "x64", "x86" })
                buildSettings.Add($"{iosGUID}.Debug|{platform}.Build.0 = Debug|iPhoneSimulator");

            var globalPoint = 1 + text.IndexOf(Environment.NewLine, text.IndexOf("GlobalSection(ProjectConfigurationPlatforms) = postSolution"));

            text = text.Insert(globalPoint, buildSettings.ToLinesString() + Environment.NewLine);

            solution.WriteAllText(text);

            Console.WriteLine("Done.");
        }

        IDictionary<string, string> GetReplacements(string projectName)
        {
            var result = new Dictionary<string, string>
            {
                {"MyProjectName", projectName.ToPascalCaseId()},
                {"myProjectName", projectName.ToCamelCaseId()},
                {"My Project Name", projectName},
                {"myprojectname", projectName.ToPascalCaseId().ToLower()}
            };

            if (AdditionalReplacements?.Any() == true)
                result.Add(AdditionalReplacements);

            return result;
        }
    }
}