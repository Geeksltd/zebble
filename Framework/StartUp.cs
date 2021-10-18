using System;
using System.Collections.Generic;
using System.Text;

namespace Zebble
{
    public abstract partial  class StartUp
    {
        public static string ApplicationName { get; internal set; }
        public static StartUp Current;
        public static bool Reinstall;
    }
}
