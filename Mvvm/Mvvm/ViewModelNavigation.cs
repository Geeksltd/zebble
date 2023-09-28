using System;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    partial class ViewModelNavigation
    {
        protected PageTransition Transition;
        readonly FullScreen From;
        Func<Task> RealGo, RealBack, RealForward, RealReplace, RealShowPopup;
        Func<Task> RealHidePopup = () => Task.CompletedTask;
        readonly ViewModel Target;

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

        public Task Go() => RunFullPage(RealGo);

        public Task Forward() => RunFullPage(RealForward);

        public Task Replace() => RunFullPage(RealReplace);

        public Task Back() => RunFullPage(RealBack);

        public async Task HidePopUp()
        {
            var modal = ViewModel.Modal;
            ViewModel.Modal = null;

            modal?.LeaveStarted();

            await RealHidePopup();

            modal?.LeaveCompleted();
        }

        public async Task ShowPopUp()
        {
            ViewModel.NavAnimationStarted.Set(LocalTime.UtcNow);

            Target.NavigationStarted();
            await Target.NavigationStartedAsync();

            if (RealShowPopup is not null) await RealShowPopup();

            Target.NavigationCompleted();
            await Target.NavigationCompletedAsync();
        }

        async Task RunFullPage(Func<Task> method)
        {
            if (From == Target) return;

            ViewModel.NavAnimationStarted.Set(LocalTime.UtcNow);

            From?.LeaveStarted();
            Target.NavigationStarted();
            await Target.NavigationStartedAsync();

            if (method != null) await method();

            From?.LeaveCompleted();
            Target.NavigationCompleted();
            await Target.NavigationCompletedAsync();
        }
    }
}