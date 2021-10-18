namespace Zebble
{
    using Windows.Storage;

    partial class StartUp
    {
        static string GetIOTempRoot() => ApplicationData.Current.TemporaryFolder.Path;

        static string GetIORoot() => ApplicationData.Current.LocalFolder.Path;
    }
}