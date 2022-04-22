namespace Zebble
{
    using System.IO;
    using Olive;

    partial class StartUp
    {
        static string GetIOTempRoot()
        {
            var result = Renderer.Context.CacheDir?.ToString();

            if (result.IsEmpty())
                result = Path.Combine(GetIORoot(), "__cache");

            return result;
        }

        static string GetIORoot()
            => System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
    }
}