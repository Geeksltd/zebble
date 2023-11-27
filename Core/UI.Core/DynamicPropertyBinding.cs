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
            TypedSource = source ?? throw new ArgumentNullException(nameof(source));
            ValueExpression = valueExpression ?? throw new ArgumentNullException(nameof(valueExpression));
        }

        public override void Apply()
        {
            CurrentBinding?.Remove();
            CurrentBinding = TypedSource.Invoke().AddBinding(Target, PropertyName, ValueExpression);
        }
    }

    class DynamicPropertyBinding : IDisposable
    {
        protected object Target;
        protected string PropertyName;
        Func<IBindable> Source;
        IBinding CurrentBinding;

        public DynamicPropertyBinding(object target, string propertyName, Func<IBindable> source)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public virtual void Apply()
        {
            CurrentBinding?.Remove();

            if (Source is null) return;
            if (Target is null) return;

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
