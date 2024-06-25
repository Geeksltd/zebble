using System;
using Olive;

namespace Zebble
{
    class DynamicPropertyBinding<TSource, TProperty> : DynamicPropertyBinding
    {
        Func<Bindable<TSource>> TypedSource;
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

            if (TypedSource is null) Dispose();
            else if (Target is null) Dispose();
            else CurrentBinding = TypedSource.Invoke().AddBinding(Target, PropertyName, ValueExpression);
        }

        public override void Dispose()
        {
            TypedSource = null;
            base.Dispose();
        }
    }

    class DynamicPropertyBinding : IEquatable<DynamicPropertyBinding>, IDisposable
    {
        protected object Target;
        protected string PropertyName;

        Func<IBindable> Source;
        IBinding CurrentBinding;

        public string Key => Target.GetHashCode() + PropertyName;

        public DynamicPropertyBinding(object target, string propertyName, Func<IBindable> source)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public virtual void Apply()
        {
            CurrentBinding?.Remove();

            if (Source is null) Dispose();
            else if (Target is null) Dispose();
            else CurrentBinding = Source.Invoke().AddBinding(Target, PropertyName);
        }

        public virtual void Dispose()
        {
            CurrentBinding?.Remove();
            Source = null;
            CurrentBinding = null;
            Target = null;
        }

        public static bool operator ==(DynamicPropertyBinding @this, DynamicPropertyBinding that)
            => @this.Equals(that);

        public static bool operator !=(DynamicPropertyBinding @this, DynamicPropertyBinding that)
            => @this.Equals(that) == false;

        public bool Equals(DynamicPropertyBinding other)
        {
            if (other is null) return false;
            return Target == other.Target && PropertyName == other.PropertyName;
        }
    }
}
