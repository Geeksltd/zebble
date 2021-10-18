using System;
using Olive;

namespace Zebble
{
    class DynamicPropertyBinding<TSource, TProperty> : DynamicPropertyBinding
    {
        Func<Bindable<TSource>> TypedSource;
        Func<TSource, TProperty> ValueExpression;
        IBinding<TSource> CurrentBinding;

        public DynamicPropertyBinding(View view, string propertyName, Func<Bindable<TSource>> source, Func<TSource, TProperty> valueExpression)
            : base(view, propertyName, source)
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

    class DynamicPropertyBinding
    {
        public View Target;
        public string PropertyName;
        Func<IBindable> Source;
        IBinding CurrentBinding;

        public DynamicPropertyBinding(View view, string propertyName, Func<IBindable> source)
        {
            Target = view;
            PropertyName = propertyName;
            Source = source;
        }

        public virtual void Apply()
        {
            CurrentBinding?.Remove();
            CurrentBinding = Source.Invoke().AddBinding(Target, PropertyName);
        }
    }
}
