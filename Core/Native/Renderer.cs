namespace Zebble
{
    using Olive;
    using System;
    using System.Reflection;
    using System.Linq;
    using System.Threading.Tasks;
#if IOS
    using BaseNativeType = UIKit.UIView;
#elif ANDROID
    using BaseNativeType = Android.Views.View;
#elif UWP
    using BaseNativeType = Windows.UI.Xaml.FrameworkElement;
#elif MAUIWindows
using BaseNativeType = object;
#endif

    internal interface IRenderOrchestrator : IDisposable { Task<BaseNativeType> Render(); }

    public partial class Renderer : IDisposable
    {
        public View View { get; private set; }
        public Renderer(View view) => View = view;
        IRenderOrchestrator RenderOrchestrator;
        bool IsDisposing;

        [EscapeGCop("The name actually refers to something else.")]
        internal BaseNativeType CreateFromNativeRenderer()
        {
            var rendererType = View.GetType().GetInterfaces().Where(i => i.Name.StartsWith(nameof(IRenderedBy)))
                 .FirstOrDefault(i => i.GenericTypeArguments?.Any() == true);

            if (rendererType is null)
                throw new RenderException(View.GetType() + " should implement IRenderedBy<...>");

            rendererType = rendererType.GenericTypeArguments.Single();

            var renderer = (INativeRenderer)rendererType.CreateInstance();

            return renderer.Render(this).GetAlreadyCompletedResult();
        }

        /// <summary>
        /// Gets the current view if it's not disposing. 
        /// </summary>
        [EscapeGCop("In this case an out parameter can improve the code.")]
        public bool IsDead(out View result)
        {
            result = View;
            if (IsDisposing || result is null) return true;
            return result.IsDisposing;
        }
    }
}