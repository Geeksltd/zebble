namespace Zebble
{
    using System;
    using System.Threading.Tasks;
#if IOS
    using BaseNativeType = UIKit.UIView;
#elif ANDROID
    using Android.Runtime;
    using BaseNativeType = Android.Views.View;
    [Preserve]
#elif UWP
    using BaseNativeType = Windows.UI.Xaml.FrameworkElement;
#else
    using BaseNativeType = object;
#endif

    public interface IRenderedBy<TRenderer> : IRenderedBy where TRenderer : INativeRenderer { }

#if ANDROID
    [Preserve]
#endif
    [Obsolete("Use IRenderedBy<TRenderer> instead.")]
    public interface IRenderedBy { }

#if ANDROID
    [Preserve]
#endif
    public interface INativeRenderer : IDisposable { Task<BaseNativeType> Render(Renderer renderer); }
}