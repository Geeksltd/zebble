namespace Zebble.Mvvm
{
    using Olive;

    public abstract class FullScreen : ViewModel
    {
    }

    public abstract class FullScreen<TSource> : FullScreen, IViewModelOf<TSource>
    {
        /// <summary>
        /// Gets the source object for this view model.
        /// It's a shortcut to defining a bindable data source.
        /// </summary>
        public Bindable<TSource> Source { get; private set; } = new Bindable<TSource>();
    }
}