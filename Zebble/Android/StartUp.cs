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
                result = Path.Combine(GetIOTempRoot(), "__cache");

            return result;
        }

        string GetIORoot()
            => System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
    }
}