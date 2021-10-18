using System;
using System.Collections.Generic;
using System.Text;

namespace Zebble.Services.Css
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SourceCodeAttribute : Attribute
    {
        public string FilePath;
        public SourceCodeAttribute(string filePath)
        {
            FilePath = filePath;
        }

    }
}
