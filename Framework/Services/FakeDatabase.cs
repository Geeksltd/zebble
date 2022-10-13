using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Olive;

namespace Zebble
{
    public abstract class FakeDataProvider
    {
        protected static readonly List<object> Everything = new();

        protected FakeDataProvider() => Initialize();

        public static Guid NextGuid() => (Everything.Count + 100).ToGuid();

        protected virtual void Initialize()
        {
            foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var item = field.GetValue(this);
                if (item is null) continue;
                if (ShouldSkip(item)) continue;
                Add(item);
            }
        }

        protected virtual bool ShouldSkip(object classMember) => false;

        public T Add<T>(T item) => item.Set(x => Everything.Add(x));

        public IEnumerable<T> GetList<T>(Func<T, bool> criteria = null)
         => Everything.OfType<T>().Where(v => criteria == null || criteria(v));

        public T FirstOrDefault<T>(Func<T, bool> criteria = null)
            => Everything.OfType<T>().FirstOrDefault(v => criteria == null || criteria(v));

        public int Count<T>(Func<T, bool> criteria = null) => GetList<T>().Count();
    }
}
