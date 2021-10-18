namespace Zebble.Services
{
    using System;
    using Olive;

    [AttributeUsage(AttributeTargets.Class)]
    public class CssBodyAttribute : Attribute
    {
        public string Body;
        public bool HasCalc;

        public CssBodyAttribute(string body)
        {
            Body = body;
            HasCalc = body.OrEmpty().Contains("calc(");
        }
    }
}