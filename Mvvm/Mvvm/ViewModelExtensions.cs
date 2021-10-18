using System;
using System.Collections.Generic;
using System.Linq;
using Olive;
using Zebble.Mvvm;

namespace Zebble
{
    public static class ViewModelExtensions
    {
        /// <summary>
        /// It will create a new view model object and binds its Source to the provided object.
        /// </summary>
        public static void Add<TViewModel, TSource>(
            this CollectionViewModel<TViewModel> @this, IEnumerable<TSource> items)
            where TViewModel : ViewModel, IViewModelOf<TSource>, new()
        {
            var viewModels = items.OrEmpty().ExceptNull()
                 .Select(v => new TViewModel().Set(x => x.Source.Set(v)))
                 .ToArray();

            @this.Add(viewModels);
        }

        /// <summary>
        /// It will create a new view model object and binds its Source to the provided object.
        /// </summary>
        public static void Add<TViewModel, TSource>(
            this CollectionViewModel<TViewModel> @this, TSource item)
            where TViewModel : ViewModel, IViewModelOf<TSource>, new()
        {
            var viewModel = new TViewModel();
            viewModel.Source.Set(item);
            @this.Add(viewModel);
        }

        /// <summary>
        /// It will create a new view model object for each item and binds its Source to the provided objects.
        /// </summary>
        public static void Replace<TViewModel, TSource>(this CollectionViewModel<TViewModel> @this, IEnumerable<TSource> items)
            where TViewModel : ViewModel, IViewModelOf<TSource>, new()
        {
            var viewModels = items.OrEmpty().ExceptNull()
              .Select(v => new TViewModel().Set(x => x.Source.Set(v)))
              .ToArray();

            @this.Replace(viewModels);
        }

        /// <summary>
        /// It will create a new view model object and binds its Source to the provided object.
        /// </summary>
        public static void Replace<TViewModel, TSource>(this CollectionViewModel<TViewModel> @this, TSource item)
            where TViewModel : ViewModel, IViewModelOf<TSource>, new()
        {
            var viewModel = new TViewModel();
            viewModel.Source.Set(item);
            @this.Replace(viewModel);
        }

        public static void Source<TSource>(this IViewModelOf<TSource> @this, TSource source)
            => @this.Source.Set(source);

        public static void Source<TSource>(this IViewModelOf<TSource> @this, Bindable<TSource> source)
            => @this.Source.Set(source.Value);
    }
}
