namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Olive;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPopupWithResult { void SetDefaultResult(); }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPopupWithResult<TResult> : IPopupWithResult
    {
        TaskCompletionSource<TResult> ResultTask { get; }
        void SetResult(TResult result);
    }

    public class PopUp<TView, TResult> : PopUp<TView>, IPopupWithResult<TResult>
        where TView : View
    {
        public TaskCompletionSource<TResult> ResultTask { get; private set; } = new TaskCompletionSource<TResult>();
        public PopUp(TView view, Page hostPage) : base(view, hostPage) { }

        void IPopupWithResult.SetDefaultResult()
        {
            ResultTask.TrySetResult(default(TResult));
            ResultTask = new TaskCompletionSource<TResult>();
        }

        void IPopupWithResult<TResult>.SetResult(TResult result)
        {
            ResultTask.TrySetResult(result);
            ResultTask = new TaskCompletionSource<TResult>();
        }
    }

    public class PopUp<TView> : PopUp where TView : View
    {
        public TView View;

        public PopUp(TView view, Page hostPage) : base(hostPage) => View = view;

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            OnRevisited.Handle(Revisited);
            OnRevisiting.Handle(Revisiting);
            await WhenShown(OnShown);
        }

        public override View GetView() => View;

        public override string ToString() => $"PopUp<{View}>";

        public override async Task OnPreRender()
        {
            await base.OnPreRender();
            await Overlay.Default.Show();
        }

        Task Revisiting(RevisitingEventArgs args) => ((View as Page)?.OnRevisiting?.Raise(args)).OrCompleted();

        async Task Revisited(RevisitingEventArgs args)
        {
            await ((View as Page)?.OnRevisited?.Raise(args)).OrCompleted();
            await ShowRevisited();
        }

        async Task ShowRevisited()
        {
            await ShowFully();
            UIWorkBatch.Current?.Flush();

            await new Nav.Transitor(this, this, View, Transition).Run();

            IsFullyVisible = true;
             
            await Nav.Navigated.Raise(new NavigationEventArgs(HostPage, this));
        }

        async Task OnShown()
        {
            await BringToFront();

            await new Nav.Transitor(this, this, View, Transition).Run();

            await ShowFully();
            IsFullyVisible = true;
             
            await Nav.Navigated.Raise(new NavigationEventArgs(HostPage, this));
        }

        async Task ShowFully()
        {
            Visible = true;
            await Overlay.Default.Show();
            await Overlay.Default.BringToFront();
            await BringToFront();
        }
    }

    public abstract class PopUp : Page
    {
        internal bool IsFullyVisible;

        public Page HostPage { get; internal set; }

        protected PopUp(Page hostPage) => HostPage = hostPage;

        public abstract View GetView();

        protected override bool ShouldDisposeWhenRemoved() => true;

        internal async Task Hide(PageTransition transition)
        {
            IsFullyVisible = false;
            var animateAway = new Nav.Transitor(this, GetView(), this, transition).Run();

            await Task.WhenAll(Overlay.Default.Hide(), animateAway);

            if (CacheViewAttribute.IsCacheable(this))
            {
                await SendToBack();
                Visible = false;
            }
            else
            {
                await this.RemoveSelf();
            }
        }
    }
}