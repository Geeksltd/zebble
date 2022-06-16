namespace Zebble.FormatZbl
{
    using System.Globalization;

    class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            new FormatZblFiles().Run();
        }
    }
}