namespace Zebble
{
    partial class StartUp
    {
        static string GetIOTempRoot() 
            => Renderer.Context.CacheDir.ToString();

        string GetIORoot() 
            => System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
    }
}