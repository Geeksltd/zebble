using System.Collections.Generic;

namespace System.Collections.Concurrent
{
    class EmptyEnumerator<T> : IEnumerator<T>
    {
        public static EmptyEnumerator<T> Instance = new EmptyEnumerator<T>();

        public T Current => default(T);
        object IEnumerator.Current => null;
        bool IEnumerator.MoveNext() => false;

        void IEnumerator.Reset() { }

        void IDisposable.Dispose() { }
    }
}