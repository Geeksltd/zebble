using System;
using System.Collections.Concurrent;
using Olive;

namespace Zebble.Mvvm
{
    partial class ViewModel
    {
        static ConcurrentDictionary<Type, ViewModel> SingletonInstances = new ConcurrentDictionary<Type, ViewModel>();

        /// <summary>
        /// Returns the current instance of the specified view model.
        /// </summary> 
        public static TViewModel The<TViewModel>() where TViewModel : ViewModel
        {
            return (TViewModel)The(typeof(TViewModel));
        }

        /// <summary>
        /// Returns the current instance of the specified view model type.
        /// </summary> 
        public static ViewModel The(Type viewmodelType)
        {
            return SingletonInstances.GetOrAdd(viewmodelType, t => t.CreateInstance<ViewModel>());
        }
    }
}