namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    partial class Nav
    {
        internal class Transitor
        {
            Animation EnterAnimation, ExitAnimation;
            TimeSpan Duration = Animation.DefaultDuration;
            readonly View ParentView, FromView, ToView;
            readonly PageTransition Transition;
            readonly TaskCompletionSource<bool> ExitAnimationCompleted = new();
            readonly TaskCompletionSource<bool> EnterAnimationCompleted = new();

            public Transitor(View parentView, View fromView, View toView, PageTransition transition)
            {
                ParentView = parentView;
                FromView = fromView;
                ToView = toView;
                Transition = transition;
            }

            public async Task Run()
            {
                UIWorkBatch.Current?.Flush();

                switch (Transition)
                {
                    case PageTransition.SlideForward: await SlideForward(); break;
                    case PageTransition.SlideBack: await SlideBack(); break;
                    case PageTransition.Fade: await Fade(); break;
                    case PageTransition.SlideUp: await SlideUp(); break;
                    case PageTransition.SlideDown: await SlideDown(); break;
                    case PageTransition.DropUp: await DropUp(); break;
                    case PageTransition.DropDown: await DropDown(); break;
                    case PageTransition.None:
                    default: await None(); break;
                }

                UIWorkBatch.Current?.Flush();

                if (FromView is Page fp && ToView is Page tp)
                {
                    NavigationEventArgs navArgs;

                    if (ParentView is PopUp popUp)
                    {
                        if (FromView is PopUp) navArgs = new NavigationEventArgs(popUp.HostPage, popUp);
                        else navArgs = new NavigationEventArgs(popUp, popUp.HostPage);
                    }
                    else navArgs = new NavigationEventArgs(fp, tp);

                    if (EnterAnimation != null)
                        EnterAnimation.OnNativeStart(() => NavigationAnimationStarted.Raise(navArgs));
                    else if (ExitAnimation != null)
                        ExitAnimation.OnNativeStart(() => NavigationAnimationStarted.Raise(navArgs));
                    else await NavigationAnimationStarted.Raise(navArgs);
                }

                await Await().ConfigureAwait(continueOnCapturedContext: false);
            }

            async Task<Animation> AnimatePage(Action<View> initial, Action<View> change = null)
            {
                return await ParentView.AddWithAnimation(ToView, Duration, initial, change).ConfigureAwait(false);
            }

            async Task SlideForward()
            {
                if (ToView != ParentView)
                {
                    EnterAnimation = await AnimatePage(m => m.Y(0).X(Device.Screen.Width), m => m.X(0)).ConfigureAwait(false);
                }

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, x => x.X(-Device.Screen.Width));
            }

            async Task SlideBack()
            {
                if (ToView != ParentView)
                    EnterAnimation = await AnimatePage(m => m.Y(0).X(-Device.Screen.Width), m => m.X(0));

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, x => x.X(Device.Screen.Width));
            }

            async Task Fade()
            {
                if (ToView != ParentView)
                {
                    Duration = Animation.FadeDuration;
                    EnterAnimation = await AnimatePage(v => v.Opacity(0).X(0).Y(0), v => v.Opacity(1));
                }
            }

            async Task SlideUp()
            {
                if (ToView != ParentView)
                {
                    EnterAnimation = await AnimatePage(m => m.X(0).Y(Device.Screen.Height).Opacity(1), m => m.Y(0));
                }

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, x => x.Y(-Device.Screen.Height));
            }

            async Task SlideDown()
            {
                if (ToView != ParentView)
                {
                    EnterAnimation = await AnimatePage(m => m.X(0).Y(-Device.Screen.Height).Opacity(1), m => m.Y(0));
                }

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, x => x.Y(Device.Screen.Height));
            }

            async Task None()
            {
                if (ToView == ParentView) return;                

                Duration = TimeSpan.Zero;
                EnterAnimation = await AnimatePage(x => x.X(0).Y(0), x => x.X(0));
            }

            async Task DropUp()
            {
                if (ToView != ParentView)
                {
                    Duration = Animation.DropDuration;
                    void initial(View x)
                    {
                        x.Opacity(0);
                        x.PreRendered.Handle(() => x.Y(x.Margin.Top() + DROP_RANGE));
                        x.Y(x.Margin.Top() + DROP_RANGE);
                    }

                    void change(View x) => x.Opacity(1).Y(x.Margin.Top());
                    EnterAnimation = await AnimatePage(initial, change);
                }

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, Animation.DropDuration,
                        x => x.Opacity(0).Y(x.Margin.Top() + DROP_RANGE));
            }

            async Task DropDown()
            {
                if (ToView != ParentView)
                {
                    Duration = Animation.DropDuration;
                    void initial(View x) { x.Opacity(0).PreRendered.Handle(() => x.Y(x.Margin.Top() - DROP_RANGE)); }
                    EnterAnimation = await AnimatePage(
                        initial,
                        x => x.Opacity(1).Y(x.Margin.Top()));
                }

                if (FromView != ParentView)
                    ExitAnimation = Animation.Create(FromView, Animation.DropDuration,
                        x => x.Opacity(0).Y(x.Margin.Top() - DROP_RANGE));
            }

            async Task Await()
            {
                if (ExitAnimation == null) ExitAnimationCompleted.TrySetResult(true);
                else ExitAnimation.OnCompleted(() => ExitAnimationCompleted.TrySetResult(true));

                if (EnterAnimation == null) EnterAnimationCompleted.TrySetResult(true);
                else EnterAnimation.OnCompleted(() => EnterAnimationCompleted.TrySetResult(true));

                if (ExitAnimation != null)
                {
                    if (EnterAnimation == null) FromView.StartAnimation(ExitAnimation);
                    else EnterAnimation.OnNativeStart(() => FromView.StartAnimation(ExitAnimation));
                }

                await Task.WhenAll(EnterAnimationCompleted.Task, ExitAnimationCompleted.Task);
            }
        }
    }
}