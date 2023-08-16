using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    /// <summary>
    /// This will handle 
    /// </summary>
    public class Overlay : Canvas
    {
        string LatestCommand = nameof(Hide);

        Animation ShowingAnimation, HidingAnimation;
        public static float VisibleOpacity = 0.35f;
        public readonly static Overlay Default = new();

        public Overlay()
        {
            Id = "Overlay";
            BlockGestures();
            Opacity = 0;
            AutoFlash = false;
            Width.BindTo(Root.Width);
            Css.backgroundColor = "#333";
            Height.BindTo(Root.Height);
            Absolute = true;
        }

        public bool BlocksGestures { get; set; } = true;

        public override async Task OnPreRender()
        {
            await base.OnPreRender();
            if (BlocksGestures) BlockGestures();
        }

        public async Task Show()
        {
            if (parent == null) await Root.Add(this);

            if (LatestCommand == nameof(Show)) return;
            else LatestCommand = nameof(Show);

            var ani = HidingAnimation;
            if (ani != null) await ani.Task;

            await BringToFront();

            if (Opacity.AlmostEquals(VisibleOpacity))
            {
                return;
            }

            ShowingAnimation = Animation.Create(this, x => x.Opacity(VisibleOpacity))
                .Easing(AnimationEasing.Linear)
                .Duration(Animation.FadeDuration)
                .OnCompleted(() => ShowingAnimation = null);

            Thread.UI.Run(() => this.Animate(ShowingAnimation).RunInParallel()).RunInParallel();
        }

        public async Task Hide()
        {
            if (parent == null) return;
            if (LatestCommand == nameof(Hide) && Opacity == 0)
            {
                await SendToBack();
                return;
            }

            LatestCommand = nameof(Hide);

            var ani = ShowingAnimation;
            if (ani != null) await ani.Task;

            if (Opacity.AlmostEquals(0))
            {
                await SendToBack();
                return;
            }

            HidingAnimation = Animation.Create(this, x => x.Opacity(0))
              .Easing(AnimationEasing.Linear)
              .Duration(Animation.FadeDuration)
              .OnCompleted(() =>
              {
                  if (LatestCommand == nameof(Hide))
                  {
                      SendToBack();
                  }
                  HidingAnimation = null;
              });

            this.Animate(HidingAnimation).RunInParallel();
        }
    }
}