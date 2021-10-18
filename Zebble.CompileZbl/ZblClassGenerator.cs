using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Olive;
using Zebble.Tooling;

namespace Zebble.CompileZbl
{
    abstract class ZblClassGenerator
    {
        protected XElement Node;
        protected string Name, Namespace, ViewModel, BaseClass, Implements;
        protected bool Cacheable;

        protected ZblClassGenerator(XElement node)
        {
            Node = node;
            Name = node.GetValue<string>("@type");
            Namespace = node.GetValue<string>("@namespace").Or("UI");
            ViewModel = node.GetValue<string>("@viewmodel");
            BaseClass = node.GetValue<string>("@base").OrEmpty().Replace("[", "<").Replace("]", ">");
            Implements = node.GetValue<string>("@implements").OrEmpty().Replace("[", "<").Replace("]", ">");

            if (ViewModel.HasValue()) Implements = Implements.WithSuffix(", ") + "ITemplate<" + ViewModel + ">";
            Cacheable = node.GetValue<string>("@cache") == "true" || ViewModel.HasValue();
        }

        protected virtual bool Public => false;

        public virtual string Generate(string filePath) => GenerateClass(filePath);

        protected virtual string GenerateSubClasses() => null;

        protected string GenerateClass(string filePath)
        {
            var r = new StringBuilder();

            if (Cacheable) r.AppendLine("[CacheView]");

            r.AppendLine("[EscapeGCop(\"Auto-generated\")]");
            r.AppendLine($"[SourceCode(@\"{filePath}\")]");
            r.AppendLine($"{"public ".OnlyWhen(Public)}partial class {Name} : " + BaseClass + Implements.WithPrefix(", "));
            r.AppendLine("{");

            MarkupViewNode.StartNewModule();

            if (ViewModel.HasValue())
                r.AppendLine($"public {ViewModel} Model = Zebble.Mvvm.ViewModel.The<{ViewModel}>();");

            r.AppendLine(DefineFields());

            r.AppendLine(GenerateInitializer());

            foreach (var sub in Node.Descendants().Where(d => d.Name == "class"))
                r.Append(new ZblSubclassGenerator(sub).Generate(filePath));

            r.AppendLine("}");

            return new CSharpFormatter(r.ToString()).Format();
        }

        string DefineFields()
        {
            return FindViews(Node).Where(x => x.NeedsClassField)
               .Select(x => $"public {x.NodeType} {x.Id} = new {x.NodeType}();")
               .ToLinesString();
        }

        string GenerateInitializer()
        {
            var r = new StringBuilder();

            var body = GenerateChildViewsInitializationBody(Node).Trim();

            if (body.IsEmpty()) return string.Empty;

            r.AppendLine("protected override async Task InitializeFromMarkup()");
            r.AppendLine("{");
            r.AppendLine($"await base.InitializeFromMarkup();{Environment.NewLine}");
            r.AppendLine(body);
            r.AppendLine("}");

            return r.ToString();
        }

        IEnumerable<MarkupViewNode> FindViews(XElement parent)
        {
            foreach (var child in parent.Elements().Except(c => c.Name == "class"))
            {
                if (child.Name.LocalName.IsNoneOf("z-loose", "z-place"))
                    yield return new MarkupViewNode(child);

                foreach (var x in FindViews(child))
                    yield return new MarkupViewNode(x.Element);
            }
        }

