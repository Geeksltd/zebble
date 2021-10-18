namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    class CssBodyProcessor
    {
        List<CssSetting> CssSettings;
        List<ZebbleCssSetting> AppSettings = new List<ZebbleCssSetting>();

        public CssBodyProcessor(string cssBody)
        {
            try
            {
                CssSettings = cssBody.Split(';').Trim().Select(s => s.Split(':').Trim())
                     .Select(s => new CssSetting(s.First(), s.Last())).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to process the CSS body: " + cssBody, ex);
            }
        }

        void Add(string key, string csharpValue, string stringValue)
        {
            AppSettings.Add(new ZebbleCssSetting
            {
                Key = key,
                CSharpValue = csharpValue,
                StringValue = stringValue,
                CastType = "ITextControl".OnlyWhen(key?.ToLower() == "text")
            });
        }

        public IEnumerable<ZebbleCssSetting> ExtractSettings()
        {
            foreach (var setting in CssSettings)
            {
                if (setting.CssKey.ToLowerOrEmpty().IsAnyOf("text-align", "vertical-align"))
                {
                    Add("TextAlignment", FindTextAlignment().WithPrefix("Alignment."), FindTextAlignment());
                }
                else
                {
                    var key = setting.CssKey;

                    if (!key.StartsWith("Background("))
                        key = setting.CssKey.Split('-', '.').Select(x => x.ToPascalCaseId()).ToString(".");

                    Add(key, setting.GetCSharpValue(), setting.GetStringValue());
                }
            }

            return AppSettings;
        }

        public string GenerateCode(string variable = "view", string property = "Css")
        {
            return ExtractSettings().OrderByDescending(x => x.GetPriority()).Select(x => x.ToCode(variable, property)).ToLinesString();
        }

        string FindTextAlignment()
        {
            var vertical = CssSettings.FirstOrDefault(x => x.CssKey.ToLower() == "vertical-align")?.CssValue?.ToPascalCaseId();
            if (vertical == "Central") vertical = "Middle";

            var horizontal = CssSettings.FirstOrDefault(x => x.CssKey.ToLower() == "text-align")?.CssValue?.ToPascalCaseId();

            if (horizontal.IsAnyOf("Right", "Left") && vertical == "Middle") vertical = null;

            if (vertical.HasValue() && horizontal.IsEmpty()) horizontal = "Middle";

            var result = new[] { vertical + horizontal }.Trim().Distinct().ToString("");

            if (result == "Center" || result == "MiddleMiddle") return "Middle";
            else return result;
        }
    }
}