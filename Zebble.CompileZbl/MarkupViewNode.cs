namespace Zebble.CompileZbl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Olive;

    class MarkupViewNode
    {
        const int MAX_SINGLE_LINE = 100;

        static Dictionary<string, int> UniqueIdCounter;

        public XElement Element;

        public MarkupViewNode(XElement element) => Element = element;

        public static void StartNewModule() => UniqueIdCounter = new Dictionary<string, int>();

        public string Name => Element.Name.LocalName;

        public string NodeType => Name + Element.GetValue<string>("@of").Or(Element.GetValue<string>("@z-of")).WithWrappers("<", ">");

        public bool IsPsudoNode() => Name.StartsWith("z-");

        public int Depth => Element.Ancestors().Count();

        public MarkupViewNode Parent
        {
            get
            {
                var node = Element.Ancestors().SkipWhile(x => x.Name.LocalName == "z-foreach")
                   .FirstOrDefault();

                if (node == null) return null;
                else return new MarkupViewNode(node);
            }
        }

        public string GetContainerId()
        {
            if (Parent.Name == "z-place") return Parent.Attr("inside");

            if (Parent.Name == "z-loose") return null;

            if (Attr("z-navBar").HasValue()) return "#NAVBAR";

            return Parent.Id;
        }

        public string Id
        {
            get
            {
                if (IsPsudoNode()) return null;

                var result = Attr("Id").TrimOrEmpty();

                if (result.IsEmpty() && !IsPsudoNode())
                {
                    int number;

                    if (UniqueIdCounter.ContainsKey(Name))
                        number = UniqueIdCounter[Name] = UniqueIdCounter[Name] + 1;
                    else UniqueIdCounter[Name] = number = 1;

                    result = "__" + Name.ToCamelCaseId() + number;

                    Element.Add(new XAttribute("Id", result));
                }

                return result;
            }
        }

        public string Attr(string name) => Element.GetValue<string>("@" + name);

        IEnumerable<MarkupPropertySetting> GetSettings()
        {
            return Element.Attributes().Select(a => new MarkupPropertySetting(a))
                  .ToList();
        }

        public IEnumerable<MarkupPropertySetting> GetBindableSettings()
        {
            return GetSettings().Where(x => x.Bindable.HasValue());
        }

        public IEnumerable<MarkupPropertySetting> GetPropertySettings()
        {
            return GetSettings()
                .Where(x => x.Key.First().IsUpper() && x.Value.HasValue())
                  .Where(x => x.Key != "Style")
                  .Except(x => x.Key == "Id" && x.Value.StartsWith("\"__"))

                  // Order:
                  .OrderByDescending(x => x.Key == "Id").ThenBy(x => x.Key == "DataSource")
                  .ToList();
        }

        public IEnumerable<MarkupPropertySetting> GetDirectSettings()
        {
            return GetPropertySettings().Where(x => x.Key.Lacks("."));
        }

        public IEnumerable<MarkupPropertySetting> GetEventHandlers()
        {
            return GetSettings()
                .Where(x => x.Bindable.IsEmpty())
                .Where(x => x.Key.StartsWith("on-") && x.Value.HasValue());
        }

        public IEnumerable<MarkupPropertySetting> GetIndirectSettings()
        {
            return GetPropertySettings().Where(x => x.Key.Contains("."));
        }

        public string GenerateAdder()
        {
            if (Attr("z-navBar").HasValue())
            {
                return "await ((NavBarPage)Page).GetNavBar().AddButton(ButtonLocation." +
                    Attr("z-navBar").ToProperCase() + ", " + Id + ");";
            }
            else if (Parent.Name == "z-loose") return null;
            else
            {
                var method = "Add";
                var anim = string.Empty;

                if (AnimationInfo.Change.HasValue())
                {
                    method += "WithAnimation";
                    anim = ", " + AnimationInfo.Generate(Id);
                }

                return "await " + GetContainerId().Unless("__class1").WithSuffix(".") + method + $"({Id}{anim});";
            }
        }

        public AddAnimationInfo AnimationInfo => AddAnimationInfo.Parse(Element);

        string GetCustomisers()
        {
            var customisers = new List<string>();

            customisers.AddRange(GetEventHandlers().Select(x => x.GenerateEventHandlerExpression()));
            customisers.AddRange(GetBindableSettings().Select(x => x.GenerateBindingExpression()));
            customisers.Add(GenerateNavigateActions().Trim());
            customisers.AddRange(GetIndirectSettings().Select(x => ".Set(x => x." + x.GenerateSetExpression() + ")"));
            customisers.Add(GenerateCodeToApplyStyles());

            if (Attr("z-cache-background-image") == "true")
                customisers.Add(".Set(x => x.WhenShown(() => Zebble.Services.ImageService.MemoryCacheBackground(x)))");

            // Add data- attributes to views
            foreach (var dataAttribute in Element.Attributes().Where(x => x.Name.LocalName.StartsWith("data-")))
            {
                var value = dataAttribute.Value;

                if (value.StartsWith("@")) value = value.TrimStart("@");
                else value = MarkupPropertySetting.GetStringExpression(value);

                customisers.Add($".Set(x => x.Data[\"{dataAttribute.Name.LocalName.TrimStart("data-")}\"] = {value})");
            }

            customisers = customisers.Trim().Distinct().ToList();

            if (customisers.Sum(v => v.Length) < MAX_SINGLE_LINE) return customisers.ToString("");
            else return customisers.ToLinesString();
        }

        public string GenerateObjectCreator()
        {
            var r = new StringBuilder();
            var directProperties = GetDirectSettings();

            var setters = directProperties.Select(x => x.Key + " = " + x.Value).ToList();

            var customisers = GetCustomisers();

            // First initialize it:            
            if (NeedsClassField)
            {
                // Already instantiated
                foreach (var s in setters)
                    r.AppendLine(Id + "." + s + ";");

                r.AppendLine(customisers.WithWrappers(Id, ";"));
            }
            else
            {
                if (Id.HasValue() && !NeedsClassField) r.Append("var " + Id + " = ");
                r.Append($"new {NodeType}({Attr("z-ctor")})");

                var isSmall = setters.Sum(v => v.Length) < MAX_SINGLE_LINE;

                if (setters.Any())
                    if (isSmall)
                        r.Append(" { " + setters.ToString(", ") + " }");
                    else
                    {
                        r.AppendLine();
                        r.AppendLine("{");
                        r.AppendLine(directProperties.Select(x => x.Key + " = " + x.Value).ToString($",{Environment.NewLine}"));
                        r.Append("}");
                    }

                if (customisers.HasValue())
                {
                    if (customisers.Length + setters.Sum(v => v.Length) > MAX_SINGLE_LINE)
                        r.AppendLine();

                    r.Append(customisers);
                }

                r.AppendLine(";");
            }

            return r.ToString();
        }

        public string GenerateNavigateActions()
        {
            var r = new StringBuilder();

            var options = new[] { "z-nav-go", "z-nav-forward", "nav-go", "nav-forward" }.
                 Select(x => new
                 {
                     Method = x.TrimStart("z-").TrimStart("nav-").ToProperCase(),
                     Target = Attr(x)
                 })
                 .Where(x => x.Target.HasValue());

            foreach (var nav in options)
            {
                r.Append(".On(x => x.Tapped, () =>");

                var parameters = Attr("z-nav-params").WithWrappers("new { ", " }");

                var transition = Attr("z-nav-transition").WithPrefix("PageTransition.");
                if (parameters.HasValue() && transition.HasValue()) transition = ", " + transition;

                var navAction = $" Nav.{nav.Method}<{nav.Target}>({parameters}{transition})";

                if (Attr("z-nav-throws") == "true") r.Append(navAction);
                else
                {
                    r.AppendLine();
                    r.AppendLine("{");
                    r.AppendLine($"try {{ return{navAction}; }}");
                    r.AppendLine("catch (Exception ex) { return Alert.Show(ex.Message); }");
                    r.AppendLine("}");
                }

                r.Append(")");
            }

            return r.ToString();
        }

        string GenerateCodeToApplyStyles()
        {
            var style = Attr("Style").OrEmpty().Trim('\"');
            if (style.IsEmpty()) return null;

            var result = new CssBodyProcessor(style).GenerateCode("x", "Style").Split(';').Trim()
                .Select(s => ".Set(x => " + s + ")").ToLinesString();

            return result.WithPrefix(Environment.NewLine);
        }

        public bool NeedsClassField
        {
            get
            {
                if (Id.IsEmpty()) return false;
                if (Id.Contains("__")) return false;
                if (IsPsudoNode()) return false;
                if (Element.Ancestors().Any(x => x.Name.LocalName == "z-foreach")) return false;

                // TODO: Test for uniqueness

                return true;
            }
        }
    }
}