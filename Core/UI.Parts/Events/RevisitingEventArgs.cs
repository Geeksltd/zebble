using System.Collections.Generic;

namespace Zebble
{
    public class RevisitingEventArgs
    {
        public RevisitMode Mode { get; set; }

        public IDictionary<string, object> NavParams { get; set; }
    }
}