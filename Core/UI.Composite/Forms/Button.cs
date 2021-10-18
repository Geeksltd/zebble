namespace Zebble
{
    using Olive;

    public class Button : TextView
    {
        public static bool DefaultAutoFlash = Config.Get("Button.Tap.AutoFlash", defaultValue: true);
        public Button() => AutoFlash = DefaultAutoFlash;

        protected override Alignment GetDefaultAlignment() => Alignment.Middle;

        protected override string GetStringSpecifier() => Text.Or(BackgroundImagePath.OrEmpty().TrimStart("Images/"));
    }
}