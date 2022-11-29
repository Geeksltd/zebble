namespace Zebble.Tooling
{
    using System.IO;

    public static class DirectoryInfoExtensions
    {
        public static void SetAttributes(this DirectoryInfo dir, FileAttributes attributes)
        {
            foreach (var subDir in dir.GetDirectories())
                SetAttributes(subDir, attributes);

            foreach (var file in dir.GetFiles())
                file.Attributes = FileAttributes.Normal;
        }

        public static void MoveUpContents(this DirectoryInfo rootDir)
        {
            var directories = rootDir.GetDirectories();
            var files = rootDir.GetFiles();

            foreach (var dir in directories)
                dir.MoveTo(Path.Combine(rootDir.Parent.FullName, dir.Name));

            foreach (var file in files)
                file.MoveTo(Path.Combine(rootDir.Parent.FullName, file.Name), overwrite: true);

            rootDir.Delete();
        }
    }
}