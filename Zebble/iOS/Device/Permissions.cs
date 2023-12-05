namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using AddressBook;
    using AVFoundation;
    using CoreLocation;
    using CoreMotion;
    using EventKit;
    using Foundation;
    using Speech;
    using Photos;
    using UIKit;
    using UserNotifications;
    using Olive;

    partial class Permissions
    {
        static CLLocationManager locationManager;

        public static Task<bool> ShouldShowDialog(Permission permission) => Task.FromResult(result: false);

        public static Task<PermissionResult> Check(Permission permission) => Task.FromResult(DoCheck(permission));

        static PermissionResult DoCheck(Permission permission)
        {
            switch (permission)
            {
                case Permission.Contacts: return CheckContacts();
                case Permission.Location: return CheckLocation();
                case Permission.BackgroundLocation: return CheckBackgroundLocation();
                case Permission.Calendar: return CheckEvents(EKEntityType.Event);
                case Permission.Camera: return CheckMedia(AVAuthorizationMediaType.Video);
                case Permission.Microphone: return CheckMedia(AVAuthorizationMediaType.Audio);
                case Permission.Albums: return CheckPhotos();
                case Permission.Reminders: return CheckEvents(EKEntityType.Reminder);
                case Permission.BodyMotions: return CheckBodyMotions();
                case Permission.Speech: return CheckSpeech();
                case Permission.LocalNotification: return CheckLocalNotification();
                default: return PermissionResult.Granted;
            }
        }

        public static async Task<PermissionResult> Request(Permission permission)
        {
            if (DoCheck(permission) == PermissionResult.Granted) return PermissionResult.Granted;

            switch (permission)
            {
                case Permission.Calendar: return await RequestEvents(EKEntityType.Event);
                case Permission.Camera: return await RequestMedia(AVAuthorizationMediaType.Video);
                case Permission.Microphone: return await RequestMedia(AVAuthorizationMediaType.Audio);
                case Permission.Contacts: return await RequestContacts();
                case Permission.Location: return await RequestLocation();
                case Permission.Albums: return await RequestPhotos();
                case Permission.Reminders: return await RequestEvents(EKEntityType.Reminder);
                case Permission.BodyMotions: return await RequestBodyMotions();
                case Permission.Speech: return await RequestSpeech();
                case Permission.LocalNotification: return await RequestLocalNotification();
                default: return PermissionResult.Granted;
            }
        }

        static PermissionResult CheckMedia(AVAuthorizationMediaType mediaType)
        {
            switch (AVCaptureDevice.GetAuthorizationStatus(mediaType))
            {
                case AVAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case AVAuthorizationStatus.Denied: return PermissionResult.Denied;
                case AVAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static PermissionResult CheckContacts()
        {
            switch (ABAddressBook.GetAuthorizationStatus())
            {
                case ABAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case ABAuthorizationStatus.Denied: return PermissionResult.Denied;
                case ABAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static Task<PermissionResult> RequestContacts()
        {
            using (var addressBook = new ABAddressBook())
            {
                var source = new TaskCompletionSource<PermissionResult>();

                addressBook.RequestAccess((success, _) =>
                    source.TrySetResult(success ? PermissionResult.Granted : PermissionResult.Denied)
                );

                return source.Task;
            }
        }

        static async Task<PermissionResult> RequestMedia(AVAuthorizationMediaType mediaType)
        {
            try
            {
                var access = await AVCaptureDevice.RequestAccessForMediaTypeAsync(mediaType);
                return access ? PermissionResult.Granted : PermissionResult.Denied;
            }
            catch (Exception ex)
            {
                Log.For(typeof(Permissions)).Error(ex, $"Failed to get {mediaType} permission.");
                return PermissionResult.Unknown;
            }
        }

        static PermissionResult CheckEvents(EKEntityType eventType)
        {
            switch (EKEventStore.GetAuthorizationStatus(eventType))
            {
                case EKAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case EKAuthorizationStatus.Denied: return PermissionResult.Denied;
                case EKAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static async Task<PermissionResult> RequestEvents(EKEntityType eventType)
        {
            using (var eventStore = new EKEventStore())
            {
                if ((await eventStore.RequestAccessAsync(eventType)).Item1) return PermissionResult.Granted;
                else return PermissionResult.Denied;
            }
        }

        static PermissionResult CheckLocation()
        {
            if (!CLLocationManager.LocationServicesEnabled) return PermissionResult.FeatureDisabled;

            if (CLLocationManager.Status == CLAuthorizationStatus.AuthorizedAlways)
                return PermissionResult.Granted;

            switch (CLLocationManager.Status)
            {
                case CLAuthorizationStatus.AuthorizedWhenInUse:
                case CLAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case CLAuthorizationStatus.Denied: return PermissionResult.Denied;
                case CLAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static PermissionResult CheckBackgroundLocation()
        {
            if (Device.OS.IsBeforeiOS(9)) return PermissionResult.Denied;

            var backgroundModes = NSBundle.MainBundle.InfoDictionary[(NSString)"UIBackgroundModes"] as NSArray;

            if (backgroundModes is null) return PermissionResult.Denied;

            if (backgroundModes.Contains((NSString)"Location")) return PermissionResult.Granted;
            if (backgroundModes.Contains((NSString)"location")) return PermissionResult.Granted;

            return PermissionResult.Denied;
        }

        static Task<PermissionResult> RequestLocation()
        {
            if (Device.OS.IsBeforeiOS(8)) return Task.FromResult(PermissionResult.Unknown);

            locationManager = new CLLocationManager();

            var source = new TaskCompletionSource<PermissionResult>();

            EventHandler<CLAuthorizationChangedEventArgs> authCallback = null;

            authCallback = (sender, e) =>
            {
                if (e.Status != CLAuthorizationStatus.NotDetermined)
                {
                    locationManager.AuthorizationChanged -= authCallback;
                    source.TrySetResult(CheckLocation());
                }
            };

            locationManager.AuthorizationChanged += authCallback;

            if (NSBundle.MainBundle.InfoDictionary.ContainsKey("NSLocationWhenInUseUsageDescription".ToNs()))
            {
                locationManager.RequestWhenInUseAuthorization();
            }
            else if (NSBundle.MainBundle.InfoDictionary.ContainsKey("NSLocationAlwaysAndWhenInUseUsageDescription".ToNs())
                    || NSBundle.MainBundle.InfoDictionary.ContainsKey("NSLocationAlwaysUsageDescription".ToNs()))
            {
                locationManager.RequestAlwaysAuthorization();
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "Failed to request for location. You should specify NSLocationAlwaysUsageDescription or NSLocationWhenInUseUsageDescription in Info.plist.");
            }

            return source.Task;
        }

        static PermissionResult CheckPhotos()
        {
            switch (PHPhotoLibrary.AuthorizationStatus)
            {
                case PHAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case PHAuthorizationStatus.Denied: return PermissionResult.Denied;
                case PHAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static Task<PermissionResult> RequestPhotos()
        {
            var source = new TaskCompletionSource<PermissionResult>();

            PHPhotoLibrary.RequestAuthorization(status =>
            {
                switch (status)
                {
                    case PHAuthorizationStatus.Authorized: source.TrySetResult(PermissionResult.Granted); break;
                    case PHAuthorizationStatus.Denied: source.TrySetResult(PermissionResult.Denied); break;
                    case PHAuthorizationStatus.Restricted: source.TrySetResult(PermissionResult.Restricted); break;
                    default: source.TrySetResult(PermissionResult.Unknown); break;
                }
            });

            return source.Task;
        }

        static PermissionResult CheckBodyMotions()
        {
            return CMMotionActivityManager.IsActivityAvailable ? PermissionResult.Granted : PermissionResult.Denied;
        }

        static async Task<PermissionResult> RequestBodyMotions()
        {
            using (var activityManager = new CMMotionActivityManager())
            {
                try
                {
                    var results = await activityManager
                        .QueryActivityAsync(NSDate.DistantPast, NSDate.DistantFuture, NSOperationQueue.MainQueue);

                    if (results != null) return PermissionResult.Granted;
                }
                catch (Exception ex)
                {
                    Log.For(typeof(Permissions)).Error(ex, "Failed to query activity manager.");
                    return PermissionResult.Denied;
                }
            }

            return PermissionResult.Unknown;
        }

        static PermissionResult CheckSpeech()
        {
            switch (SFSpeechRecognizer.AuthorizationStatus)
            {
                case SFSpeechRecognizerAuthorizationStatus.Authorized: return PermissionResult.Granted;
                case SFSpeechRecognizerAuthorizationStatus.Denied: return PermissionResult.Denied;
                case SFSpeechRecognizerAuthorizationStatus.Restricted: return PermissionResult.Restricted;
                default: return PermissionResult.Unknown;
            }
        }

        static Task<PermissionResult> RequestSpeech()
        {
            if (Device.OS.IsBeforeiOS(10)) return Task.FromResult(PermissionResult.Unknown);

            var source = new TaskCompletionSource<PermissionResult>();

            SFSpeechRecognizer.RequestAuthorization(status =>
            {
                switch (status)
                {
                    case SFSpeechRecognizerAuthorizationStatus.Authorized: source.TrySetResult(PermissionResult.Granted); break;
                    case SFSpeechRecognizerAuthorizationStatus.Denied: source.TrySetResult(PermissionResult.Denied); break;
                    case SFSpeechRecognizerAuthorizationStatus.Restricted: source.TrySetResult(PermissionResult.Restricted); break;
                    default: source.TrySetResult(PermissionResult.Unknown); break;
                }
            });
            return source.Task;
        }

        static PermissionResult CheckLocalNotification()
        {
            if (Device.OS.IsAtLeastiOS(10))
            {
                switch (UNUserNotificationCenter.Current.GetNotificationSettingsAsync().GetAwaiter().GetResult().AuthorizationStatus)
                {
                    case UNAuthorizationStatus.Authorized: return PermissionResult.Granted;
                    case UNAuthorizationStatus.Denied: return PermissionResult.Denied;
                    case UNAuthorizationStatus.NotDetermined: default: return PermissionResult.Unknown;
                }
            }
            else if (Device.OS.IsAtLeastiOS(8))
            {
                switch (UIApplication.SharedApplication.CurrentUserNotificationSettings.Types)
                {
                    case UIUserNotificationType.Alert | UIUserNotificationType.Sound | UIUserNotificationType.Badge:
                        return PermissionResult.Granted;
                    case UIUserNotificationType.None: return PermissionResult.Denied;
                    default: return PermissionResult.Unknown;
                }
            }
            else return PermissionResult.Unknown;
        }

        static Task<PermissionResult> RequestLocalNotification()
        {
            var source = new TaskCompletionSource<PermissionResult>();

            if (Device.OS.IsAtLeastiOS(10))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(
                        UNAuthorizationOptions.Alert |
                        UNAuthorizationOptions.Badge |
                        UNAuthorizationOptions.Sound,
                        (approved, error) =>
                        {
                            source.TrySetResult(approved ? PermissionResult.Granted : PermissionResult.Denied);
                        });
            }
            else if (Device.OS.IsAtLeastiOS(8))
            {
                var settings = UIUserNotificationSettings.GetSettingsForTypes(
                        UIUserNotificationType.Alert |
                        UIUserNotificationType.Badge |
                        UIUserNotificationType.Sound,
                        new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);

                source.TrySetResult(CheckLocalNotification());
            }

            return source.Task;
        }
    }
}