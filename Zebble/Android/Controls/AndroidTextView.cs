namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using Android.Runtime;
    using Android.Util;
    using Android.Widget;
    using Zebble.Device;
    using zbl = Zebble;

    public class AndroidTextView : TextView, IZebbleAndroidControl, IPaddableControl
    {
        zbl.TextView View;

        public AndroidTextView(zbl.TextView view) : base(Renderer.Context)
        {
            View = view;
            CreateLabel();
            HandleEvents();
        }

        [Preserve]
        public AndroidTextView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        internal static Android.Views.View Create(zbl.TextView view)
        {
            if (view.Effective.HasBackgroundImage())
                return new AndroidControlWrapper<AndroidTextView>(view, new AndroidTextView(view)).Render();
            else return (Android.Views.View)new AndroidTextView(view);
        }

        public Task<Android.Views.View> Render() => Task.FromResult((Android.Views.View)this);

        void CreateLabel()
        {
            SetFont();
            Gravity = View.TextAlignment.Render();
            SetTextColor(View.TextColor.Render());
            Text = View.TransformedText;
            OptionChanged();
            ViewLineHeightChanged();
        }

        void ViewLineHeightChanged()
        {
            if (IsDead(out var view)) return;
            if (view.LineHeight == null) return;

            var fontLineHeight = view.Font.GetLineHeight();
            var viewLineHeight = view.LineHeight.Value;
            // Line height cannot be less than text height
            if (viewLineHeight > fontLineHeight)
            {
                var lineTopBottomSpacing = viewLineHeight - fontLineHeight;
                SetLineSpacing(Scale.ToDevice(lineTopBottomSpacing), 1f);
            }
        }

        void HandleEvents()
        {
            View.TextChanged.HandleOnUI(ViewTextChanged);
            View.TextAlignmentChanged.HandleOnUI(TextAlignmentChanged);
            View.FontChanged.HandleOnUI(SetFont);
            View.LineHeightChanged.HandleOnUI(ViewLineHeightChanged);
        }

        bool IsDead(out zbl.TextView view)
        {
            view = View;
            if (view?.IsDisposing == true) view = null;
            return view is null;
        }

        void TextAlignmentChanged()
        {
            if (!IsDead(out var view))
                Gravity = view.TextAlignment.Render();
        }

        void ViewTextChanged()
        {
            if (IsDead(out var view)) return;

            Text = view.TransformedText ?? string.Empty;
            OptionChanged();
            ViewLineHeightChanged();
        }

        void SetFont()
        {
            if (IsDead(out var view)) return;

            SetTextSize(ComplexUnitType.Px, Scale.ToDevice(view.Font.EffectiveSize));
            Typeface = view.Font.Render();
            SetIncludeFontPadding(includepad: false);
            OptionChanged();
            ViewLineHeightChanged();
        }

        void OptionChanged()
        {
            if (View == null) return;
            if (!View.ShouldWrap())
            {
                Ellipsize = Android.Text.TextUtils.TruncateAt.End;
                SetMaxLines(1);
            }
            else
            {
                Ellipsize = null;
                SetMaxLines(int.MaxValue);
            }
        }

        protected override void Dispose(bool disposing)
        {
            View = null;
            base.Dispose(disposing);
        }
    }
}