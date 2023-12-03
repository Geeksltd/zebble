namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    public static class ViewPropertySetterExtensions
    {
        public static TView X<TView>(this TView view, Length.LengthRequest value) where TView : View
        {
            return view.Set(x => x.Style.X(value));
        }

        public static TView Y<TView>(this TView view, Length.LengthRequest value) where TView : View
        {
            return view.Set(x => x.Style.Y(value));
        }

        public static TView Height<TView>(this TView view, Length.LengthRequest value) where TView : View
        {
            return view.Set(x => x.Style.Height(value));
        }

        public static TView Width<TView>(this TView view, Length.LengthRequest value) where TView : View
        {
            return view.Set(x => x.Style.Width(value));
        }

        public static TView X<TView>(this TView view, int value) where TView : View
        {
            return view.Set(x => x.Style.X(value));
        }

        public static TView Y<TView>(this TView view, int value) where TView : View
        {
            return view.Set(x => x.Style.Y(value));
        }

        public static TView Height<TView>(this TView view, int value) where TView : View
        {
            return view.Set(x => x.Style.Height(value));
        }

        public static TView Width<TView>(this TView view, int value) where TView : View
        {
            return view.Set(x => x.Style.Width(value));
        }

        public static TView X<TView>(this TView view, float value) where TView : View
        {
            return view.Set(x => x.Style.X(value));
        }

        public static TView Y<TView>(this TView view, float value) where TView : View
        {
            return view.Set(x => x.Style.Y(value));
        }

        public static TView Height<TView>(this TView view, float value) where TView : View
        {
            return view.Set(x => x.Style.Height(value));
        }

        public static TView Width<TView>(this TView view, float value) where TView : View
        {
            return view.Set(x => x.Style.Width(value));
        }

        public static TView Id<TView>(this TView view, string value) where TView : View => view.Set(x => x.Id = value);

        public static TView Rotation<TView>(this TView view, float? value) where TView : View
        {
            return view.Set(x => x.Style.Rotation = value);
        }

        public static TView Scale<TView>(this TView view, float? x, float? y) where TView : View
        {
            if (x.HasValue) view.Set(v => v.Style.ScaleX = x);
            if (y.HasValue) view.Set(v => v.Style.ScaleY = y);
            return view;
        }

        public static TView Scale<TView>(this TView view, float xAndY) where TView : View
        {
            return view.Scale(xAndY, xAndY);
        }

        public static TView RotationX<TView>(this TView view, float? value) where TView : View
        {
            return view.Set(x => x.Style.RotationX = value);
        }

        public static TView RotationY<TView>(this TView view, float? value) where TView : View
        {
            return view.Set(x => x.Style.RotationY = value);
        }

        public static TView ScaleX<TView>(this TView view, float? value) where TView : View
        {
            return view.Set(x => x.Style.ScaleX = value);
        }

        public static TView ScaleY<TView>(this TView view, float? value) where TView : View
        {
            return view.Set(x => x.Style.ScaleY = value);
        }

        public static TView CssClass<TView>(this TView view, string value) where TView : View => view.Set(x => x.CssClass = value);

        public static TView ZIndex<TView>(this TView view, int value) where TView : View => view.Set(x => x.Style.ZIndex = value);

        public static TView Visible<TView>(this TView view, bool value = true) where TView : View => view.Set(x => x.Style.Visible = value);

        public static TView Ignored<TView>(this TView view, bool value = true) where TView : View => view.Set(x => x.Style.Ignored = value);

        /// <summary>Sets Visible to false.</summary>
        public static TView Hide<TView>(this TView view) where TView : View => view.Visible(value: false);

        public static TView Absolute<TView>(this TView view, bool value = true) where TView : View => view.Set(x => x.Style.Absolute = value);

        public static TView Opacity<TView>(this TView view, float value) where TView : View => view.Set(x => x.Style.Opacity = value);

        /// <summary>
        /// Will set the margin-top of this view to a value that makes its bottom the same as the specified sibling.
        /// </summary>
        public static TView BottomAlign<TView>(this TView view, View sibling) where TView : View
        {
            void set() => view.Margin.Top.BindTo(sibling.Height, view.Height, (sh, vh) => sh - vh);

            if (view.IsRendered()) set();
            else view.PreRendered.HandleWith(set);

            return view;
        }

        /// <summary>
        /// Will set the margin-left of this view in a way to center align it in its parent horizontally. It assumes no siblings exist.
        /// </summary>
        public static TView CenterHorizontally<TView>(this TView view) where TView : View
        {
            void set()
            {
                view.Margin.Left.BindTo(view.parent.Width, view.parent.Padding.Left, view.parent.Padding.Right, view.Width, (pw, ppl, ppr, vw) => (pw - (vw + ppl + ppr + view.parent.Effective.BorderTotalHorizontal())) / 2);
            }

            if (view.IsRendered()) set();
            else view.PreRendered.HandleWith(set);

            return view;
        }

        /// <summary>
        /// Will set the margin-top of this view in a way to middle align it in its parent vertically. It assumes no siblings exist.
        /// </summary>
        public static TView CenterVertically<TView>(this TView view) where TView : View
        {
            void set()
            {
                view.Margin.Top.BindTo(view.parent.Height, view.parent.Padding.Top, view.parent.Padding.Top, view.Height, (ph, ppt, ppb, vh) => (ph - (vh + ppt + ppb + view.parent.Effective.BorderTotalVertical())) / 2);
            }

            if (view.IsRendered()) set();
            else view.PreRendered.HandleWith(set);

            return view;
        }

        /// <summary>
        /// Will set the margin-top of this view in a way to middle align it in its parent vertically. It assumes no siblings exist.
        /// </summary>
        public static TView Center<TView>(this TView view) where TView : View
        {
            return view.CenterHorizontally().CenterVertically();
        }

        public static TView BackgroundColor<TView>(this TView view, object color)
            where TView : View
        {
            return view.ApplyColor(x => x, nameof(View.BackgroundColor), (x, clr) => x.BackgroundColor = clr, color);
        }

        public static TView Background<TView>(this TView view, object color = null, string path = null, Alignment? alignment = null, Stretch? stretch = null)
            where TView : View
        {
            view.BackgroundColor(color);
            if (path != null) view.Style.BackgroundImagePath = path;
            if (alignment.HasValue) view.Style.BackgroundImageAlignment = alignment.Value;
            if (stretch.HasValue) view.Style.BackgroundImageStretch = stretch.Value;

            return view;
        }

        /// <summary>
        /// Sets the radius of this object to 50% of its height.
        /// If it doesn't have an height specified, then it will be set to the ActualHeight during the Rendered event.
        /// </summary>
        public static TView Round<TView>(this TView view) where TView : View
        {
            view.Height.Changed.Handle(() => view.BorderRadius(view.ActualHeight / 2));
            return view.BorderRadius(view.ActualHeight / 2);
        }

        public static TView Margin<TView>(this TView view, float? all = null, float? horizontal = null, float? vertical = null, float? top = null, float? right = null, float? bottom = null, float? left = null) where TView : View
        {
            view.Style.Margin(all, horizontal, vertical, top, right, bottom, left);
            return view;
        }

        public static TView Padding<TView>(this TView view, float? all = null, float? horizontal = null, float? vertical = null, float? top = null, float? right = null, float? bottom = null, float? left = null) where TView : View
        {
            view.Style.Padding(all, horizontal, vertical, top, right, bottom, left);
            return view;
        }

        public static TView Border<TView>(this TView view, float? all = null, float? top = null, float? right = null, float? bottom = null, float? left = null, object color = null)
            where TView : View
        {
            if (all.HasValue) view.Style.Border.Width = all.Value;

            if (top.HasValue) view.Style.Border.Top = top.Value;
            if (left.HasValue) view.Style.Border.Left = left.Value;
            if (bottom.HasValue) view.Style.Border.Bottom = bottom.Value;
            if (right.HasValue) view.Style.Border.Right = right.Value;

            if (color != null)
            {
                view.ApplyColor(x => x.Style.Border, nameof(View.Style.Border.Color), (x, clr) => x.Style.Border.Color = clr, color);
            }

            return view;
        }

        public static TView BorderRadius<TView>(this TView view, float? all = null, float? top = null, float? left = null, float? right = null, float? bottom = null, float? topLeft = null, float? topRight = null, float? bottomRight = null, float? bottomLeft = null) where TView : View
        {
            if (all.HasValue) view.Style.BorderRadius = all.Value;

            if (top.HasValue) view.Style.BorderRadius.TopLeft = view.Style.borderRadius.TopRight = top.Value;
            if (left.HasValue) view.Style.BorderRadius.TopLeft = view.Style.borderRadius.BottomLeft = left.Value;
            if (right.HasValue) view.Style.BorderRadius.TopRight = view.Style.borderRadius.BottomRight = right.Value;
            if (bottom.HasValue) view.Style.BorderRadius.BottomLeft = view.Style.borderRadius.BottomRight = bottom.Value;

            if (topLeft.HasValue) view.Style.BorderRadius.TopLeft = topLeft.Value;
            if (topRight.HasValue) view.Style.BorderRadius.TopRight = topRight.Value;

            if (bottomLeft.HasValue) view.Style.BorderRadius.BottomLeft = bottomLeft.Value;
            if (bottomRight.HasValue) view.Style.BorderRadius.BottomRight = bottomRight.Value;

            return view;
        }

        public static float Top(this Gap gap) => gap.Top.currentValue;

        public static float Bottom(this Gap gap) => gap.Bottom.currentValue;

        public static float Left(this Gap gap) => gap.Left.currentValue;

        public static float Right(this Gap gap) => gap.Right.currentValue;

        public static void Top(this Gap gap, float value) => gap.Top.Set(value);

        public static void Bottom(this Gap gap, float value) => gap.Bottom.Set(value);

        public static void Left(this Gap gap, float value) => gap.Left.Set(value);

        public static void Right(this Gap gap, float value) => gap.Right.Set(value);

        public static void Top(this GapRequest gap, float value) => gap.Top = value;

        public static void Bottom(this GapRequest gap, float value) => gap.Bottom = value;

        public static void Left(this GapRequest gap, float value) => gap.Left = value;

        public static void Right(this GapRequest gap, float value) => gap.Right = value;

        /// <summary>
        /// Gets the sum of the current left and right values.
        /// </summary>
        public static float Horizontal(this Gap gap) => gap.Left.currentValue + gap.Right.currentValue;

        /// <summary>
        /// Gets the sum of the current top and bottom values.
        /// </summary>
        public static float Vertical(this Gap gap) => gap.Top.currentValue + gap.Bottom.currentValue;

        /// <summary>Sets both width and height to the same value.</summary>
        public static TView Size<TView>(this TView view, Length.LengthRequest size) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(size).Height(size));
        }

        public static TView Size<TView>(this TView view, Length.LengthRequest width, Length.LengthRequest height) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(width).Height(height));
        }

        public static TView Size<TView>(this TView view, Length.LengthRequest width, float height) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(width).Height(height));
        }

        public static TView Size<TView>(this TView view, float width, Length.LengthRequest height) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(width).Height(height));
        }

        /// <summary>Sets both width and height to the same value.</summary>
        public static TView Size<TView>(this TView view, float size) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(size).Height(size));
        }

        public static TView Size<TView>(this TView view, float width, float height) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(width).Height(height));
        }

        /// <summary>Sets both width and height to the same value.</summary>
        public static TView Size<TView>(this TView view, int size) where TView : View
        {
            return view.ChangeInBatch(() => view.Width(size).Height(size));
        }

        public static TView Text<TView>(this TView view, object text) where TView : TextControl
        {
            if (text is Bindable<string> bindable) return view.Bind(nameof(TextControl.Text), () => bindable);
            return view.Set(x => x.Text = (string)text);
        }

        public static TView Wrap<TView>(this TView view, bool? value = true) where TView : TextView => view.Set(x => x.Style.WrapText = value);

        public static TView Text<TView>(this TView view, Func<TView, string> text) where TView : TextControl => view.Set(x => x.Text = text(view));

        public static TView TextColor<TView>(this TView view, object color)
            where TView : TextControl
        {
            return view.ApplyColor(x => x, nameof(TextControl.TextColor), (x, clr) => x.TextColor = clr, color);
        }

        public static TView TextAlignment<TView>(this TView view, Alignment value) where TView : TextControl => view.Set(x => x.Style.TextAlignment = value);

        public static TView AutoSizeWidth<TView>(this TView view, bool auto = true) where TView : TextView => view.Set(x => x.AutoSizeWidth = auto);

        public static TView Font<TView>(this TView view, Font font, object color = null)
            where TView : TextControl
        {
            if (font != null) view.Style.Font = font;
            if (color != null) view.TextColor(color);
            return view;
        }

        public static TView Font<TView>(this TView view, float? size = null, bool? bold = null, bool? italic = null, object color = null)
            where TView : TextControl
        {
            return view.ChangeInBatch(() =>
            {
                if (size.HasValue) view.Style.Font.Size = size.Value;
                if (bold.HasValue) view.Style.Font.Bold = bold.Value;
                if (italic.HasValue) view.Style.Font.Italic = italic.Value;
                if (color != null) view.TextColor(color);
            });
        }

        public static TView Alignment<TView>(this TView view, Alignment value) where TView : ImageView => view.Set(x => x.Alignment = value);

        public static TView Stretch<TView>(this TView view, Stretch value) where TView : ImageView => view.Set(x => x.Stretch = value);

        public static TView FailedPlaceholderImagePath<TView>(this TView view, string value) where TView : ImageView => view.Set(x => x.FailedPlaceholderImagePath = value);

        public static TView Path<TView>(this TView view, string value) where TView : ImageView => view.Set(x => x.Path = value);

        public static TView ImageData<TView>(this TView view, byte[] value) where TView : ImageView => view.Set(x => x.ImageData = value);

        public static TData Data<TData>(this View view, string key) => (TData)view.Data.GetOrDefault(key);

        public static TView Data<TView>(this TView view, string key, object value) where TView : View => view.Set(x => x.Data[key] = value);

        public static TView Lines<TView>(this TView view, int value) where TView : TextInput => view.Set(x => x.Lines = value);

        public static async Task<TView> Direction<TView>(this TView view, RepeatDirection value) where TView : Stack
        {
            if (view != null) await view.SetDirection(value);
            return view;
        }

        public static async Task<TView> HorizontalAlignment<TView>(this TView view, HorizontalAlignment value) where TView : Stack
        {
            if (view != null) await view.SetHorizontalAlignment(value);
            return view;
        }

        public static TView ChangeInBatch<TView>(this TView view, Action<TView> change) where TView : View
        {
            return ChangeInBatch(view, () => change(view));
        }

        public static TView ChangeInBatch<TView>(this TView view, Action change) where TView : View
        {
            if (change is null) return view;

            // Fast mode?
            if (view.parent == null && (view is ImageView || view is TextControl || view.AllChildren.None()))
            {
                change();
                return view;
            }

            view.StyleApplyingContext = new BatchUIChangeContext();

            view.Lengths.Do(b => b.Suspend());
            change();
            view.StyleApplyingContext?.ApplyChanges();
            view.Lengths.Do(x => x.Resume());

            view.StyleApplyingContext = null;
            return view;
        }

        public static Length MinLimit(this Length @this, float? value) => @this.Set(x => x.MinLimit = value);

        public static Length MaxLimit(this Length @this, float? value) => @this.Set(x => x.MaxLimit = value);

        public static TView PlaceholderColor<TView>(this TView view, object color)
            where TView : TextInput
        {
            return view.ApplyColor(x => x, nameof(TextInput.PlaceholderColor), (x, clr) => x.PlaceholderColor = clr, color);
        }

        static TView ApplyColor<TView>(this TView view, Func<TView, object> targetExpression, string propertyName, Action<TView, Color> update, object value)
            where TView : View
        {
            if (value is null) return view;

            if (value is Bindable<Color> bindColor) return view.Bind(targetExpression, propertyName, () => bindColor);
            if (value is Bindable<GradientColor> bindGradient) return view.Bind(targetExpression, propertyName, () => bindGradient);

            if (value is Color color) return view.Set(x => update(x, color));
            if (value is GradientColor gradient) return view.Set(x => update(x, gradient));

            return view.Set(x => update(x, (Color)(string)value));
        }
    }
}