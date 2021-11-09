namespace Zebble.IOS
{
    using CoreGraphics;
    using Foundation;
    using System;
    using UIKit;

    public class IosLabel : UIView, UIChangeCommand.IHandler
    {
        TextView View;
        UIStringAttributes StringAttributes;

        protected UILabel Label;
        protected NSMutableAttributedString AttributedString;

        public IosLabel(TextView view)
        {
            View = view;
            CreateInnerLabel();
            HandleEvents();
            ViewTextChanged();
        }

        void CreateInnerLabel()
        {
            Label = new UILabel
            {
                TextColor = View.TextColor.Render(),
                Font = View.Font.Render(),
                TextAlignment = View.TextAlignment.Render(),
                TranslatesAutoresizingMaskIntoConstraints = false,
                AutoresizingMask = UIViewAutoresizing.None
            };

            StringAttributes = new UIStringAttributes
            {
                ParagraphStyle = new NSMutableParagraphStyle
                {
                    Alignment = View.TextAlignment.Render(),
                    HeadIndent = 0,
                    FirstLineHeadIndent = 0,
                    TailIndent = 0,
                    LineSpacing = View.LineHeight ?? 0
                }
            };

            AddSubview(Label);
            ConfigConstraints();
            ClipsToBounds = true;
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            SyncInnerView();
        }

        void SyncInnerView()
        {
            if (View?.Effective is null) return;
            if (Label == null) return;
            if (Frame == null) return;

            Label.Frame = View.GetEffectiveFrame();
        }

        void ConfigConstraints()
        {
            NSLayoutConstraint Create(NSLayoutAttribute attribute) => NSLayoutConstraint.Create(Label, attribute, NSLayoutRelation.Equal, this, attribute, 1.0f, 0);

            var top = Create(NSLayoutAttribute.Top);
            // left for ltr, right for rtl
            var leading = Create(NSLayoutAttribute.Leading);

            var width = Create(NSLayoutAttribute.Width);
            var height = Create(NSLayoutAttribute.Height);

            AddConstraints(new[] { top, leading, width, height });
        }

        void HandleEvents()
        {
            View.TextChanged.HandleOnUI(ViewTextChanged);
            View.TextAlignmentChanged.HandleOnUI(ViewTextAlignmentChanged);
            View.FontChanged.HandleOnUI(ViewFontChanged);
            View.LineHeightChanged.HandleOnUI(ViewLineHeightChanged);
            View.PaddingChanged.HandleOnUI(SyncInnerView);
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            if (property == "TextColor")
            {
                var color = (change as TextColorChangedEventArgs).Value.Render();
                if (change.Animation == null) Label.TextColor = color;
            }
            else if (property == "Bounds")
                SyncInnerView();
        }

        void ViewLineHeightChanged()
        {
            if (View.LineHeight is null) return;

            StringAttributes.ParagraphStyle.LineSpacing = View.LineHeight.Value - View.Font.GetLineHeight();
            SyncInnerView();
        }

        void ViewTextChanged()
        {
            var shouldWrap = View.ShouldWrap();
            Label.Lines = shouldWrap ? 0 : 1;
            Label.LineBreakMode = StringAttributes.ParagraphStyle.LineBreakMode = shouldWrap ? UILineBreakMode.WordWrap : UILineBreakMode.TailTruncation;
            Label.AttributedText = AttributedString = new NSMutableAttributedString(View.TransformedText, StringAttributes);
            ViewLineHeightChanged();
            SyncInnerView();
        }

        void ViewTextAlignmentChanged()
        {
            Label.TextAlignment = StringAttributes.ParagraphStyle.Alignment = View.TextAlignment.Render();

            if (View.TextAlignment.ToVerticalAlignment() == VerticalAlignment.Middle)
                Label.BaselineAdjustment = UIBaselineAdjustment.AlignCenters;

            SyncInnerView();
        }

        void ViewFontChanged()
        {
            Label.Font = View.Font.Render();
            ViewLineHeightChanged();
            SyncInnerView();
        }

        public override bool PointInside(CGPoint point, UIEvent uievent)
        {
            if (View?.Tapped is null) return false;
            try
            {
                return base.PointInside(point, uievent);
            }
            catch
            {
                // Already deallocated.
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Label?.RemoveFromSuperview();
                Label?.Dispose();
                Label = null;
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}