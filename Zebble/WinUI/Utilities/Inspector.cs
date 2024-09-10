using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Zebble.WinUI;
using Olive;

namespace Zebble
{
    partial class UIRuntime
    {
        static IInspector inspector;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IInspector Inspector
        {
            get
            {
                if (inspector != null) return inspector;
                try
                {
                    var type = Config.Get("Zebble.Inspector.Type", "Zebble.WinUI.Inspector, Zebble.Inspector");
                    var inspectorType = Type.GetType(type);
                    if (inspectorType is null) throw new RenderException("Type not found: " + type);
                    inspector = inspectorType.CreateInstance() as IInspector;
                    if (inspector is null) throw new RenderException(inspectorType.FullName + " is not an IInspector!");
                    return inspector;
                }
                catch (Exception ex)
                {
                    throw new RenderException("Failed to create the inspector: " + ex.Message);
                }
            }
        }
    }

    namespace WinUI
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public interface IInspector
        {
            int CurrentWidth { get; }
            bool IsRotating { get; }
            Task PrepareRuntimeRoot();
            Task Collapse();
            Task Load(View view = null);
            Task DomUpdated(View view);
        }
    }
}