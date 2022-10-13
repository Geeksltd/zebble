namespace System.Collections.Generic
{
    public class Pair
    {
        public static KeyValuePair<TKey, TValue> Of<TKey, TValue>(TKey key, TValue value) => new(key, value);
    }
}