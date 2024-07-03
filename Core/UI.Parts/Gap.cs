namespace Zebble
{
    using System;
    using System.Linq;
    using Olive;

    public class Gap
    {
        public readonly Length Left, Right, Top, Bottom;

        internal Gap(View owner, Length.LengthType left, Length.LengthType right, Length.LengthType top, Length.LengthType bottom)
        {
            Left = new Length(owner, left);
            Right = new Length(owner, right);
            Top = new Length(owner, top);
            Bottom = new Length(owner, bottom);
        }

        public override string ToString() => $"{Top}, {Right}, {Bottom}, {Left}";

        internal void Dispose()
        {
            Left?.Dispose();
            Right?.Dispose();
            Top?.Dispose();
            Bottom?.Dispose();
			GC.SuppressFinalize(this);
        }

        internal void Update()
        {
            Left?.Update();
            Right?.Update();
            Top?.Update();
            Bottom?.Update();
        }

        /// <summary>
        /// Determines if any side of this gap has a non-zero gap.
        /// </summary>
        public bool HasValue()
        {
            return new[] { Left.currentValue, Right.currentValue, Top.currentValue, Bottom.currentValue }
            .Any(x => !x.AlmostEquals(0));
        }
    }

    public class GapRequest
    {
        readonly Stylesheet Sheet;
        readonly GapRequest DirectStyle;
        readonly Gap Master;
        internal Length.LengthRequest left, right, top, bottom;

        internal GapRequest(Gap master, Stylesheet sheet, GapRequest directStyle)
        {
            Sheet = sheet;
            DirectStyle = directStyle;
            Master = master;
        }

        public Length.LengthRequest Left
        {
            get => left;
            set { left = value; ((DirectStyle?.left) ?? value)?.Apply(Master.Left); }
        }

        public Length.LengthRequest Right
        {
            get => right;
            set { right = value; ((DirectStyle?.right) ?? value)?.Apply(Master.Right); }
        }

        public Length.LengthRequest Top
        {
            get => top;
            set { top = value; ((DirectStyle?.top) ?? value)?.Apply(Master.Top); }
        }

        public Length.LengthRequest Bottom
        {
            get => bottom;
            set { bottom = value; ((DirectStyle?.bottom) ?? value)?.Apply(Master.Bottom); }
        }
    }
}