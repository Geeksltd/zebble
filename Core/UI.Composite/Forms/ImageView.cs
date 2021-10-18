namespace Zebble
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public class ImageView : View, FormField.IControl, IAutoContentHeightProvider, IAutoContentWidthProvider
    {
        readonly AsyncEvent AutoContentWidthChanged = new AsyncEvent();
        readonly AsyncEvent AutoContentHeightChanged = new AsyncEvent();

        public ImageView() : base()
        {
            Width.Set(Length.AutoStrategy.Container).UpdateOn(BackgroundImageChanged);
            Height.Set(30).UpdateOn(BackgroundImageChanged, Width.Changed);
            Height?.Update();
        }

        string failedPlaceholderImagePath;
        public string FailedPlaceholderImagePath
        {
            get => failedPlaceholderImagePath;
            set
            {
                Services.ImageService.SetFailedPlaceholderImagePath(value);
                failedPlaceholderImagePath = value;
            }
        }

        public string Path
        {
            get => BackgroundImagePath;
            set => Style.BackgroundImagePath = value;
        }

        public byte[] ImageData
        {
            get => BackgroundImageData;
            set => Style.BackgroundImageData = value;
        }

        public Alignment Alignment
        {
            get => BackgroundImageAlignment;
            set => Style.BackgroundImageAlignment = value;
        }

        public Stretch Stretch
        {
            get => BackgroundImageStretch;
            set => Style.BackgroundImageStretch = value;
        }

        public bool? IsLazyLoaded { get; set; }

        object FormField.IControl.Value
        {
            get => Path;
            set => Style.BackgroundImagePath = value.ToStringOrEmpty();
        }

        bool CanDetermineDimensions()
        {
            if (Stretch == Stretch.Fill || Stretch == Stretch.AspectFill) return false;
            if (Path.IsEmpty()) return false;

            if (Path.IsUrl()) return false;

            return true;
        }

        #region AutoSizing
        float IAutoContentWidthProvider.Calculate()
        {
            if (Path.IsEmpty()) return 0;

            if (Path.IsUrl()) return 0;

            Width.UpdateOn(Height.Changed, BackgroundImageParametersChanged);
            var size = Services.ImageService.GetViewSize(Path).LimitTo(Device.Screen.GetSize());

            var imageWidthAndPadding = size.Width + this.HorizontalPaddingAndBorder();

            if (Stretch == Stretch.Fit)
            {
                if (Height.AutoOption == Length.AutoStrategy.Content)
                {
                    // What box to fit in?! Use original size.
                    return imageWidthAndPadding;
                }
                else
                {
                    if (Height.IsUnknown) return size.Width;

                    var boxHeight = (Height.currentValue - this.VerticalPaddingAndBorder()).LimitMin(0);
                    if (boxHeight == 0 || size.Height == 0) return 0;

                    var imageWidthRatio = size.Width / size.Height;
                    return boxHeight * imageWidthRatio + this.HorizontalPaddingAndBorder();
                }
            }
            else
            {
                // I'm supposed to stretch. It should never get to this line, but just in case:
                return imageWidthAndPadding;
            }
        }

        float IAutoContentHeightProvider.Calculate()
        {
            if (Path.IsEmpty()) return 0;
            if (Path.IsUrl()) return 0;

            Height.UpdateOn(Width.Changed, BackgroundImageParametersChanged);

            var size = Services.ImageService.GetViewSize(Path).LimitTo(Device.Screen.GetSize());

            var imageHeightAndPadding = size.Height + this.VerticalPaddingAndBorder();

            if (Stretch == Stretch.Fit)
            {
                if (Width.AutoOption == Length.AutoStrategy.Content)
                {
                    // What box to fit in?! Use original size.
                    return imageHeightAndPadding;
                }
                else
                {
                    if (Width.IsUnknown) return size.Height;

                    var boxWidth = (Width.currentValue - this.HorizontalPaddingAndBorder()).LimitMin(0);
                    if (boxWidth == 0 || size.Width == 0) return 0;

                    var imageHeightRatio = size.Height / size.Width;

                    return boxWidth * imageHeightRatio + this.VerticalPaddingAndBorder();
                }
            }
            else
            {
                // I'm supposed to stretch. It should never get to this line, but just in case:
                return imageHeightAndPadding;
            }
        }

        AsyncEvent IAutoContentWidthProvider.Changed => AutoContentWidthChanged;
        AsyncEvent IAutoContentHeightProvider.Changed => AutoContentHeightChanged;

        bool IAutoContentHeightProvider.DependsOnChildren() => false;

        bool IAutoContentWidthProvider.DependsOnChildren() => false;

        public override async Task OnRendered()
        {
            await base.OnRendered();

            if (Width.AutoOption != Length.AutoStrategy.Content || Height.AutoOption != Length.AutoStrategy.Content) return;
            if (!Path.IsUrl()) return;

            Log.For(this).Warning("Failed to get the automatic width or height of ImageView as its source is a URL. Set a fixed size in CSS for remote images: " + GetFullPath());
        }

        #endregion

        public override void Dispose()
        {
            AutoContentWidthChanged?.Dispose();
            AutoContentHeightChanged?.Dispose();
            base.Dispose();
        }

        public override IEnumerable<View> AllDescendents() => Enumerable.Empty<View>();
    }
}