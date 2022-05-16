using Java.Lang;

namespace System
{
    public static class ExceptionExtensions
    {
        public static Throwable Render(this Exception exception) => Throwable.FromException(exception);
    }
}