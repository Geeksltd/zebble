using System;
using Olive;

namespace Zebble
{
    class DynamicPropertyBinding<TSource, TProperty> : DynamicPropertyBinding
    {
        readonly Func<Bindable<TSource>> TypedSource;
        readonly Func<TSource, TProperty> ValueExpression;
        IBinding<TSource> CurrentBinding;

        public DynamicPropertyBinding(object target, string propertyName, Func<Bindable<TSource>> source, Func<TSource, TProperty> valueExpression)
            : base(target, propertyName, source)
        {
            TypedSource = source;
            ValueExpression = valueExpression;
        }

        public override void Apply()
        {
            CurrentBinding?.Remove();
            CurrentBinding = TypedSource.Invoke().AddBinding(Target, PropertyName, ValueExpression);
        }
    }

    class DynamicPropertyBinding : IDisposable
    {
        public object Target;
        public string PropertyName;
        Func<IBindable> Source;
        IBinding CurrentBinding;

        public DynamicPropertyBinding(object target, string propertyName, Func<IBindable> source)
        {
            Target = target;
            PropertyName = propertyName;
            Source = source;
        }

        public virtual void Apply()
        {
            CurrentBinding?.Remove();
            CurrentBinding = Source.Invoke().AddBinding(Target, PropertyName);
        }

        public void Dispose()
        {
            CurrentBinding?.Remove();
            Source = null;
            CurrentBinding = null;
            Target = null;
        }
    }
}
