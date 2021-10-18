namespace Zebble.Tooling
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;

    static class Runtime
    {
        internal static bool IsWindows() => OS == OSPlatform.Windows;

        internal static OSPlatform OS
        {
            get
            {
                var result = new[]
                {
                    OSPlatform.Windows
                }.FirstOrDefault(RuntimeInformation.IsOSPlatform);

                if (result == default)
                    throw new NotSupportedException(RuntimeInformation.OSDescription);

                return result;
            }
        }
    }
}