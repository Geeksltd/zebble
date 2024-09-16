﻿namespace Zebble.Mvvm
{
    using Olive;

    public abstract partial class ModalScreen : ViewModel
    {
    }

    public abstract class ModalScreen<TSource> : ModalScreen, IViewModelOf<TSource>
    {
        /// <summary>
        /// Gets the source object for this view model.
        /// It's a shortcut to defining a bindable data source.
        /// </summary>
        public Bindable<TSource> Source { get; private set; } = new Bindable<TSource>();
    }
}