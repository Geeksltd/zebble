namespace Zebble
{
    using System.Collections.Generic;

    partial class UIRuntime
    {        
        public static readonly AsyncEvent<Dictionary<string, string>> OnParameterRecieved = new();
        public static bool SkipPageRefresh = false; 
        public static bool IsDevMode => false;
    }
}