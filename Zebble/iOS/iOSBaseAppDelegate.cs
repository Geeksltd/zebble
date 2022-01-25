namespace Zebble.IOS
{
    using System;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;
    using UserNotifications;

    public abstract class iOSBaseAppDelegate : UIResponder , IUIApplicationDelegate
    {
        protected bool IsReady;

        [Export("window")]
        public UIWindow Window { get; set; }

        protected abstract Task Initialize();

        public virtual void InitializeComponents() { }

        [Export("applicationDidBecomeActive:")]
        public virtual void OnActivated(UIApplication application)
        {
            Device.App.RaiseStarted();
        }

        [Export("applicationDidEnterBackground:")]
        public virtual void DidEnterBackground(UIApplication application)
        {
            Device.App.RaiseWentIntoBackground();
            if (Device.Keyboard.IsOpen) Device.Keyboard.CollapseScrollerForKeyboard();
        }

        [Export("applicationWillTerminate:")]
        public virtual void WillTerminate(UIApplication application) => Device.App.RaiseStopping();

        [Export("applicationDidReceiveMemoryWarning:")]
        public virtual void ReceiveMemoryWarning(UIApplication application) => Device.App.RaiseReceivedMemoryWarning();

        [Export("applicationWillEnterForeground:")]
        public virtual void WillEnterForeground(UIApplication application)
        {
            // Called as part of the transition from background to active state.
            // Here you can undo many of the changes made on entering the background.
            Device.App.RaiseCameToForeground();
        }

        [Export("application:didFinishLaunchingWithOptions:")]
        public virtual bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Override point for customization after application launch.

            UIThread.Dispatcher = this;

            Device.Screen.StatusBar.Height = (float)UIApplication.SharedApplication.StatusBarFrame.Height;

            Initialize();

            UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();

            //while (!IsReady) Task.Delay(100).GetAwaiter().GetResult();

            UIRuntime.OnFinishedLaunching.Raise();

            return true;
        }

        [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
        public virtual async void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            if (UIRuntime.DidReceiveRemoteNotification == null) return;

            var receivedNewData = await UIRuntime.DidReceiveRemoteNotification.Invoke(userInfo);
            completionHandler(receivedNewData ? UIBackgroundFetchResult.NewData : UIBackgroundFetchResult.NoData);
        }

        [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
        public virtual async void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            await UIRuntime.FailedToRegisterForRemoteNotifications.Raise(error);
        }

        [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
        public virtual async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            await UIRuntime.RegisteredForRemoteNotifications.Raise(deviceToken);
        }

        [Export("application:didReceiveRemoteNotification:")]
        public virtual async void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            await UIRuntime.DidReceiveRemoteNotification?.Invoke(userInfo);
        }

        [Export("application:didReceiveLocalNotification:")]
        public virtual void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            UIRuntime.OnParameterRecieved.Raise(notification.UserInfo);
        }

        internal class NotificationDelegate : UNUserNotificationCenterDelegate
        {
            public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
            {
                completionHandler(UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound);
            }
        }
    }
}