using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    public abstract partial class ViewModel : INotifyPropertyChanged
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event PropertyChangedEventHandler PropertyChanged;

        protected ViewModel()
        {
            var bindables = GetType()
                .GetPropertiesAndFields(BindingFlags.Public | BindingFlags.Instance)
               .Where(c => c.GetPropertyOrFieldType().IsA<Bindable>()).ToArray();

            foreach (var bindableProperty in bindables)
            {
                dynamic bindable = bindableProperty.GetValue(this);
                Action apply = () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(bindableProperty.Name));
                bindable.Changed += apply;
            }
        }

        protected internal virtual Task EagerLoad() => Task.CompletedTask;

        #region Hide default members

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        #endregion
    }

    public interface IViewModelOf<TSource>
    {
        Bindable<TSource> Source { get; }
    }

    public abstract class ViewModel<TSource> : ViewModel, IViewModelOf<TSource>
    {
        /// <summary>
        /// Gets the source object for this view model.
        /// It's a shortcut to defining a bindable data source.
        /// </summary>
        public Bindable<TSource> Source { get; private set; } = new Bindable<TSource>();
    }
}