namespace Zebble.AndroidOS
{
    using System;
    using Android.App;
    using Android.OS;
    using Android.Runtime;

    public abstract class BaseApplication : Application, Application.IActivityLifecycleCallbacks
    {
        [Preserve]
        protected BaseApplication(IntPtr handle, JniHandleOwnership transer) : base(handle, transer) { }

        public override void OnCreate()
        {
            Renderer.appContext = ApplicationContext;

            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                var stackTracep = e.Exception.StackTrace;
                throw e.Exception;
            };

            base.OnCreate();
            RegisterActivityLifecycleCallbacks(this);
        }

        public override void OnLowMemory()
        {
            Device.App.RaiseReceivedMemoryWarning();
            base.OnLowMemory();
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }

        public virtual void OnActivityCreated(Activity activity, Bundle savedInstanceState) { }

        public virtual void OnActivityDestroyed(Activity activity) { }

        public virtual void OnActivityPaused(Activity activity) { }

        public virtual void OnActivityResumed(Activity activity) { }

        public virtual void OnActivitySaveInstanceState(Activity activity, Bundle outState) { }

        public virtual void OnActivityStarted(Activity activity) { }

        public virtual void OnActivityStopped(Activity activity) { }
    }
}