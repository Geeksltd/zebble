namespace Zebble
{
    using System;

    /// <summary>
    /// When applied to a class, it makes it visible in the .ZBL markup intellisense.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MarkupAddableAttribute : Attribute { }
}