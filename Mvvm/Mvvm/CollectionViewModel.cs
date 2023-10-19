using Olive;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Zebble.Mvvm
{
    interface ICollectionViewModel : IEnumerable { }

    public class CollectionViewModel<TViewModel> : BindableCollection<TViewModel>, ICollectionViewModel where TViewModel : ViewModel
    {
    }

    public static class BindableExtensions
    {
        static readonly ConcurrentDictionary<string, IBindable> Dependents = new();

        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this Bindable<TValue> @this, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            return @this.Get<TValue, TTarget, TViewModel>(null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this Bindable<TValue> @this, string uniqueIdentifier, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            string key = $"{callerFile}|{callerLine}|{uniqueIdentifier}";
            return (CollectionViewModel<TViewModel>)Dependents.GetOrAdd(key, delegate
            {
                var collectionBindable = new CollectionViewModel<TViewModel>();
                collectionBindable.Bind(@this, source => valueProvider(source).Select(v => new TViewModel().Set(x => x.Source.Set(v))).ToList());
                return collectionBindable;
            });
        }
    }
}