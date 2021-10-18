namespace Zebble
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyGroupAttribute : Attribute
    {
        public string Group { get; set; }
        public PropertyGroupAttribute(string group) => Group = group;
    }
}