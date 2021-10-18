using System;

namespace Zebble
{
    class JavaWrapper : Java.Lang.Object
    {
        object Object;

        public JavaWrapper(object obj) => Object = obj;

        public override string ToString() => Object.ToString();
    }
}