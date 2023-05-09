namespace Zebble.Device
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public enum Permission
    {
        Albums, Camera, Microphone, BodyMotions, Location, Contacts, Calendar, Reminders, PhoneCall, SendSms, Speech,
        LocalNotification, Vibration, Fingerprint

#if ANDROID
              , ReadPhoneState, ReadCallLog, WriteCallLog, AddVoicemail, UseSip, ProcessOutgoingCalls,
        ReceiveSms, ReadSms, ReceiveWapPush, ReceiveMms, ExternalStorage, RecordAudio
#elif IOS
        , BackgroundLocation
#endif
    }

    public enum PermissionResult
    {
        Granted,
        Denied,

        /// <summary>Currently only used for location services.</summary>
        FeatureDisabled,

        /// <summary>Restricted - only for iOS</summary>
        Restricted,
        Unknown
    }

    public static partial class Permissions
    {
        public static async Task<Dictionary<Permission, PermissionResult>> RequestAll(params Permission[] permissions)
        {
            var result = new Dictionary<Permission, PermissionResult>();

            foreach (var p in permissions) result.Add(p, await Request(p));

            return result;
        }
    }
}