        string GenerateChildViewsInitializationBody(XElement type)
        {
            var r = new StringBuilder();

            r.AppendLine(GenerateComponentInitializer(type));

            // Add data settings:
            foreach (var setting in new MarkupViewNode(type).GetIndirectSettings())
                r.AppendLine(setting.GenerateSetExpression() + ";");

            r.AppendLine();

            var containerLoops = new List<XElement>();

            var currentLoopItems = new List<MarkupViewNode>();

            foreach (var item in FindViews(type))
            {
                var tobeClosedLoop = containerLoops.Where(i => item.Element.Ancestors().Lacks(i)).ToArray();

                foreach (var loopItem in tobeClosedLoop)
                {
                    FlushLoopCode();

                    r.AppendLine($"}}{Environment.NewLine}");
                    containerLoops.Remove(loopItem);
                }

                if (item.Name == "z-foreach")
                {
                    FlushLoopCode();
                    containerLoops.Add(item.Element);
                    r.AppendLine();
                    r.AppendLine($"foreach(var {item.Attr("var").Or("item")} in {item.Attr("in").Or("???")})");
                    r.AppendLine("{");
                    continue;
                }
                else currentLoopItems.Add(item);

                r.AppendLine(item.GenerateObjectCreator());
                r.AppendLine();
            }

            void FlushLoopCode()
            {
                var views = currentLoopItems.Where(x => x.Id.HasValue()).OrderByDescending(x => x.Depth);

                foreach (var item in views.GroupBy(x => x.GetContainerId()))
                {
                    if (item.IsSingle() || item.Key == "#NAVBAR" ||
                        item.Any(x => x.AnimationInfo.Change.HasValue()))
                    {
                        foreach (var x in item)
                            r.AppendLine(x.GenerateAdder());
                    }
                    else r.AppendLine("await " + item.Key.Unless("__class1").WithSuffix(".") +
                        $"AddRange(new View[] {{ {item.Select(x => x.Id).ToString(", ")} }});");
                }

                currentLoopItems.Clear();
            }

            FlushLoopCode();

            while (containerLoops.Any())
            {
                // Close the loop:
                r.AppendLine($"}}{Environment.NewLine}");
                containerLoops.RemoveAt(containerLoops.Count - 1);
            }

            return r.ToString();
        }

        string GenerateComponentInitializer(XElement type)
        {
            var r = new StringBuilder();

            // Add data- attributes to Components
            foreach (var setting in type.Attributes().Where(x => x.Name.LocalName.StartsWith("data-")))
            {
                var value = setting.Value;

                if (value.StartsWith("@")) value = value.TrimStart("@");
                else value = MarkupPropertySetting.GetStringExpression(value);

                r.AppendLine($"Data[\"{setting.Name.LocalName.TrimStart("data-")}\"] = {value};");
            }

            var node = new MarkupViewNode(type);

            foreach (var setting in node.GetDirectSettings())
                r.AppendLine("this." + setting.GenerateSetExpression() + ";");

            r.AppendLine();

            foreach (var setting in node.GetEventHandlers())
                r.Append(setting.Key.TrimStart("on-") + ".Handle(" + setting.Value.Trim('\"') + ");");

            foreach (var setting in node.GetBindableSettings())
                r.Append($"{Environment.NewLine}this" + setting.GenerateBindingExpression() + ";");

            r.AppendLine(node.GenerateNavigateActions().WithWrappers("this", ";"));
            r.AppendLine();
            return r.ToString();
        }
    }

    class ZblSubclassGenerator : ZblClassGenerator
    {
        public ZblSubclassGenerator(XElement node) : base(node)
        {
        }

        protected override bool Public => true;
    }

    class ZblRootClassGenerator : ZblClassGenerator
    {
        public ZblRootClassGenerator(XElement node) : base(node)
        {
        }

        public override string Generate(string filePath)
        {
            var r = new StringBuilder();

            r.AppendLine("#region " + Namespace + "." + Name);

            r.AppendLine("namespace " + Namespace);
            r.AppendLine("{");
            r.AppendLine(@"using System;");
            r.AppendLine(@"using System.Collections.Generic;");
            r.AppendLine(@"using System.Linq;");
            r.AppendLine(@"using System.Text;");
            r.AppendLine(@"using System.Threading.Tasks;");
            r.AppendLine(@"using Domain;");
            r.AppendLine(@"using Zebble;");
            r.AppendLine(@"using Zebble.Plugin;");
            r.AppendLine(@"using Olive;");
            r.AppendLine(@"using Zebble.Services.Css;");
            r.AppendLine();

            r.AppendLine(GenerateClass(filePath));

            r.AppendLine("}");
            r.AppendLine("#endregion");
            r.AppendLine();

            return new CSharpFormatter(r.ToString()).Format();
        }

        protected override string GenerateSubClasses()
        {
            return Node.Descendants().Where(d => d.Name == "class").Select(sub => new ZblSubclassGenerator(sub).Generate("") + Environment.NewLine).ToLinesString();
        }
    }
}