namespace Zebble.AndroidOS
{
    using System;
    using Android.App;
    using Android.Content;
    using Android.Content.PM;
    using Android.Content.Res;
    using Android.OS;
    using Android.Runtime;
    using AndroidX.AppCompat.App;
    using Zebble;
    using Olive;
    using Android.Views;

    public class BaseActivity : AppCompatActivity
    {
        protected static bool IsFirstRun = true;
        public static Android.Views.View RootView = null;

        public static BaseActivity Current => (BaseActivity)UIRuntime.CurrentActivity;

        bool IsWindowFocused, IsBackPressed, GoneToBackground;

        public BaseActivity() => UIRuntime.CurrentActivity = this;

        [Preserve]
        protected BaseActivity(IntPtr reference, JniHandleOwnership handle) : base(reference, handle)
        {
            UIRuntime.CurrentActivity = this;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            ActionBar?.Hide();

            OnNewIntent(Intent);

            DetectSoftKeyboardHeight();
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            Device.Screen.OrientationChanged.SignalRaiseOn(Thread.Pool);
        }

        public override async void OnBackPressed()
        {
            IsBackPressed = true;
            await Nav.OnHardwareBack();
        }

        protected override void OnResume()
        {
            try { base.OnResume(); }
            catch (Java.Lang.IllegalArgumentException error) { Log.For(this).Error(error); }

            if (GoneToBackground) return;
            if (IsFinishing) return;

            Device.App.RaiseCameToForeground();
        }

        protected override void OnPause()
        {
            if (Device.Keyboard.IsOpen)
            {
                Device.Keyboard.KeepKeyboardOpen = true;
                Window?.SetSoftInputMode(SoftInput.StateAlwaysVisible);
                var token = CurrentFocus?.WindowToken;
                Device.Keyboard.InputManager?.HideSoftInputFromWindow(token, Android.Views.InputMethods.HideSoftInputFlags.ImplicitOnly);
                UIRuntime.CurrentActivity?.Window?.SetSoftInputMode(SoftInput.AdjustResize);
            }

            Device.App.RaiseWentIntoBackground();

            base.OnPause();
        }

        protected override void OnStart()
        {
            if (GoneToBackground)
            {
                GoneToBackground = false;
                Device.App.RaiseStarted();
            }

            base.OnStart();
        }

        protected override void OnStop()
        {
            if (!IsWindowFocused)
            {
                GoneToBackground = true;
                Device.App.RaiseWentIntoBackground();
            }

            if (IsFinishing) Device.App.RaiseStopping();

            base.OnStop();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            IsWindowFocused = hasFocus;

            if (IsBackPressed && !hasFocus)
            {
                IsBackPressed = false;
                IsWindowFocused = true;
            }

            base.OnWindowFocusChanged(hasFocus);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Device.Permissions.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            UIRuntime.OnNewIntent?.Raise(intent);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            UIRuntime.OnActivityResult.Raise(new Tuple<int, Result, Intent>(requestCode, resultCode, data));
        }

        public void DetectSoftKeyboardHeight()
        {
            RootView = this.FindViewById(Android.Resource.Id.Content);
            if (RootView != null)
                RootView.ViewTreeObserver.AddOnGlobalLayoutListener(new MyLayoutListener());
        }
    }

    public class MyLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        public void OnGlobalLayout()
        {
            const int KeyboardVisibilityValue = 150;

            if (BaseActivity.RootView == null) return;
            var diff = BaseActivity.RootView.RootView.Height - BaseActivity.RootView.Height;
            if (diff > KeyboardVisibilityValue)
            {
                Device.Keyboard.SoftKeyboardHeight = diff;
                Device.Keyboard.RaiseShown();
            }
        }
    }
}