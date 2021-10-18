namespace System
{
    partial class ZebbleExtensions
    {
        /// <summary>
        /// Performs a specified action on this item if it is not null. If it is null, it simply ignores the action.
        /// </summary>
        [Diagnostics.DebuggerStepThrough]
        public static void Perform<T>(this T item, Action<T> action) where T : class
        {
            if (item != null) action?.Invoke(item);
        }

        /// <summary>
        /// Gets a specified member of this object. If this is null, null will be returned. Otherwise the specified expression will be returned.
        /// </summary>
        [Diagnostics.DebuggerStepThrough]
        public static K Get<T, K>(this T item, Func<T, K> selector)
        {
            if (item is null) return default;

            try { return selector(item); }
            catch (NullReferenceException) { return default; }
        }

        [Diagnostics.DebuggerStepThrough]
        public static T Set<T>(this T entity, Action<T> action)
        {
            if (entity == null) return entity;
            action(entity);
            return entity;
        }
    }
}