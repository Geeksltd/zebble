using Olive;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace Zebble
{
    partial class ViewExtensions
    {
        /// <summary>
        /// Binds the specified property of this view to a bindable object.
        /// </summary> 
        public static TView Bind<TView>(this TView @this, string propertyName, Func<IBindable> bindable) where TView : View
            => @this.Bind(x => x, propertyName, bindable);

        /// <summary>
        /// Binds the specified property of this view to a bindable object.
        /// </summary> 
        public static TView Bind<TView>(this TView @this, Func<TView, object> targetExpression, string propertyName, Func<IBindable> bindable) where TView : View
        {
            @this.RegisterPropertyBinding(new DynamicPropertyBinding(targetExpression(@this), propertyName, bindable));
            return @this;
        }

        public static TView Bind<TView, TSource, TProperty>(this TView @this, string propertyName, Func<Bindable<TSource>> bindable, Func<TSource, TProperty> valueExpression) where TView : View
            => @this.Bind(x => x, propertyName, bindable, valueExpression);

        public static TView Bind<TView, TSource, TProperty>(this TView @this, Func<TView, object> targetExpression, string propertyName, Func<Bindable<TSource>> bindable, Func<TSource, TProperty> valueExpression) where TView : View
        {
            @this.RegisterPropertyBinding(new DynamicPropertyBinding<TSource, TProperty>(targetExpression(@this), propertyName, bindable, valueExpression));
            return @this;
        }
    }

    partial class View
    {
        readonly ConcurrentDictionary<string, DynamicPropertyBinding> DynamicBindings = [];

        internal void RegisterPropertyBinding(DynamicPropertyBinding definition)
        {
            if (definition is null) return;
            DynamicBindings.TryGet(definition.Key)?.Dispose();
            DynamicBindings[definition.Key] = definition;
            definition.Apply();
        }

        /// <summary>
        /// Reapplies the bindings on the properties of this view.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void RefreshBindings() => BatchStyleChange(RefreshAllBindings);

        void RefreshAllBindings()
        {
            DynamicBindings.Values.Do(x => x.Apply());

            foreach (var c in AllChildren.ToArray())
                c.RefreshAllBindings();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void SetViewModelValue(object value)
        {
            var templateType = GetType();

            var modelMember = templateType.GetPropertyOrField("Model")
                ?? throw new RenderException(templateType.GetProgrammingName() + " does not define a property or field named Model.");

            var type = modelMember.GetPropertyOrFieldType();
            if (type.IsA<IBindable>())
            {
                var model = (IBindable)modelMember.GetValue(this);
                if (model == null)
                {
                    model = (IBindable)modelMember.GetPropertyOrFieldType().CreateInstance();
                    modelMember.SetValue(this, model);
                }

                try { model.Value = value; } catch (Exception ex) { Throw(ex); }
            }
            else
            {
                try { modelMember.SetValue(this, value); } catch (Exception ex) { Throw(ex); }
            }

            void Throw(Exception ex)
            {
                throw new InvalidCastException(templateType.GetProgrammingName() +
                 $".Model is of type {type.GetProgrammingName()} and cannot be set to {value?.GetType().GetProgrammingName() ?? "NULL"}.", ex);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetViewModelValue()
        {
            var templateType = GetType();

            var modelMember = templateType.GetPropertyOrField("Model")
                ?? throw new RenderException(templateType.GetProgrammingName() + " does not define a property or field named Model.");

            var result = modelMember.GetValue(this);

            if (result is IBindable model) return model.Value;
            else return result;
        }
    }
}