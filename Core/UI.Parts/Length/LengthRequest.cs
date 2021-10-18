using System;

namespace Zebble
{
    public partial class Length
    {
        public abstract partial class LengthRequest
        {
            internal abstract void Apply(Length on);

            public static implicit operator LengthRequest(float value) => new FixedLengthRequest(value);
            public static implicit operator LengthRequest(int value) => new FixedLengthRequest(value);
            public static implicit operator LengthRequest(double value) => new FixedLengthRequest((float)value);
            public static implicit operator LengthRequest(AutoStrategy value) => new AutoLengthRequest(value);
        }

        public class FixedLengthRequest : LengthRequest
        {
            public float Value;
            public FixedLengthRequest(float value) => Value = value;

            internal override void Apply(Length on) => on.Set(Value);

            public static implicit operator FixedLengthRequest(float value) => new FixedLengthRequest(value);
            public static implicit operator FixedLengthRequest(int value) => new FixedLengthRequest(value);
            public static implicit operator FixedLengthRequest(double value) => new FixedLengthRequest((float)value);

            public override string ToString() => Value + "px";
        }

        public class PercentageLengthRequest : LengthRequest
        {
            public float Percent;
            public PercentageLengthRequest(float percent) => Percent = percent;
            internal override void Apply(Length on) => on.Set(Percent.Percent());

            public override string ToString() => Percent + "%";
        }

        public class AutoLengthRequest : LengthRequest
        {
            public AutoStrategy Strategy;
            public AutoLengthRequest(AutoStrategy strategy) => Strategy = strategy;
            internal override void Apply(Length on) => on.Set(Strategy);

            public static implicit operator AutoLengthRequest(AutoStrategy value) => new AutoLengthRequest(value);

            public override string ToString() => "Auto from " + Strategy;
        }

        public class BindingLengthRequest : LengthRequest
        {
            public Length[] Lengths;
            public Func<float[], float> Expression;

            public BindingLengthRequest(Func<float> expression) : this(EmptyLengths, x => expression()) { }

            public BindingLengthRequest(Length another) : this(another, x => x) { }

            public BindingLengthRequest(Length another, Func<float, float> expression)
                : this(new[] { another }, x => expression(x[0])) { }

            public BindingLengthRequest(Length l1, Length l2, Func<float, float, float> expression)
                : this(new[] { l1, l2 }, values => expression(values[0], values[1])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Func<float, float, float, float> expression)
                : this(new[] { l1, l2, l3 }, values => expression(values[0], values[1], values[2])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Length l4,
                Func<float, float, float, float, float> expression)
            : this(new[] { l1, l2, l3, l4 }, values => expression(values[0], values[1], values[2], values[3])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Length l4, Length l5,
                Func<float, float, float, float, float, float> expression)
            : this(new[] { l1, l2, l3, l4, l5 },
              values => expression(values[0], values[1], values[2], values[3], values[4]))
            { }

            #region Double to float
            public BindingLengthRequest(Func<double> expression) : this(EmptyLengths, x => (float)expression()) { }

            public BindingLengthRequest(Length another, Func<float, double> expression)
                : this(new[] { another }, x => (float)expression(x[0])) { }

            public BindingLengthRequest(Length l1, Length l2, Func<float, float, double> expression)
                : this(new[] { l1, l2 }, values => (float)expression(values[0], values[1])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Func<float, float, float, double> expression)
                : this(new[] { l1, l2, l3 }, values => (float)expression(values[0], values[1], values[2])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Length l4,
                Func<float, float, float, float, double> expression)
            : this(new[] { l1, l2, l3, l4 }, values => (float)expression(values[0], values[1], values[2], values[3])) { }

            public BindingLengthRequest(Length l1, Length l2, Length l3, Length l4, Length l5,
                Func<float, float, float, float, float, double> expression)
            : this(new[] { l1, l2, l3, l4, l5 },
              values => (float)expression(values[0], values[1], values[2], values[3], values[4]))
            { }
            #endregion

            public BindingLengthRequest(Length[] lengths, Func<float[], float> expression)
            {
                Lengths = lengths;
                Expression = expression;
            }

            internal override void Apply(Length on) => on.BindTo(Lengths, Expression);

            public override string ToString() => "BindingLengthRequest -> " + Lengths.Length;
        }
    }
}