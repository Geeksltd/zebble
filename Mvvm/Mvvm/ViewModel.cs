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
                var bindable = bindableProperty.GetValue(this) as Bindable;
                if (bindable is null) continue;
                var changedEvent = bindable.GetType().GetEvent("Changed");
                if (changedEvent is null) continue;
                changedEvent.AddEventHandler(bindable, () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(bindableProperty.Name)));
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public EventHandler Call(string name)
        {
            var method = GetType().GetMethod(name) ?? throw new Exception("No method found named: " + name);
            return new EventHandler((sender, args) => method.Invoke(this, null));
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