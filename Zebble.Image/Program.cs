namespace Zebble.Image
{
    using Zebble.Tooling;

    class Program
    {
        static void Main(string[] args)
        {
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