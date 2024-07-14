using Olive;
using System;
using System.Collections;
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
        public static BindableCollection<TTarget> Get<TValue, TTarget>(this Bindable<TValue> @this, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return Get(@this, null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static BindableCollection<TTarget> Get<TValue, TTarget>(this Bindable<TValue> @this, string uniqueIdentifier, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return @this.GetDependent(uniqueIdentifier, _ =>
            {
                var collectionBindable = new BindableCollection<TTarget>();
                collectionBindable.Bind(@this, source => valueProvider(source).ToList());
                return collectionBindable;
            }, callerFile, callerLine);
        }

        public static BindableCollection<TTarget> Get<TValue, TTarget>(this BindableCollection<TValue> @this, Func<IEnumerable<TValue>, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return Get(@this, null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static BindableCollection<TTarget> Get<TValue, TTarget>(this BindableCollection<TValue> @this, string uniqueIdentifier, Func<IEnumerable<TValue>, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
        {
            return @this.GetDependent(uniqueIdentifier, _ =>
            {
                var collectionBindable = new BindableCollection<TTarget>();
                collectionBindable.Bind(@this, source => valueProvider(source).ToList());
                return collectionBindable;
            }, callerFile, callerLine);
        }

        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this Bindable<TValue> @this, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            return @Get<TValue, TTarget, TViewModel>(@this, null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this Bindable<TValue> @this, string uniqueIdentifier, Func<TValue, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            return @this.GetDependent(uniqueIdentifier, _ =>
            {
                var collectionBindable = new CollectionViewModel<TViewModel>();
                collectionBindable.Bind(@this, source => valueProvider(source).Select(v => new TViewModel().Set(x => x.Source.Set(v))).ToList());
                return collectionBindable;
            }, callerFile, callerLine);
        }

        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this BindableCollection<TValue> @this, Func<IEnumerable<TValue>, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            return @this.Get<TValue, TTarget, TViewModel>(null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static CollectionViewModel<TViewModel> Get<TValue, TTarget, TViewModel>(this BindableCollection<TValue> @this, string uniqueIdentifier, Func<IEnumerable<TValue>, IEnumerable<TTarget>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel, IViewModelOf<TTarget>, new()
        {
            return @this.GetDependent(uniqueIdentifier, _ =>
            {
                var collectionBindable = new CollectionViewModel<TViewModel>();
                collectionBindable.Bind(@this, source => valueProvider(source).Select(v => new TViewModel().Set(x => x.Source.Set(v))).ToList());
                return collectionBindable;
            }, callerFile, callerLine);
        }

        public static CollectionViewModel<TViewModel> Get<TViewModel>(this CollectionViewModel<TViewModel> @this, Func<IEnumerable<TViewModel>, IEnumerable<TViewModel>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel
        {
            return Get(@this, null, valueProvider, callerFile, callerLine);
        }

        //
        // Summary:
        //     Returns a durable unique nested Bindable whose value remains in sync with this
        //     instance.
        public static CollectionViewModel<TViewModel> Get<TViewModel>(this CollectionViewModel<TViewModel> @this, string uniqueIdentifier, Func<IEnumerable<TViewModel>, IEnumerable<TViewModel>> valueProvider, [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0) where TViewModel : ViewModel
        {
            return @this.GetDependent(uniqueIdentifier, _ =>
            {
                var collectionBindable = new CollectionViewModel<TViewModel>();
                collectionBindable.Bind(@this, source => valueProvider(source).ToList());
                return collectionBindable;
            }, callerFile, callerLine);
        }
    }
}