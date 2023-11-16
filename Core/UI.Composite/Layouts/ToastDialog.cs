namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    class ToastDialog : Canvas
    {
        public readonly static AsyncEvent<string> ToastShown = new();
        public readonly Stack MessageContainer = new(RepeatDirection.Horizontal);
        public readonly TextView Label = new() { Id = "Label" };
        const int DROP_RANGE = 30;

        static readonly ConcurrentList<string> CurrentToastMessages = new();

        public ToastDialog(string message) => Message = message;

        public string Message { get; set; }

        public TimeSpan Duration { get; set; } = 2.Seconds();

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await Add(MessageContainer);

            await MessageContainer.Add(Label.Set(x => x.Text = Message));

            this.On(x => x.Tapped, Hide);
        }

        public async Task Show()
        {
            if (CurrentToastMessages.Contains(Message)) return;

            CurrentToastMessages.Add(Message);
            await ToastShown.Raise(Message);

            try
            {
                Nav.NavigationAnimationStarted.Handle(BringToFront);

                await WhenShown(BringToFront);

                var ani = await Root.AddWithAnimation(this,
                       initialState: l => l.Y(-DROP_RANGE).Opacity(0),
                        change: l => l.Y(Margin.Top() + Device.Screen.SafeAreaInsets.Top).Opacity(1));

                await ani.Task;

                await Task.Delay(Duration);
                await SendToBack();

                await Hide();
            }
            finally
            {
                CurrentToastMessages.Remove(Message);
            }
        }

        public async Task Hide()
        {
            await this.Animate(l => l.Y(-DROP_RANGE).Opacity(0));
            await Root.Remove(this);
            Nav.NavigationAnimationStarted.RemoveHandler(BringToFront);
        }
    }
}