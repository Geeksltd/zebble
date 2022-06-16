namespace Zebble.Image
{
    using System.Globalization;
    using Zebble.Tooling;

    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            switch (args.GetCommand())
            {
                case "update-sizes":
                    new MetaXmlGenerator().Generate();
                    break;

                case "splash":
                    new SplashImageCreator().Run();
                    break;

                default:
                    new Unknown().Execute();
                    break;
            }
        }
    }
}