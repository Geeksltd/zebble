using System;

namespace Zebble
{
    public class InvalidStateException : Exception
    {
        public InvalidStateException(string error) : base(error) { }
    }

    public class RenderException : Exception
    {
        public RenderException(string error) : base(error) { }
        public RenderException(string error, Exception inner) : base(error, inner) { }
    }
    public class BadDataException : Exception
    {
        public BadDataException(string error) : base(error) { }
        public BadDataException(string error, Exception inner) : base(error, inner) { }
    }
}