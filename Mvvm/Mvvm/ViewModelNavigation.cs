using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    partial class ViewModelNavigation
    {
        protected PageTransition Transition;
        FullScreen From;
        Func<Task> RealGo, RealBack, RealForward, RealReplace, RealShowPopup;
        Func<Task> RealHidePopup = () => Task.CompletedTask;
        ViewModel Target;

        public ViewModelNavigation(ViewModel target, PageTransition transition)
        {
            From = ViewModel.Page;
            Target = target;
            Transition = transition;

            if (target is FullScreen full)
                ViewModel.Page = full;

            ViewModel.Modal = target as ModalScreen;

            Configure();
        }

        partial void Configure();

        public void Go() => RunFullPage(RealGo);
        public void Forward() => RunFullPage(RealForward);
        public void Replace() => RunFullPage(RealReplace);
        public void Back() => RunFullPage(RealBack);

        public void HidePopUp()
        {
            var modal = ViewModel.Modal;
            ViewModel.Modal = null;
            modal?.LeaveStarted();
            RealHidePopup().ContinueWith(t => modal?.LeaveCompleted());
        }

        public void ShowPopUp()
        {
            ViewModel.NavAnimationStarted.Set(LocalTime.UtcNow);
            Target.NavigationStarted();

            Task.Delay(1000 / 60).ContinueWith(async t =>
            {
                if (RealShowPopup is not null) await RealShowPopup();
                await Task.Factory.StartNew(Target.NavigationStartedAsync);

                Target.NavigationCompleted();
                await Target.NavigationCompletedAsync();
            });
        }

        void RunFullPage(Func<Task> method)
        {
            if (From == Target) return;

            ViewModel.NavAnimationStarted.Set(LocalTime.UtcNow);

            From?.LeaveStarted();
            Target.NavigationStarted();
            var asyncStarted = Task.Factory.StartNew(Target.NavigationStartedAsync);

            Task.Delay(1000 / 60).ContinueWith(async t =>
            {
                if (method != null) await method();
                await asyncStarted;

                From?.LeaveCompleted();
                Target.NavigationCompleted();
                await Target.NavigationCompletedAsync();
            });
        }
    }
}