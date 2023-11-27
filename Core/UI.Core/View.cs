namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services;
    using Olive;
    using System.Collections.Concurrent;
    using System.Reflection;

    public abstract partial class View : IDisposable
    {
        bool IsInitialized, IsFlashing, enabled = true;
        internal bool ignored, absolute;
        internal int zIndex;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Native;

        internal Renderer Renderer;
        public bool IsDisposed { get; internal set; }
        internal string id;

        /// <summary>
        /// A lock object used for adding and removing the document object model, i.e. the descendents of this view.
        /// If you're making large changes to the UI structure in response to user gesture events,
        /// then wrap your code in a using block to await this lock's LockAsync() method return,
        /// to prevent conflicting concurrent changes.
        /// </summary>
        public readonly AsyncLock DomLock = new();

        Dictionary<string, object> data;
        internal Effective Effective;

        protected internal readonly AsyncEvent Shown = new();
        public readonly AsyncEvent PreRendered = new();
        public readonly AsyncEvent Rendered = new();
        public readonly AsyncEvent Initializing = new();
        public readonly AsyncEvent Initialized = new();
        public readonly AsyncEvent Flashed = new();
        public readonly AsyncEvent BackgroundImageChanged = new();
        public readonly AsyncEvent<UIChangedEventArgs<bool>> IgnoredChanged = new();
        public readonly AsyncEvent AbsoluteChanged = new();
        public readonly AsyncEvent VisibilityChanged = new();
        public readonly AsyncEvent ZIndexChanged = new();
        public readonly AsyncEvent BorderChanged = new();
        public readonly AsyncEvent BorderRadiusChanged = new();
        /// <summary>
        /// Fired when the view has a new ViewModel applied as items are being rearranged within a CollectionView.
        /// </summary>
        public readonly AsyncEvent ReusedInCollectionView = new();

        internal readonly AsyncEvent VerticalBorderSizeChanged = new();
        internal readonly AsyncEvent HorizontalBorderSizeChanged = new();
        internal readonly AsyncEvent BackgroundImageParametersChanged = new();

        public readonly AsyncEvent<UIChangedEventArgs<float>> OpacityChanged = new();

        internal readonly Length[] Lengths;

        protected View()
        {
            CssReference = new CssReference(GetType());
            X = new Length(this, Length.LengthType.X);
            Y = new Length(this, Length.LengthType.Y);
            Padding = new Gap(this, Length.LengthType.PaddingLeft, Length.LengthType.PaddingRight, Length.LengthType.PaddingTop, Length.LengthType.PaddingTop);
            Margin = new Gap(this, Length.LengthType.MarginLeft, Length.LengthType.MarginRight, Length.LengthType.MarginTop, Length.LengthType.MarginBottom);
            Width = new Length(this, Length.LengthType.Width);
            Height = new Length(this, Length.LengthType.Height);

            Lengths = new[] { X, Y, Width, Height, Padding.Left, Padding.Right, Padding.Bottom, Padding.Top, Margin.Top, Margin.Bottom, Margin.Left, Margin.Right };

            Effective = new Effective(this);
            Style = new Stylesheet(this, isCss: false);
            Css = new Stylesheet(this, isCss: true);

            new[] { Margin.Left, Margin.Right }.Do(x => x.Changed.HandleWith(OnHorizontalMarginChanged));
            new[] { Margin.Top, Margin.Bottom }.Do(x => x.Changed.HandleWith(OnVerticalMarginChanged));

            if (UIRuntime.IsDevMode)
            {
                new AbstractAsyncEvent[] {Shown, PreRendered, Rendered, Initializing, Initialized, IgnoredChanged,
                     Flashed, Touched, Tapped, LongPressed, Swiped, Panning, PanFinished, BackgroundImageChanged,
                     PaddingChanged, UserRotating, Pinching }
                .Do(x => x.SetOwner(this));
            }
        }

        public string GetFullPath()
        {
            var path = WithAllParents().Reverse().ToArray();
            if (path.Contains(Root)) return path.SkipWhile(x => x != Root).Skip(1).ToString(" ➔ ");
            else return path.ToString(" ➔ ");
        }

        internal async void RaiseOpacityChanged()
        {
            if (OpacityChanged.IsHandled())
                await OpacityChanged.Raise(new UIChangedEventArgs<float>(this, Opacity));

            if (IsRendered())
                UIWorkBatch.Publish(this, "Opacity", new UIChangedEventArgs<float>(this, Opacity));
        }

        internal void RaiseBackgroundImageChanged() => BackgroundImageChanged.Raise();

        internal void RaiseAbsoluteChanged() => AbsoluteChanged.Raise();

        internal void RaiseBackgroundImageParamsChanged() => BackgroundImageParametersChanged.Raise();

        internal void RaiseVisibilityChanged()
        {
            if (VisibilityChanged.IsHandled()) VisibilityChanged.Raise();

            if (IsRendered())
                UIWorkBatch.Publish(this, "Visibility", null);
        }

        internal void RaiseZIndexChanged()
        {
            if (ZIndexChanged.IsHandled())
                ZIndexChanged.Raise();

            if (IsRendered())
                UIWorkBatch.Publish(this, "ZIndex", null);
        }

        internal void RaiseBackgroundColorChanged()
        {
            if (IsRendered())
                UIWorkBatch.Publish(this, "BackgroundColor", new UIChangedEventArgs<Color>(this, BackgroundColor));
        }

        internal void RaiseTransformationChanged()
        {
            if (IsRendered())
                UIWorkBatch.Publish(this, "Transform", new TransformationChangedEventArgs(this));
        }

        internal void RaiseBorderChanged() => BorderChanged.Raise();

        internal void RaiseBorderRadiusChanged() => BorderRadiusChanged.Raise();

        internal void RaiseHorizontalBorderSizeChanged() => HorizontalBorderSizeChanged.Raise();

        internal void RaiseVerticalBorderSizeChanged() => VerticalBorderSizeChanged.Raise();

        internal void UpdateBackgroundImageIfNeeded()
        {
            if (BackgroundImagePath.IsEmpty() && BackgroundImageData.None()) return;

            RaiseBackgroundImageParamsChanged();

            if (ImageService.ShouldMemoryCache(BackgroundImagePath)) return;

            RaiseBackgroundImageChanged();
        }

        public Task BringToFront()
        {
            var newZIndex = parent?.CurrentChildren.Except(this).MaxOrDefault(x => x.zIndex) ?? 0;
            newZIndex = Math.Max(zIndex, newZIndex);
            ZIndex = newZIndex + 1;
            return Task.CompletedTask;
        }

        public Task SendToBack()
        {
            var newZIndex = parent?.CurrentChildren.Except(this).MinOrDefault(x => x.zIndex) ?? 0;
            newZIndex = Math.Min(zIndex, newZIndex).LimitMax(0);
            ZIndex = newZIndex - 1;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Provides a dictionary that can be used to attach any custom data on the view.
        /// </summary>
        public Dictionary<string, object> Data { get { if (data is null) data = new Dictionary<string, object>(); return data; } }

        public string Id
        {
            get => id;
            set { id = value.OrNullIfEmpty(); CssReference.SetId(id); }
        }

        [PropertyGroup("Visibility")]
        public virtual bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value) return;
                enabled = value;
                SetPseudoCssState("disabled", !value).GetAwaiter();
            }
        }

        [PropertyGroup("Background")]
        public string BackgroundImagePath
        {
            get => Style.backgroundImagePath ?? Css.backgroundImagePath;
            set => Style.BackgroundImagePath = value;
        }

        string BackgroundImageDataHash = string.Empty;
        public byte[] BackgroundImageData
        {
            get => Style.backgroundImageData ?? Css.backgroundImageData;
            set
            {
                Style.BackgroundImageData = value;
                BackgroundImageDataHash = (value ?? Array.Empty<byte>()).ToBase64String().ToSimplifiedSHA1Hash();
            }
        }

        public bool HasAnimatedBackgroundImage { get; set; }

        [PropertyGroup("Background")]
        public Stretch BackgroundImageStretch
        {
            get => Style.backgroundImageStretch ?? Css.backgroundImageStretch ?? Stretch.Fit;
            set => Style.BackgroundImageStretch = value;
        }

        [PropertyGroup("Background")]
        public Alignment BackgroundImageAlignment
        {
            get => Style.backgroundImageAlignment ?? Css.backgroundImageAlignment ?? Alignment.Left;
            set => Style.BackgroundImageAlignment = value;
        }

        [PropertyGroup("Frame")]
        public bool Absolute { get => absolute; set => Style.Absolute = value; }

        [PropertyGroup("Visibility")]
        public virtual bool Ignored { get => ignored; set => Style.Ignored = value; }

        public virtual Task IgnoredAsync(bool value = true) => Style.IgnoredAsync(value);

        internal async Task OnIgnoreChanged()
        {
            if (IgnoredChanged.IsHandled())
                await IgnoredChanged.Raise(new UIChangedEventArgs<bool>(this, ignored));

            if (VisibilityChanged.IsHandled())
                await VisibilityChanged.Raise(); // Native.

            if (IsRendered())
                UIWorkBatch.Publish(this, "Visibility", new UIChangedEventArgs<bool>(this, ignored));
        }

        [PropertyGroup("Visibility")]
        public float Opacity { get => Style.opacity ?? Css.opacity ?? 1; set => Style.Opacity = value; }

        /// <summary>
        /// Rotation around the Z Index (aka 2D rotate).
        /// </summary>
        [PropertyGroup("Transformation")]
        public float Rotation { get => Style.rotation ?? Css.rotation ?? 0; set => Style.Rotation = value; }

        /// <summary>
        /// The percentage point around which transformations are applied.
        /// Default is 0.5 0.5 (which means center). Use the Css transform-origin format for value.
        /// </summary>
        [PropertyGroup("Transformation")]
        public string TransformOrigin
        {
            get => Style.transformOrigin.Or(Css.transformOrigin).Or("0.5 0.5").ToLower();
            set => Style.TransformOrigin = value;
        }

        internal float TransformOriginX => TransformOrigin.Split(' ')[0].To<float>();

        internal float TransformOriginY => TransformOrigin.Split(' ')[1].To<float>();

        [PropertyGroup("Transformation")]
        public float RotationX { get => Style.rotationX ?? Css.rotationX ?? 0; set => Style.RotationX = value; }

        [PropertyGroup("Transformation")]
        public float RotationY { get => Style.rotationY ?? Css.rotationY ?? 0; set => Style.RotationY = value; }

        [PropertyGroup("Transformation")]
        public float ScaleX { get => Style.scaleX ?? Css.scaleX ?? 1; set => Style.ScaleX = value; }

        [PropertyGroup("Transformation")]
        public float ScaleY { get => Style.scaleY ?? Css.scaleY ?? 1; set => Style.ScaleY = value; }

        [PropertyGroup("Visibility")]
        public virtual bool Visible { get => Style.visible ?? Css.visible ?? true; set => Style.Visible = value; }

        public virtual bool CanCancelTouches => false;

        public Task Flash()
        {
            if (IsFlashing) return Task.CompletedTask;
            IsFlashing = true;

            // We want to make sure that the UI thread has received a signal before this method returns.
            // But we shouldn't wait for the actual flashing logic to complete.
            return Thread.UI.Run(() =>
             {
                 (Flashed.IsHandled() ? Flashed.Raise() : ApplyDefaultFlash())
                     .ContinueWith(t => IsFlashing = false)
                     .GetAwaiter();
             });
        }

        protected virtual async Task ApplyDefaultFlash()
        {
            using (Stylesheet.Preserve(this, x => x.Opacity))
            {
                var currentOpacity = Opacity;
                Style.Opacity = 0.5f * currentOpacity; // Immediately halve it

                // Then gradually take it back to what it was
                await this.Animate(Animation.FlashDuration, x => x.Opacity(currentOpacity));
            }
        }

        [PropertyGroup("Background")]
        public virtual Color BackgroundColor
        {
            get => Style.backgroundColor ?? Css.backgroundColor ?? Colors.Transparent;
            set => Style.BackgroundColor = value;
        }

        public virtual IBorder Border { get => Effective.Border; set => Style.Border = (Border)value; }

        public virtual IBorderRadius BorderRadius { get => Effective.BorderRadius; set => Style.BorderRadius = (BorderRadius)value; }

        [PropertyGroup("Visibility")]
        public int ZIndex { get => zIndex; set => Style.ZIndex = value; }

        internal int GetEffectiveZOrder()
        {
            var withSiblings = parent?.CurrentChildren.ToArray();
            if (withSiblings == null) return 0;

            return withSiblings.OrderBy(x => x.ZIndex).ThenBy(x => withSiblings.IndexOf(x)).IndexOf(this).LimitMin(0);
        }

        internal bool IsEffectivelyVisible(bool includingOpacity = false)
        {
            return Visible && !Ignored && (!includingOpacity || Opacity > 0);
        }

        /// <summary>
        /// Determines if this element is practically visible considering the Size,
        /// Position, Visible, Ignored and opacity settings of itself and its parents.
        /// </summary>
        public bool IsVisibleOnScreen()
        {
            if (this == Root) return true;

            for (var parent = this; parent != Root; parent = parent.parent)
            {
                if (parent == null) return false;
                if (!parent.IsEffectivelyVisible(includingOpacity: true)) return false;
            }

            if (ActualWidth == 0 || ActualHeight == 0) return false;

            var absoluteX = CalculateAbsoluteX();
            var absoluteY = CalculateAbsoluteY();

            if (absoluteY >= Root.ActualHeight) return false;
            if (absoluteX >= Root.ActualWidth) return false;

            if (absoluteX <= -ActualWidth) return false;
            if (absoluteY <= -ActualHeight) return false;

            return true;
        }

        public virtual async Task Initialize()
        {
            await OnInitializing();
            await OnInitialized();
        }

        public virtual async Task OnInitializing()
        {
            await (Initializing?.Raise()).OrCompleted();
            await InitializeFromMarkup();
        }

        /// <summary>
        /// Creates child objects from a ZBL markup file.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual Task InitializeFromMarkup() => Task.CompletedTask;

        public virtual async Task OnInitialized()
        {
            await (Initialized?.Raise()).OrCompleted();
            IsInitialized = true;
        }

        public virtual Task OnPreRender() => PreRendered?.Raise();

        public virtual Task OnRendered()
        {
            // Used by the renderers
            new[] { Padding.Left, Padding.Right, Padding.Top, Padding.Bottom }.Do(x => x.Changed.Event += () => PaddingChanged.Raise());

            return Rendered.Raise();
        }

        public override string ToString()
        {
            var type = CssEngine.GetCssTags(GetType()).First();

            if (id.HasValue()) type += " #" + id;
            if (cssClass.HasValue()) type += " ." + cssClass;

            return type;
        }

        protected virtual string GetStringSpecifier() => null;

        static ConcurrentDictionary<Type, FieldInfo[]> EventFields = new();

        void DisposeEvents()
        {
            var fields = EventFields.GetOrAdd(GetType(), x =>
            x.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(v => v.FieldType.IsA<AbstractAsyncEvent>()).ToArray());

            fields.Select(v => v.GetValue(this)).OfType<AbstractAsyncEvent>().Do(v => v.Dispose());            
        }

        [EscapeGCop("It's a special case, so async void is fine.")]
        public virtual async void Dispose()
        {
            using (await DomLock.Lock())
            {
                IsDisposing = true;

                DisposeEvents();
                DynamicBindings.Do(x => x.Dispose());
                DynamicBindings.Clear();

                Width?.Dispose();
                Height?.Dispose();
                X?.Dispose();
                Y?.Dispose();
                Margin?.Dispose();
                Padding?.Dispose();

                parent = null;
                AllChildren.Do(c => c.Dispose());
                AllChildren.Clear();

                Native = null;

                var renderer = Renderer;
                Renderer = null;

                void disposeNative()
                {
                    try { renderer?.Dispose(); }
                    catch (Exception ex) { Log.For(this).Error(ex, "Disposing the native renderer failed."); }
                }

                if (renderer != null)
                    if (Thread.UI.IsRunning()) disposeNative();
                    else Thread.UI.Post(disposeNative);

                IsDisposed = true;
            }
        }
    }
}