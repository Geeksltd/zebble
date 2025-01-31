namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.System;
    using Windows.UI.Text;
    using Windows.UI.Xaml.Input;
    using animation = Windows.UI.Xaml.Media.Animation;
    using controls = Windows.UI.Xaml.Controls;
    using foundation = Windows.Foundation;
    using media = Windows.UI.Xaml.Media;
    using ui = Windows.UI;
    using xaml = Windows.UI.Xaml;
    using Olive;

    public static class UWPRenderExtensions
    {
        static readonly Dictionary<int, media.Brush> BrushesCache = new();

        internal static void RespondToEvents(this controls.Canvas canvas)
        {
            // Without setting the background, the events won't fire :-)
            canvas.Background = Colors.Transparent.RenderBrush();
        }

        internal static xaml.CornerRadius RenderCornerRadius(this Effective effective)
        {
            return new xaml.CornerRadius(
                effective.BorderRadiusTopLeft(),
                effective.BorderRadiusTopRight(),
                effective.BorderRadiusBottomRight(),
                effective.BorderRadiusBottomLeft());
        }

        public static media.Brush RenderBrush(this Color color)
        {
            return BrushesCache.GetOrAdd(color.GetHashCode(), () =>
            {
                return (color as GradientColor)?.RenderBrush() ?? new media.SolidColorBrush(Render(color));
            });
        }

        static media.Brush RenderBrush(this GradientColor color)
        {
            var result = new media.LinearGradientBrush
            {
                StartPoint = color.StartPoint.Render(),
                EndPoint = color.EndPoint.Render()
            };

            foreach (var t in color.Items)
            {
                result.GradientStops.Add(new media.GradientStop
                {
                    Color = t.Color.Render(),
                    Offset = t.StopAtPercentage.RenderPercentage()
                });
            }

            return result;
        }

        public static InputScope Render(this KeyboardActionType actionType, TextMode mode)
        {
            var result = new InputScope();

            switch (actionType)
            {
                case KeyboardActionType.Default:
                    result.Names.Add(new InputScopeName(InputScopeNameValue.Default));
                    break;
                //case KeyboardActionType.Continue:
                //    result.Names.Add(new InputScopeName(InputScopeNameValue.);
                //    break;
                //case KeyboardActionType.Done:
                //    break;
                //case KeyboardActionType.Go:
                //    break;                
                case KeyboardActionType.Search:
                    result.Names.Add(new InputScopeName(InputScopeNameValue.Search));
                    break;
                case KeyboardActionType.Send:
                    result.Names.Add(new InputScopeName(InputScopeNameValue.Chat));
                    break;
                default:
                    result.Names.Add(new InputScopeName(InputScopeNameValue.Text));
                    break;
            }

            if (mode == TextMode.Email)
                result.Names.Add(new InputScopeName(InputScopeNameValue.EmailNameOrAddress));

            if (mode == TextMode.Telephone)
                result.Names.Add(new InputScopeName(InputScopeNameValue.TelephoneNumber));

            if (mode == TextMode.Url)
                result.Names.Add(new InputScopeName(InputScopeNameValue.Url));

            if (mode == TextMode.Integer)
                result.Names.Add(new InputScopeName(InputScopeNameValue.Number));

            if (mode == TextMode.Decimal)
                result.Names.Add(new InputScopeName(InputScopeNameValue.CurrencyAmount));

            if (mode == TextMode.PersonName)
                result.Names.Add(new InputScopeName(InputScopeNameValue.PersonalFullName));

            return result;
        }

        public static ui.Color Render(this Color color)
        {
            if (color is null) color = Colors.Transparent;
            return ui.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        }

        /// <summary>
        /// Return the size + position as LayoutParams
        /// </summary>
        public static foundation.Rect GetRect(this View view)
        {
            return new foundation.Rect(view.ActualX, view.ActualY, view.ActualWidth, view.CalculateTotalHeight());
        }

        public static xaml.Thickness RenderPadding(this TextView view)
        {
            if (view.ShouldIgnoreHorizontalPadding())
                return new xaml.Thickness(0, view.Padding.Top(), 0, view.Padding.Bottom());
            return view.Padding.RenderThickness();
        }

        public static xaml.Thickness RenderThickness(this Gap gap)
        {
            return new xaml.Thickness(gap.Left(), gap.Top(), gap.Right(), gap.Bottom());
        }

        public static xaml.Thickness RenderThickness(this IBorder border)
        {
            return new xaml.Thickness(border.Left, border.Top, border.Right, border.Bottom);
        }

        public static media.AlignmentX RenderX(this Alignment? alignment)
        {
            if (alignment is null) return media.AlignmentX.Left;

            if (new[] { Alignment.BottomLeft, Alignment.Left, Alignment.TopLeft, Alignment.None }.Contains(alignment))
                return media.AlignmentX.Left;

            if (new[] { Alignment.BottomRight, Alignment.Right, Alignment.TopRight }.Contains(alignment))
                return media.AlignmentX.Right;

            return media.AlignmentX.Center;
        }

        public static xaml.HorizontalAlignment RenderHorizontalAlignment(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.BottomLeft:
                case Alignment.Left:
                case Alignment.TopLeft:
                case Alignment.None:
                    return xaml.HorizontalAlignment.Left;
                case Alignment.BottomRight:
                case Alignment.Right:
                case Alignment.TopRight:
                    return xaml.HorizontalAlignment.Right;
                case Alignment.Justify:
                    return xaml.HorizontalAlignment.Stretch;
                default:
                    return xaml.HorizontalAlignment.Center;
            }
        }

        public static media.AlignmentX RenderX(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.BottomLeft:
                case Alignment.Left:
                case Alignment.TopLeft:
                case Alignment.Justify:
                case Alignment.None:
                    return media.AlignmentX.Left;
                case Alignment.BottomRight:
                case Alignment.Right:
                case Alignment.TopRight:
                    return media.AlignmentX.Right;
                case Alignment.Bottom:
                case Alignment.Top:
                default:
                    return media.AlignmentX.Center;
            }
        }

        public static media.AlignmentY RenderY(this Alignment alignment)
        {
            if (new[] { Alignment.BottomLeft, Alignment.BottomMiddle, Alignment.BottomRight, Alignment.Bottom }.Contains(alignment))
                return media.AlignmentY.Bottom;

            if (new[] { Alignment.TopLeft, Alignment.TopMiddle, Alignment.TopRight, Alignment.None, Alignment.Top }.Contains(alignment))
                return media.AlignmentY.Top;

            return media.AlignmentY.Center;
        }

        public static xaml.TextAlignment RenderTextAlignment(this Alignment alignment)
        {
            if (new[] { Alignment.BottomLeft, Alignment.Left, Alignment.TopLeft, Alignment.None }.Contains(alignment))
                return xaml.TextAlignment.Left;

            if (new[] { Alignment.BottomRight, Alignment.Right, Alignment.TopRight }.Contains(alignment))
                return xaml.TextAlignment.Right;

            if (new[] { Alignment.Justify }.Contains(alignment))
                return xaml.TextAlignment.Justify;

            return xaml.TextAlignment.Center;
        }

        public static xaml.VerticalAlignment RenderVerticalAlignment(this Alignment alignment)
        {
            if (new[] { Alignment.BottomLeft, Alignment.BottomMiddle, Alignment.BottomRight }.Contains(alignment))
                return xaml.VerticalAlignment.Bottom;

            if (new[] { Alignment.TopLeft, Alignment.TopMiddle, Alignment.TopRight, Alignment.None }.Contains(alignment))
                return xaml.VerticalAlignment.Top;

            return xaml.VerticalAlignment.Center;
        }

        public static media.Stretch Render(this Stretch stretch)
        {
            switch (stretch)
            {
                case Stretch.Fill: return media.Stretch.Fill;
                case Stretch.AspectFill: return media.Stretch.UniformToFill;
                case Stretch.Default: return media.Stretch.None;
                default: return media.Stretch.Uniform;
            }
        }

        public static media.Brush GetBackgroundColorBrush(this View view)
        {
            if (view.BackgroundColor.GetType().Equals(typeof(GradientColor)))
            {
                var gradientBackground = (GradientColor)view.BackgroundColor;
                var liner = new media.LinearGradientBrush { StartPoint = gradientBackground.StartPoint.Render(), EndPoint = gradientBackground.EndPoint.Render() };
                ((GradientColor)view.BackgroundColor).Items.Do(t =>
                {
                    liner.GradientStops.Add(new media.GradientStop { Color = t.Color.Render(), Offset = t.StopAtPercentage.RenderPercentage() });
                });
                return liner;
            }
            else
            {
                return view.BackgroundColor.RenderBrush();
            }
        }

        public static double RenderPercentage(this float location) => location * 0.01f;

        public static foundation.Point Render(this Point point) => new(point.X, point.Y);

        public static void RemoveFromSuperview(this xaml.UIElement view)
        {
            if (media.VisualTreeHelper.GetParent(view) is controls.Panel parent)
                parent.Children.Remove(view);
        }

        internal static void SetPosition(this View view, xaml.FrameworkElement rendered, BoundsChangedEventArgs args)
        {
            if ((view.parent is ScrollView scroller) && scroller.Refresh.Enabled)
                args.Y += scroller.Refresh.Indicator.CalculateTotalHeight();

            var leftAnimation = Animation.GetCurrentlyRunningAnimationValue<double?>(rendered, "(Canvas.Left)");
            var topAnimation = Animation.GetCurrentlyRunningAnimationValue<double?>(rendered, "(Canvas.Top)");

            var oldLeft = (leftAnimation ?? controls.Canvas.GetLeft(rendered)).Round(3);
            var oldTop = (topAnimation ?? controls.Canvas.GetTop(rendered)).Round(3);

            var isLeftChanged = !oldLeft.AlmostEquals(args.X);
            var isTopChanged = !oldTop.AlmostEquals(args.Y);

            if (args.Animated())
            {
                if (isLeftChanged) args.Animation.AddTimeline(() => oldLeft, () => args.X, rendered, rendered, "(Canvas.Left)");
                if (isTopChanged) args.Animation.AddTimeline(() => oldTop, () => args.Y, rendered, rendered, "(Canvas.Top)");
            }
            else
            {
                if (isLeftChanged)
                    controls.Canvas.SetLeft(rendered, args.X);

                if (isTopChanged)
                    controls.Canvas.SetTop(rendered, args.Y);
            }
        }

        internal static Size GetNativeSize(this View view, xaml.FrameworkElement native, BoundsChangedEventArgs args = null)
        {
            var width = args?.Width ?? view.ActualWidth;
            var height = args?.Height ?? view.ActualHeight;

            if (!(native is controls.Border))
            {
                width -= view.Effective.BorderTotalHorizontal();
                height -= view.Effective.BorderTotalVertical();
            }

            if (width < 0) width = 0;
            if (height < 0) height = 0;

            if (view is ImageView)
            {
                width = (width - view.HorizontalPaddingAndBorder()).LimitMin(0);
                height = (height - view.VerticalPaddingAndBorder()).LimitMin(0);
            }

            return new Size(width, height);
        }

        internal static void SetSize(this View view, xaml.FrameworkElement native, BoundsChangedEventArgs args)
        {
            var newSize = view.GetNativeSize(native, args);

            var oldWidth = native.Width;
            var oldHeight = native.Height;

            var isWidthChnged = !oldWidth.AlmostEquals(newSize.Width) || view is ImageView;
            var isHeightChanged = !oldHeight.AlmostEquals(newSize.Height) || view is ImageView;
            if (!isWidthChnged && !isHeightChanged) return;

            if (args.Animated())
            {
                if (isWidthChnged)
                    args.Animation.AddTimeline(() => native.Width, () => newSize.Width, native, native, "(FrameworkElement.Width)");

                if (isHeightChanged)
                    args.Animation.AddTimeline(() => native.Height, () => newSize.Height, native, native, "(FrameworkElement.Height)");
            }
            else
            {
                if (isWidthChnged) native.Width = newSize.Width;
                if (isHeightChanged) native.Height = newSize.Height;

                (view as Canvas)?.ApplyClip(native);
            }
        }

        public static void ApplyClip(this Canvas canvas, xaml.FrameworkElement native)
        {
            if (canvas.ClipChildren)
            {
                if (native.Clip is null) native.Clip = new media.RectangleGeometry();
                native.Clip.Rect = new foundation.Rect(0, 0, native.Width, native.Height);
            }
            else native.Clip = null;
        }

        public static animation.EasingFunctionBase RenderEasing(this Animation ani)
        {
            var easing = ani.Easing;
            var factor = ani.EasingFactor;

            if (easing == AnimationEasing.Linear) return null;

            animation.EasingFunctionBase result;
            switch (factor)
            {
                case EasingFactor.Quadratic: result = new animation.QuadraticEase(); break;
                case EasingFactor.Cubic: result = new animation.CubicEase(); break;
                case EasingFactor.Quartic: result = new animation.QuarticEase(); break;
                case EasingFactor.Quintic: result = new animation.QuinticEase(); break;
                default: throw new NotSupportedException(factor + " is not implemented!");
            }

            switch (easing)
            {
                case AnimationEasing.EaseIn: result.EasingMode = animation.EasingMode.EaseIn; break;
                case AnimationEasing.EaseOut: result.EasingMode = animation.EasingMode.EaseOut; break;
                case AnimationEasing.EaseInOut: result.EasingMode = animation.EasingMode.EaseInOut; break;
                case AnimationEasing.EaseInBounceOut: result = new animation.BounceEase { Bounces = ani.Bounces, Bounciness = ani.Bounciness }; break;
                default: throw new NotSupportedException(easing + " is not supported.");
            }

            return result;
        }

        public static void AnimateTransform<TValue>(this xaml.FrameworkElement target, xaml.DependencyObject component, Animation animation, string propertyPath, Func<TValue> from, Func<TValue> to)
        {
            animation.AddTimeline(from, to, target, component, propertyPath);
        }

        public static void Animate<TValue>(this xaml.FrameworkElement target, Animation animation, string propertyPath, Func<TValue> from, Func<TValue> to)
        {
            animation.AddTimeline(from, to, target, target, propertyPath);
        }

        public static xaml.FrameworkElement DisconnectFromParent(this xaml.FrameworkElement rendered)
        {
            var parent = media.VisualTreeHelper.GetParent(rendered);

            (parent as controls.Panel)?.Children.Remove(rendered);
            (parent as controls.ContentControl).Perform(x => x.Content = null);
            (parent as controls.Border).Perform(x => x.Child = null);

            return rendered;
        }

        public static IRandomAccessStream ToRandomAccessStream(this byte[] data)
        {
            return data.AsBuffer().AsStream().AsRandomAccessStream();
        }

        internal static void RenderFont(this controls.Control control, IFont font)
        {
            control.FontFamily = font.Render();
            control.FontSize = font.EffectiveSize;
            control.FontWeight = font.Bold ? FontWeights.Bold : FontWeights.Normal;
            control.FontStyle = font.Italic ? FontStyle.Italic : FontStyle.Normal;
        }

        internal static void RenderFont(this controls.TextBlock textBlock, IFont font)
        {
            textBlock.FontFamily = font.Render();
            textBlock.FontSize = font.EffectiveSize;
            textBlock.FontWeight = font.Bold ? FontWeights.Bold : FontWeights.Normal;
            textBlock.FontStyle = font.Italic ? FontStyle.Italic : FontStyle.Normal;
        }

        internal static Point ToPoint(this foundation.Point point)
        {
            return new Point((float)point.X.Round(0), (float)point.Y.Round(0));
        }

        public static async Task<byte[]> ReadAllBytes(this StorageFile file)
        {
            if (file is null) return null;

            using (var reader = await file.OpenSequentialReadAsync().AsTask())
            using (var stream = reader.AsStreamForRead())
                return await stream.ReadAllBytesAsync();
        }

        public static async Task<FileInfo> SaveToTempFile(this StorageFile file)
        {
            if (file is null) return null;

            var result = Device.IO.CreateTempDirectory().GetFile(file.Name.Or("No.Name"));
            var storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Directory.FullName);
            await file.CopyAsync(storageFolder, result.Name);

            return result;
        }

        public static TChild FindChildInTemplate<TChild>(this xaml.DependencyObject parent, string id = null) where TChild : xaml.DependencyObject
        {
            var childCount = media.VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < childCount; i++)
            {
                var child = media.VisualTreeHelper.GetChild(parent, i);

                if (child is TChild && (id.IsEmpty() || (string)child.GetValue(xaml.FrameworkElement.NameProperty) == id))
                    return child as TChild;
            }

            // Not found in direct children. Search recursively:
            for (var i = 0; i < childCount; i++)
            {
                var child = media.VisualTreeHelper.GetChild(parent, i);

                var result = FindChildInTemplate<TChild>(child, id);
                if (result != null) return result;
            }

            return null;
        }

        public static bool IsShiftHeld(this PointerRoutedEventArgs args)
        {
            return (args.KeyModifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;
        }

        public static bool IsControlHeld(this PointerRoutedEventArgs args)
        {
            return (args.KeyModifiers & VirtualKeyModifiers.Control) == VirtualKeyModifiers.Control;
        }

        public static Task<StorageFile> ToStorageFile(this FileInfo file)
        {
            return StorageFile.GetFileFromPathAsync(file.FullName).AsTask();
        }

        internal static bool Render(this SpellCheckingType spellCheckingType)
        {
            switch (spellCheckingType)
            {
                case SpellCheckingType.Default:
                case SpellCheckingType.Yes:
                default:
                    return true;
                case SpellCheckingType.No:
                    return false;
            }
        }

        internal static bool Render(this AutoCapitalizationType spellCheckingType)
        {
            switch (spellCheckingType)
            {
                case AutoCapitalizationType.AllCharacters:
                case AutoCapitalizationType.Sentences:
                case AutoCapitalizationType.Words:
                default:
                    return true;
                case AutoCapitalizationType.None:
                    return false;
            }
        }

        internal static media.PlaneProjection RenderProjection(this TransformationChangedEventArgs args)
        {
            return new media.PlaneProjection
            {
                CenterOfRotationX = args.OriginX,
                CenterOfRotationY = args.OriginY,
                RotationX = args.RotateX,
                RotationY = -args.RotateY,
                RotationZ = -args.RotateZ
            };
        }

        internal static media.ScaleTransform RenderTransform(this TransformationChangedEventArgs args)
        {
            return new media.ScaleTransform
            {
                CenterX = args.AbsoluteOriginX(),
                CenterY = args.AbsoluteOriginY(),
                ScaleX = args.ScaleX,
                ScaleY = args.ScaleY
            };
        }
    }
}