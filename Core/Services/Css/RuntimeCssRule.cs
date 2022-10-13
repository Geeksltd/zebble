namespace Zebble.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    class RuntimeCssRule : CssRule
    {
        public Dictionary<string, string> Settings = new();

        public Action<Stylesheet> Setter;

        public override Task Apply(View view)
        {
            foreach (var setting in Settings)
            {
                var owner = FindProperty(view, setting.Key);
                if (owner is null)
                {
                    Log.For(this).Error("Failed to parse the css setting: " + setting.Key + "->" + setting.Value);
                    continue;
                }

                if (setting.Value is null)
                {
                    Log.For(this).Error("Css setting value is null for: " + setting.Key);
                    continue;
                }

                object value;
                try
                {
                    var targetType = owner.Item2.GetPropertyOrFieldType();
                    if (targetType == typeof(Length.LengthRequest))
                    {
                        if (setting.Value.Contains("px"))
                            value = new Length.FixedLengthRequest(setting.Value.Replace("px", "").To<float>());
                        else if (setting.Value.Contains("%"))
                            value = new Length.PercentageLengthRequest(setting.Value.Replace("%", "").To<float>());
                        else
                            value = new Length.FixedLengthRequest(setting.Value.To<float>());
                    }
                    else if (targetType == typeof(GapRequest))
                    {
                        Gap master = null;
                        if (setting.Key.Contains("Margin"))
                            master = view.Margin;
                        else if (setting.Key.Contains("Padding"))
                            master = view.Padding;


                        var numberParts = setting.Value.ToCharArray().Where(v => v.IsDigit() || v == ' ')
                            .ToString("").KeepReplacing("  ", " ")
                            .Split(' ')
                            .Trim()
                            .Select(v => v.To<float>())
                            .ToArray();

                        var top = numberParts.ElementAtOrDefault(0);
                        var bottom = top;
                        var left = top;
                        var right = top;

                        if (numberParts.Length == 2) left = right = numberParts[1];
                        if (numberParts.Length == 4)
                        {
                            right = numberParts[1];
                            bottom = numberParts[2];
                            left = numberParts[3];
                        }

                        value = new GapRequest(master, null, null) { bottom = bottom, left = left, right = right, top = top };
                    }
                    else
                        value = ParseValue(setting.Value, targetType);
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex, "Failed to convert the css value of '" + setting.Value + "' to type " +
                        owner.Item2?.GetPropertyOrFieldType());
                    continue;
                }

                try
                {
                    owner.Item2.SetValue(owner.Item1, value);
                }
                catch (Exception ex)
                {
                    Log.For(this).Error(ex, "Failed to set the value of " + owner.Item2?.DeclaringType?.Name + "." +
                        owner.Item2?.Name + " property from '" + value);
                }
            }

            Setter?.Invoke(view.Css);

            return Task.CompletedTask;
        }

        static object ParseValue(string value, Type targetType)
        {
            if (targetType.Name == "Color")
                return Colors.FromName(value) ?? Color.Parse(value);

            return value.To(targetType);
        }

        Tuple<object, MemberInfo> FindProperty(object owner, string property)
        {
            try
            {
                var firstPart = property.OrEmpty().Split('.').Trim().FirstOrDefault();
                if (firstPart.IsEmpty()) return null;

                var prop = owner.GetType().GetPropertyOrField(firstPart);
                if (prop is null) return null;

                property = property.TrimStart(firstPart).TrimStart(".");
                if (property.IsEmpty()) return Tuple.Create(owner, prop);
                else return FindProperty(prop.GetValue(owner), property);
            }
            catch
            {
                return null;
            }
        }

        bool Matches(View view, string direct)
        {
            try
            {
                return FindIfMatches(view, direct);
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex, $"Failed to determine if view '{view}' matches CSS selector '{direct}'.");
                return false;
            }
        }

        bool FindIfMatches(View view, string direct)
        {
            var tag = direct.RemoveFrom(".").RemoveFrom("#").RemoveFrom(":");
            if (tag.HasValue())
            {
                if (tag != "*" && CssEngine.GetCssTags(view.GetType()).Lacks(tag)) return false;
                direct = direct.TrimStart(tag);
                if (direct.IsEmpty()) return true;
            }

            while (true)
            {
                if (direct.StartsWithAny(".", "#", ":"))
                {
                    var until = direct.Substring(1).IndexOfAny(new[] { '.', '#', ':' });

                    if (until == -1) until = direct.Length;
                    else until++;

                    var part = direct.Substring(1, until - 1);

                    if (direct.StartsWith("."))
                    {
                        if (!HasClass(view, part)) return false;
                    }
                    else if (direct.StartsWith(":"))
                    {
                        if (!view.PseudoCssState.ContainsWholeWord(part)) return false;
                    }
                    else if (view.id != part) return false;

                    direct = direct.Substring(1).Substring(part.Length).Trim();

                    if (direct.IsEmpty()) break;
                }
                else
                {
                    Log.For(this).Error("CSS PARSE ERROR: " + direct);
                    return false;
                }
            }

            return true;
        }

        public override bool Matches(View view)
        {
            try
            {
                var parts = Selector.Split(' ').Trim().ToArray().Reverse().ToArray();

                if (parts.Any())
                {
                    if (!Matches(view, parts.First())) return false;

                    var shouldRecurseUp = true;

                    foreach (var part in parts.ExceptFirst())
                    {
                        if (part == ">") { shouldRecurseUp = false; continue; }

                        view = view.parent;

                        while (true)
                        {
                            if (view is null) return false;
                            if (Matches(view, part))
                            {
                                shouldRecurseUp = true;
                                break;
                            }

                            if (!shouldRecurseUp) return false;
                            else shouldRecurseUp = true;

                            view = view.parent;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex, "Failed to check if the CSS rule '" + this + "' matches '" + view);
                return false;
            }

            return true;
        }

        public override string Body
        {
            get => base.Body;
            set
            {
                base.Body = value.OrEmpty();

                Settings.Clear();

                new CssBodyProcessor(base.Body).ExtractSettings()
                    .Except(x => x.StringValue == "{{{CALC}}}")
                   .Do(s => Settings[s.GetPropertyPath()] = s.StringValue);
            }
        }
    }
}