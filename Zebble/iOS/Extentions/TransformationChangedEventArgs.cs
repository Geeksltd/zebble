using CoreAnimation;
using Olive;

namespace Zebble
{
    partial class TransformationChangedEventArgs
    {
        public CATransform3D Render()
        {
            var result = CATransform3D.Identity;

            result = result.Rotate(RotateX.ToRadians(), 1, 0, 0);
            result = result.Rotate(RotateY.ToRadians(), 0, 1, 0);
            result = result.Rotate(RotateZ.ToRadians(), 0, 0, 1);

            return result.Scale(ScaleX, ScaleY, 1);
        }
    }
}
