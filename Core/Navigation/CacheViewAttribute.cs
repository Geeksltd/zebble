namespace Zebble
{
    using System;
    using Olive;

    [AttributeUsage(AttributeTargets.Class)]
    public class CacheViewAttribute : Attribute
    {
        static bool? isDisabled;
        public static bool IsDisabled
        {
            get
            {
                if (isDisabled is null) isDisabled = Config.Get("Disable.View.Caching", defaultValue: false);

                return isDisabled.Value;
            }
        }

        public static bool IsCacheable(Page page)
        {
            if (IsDisabled || page == null) return false;
            return page is ITemplate || page.GetType().Defines<CacheViewAttribute>(inherit: true);
        }

        public static bool IsCacheable(PopUp page)
        {
            var view = page?.GetView();

            if (IsDisabled || view == null) return false;
            return view is ITemplate || view.GetType().Defines<CacheViewAttribute>(inherit: true);
        }
    }
}