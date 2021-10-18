namespace Zebble.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    public abstract partial class CssRule
    {
        internal int specificity, AddingOrder;
        string selector;
        public DevicePlatform? Platform;
        static int GlobalAddingOrderCounter;

        protected CssRule()
        {
            var selector = GetType().GetCustomAttribute<CssSelectorAttribute>();

            if (selector != null)
            {
                File = selector?.File;
                Platform = selector?.Platform;
                Selector = selector?.Selector;
            }

            Body = GetType().GetCustomAttribute<CssBodyAttribute>()?.Body;

            AddingOrder = GlobalAddingOrderCounter++;
        }

        public override string ToString() => File + " : " + Selector;

        public int Specificity => specificity;
        public string Selector { get => selector; set { selector = value.OrEmpty(); UpdateSpecificity(); } }

        void UpdateSpecificity()
        {
            // CSS specification: https://www.w3.org/TR/CSS21/cascade.html#specificity

            var numberofIds = selector.AllIndices("#").Count();

            // count the number of other attributes and pseudo-classes in the selector(= c)
            var numberOfOtherSpecifiers = selector.AllIndices('.').Count() + selector.AllIndices(':').Count();
            // TODO: Attributes also get added here if any.
            if (Platform.HasValue) numberOfOtherSpecifiers++;

            var elementNames = selector.Split(' ').Trim().Except(x => x.StartsWithAny(".", "#", ":", ">", "*")).ToArray();

            specificity = 10 * (elementNames.Length * 10 + numberOfOtherSpecifiers * 100 + numberofIds * 1000);

            // Our own rule for C# type hierarchy
            specificity += elementNames.Sum(InheritanceDepth.Of);

            // Add one for the file name:
            if (!File.OrEmpty().Split(Path.DirectorySeparatorChar).Last().StartsWith("_"))
                specificity += 1;
        }

        public string File { get; set; }
        public virtual string Body { get; set; }

        public abstract bool Matches(View view);

        public abstract Task Apply(View view);

        protected static bool HasClass(View view, string cssClass) => view.CssClassParts?.Contains(cssClass) ?? false;

        internal bool HasCalc() => GetType().GetCustomAttribute<CssBodyAttribute>()?.HasCalc == true;
    }
}