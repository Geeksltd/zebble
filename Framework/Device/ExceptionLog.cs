using System;
using Olive;

namespace Zebble.Device
{
    public class ExceptionLog
    {
        public Exception Exception;
        public string File { get; set; }
        public string Member { get; set; }
        public int Line { get; set; }

        internal ExceptionLog(Exception exception, string file, string member, int line)
        {
            Exception = exception;
            File = file;
            Member = member;
            Line = line;
        }

        public override string ToString() => File + ":" + Line + " + ." + Member + Environment.NewLine + Exception.ToLogString();
    }
}
