namespace Zebble.Services
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class CssSelectorAttribute : Attribute
    {
        public DevicePlatform? Platform;
        public string File, Selector;
        public CssSelectorAttribute(string file, string selector)
        {
            File = file;
            Selector = selector;
        }

        public CssSelectorAttribute(DevicePlatform platform, string file, string selector) : this(file, selector)
        {
            Platform = platform;
        }
    }
}