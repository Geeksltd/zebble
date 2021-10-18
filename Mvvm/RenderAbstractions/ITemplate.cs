using System;
using Zebble.Mvvm;

namespace Zebble
{
    /// <summary>
    /// Binds this view to a singleton view model.
    /// </summary>
    public interface ITemplate { }

    /// <summary>
    /// Binds this view to a singleton view model.
    /// </summary>
    public interface ITemplate<TViewModel> : ITemplate
    {
    }

    public static class TemplateExtensions
    {
        /// <summary>
        /// Returns the singleton view model instance shown by this view.
        /// </summary>
        public static TViewModel Model<TViewModel>(this ITemplate<TViewModel> @this) where TViewModel : ViewModel
        {
            return ViewModel.The<TViewModel>();
        }
    }
}