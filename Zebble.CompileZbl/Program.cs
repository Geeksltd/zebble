namespace Zebble.CompileZbl
{
    using System.Globalization;

    class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            new MarkupToCSharpConvertor().Run();
        }
    }
}