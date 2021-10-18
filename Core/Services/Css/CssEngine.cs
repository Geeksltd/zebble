namespace Zebble.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Olive;

    public class CssEngine
    {
        static DevicePlatform? platform;

        internal static Dictionary<CssReference, CssRule[]> SelectorCache = new Dictionary<CssReference, CssRule[]>();
        static readonly List<CssRule> Rules = new List<CssRule>();
        static Dictionary<Type, string[]> TypeCssTagsCache = new Dictionary<Type, string[]>();

        static Dictionary<string, List<CssRule>> ClassRules = new Dictionary<string, List<CssRule>>();
        static Dictionary<string, List<CssRule>> TagRules = new Dictionary<string, List<CssRule>>();
        static Dictionary<string, List<CssRule>> IdRules = new Dictionary<string, List<CssRule>>();
        static List<CssRule> WildCardRules = new List<CssRule>();

        public static readonly List<CssRule> InspectionRules = new List<CssRule>();
        public static DevicePlatform Platform
        {
            get
            {
                if (platform is null)
                {
                    if (Device.OS.Platform == DevicePlatform.Windows)
                        platform = Config.Get("Dev.Css.Platform").TryParseAs<DevicePlatform>();

                    platform = platform ?? Device.OS.Platform;
                }

                return platform.Value;
            }
            set
            {
                platform = value;
                ClearNonDynamics();
                UIRuntime.AppAssembly.ExportedTypes.FirstOrDefault(x => x.Name == "CssStyles")
                    ?.GetMethod("LoadAll")?.Invoke(null, new object[0]);
            }
        }

        public static void Add(string selector, Action<Stylesheet> applySettings)
        {
            Add(new RuntimeCssRule { Selector = selector, Setter = applySettings });
        }

        public static void Add(CssRule rule)
        {
            Rules.Add(rule);

            var target = new CssSelectorElement(rule.Selector.Trim().Split(' ').Last());

            if (!target.Classes.None())
            {
                foreach (var cls in target.Classes)
                {
                    lock (ClassRules)
                        if (ClassRules.TryGetValue(cls, out var existing)) existing.Add(rule);
                        else ClassRules.Add(cls, new List<CssRule> { rule });
                }
            }
            else if (target.ID.HasValue())
            {
                lock (IdRules)
                    if (IdRules.TryGetValue(target.ID, out var existing)) existing.Add(rule);
                    else IdRules.Add(target.ID, new List<CssRule> { rule });
            }
            else if (target.IsWildCard) WildCardRules.Add(rule);
            else if (target.Tag.HasValue())
            {
                lock (TagRules)
                    if (TagRules.TryGetValue(target.Tag, out var existing)) existing.Add(rule);
                    else TagRules.Add(target.Tag, new List<CssRule> { rule });
            }
        }

        internal static void ClearNonDynamics()
        {
            SelectorCache.Clear();

            var lists = new List<List<CssRule>> { Rules, WildCardRules };

            lists.AddRange(new[] { ClassRules, IdRules, TagRules }.SelectMany(x => x.Values));

            foreach (var list in lists)
                foreach (var rule in list.Except(x => x.HasCalc()).ToArray())
                    if (rule.Selector.StartsWith("InspectionBox"))
                        InspectionRules.Add(rule);

            Rules.Clear();
        }

        internal static void Remove(string file, string selector)
        {
            var rule = Rules.FirstOrDefault(x => x.File == file && x.Selector == selector);
            if (rule is null) return;
            Rules.Remove(rule);

            new[] { ClassRules, TagRules, IdRules }.Do(bag =>
             {
                 foreach (var item in bag)
                     if (item.Value.Contains(rule)) item.Value.Remove(rule);
             });
        }

        public static async Task Apply(View view)
        {
            var matchedRules = FindMatchedRules(view);

            foreach (var rule in matchedRules)
            {
                try { await rule.Apply(view); }
                catch (Exception ex)
                {
                    Log.For<CssEngine>().Error(ex, "Failed to apply css rule " + rule);
                }
            }
        }

        public static string Diagnose(View view)
        {
            var r = new StringBuilder();

            var rules = FindMatchedRules(view).Distinct(x => x.File + "~" + x.Selector + "~" + x.Body).Reverse().ToArray();

            var file = "";

            foreach (var rule in rules)
            {
                rule.File = rule.File.Or("undetected scss file >:( ");

                if (rule.File != file)
                {
                    r.AppendLine("🗋 " + rule.File);
                    file = rule.File;
                }

                r.AppendLine(rule.Selector);
                foreach (var setting in rule.Body.Split(';').Trim())
                    r.AppendLine("    " + setting + ";");
                r.AppendLine("");
            }

            return r.ToString().Trim();
        }

        static CssRule[] FindMatchedRules(View view)
        {
            if (!SelectorCache.TryGetValue(view.CssReference, out var result))
            {
                result = FindRules(view);
                lock (SelectorCache)
                    return SelectorCache[view.CssReference.CloneForCacheKey()] = result;
            }

            return result;
        }

        static CssRule[] FindRules(View view)
        {
            var result = FindPossibleRules(view);
#if UWP
            if (UIRuntime.IsDevMode)
            {
                result = result.Where(x => x.Platform == null || x.Platform == Platform).ToList();

                if (view.WithAllParents().Any(x => x.id == "ZebbleInspectionBox"))
                    result = result.OfType<RuntimeCssRule>().Cast<CssRule>().ToList();
            }
#endif

            return result.Where(r => r.Matches(view)).OrderBy(x => x.specificity).ThenBy(x => x.AddingOrder).ToArray();
        }

        static List<CssRule> FindPossibleRules(View view)
        {
            IEnumerable<CssRule> result = WildCardRules;

            List<CssRule> items;

            if (view.id.HasValue())
                lock (IdRules)
                    if (IdRules.TryGetValue(view.id, out items))
                        result = result.Concat(items);

            if (view.CssClassParts != null)
                lock (ClassRules)
                    foreach (var cls in view.CssClassParts)
                        if (ClassRules.TryGetValue(cls, out items)) result = result.Concat(items);

            foreach (var tag in GetCssTags(view.GetType()))
                lock (TagRules)
                    if (TagRules.TryGetValue(tag, out items))
                        result = result.Concat(items);

            return result.ToList();
        }

        public static string[] GetCssTags(Type type)
        {
            lock (TypeCssTagsCache)
            {
                if (TypeCssTagsCache.TryGetValue(type, out var result)) return result;

                var resultList = new List<string>();

                for (var t = type; t != typeof(View); t = t.BaseType)
                    resultList.Add(GetCssTag(t));

                result = resultList.ToArray();

                return TypeCssTagsCache[type] = result;
            }
        }

        static string GetCssTag(Type type)
        {
            var result = GetTypeDirectTag(type);

            for (var parentType = type.DeclaringType; parentType != null; parentType = parentType.DeclaringType)
                result = GetTypeDirectTag(parentType) + "-" + result;

            return result;
        }

        static string GetTypeDirectTag(Type type)
        {
            if (type.Name.Lacks("`")) return type.Name;

            var result = type.Name.RemoveFrom("`");

            result += "--" + type.GetGenericArguments().Select(GetTypeDirectTag).ToString("_");

            return result;
        }

        public static View FindParentById(View view, string parentId)
        {
            var parent = view.parent;

            while (true)
            {
                if (parent is null) return null;
                else if (parent.id == parentId) return parent;

                parent = parent.parent;
            }
        }

        public static View FindParentByCssClass(View view, string parentCssClass)
        {
            var parent = view.parent;

            while (true)
            {
                if (parent is null) return null;
                else if (parent.CssClassParts?.Contains(parentCssClass) == true) return parent;

                parent = parent.parent;
            }
        }

        public static View FindParentByType<TParent>(View view) where TParent : View
        {
            var parent = view.parent;

            while (true)
            {
                if (parent is null) return null;
                else if (parent is TParent) return parent;

                parent = parent.parent;
            }
        }
    }
}