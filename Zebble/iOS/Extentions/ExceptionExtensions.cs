using Foundation;
using System.Collections.Generic;
using System.Linq;

namespace System
{
    public static class ExceptionExtensions
    {
        public static NSError Render(this Exception exception)
        {
            var crashInfo = new Dictionary<object, object>
            {
                [NSError.LocalizedDescriptionKey] = exception.Message,
                ["StackTrace"] = exception.StackTrace
            };

            return new NSError(
                new NSString(exception.GetType().FullName),
                -1,
                NSDictionary.FromObjectsAndKeys(
                    crashInfo.Values.ToArray(),
                    crashInfo.Keys.ToArray(),
                    crashInfo.Count
                )
            );
        }
    }
}