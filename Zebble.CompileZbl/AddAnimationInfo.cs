namespace Zebble.CompileZbl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Olive;

    class AddAnimationInfo
    {
        public string Change, Duration, Easing, Factor;

        internal static AddAnimationInfo Parse(XElement element)
        {
            var result = new AddAnimationInfo
            {
                Change = element.GetValue<string>("@z-animate-to"),
                Duration = element.GetValue<string>("@z-animate-duration"),
                Easing = element.GetValue<string>("@z-animate-easing"),
                Factor = element.GetValue<string>("@z-animate-factor")
            };

            if (result.Duration.HasValue() && result.Duration.Is<int>())
                result.Duration += ".Milliseconds()";

            if (result.Easing.HasValue() && result.Easing.IsAnyOf(Enum.GetNames(typeof(AnimationEasing))))
                result.Easing = nameof(AnimationEasing) + "." + result.Easing;

            if (result.Factor.HasValue() && result.Factor.IsAnyOf(Enum.GetNames(typeof(EasingFactor))))
                result.Factor = nameof(EasingFactor) + "." + result.Factor;

            return result;
        }

        internal string Generate(string id)
        {
            var settings = new List<string>();

            if (Duration.HasValue()) settings.Add("Duration = " + Duration);

            if (Easing.HasValue()) settings.Add("Easing = " + Easing);
            if (Factor.HasValue()) settings.Add("Factor = " + Factor);

            var change = Change.Split(';').Trim().Select(x => id + ".Style." + x).ToString(";");
            if (change.Contains(';')) change = change.WithWrappers("{ ", "; }");
            settings.Add("Change = () => " + change);

            return $"{Environment.NewLine}new Animation {{ {settings.ToString(", ")} }}";
        }
    }
}