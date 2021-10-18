namespace Zebble.IOS
{
    using System;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;
    using UserNotifications;

    public abstract class BaseAppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        public override void OnActivated(UIApplication app)
        {
            Device.App.RaiseStarted();
        }

        public override void DidEnterBackground(UIApplication app)
        {
            Device.App.RaiseWentIntoBackground();
            if (Device.Keyboard.IsOpen) Device.Keyboard.CollapseScrollerForKeyboard();
        }

        public override void WillTerminate(UIApplication application) => Device.App.RaiseStopping();

        public override void ReceiveMemoryWarning(UIApplication app) => Device.App.RaiseReceivedMemoryWarning();

        public override void OnResignActivation(UIApplication app)
        {
            // Invoked when the application is about to move from active to inactive state.
            // This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message)
            // or when the user quits the application and it begins the transition to the background state.
            // Games should use this method to pause the game.
        }

        public override void WillEnterForeground(UIApplication app)
        {
            // Called as part of the transition from background to active state.
            // Here you can undo many of the changes made on entering the background.
            Device.App.RaiseCameToForeground();
        }

        protected bool IsReady;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            UIThread.Dispatcher = this;

            Device.Screen.StatusBar.Height = (float)UIApplication.SharedApplication.StatusBarFrame.Height;

            Initialize();

            UNUserNotificationCenter.Current.Delegate = new NotificationDelegate();

            //while (!IsReady) Task.Delay(100).GetAwaiter().GetResult();

            UIRuntime.OnFinishedLaunching.Raise();

            return true;
        }

        protected abstract Task Initialize();

        public virtual void InitializeComponents() { }

        public override async void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            var receivedNewData = await UIRuntime.DidReceiveRemoteNotification.Invoke(userInfo);
            completionHandler(receivedNewData ? UIBackgroundFetchResult.NewData : UIBackgroundFetchResult.NoData);
        }

        public override async void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            await UIRuntime.FailedToRegisterForRemoteNotifications.Raise(error);
        }

        public override async void RegisteredForRemoteNotifications(UIApplication application, NSData token)
        {
            await UIRuntime.RegisteredForRemoteNotifications.Raise(token);
        }

        // Note: Does the mere presence of this override cause a problem when not using background notifications?
        public override async void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            await UIRuntime.DidReceiveRemoteNotification.Invoke(userInfo);
        }

        public override void ReceivedLocalNotification(UIApplication application, UILocalNotification notification)
        {
            UIRuntime.OnParameterRecieved.Raise(notification.UserInfo);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            UIRuntime.OnOpenUrl?.Raise(url);
            return base.OpenUrl(application, url, sourceApplication, annotation);
        }
        
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            return OpenUrl(app, url, sourceApplication: null, options);
        }

        public bool OpenUrl(UIApplication app, NSUrl url, string sourceApplication, NSDictionary options)
        {
            UIRuntime.OnOpenUrlWithOptions?.Raise(new Tuple<UIApplication, NSUrl, string, NSDictionary>(app, url, sourceApplication, options));
            return true;
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