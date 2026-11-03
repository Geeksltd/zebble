namespace Zebble.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Olive;

    public class CssEngine
    {
        static DevicePlatform? platform;

        internal static ConcurrentDictionary<CssReference, CssRule[]> SelectorCache = new();
        static readonly List<CssRule> Rules = new();
        static readonly ConcurrentDictionary<Type, string[]> TypeCssTagsCache = new();

        static readonly ConcurrentDictionary<string, List<CssRule>> ClassRules = new();
        static readonly ConcurrentDictionary<string, List<CssRule>> TagRules = new();
        static readonly ConcurrentDictionary<string, List<CssRule>> IdRules = new();
        static readonly List<CssRule> WildCardRules = new();

        public static readonly List<CssRule> InspectionRules = new();

        public static DevicePlatform Platform
        {
            get
            {
                if (platform is null)
                {
                    if (Device.OS.Platform == DevicePlatform.Windows)
                        platform = Config.Get("Dev.Css.Platform").TryParseAs<DevicePlatform>();

                    platform ??= Device.OS.Platform;
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
                    ClassRules.GetOrAdd(cls, () => new List<CssRule>()).Add(rule);
            }
            else if (target.ID.HasValue())
                IdRules.GetOrAdd(target.ID, new List<CssRule>()).Add(rule);
            else if (target.IsWildCard) WildCardRules.Add(rule);
            else if (target.Tag.HasValue())
                TagRules.GetOrAdd(target.Tag, () => new List<CssRule>()).Add(rule);
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
            if (SelectorCache.TryGetValue(view.CssReference, out var result))
                return result;

            return SelectorCache[view.CssReference.CloneForCacheKey()] = FindRules(view);
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
                if (IdRules.TryGetValue(view.id, out items))
                    result = result.Concat(items);

            if (view.CssClassParts != null)
                foreach (var cls in view.CssClassParts)
                    if (ClassRules.TryGetValue(cls, out items)) result = result.Concat(items);

            foreach (var tag in GetCssTags(view.GetType()))
                if (TagRules.TryGetValue(tag, out items))
                    result = result.Concat(items);

            return result.ToList();
        }

        public static string[] GetCssTags(Type type)
        {
            return TypeCssTagsCache.GetOrAdd(type, () =>
            {
                var resultList = new List<string>();

                for (var t = type; t != typeof(View); t = t.BaseType)
                    resultList.Add(GetCssTag(t));

                return resultList.ToArray();
            });
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