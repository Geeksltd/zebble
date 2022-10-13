namespace Zebble
{
    using System;
    using Olive;

    partial class Length
    {
        public readonly AsyncEvent Changed = new();
        private bool IsSuspended;
        private bool ShouldCascade;
        private readonly bool RaisesBoundsChanged;
        float ValueAtSuspension;

        internal void Suspend()
        {
            if (IsSuspended) return;
            IsSuspended = true;
            ValueAtSuspension = currentValue;
        }

        internal void Resume()
        {
            IsSuspended = false;
            if (ShouldCascade)
            {
                ShouldCascade = false;
                if (!ValueAtSuspension.AlmostEquals(currentValue)) CascadeChanged();
            }
        }

        void CascadeChanged()
        {
            if (IsSuspended)
            {
                ShouldCascade = true;
                return;
            }

            var owner = Owner;
            if (owner is null) return;

            if (UIRuntime.IsDevMode)
                owner.StyleApplyingContext?.TrackCascade(this);

            Changed.Raise();

            if (Type == LengthType.Y || Type == LengthType.Height)
            {
                if (owner.parent is null) owner.ParentSet.HandleWith(UpdateNonStackParentHeight);
                else UpdateNonStackParentHeight();
            }

            if (RaisesBoundsChanged)
                owner.RaiseBoundsChanged();
        }
    }
}