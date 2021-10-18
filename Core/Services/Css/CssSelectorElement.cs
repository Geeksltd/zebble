namespace Zebble.Services
{
    using System;
    using System.Linq;
    using Olive;

    class CssSelectorElement
    {
        public string ID, State, Tag;
        public string[] Classes;
        public bool IsWildCard => Tag == "*";

        public CssSelectorElement(string text)
        {
            var byColon = text.Trim().Split(':').Trim().ToArray();
            switch (byColon.Length)
            {
                case 1: break;
                case 2: State = byColon[1]; text = byColon[0]; break;
                default: throw new ArgumentException("Unrecognised CSS selector section format: " + text);
            }

            if (text.Contains("."))
            {
                Classes = text.RemoveBefore(".", caseSensitive: true).Split('.').Trim().ToArray();
                text = text.RemoveFrom(".");
            }

            if (text.Contains("#"))
            {
                ID = text.RemoveBeforeAndIncluding("#", caseSensitive: true).Trim();
                text = text.RemoveFrom("#");
            }

            Tag = text;
        }
    }
}