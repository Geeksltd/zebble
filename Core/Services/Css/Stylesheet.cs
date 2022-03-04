namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    [EscapeGCop("X and Y are good names")]
    public partial class Stylesheet
    {
        View Owner;
        internal bool IsCss;

        internal string backgroundImagePath, transformOrigin;
        internal byte[] backgroundImageData;
        internal Stretch? backgroundImageStretch;
        internal bool? wrapText, absolute, visible, ignored;
        internal Color backgroundColor, textColor;
        internal Border border;
        internal BorderRadius borderRadius;
        internal Font font;
        internal Alignment? textAlignment, backgroundImageAlignment;
        internal float? opacity, scaleX, scaleY, rotation, rotationX, rotationY;
        internal Length.LengthRequest width, height, x, y;
        internal int? zIndex;
        internal TextTransform? textTransform;
        public readonly GapRequest Margin, Padding;

        public Stylesheet(View owner, bool isCss)
        {
            IsCss = isCss;
            Owner = owner;
            Margin = new GapRequest(owner.Margin, this, IsCss ? owner.Style.Margin : null);
            Padding = new GapRequest(owner.Padding, this, IsCss ? owner.Style.Padding : null);
        }

        public string Type => IsCss ? "Css" : "Styles";

        // Note: For every property added here, also add it to ZebbleCssSetting.cs

        /// <summary>Rotation in degrees around the Z axis.</summary>
        public float? Rotation
        {
            get => rotation;
            set { if (value == rotation) return; rotation = value; Owner.RaiseTransformationChanged(); }
        }

        /// <summary>Rotation in degrees around the X axis.</summary>
        public float? RotationX
        {
            get => rotationX;
            set { if (value == rotationX) return; rotationX = value; Owner.RaiseTransformationChanged(); }
        }

        /// <summary>Rotation in degrees around the Y axis.</summary>
        public float? RotationY
        {
            get => rotationY;
            set { if (value == rotationY) return; rotationY = value; Owner.RaiseTransformationChanged(); }
        }

        public float? ScaleX
        {
            get => scaleX;
            set { if (value == scaleX) return; scaleX = value; Owner.RaiseTransformationChanged(); }
        }

        public float? ScaleY
        {
            get => scaleY;
            set { if (value == scaleY) return; scaleY = value; Owner.RaiseTransformationChanged(); }
        }

        public float? Opacity
        {
            get => opacity;
            set { if (opacity != value) Change(ref OpacityChangeTracker, ref opacity, value, Owner.RaiseOpacityChanged); }
        }

        public Alignment? BackgroundImageAlignment
        {
            get => backgroundImageAlignment;
            set
            {
                if (backgroundImageAlignment != value)
                    Change(ref BackgroundImageAlignmentChangeTracker, ref backgroundImageAlignment, value, Owner.UpdateBackgroundImageIfNeeded);
            }
        }

        public Stretch? BackgroundImageStretch
        {
            get => backgroundImageStretch;
            set
            {
                if (backgroundImageStretch == value) return;

                Change(ref BackgroundImageStretchChangeTracker, ref backgroundImageStretch, value,
                    Owner.UpdateBackgroundImageIfNeeded);
            }
        }

        public string BackgroundImagePath
        {
            get => backgroundImagePath;
            set
            {
                if (backgroundImagePath == value) return;

                if (UIRuntime.IsDevMode && Owner is ScrollView && value.HasValue())
                    throw new Exception("ScrollView cannot accept a background image directly. Either set it for its container or first (wrapper) child.");

                Owner.Width.Changed.HandleWith(Owner.UpdateBackgroundImageIfNeeded);
                Owner.Height.Changed.HandleWith(Owner.UpdateBackgroundImageIfNeeded);

                Change(ref BackgroundImagePathChangeTracker, ref backgroundImagePath, value,
                    () => Owner.RaiseBackgroundImageChanged());
            }
        }

        public string TransformOrigin
        {
            get => transformOrigin;
            set
            {
                value = value?.ToLower().Replace("\"", "");
                if (value.IsEmpty()) return;
                if (transformOrigin == value) return;
                transformOrigin = value;

                if (transformOrigin == "center") transformOrigin = "0.5 0.5";
                else if (transformOrigin == "left") transformOrigin = "0 0.5";
                else if (transformOrigin == "right") transformOrigin = "0 1";
                else if (transformOrigin == "top") transformOrigin = "0.5 0";
                else if (transformOrigin == "bottom") transformOrigin = "0.5 1";
                else if (transformOrigin == "top left") transformOrigin = "0 0";
                else if (transformOrigin == "top right") transformOrigin = "1 0";
                else if (transformOrigin == "bottom left") transformOrigin = "0 1";
                else if (transformOrigin == "bottom right") transformOrigin = "1 1";

                var parts = transformOrigin.Split(' ');
                if (parts.Length != 2) throw new Exception("Invalid transform-origin value: " + transformOrigin);

                parts = parts.Select(x => x.EndsWith("%") ? "0." + x.TrimEnd("%") : x).ToArray();
                foreach (var p in parts)
                    if (!p.Is<float>()) throw new Exception("Invalid transform-origin value: " + transformOrigin);

                Owner.RaiseTransformationChanged();
            }
        }

        public byte[] BackgroundImageData
        {
            get => backgroundImageData;
            set
            {
                if (backgroundImageData == value) return;

                if (UIRuntime.IsDevMode && Owner is ScrollView && value?.Any() == true)
                    throw new Exception("ScrollView cannot accept a background image directly. Either set it for its container or first (wrapper) child.");

                backgroundImageData = value;
                Owner.RaiseBackgroundImageChanged();
                Owner.Width.Changed.HandleWith(Owner.UpdateBackgroundImageIfNeeded);
                Owner.Height.Changed.HandleWith(Owner.UpdateBackgroundImageIfNeeded);
            }
        }

        public bool? Absolute
        {
            get => absolute;
            set
            {
                if (absolute == value) return;
                absolute = value;

                var previousOwnerAbsolute = Owner.absolute;
                Owner.absolute = Owner.Style.absolute ?? Owner.Css.absolute ?? false;
                if (previousOwnerAbsolute == Owner.absolute) return;

                Owner.RaiseAbsoluteChanged();
            }
        }

        public TextTransform? TextTransform
        {
            get => textTransform;
            set { textTransform = value; (Owner as TextControl)?.OnTextTransformChanged(); }
        }

        public bool? Ignored
        {
            get => ignored;
            set
            {
                if (ignored == value) return;
                if (Owner.IsRendered())
                {
                    Log.For(this).Error("DO NOT SET Ignored at runtime. Set IgnoredAsync()");
                    Task.Factory.RunSync(() => IgnoredAsync(value));
                }
                else IgnoredAsync(value).GetAwaiter();
            }
        }

        public Task IgnoredAsync(bool? value = true)
        {
            if (ignored == value) return Task.CompletedTask;
            ignored = value;

            var previousOwnerIgnored = Owner.ignored;
            Owner.ignored = Owner.Style.ignored ?? Owner.Css.ignored ?? false;
            if (previousOwnerIgnored == Owner.ignored) return Task.CompletedTask;

            var parent = Owner.parent;

            if (parent != null)
            {
                if (parent.Height.AutoOption == Length.AutoStrategy.Content) parent.Height.Update();
                if (parent.Width.AutoOption == Length.AutoStrategy.Content) parent.Width.Update();

                if ((parent as Stack)?.Direction == RepeatDirection.Horizontal)
                    parent.AllChildren.Do(x => x.Width.Update());
            }

            return Owner.OnIgnoreChanged();
        }

        public bool? Visible
        {
            get => visible;
            set
            {
                if (value == visible) return;
                visible = value; Owner.RaiseVisibilityChanged();
            }
        }

        public bool? WrapText
        {
            get => wrapText;
            set
            {
                if (wrapText == value) return;

                if (wrapText != value)
                    Change(ref WrapTextChangeTracker, ref wrapText, value, () =>
                    {
                        if (Owner is TextView tv && tv.Text.HasValue() &&
                        tv.Width.AutoOption == Length.AutoStrategy.Content)
                            Owner.Width.Update();
                    });
            }
        }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                if (value == backgroundColor) return;

                if (!Owner.IsRendered()) backgroundColor = value;
                else
                {
                    var prev = Owner.BackgroundColor;
                    backgroundColor = value;
                    if (Owner.BackgroundColor != prev) Owner.RaiseBackgroundColorChanged();
                }
            }
        }

        public Color TextColor
        {
            get => textColor;
            set
            {
                if (textColor != value)
                    Change(ref TextColorChangeTracker, ref textColor, value, () => (Owner as TextControl)?.RaiseTextColorChanged());
            }
        }

        public Alignment? TextAlignment
        {
            get => textAlignment;
            set
            {
                if (value.HasValue && Owner is Stack stack)
                    stack.HorizontalAlignment = value.Value.ToHorizontalAlignment();

                if (textAlignment == value) return;
                textAlignment = value;
                (Owner as TextControl)?.RaiseTextAlignmentChanged();
            }
        }

        public virtual Border Border
        {
            get
            {
                if (border is null)
                {
                    border = new Border();
                    border.Changed += () => Owner.RaiseBorderChanged();
                    border.HorizontalSizeChanged += () => Owner.RaiseHorizontalBorderSizeChanged();
                    border.VerticalSizeChanged += () => Owner.RaiseVerticalBorderSizeChanged();
                }

                return border;
            }
            set
            {
                if (border == value) return;

                border = value ?? new Border();
                Owner.RaiseBorderChanged();
                border.Changed += () => Owner.RaiseBorderChanged();
                border.HorizontalSizeChanged += () => Owner.RaiseHorizontalBorderSizeChanged();
                border.VerticalSizeChanged += () => Owner.RaiseVerticalBorderSizeChanged();
            }
        }

        public virtual BorderRadius BorderRadius
        {
            get
            {
                if (borderRadius is null)
                {
                    borderRadius = new BorderRadius();
                    borderRadius.Changed += () => Owner.RaiseBorderRadiusChanged();
                }

                return borderRadius;
            }
            set
            {
                if (borderRadius == value) return;

                borderRadius = value ?? new BorderRadius();
                Owner.RaiseBorderRadiusChanged();
                borderRadius.Changed += () => Owner.RaiseBorderChanged();
            }
        }

        public virtual Font Font
        {
            get
            {
                if (font is null)
                {
                    font = new Font();
                    font.Changed += () => (Owner as TextControl)?.RaiseFontChanged();
                }

                return font;
            }
            set
            {
                if (font == value) return;

                font = value ?? new Font();

                (Owner as TextControl)?.RaiseFontChanged();
                font.Changed += () => (Owner as TextControl)?.RaiseFontChanged();
            }
        }

        public int? ZIndex
        {
            get => zIndex;
            set
            {
                if (zIndex == value) return;
                zIndex = value;

                var previousOwnerZIndex = Owner.zIndex;
                Owner.zIndex = Owner.Style.zIndex ?? Owner.Css.zIndex ?? 0;
                if (previousOwnerZIndex == Owner.zIndex) return;

                if (Owner.IsRendered()) Owner.RaiseZIndexChanged();
            }
        }

        // LAYOUT ==========================================================

        public Length.LengthRequest X
        {
            get => x;
            set
            {
                x = value;
                (Owner.Style.x ?? Owner.Css.x)?.Apply(Owner.X);
            }
        }

        public Length.LengthRequest Y
        {
            get => y;
            set
            {
                y = value;
                (Owner.Style.y ?? Owner.Css.y)?.Apply(Owner.Y);
            }
        }

        public Length.LengthRequest Width
        {
            get => width;
            set
            {
                width = value;
                (Owner.Style.width ?? Owner.Css.width)?.Apply(Owner.Width);
            }
        }

        public Length.LengthRequest Height
        {
            get => height;
            set
            {
                height = value;
                (Owner.Style.height ?? Owner.Css.height)?.Apply(Owner.Height);
            }
        }
    }
}