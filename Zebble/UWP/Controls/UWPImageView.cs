namespace Zebble.UWP
{
    using System;
    using System.Threading.Tasks;
    using controls = Windows.UI.Xaml.Controls;
    using media = Windows.UI.Xaml.Media;
    using xaml = Windows.UI.Xaml;

    class UWPImageView : IRenderOrchestrator
    {
        ImageView View;
        controls.Border Result;
        readonly EventHandlerDisposer EventHandlerDisposer = new EventHandlerDisposer();

        public UWPImageView(ImageView view) => View = view;

        public async Task<xaml.FrameworkElement> Render()
        {
            Result = new controls.Border();

            View.BackgroundImageChanged.HandleOnUI(LoadImageAsync);
            View.BackgroundImageParametersChanged.HandleOnUI(LoadImageAsync);
            View.PaddingChanged.HandleOnUI(PaddingChanged);
            View.BorderChanged.HandleOnUI(BorderChanged);

            if (Services.ImageService.ShouldMemoryCache(View.Path)) LoadImageAsync();
            else Result.Loading += Result_Loading;

            PaddingChanged();
            BorderChanged();

            return Result;
        }

        void Result_Loading(xaml.FrameworkElement _, object __) => LoadImageAsync();

        void BorderChanged()
        {
            if (!IsDisposing())
                Result.CornerRadius = View.Effective.RenderCornerRadius();
        }

        void PaddingChanged()
        {
            if (!IsDisposing())
                Result.Margin = View.Padding.RenderThickness();
        }

        bool IsDisposing() => View?.IsDisposing != false;

        void LoadImageAsync()
        {
            EventHandlerDisposer.DisposeAll();
            if (IsDisposing()) return;
            Services.ImageService.Draw(View, DrawImage);
        }

        void DrawImage(object imageObj)
        {
            if (IsDisposing()) return;

            Result.Background = new media.ImageBrush
            {
                ImageSource = imageObj as media.Imaging.BitmapImage,
                Stretch = View.Stretch.Render(),
                AlignmentX = View.Alignment.RenderX(),
                AlignmentY = View.Alignment.RenderY()
            };
        }

        public void Dispose()
        {
            Result.Set(x => x.Loading -= Result_Loading);
            EventHandlerDisposer.DisposeAll();
            View = null;
            Result = null;
        }
    }
}