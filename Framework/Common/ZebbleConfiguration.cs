namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.Extensions.Configuration;
    using Olive;

    public class ZebbleConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new ZebbleConfigurationProvider();

        class ZebbleConfigurationProvider : ConfigurationProvider
        {
            public override void Load()
            {
                Data = LoadValues();
            }
            
            static IDictionary<string, string> LoadValues()
            {
                try
                {
#if UWP || ANDROID || IOS
                    var text = UIRuntime.GetEmbeddedResources()
                        .FirstOrDefault(x => x.Key.EndsWith("config.xml", StringComparison.OrdinalIgnoreCase))
                        .Value?.Invoke()?.ToString(Encoding.UTF8);
#else
                    var text = Device.IO.File("config.xml").ReadAllText();
#endif

                    if (text.HasValue())
                    {
                        var root = text.ToLines().Trim().SkipWhile(x => x.EndsWith("?>")).ToLinesString().To<XElement>();
                        return root.Elements().ToDictionary(x => x.Name.LocalName, x => x.GetValue<string>("@value").Or(x.Value));
                    }
                }
                catch (Exception ex) { throw new Exception("Could not read the config.xml file", ex); }

                Log.For<ZebbleConfigurationSource>().Error("Config file was not found.");
                return new Dictionary<string, string>();
            }
        }

    }
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddZebbleConfiguration(this IConfigurationBuilder builder) => builder.Add(new ZebbleConfigurationSource());
    }
}