namespace Zebble.Device
{
    using System;
    using System.Threading.Tasks;
    using Windows.ApplicationModel.Contacts;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Geolocation;
    using Olive;

    partial class Permissions
    {
        static readonly Guid SENSOR_CLASS_ID = "9D9E0118-1807-4F2E-96E4-2CE57142E196".To<Guid>();

        public static Task<bool> ShouldShowDialog(Permission permission) => Task.FromResult(result: false);

        /// <summary>
        /// Checks if a permission is already granted.
        /// </summary>
        public static async Task<PermissionResult> Check(Permission permission)
        {
            switch (permission)
            {
                case Permission.PhoneCall:
                case Permission.Albums:
                case Permission.Reminders:
                case Permission.Calendar:
                case Permission.Camera:
                case Permission.SendSms:
                case Permission.Vibration:
                case Permission.Microphone:
                case Permission.Speech:
                case Permission.Fingerprint:
                    return PermissionResult.Granted;

                case Permission.Location: return await RequestLocationAccess();
                case Permission.BodyMotions: return RequestBodyMotions();

                case Permission.Contacts:
                    return await CanAccessContacts() ? PermissionResult.Granted : PermissionResult.Denied;

                case Permission.LocalNotification: return PermissionResult.Granted;

                default: throw new NotImplementedException(permission + " is not recognized.");
            }
        }

        static async Task<bool> CanAccessContacts()
        {
            return await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite) != null;
        }

        static async Task<PermissionResult> RequestLocationAccess()
        {
            var status = await Thread.UI.Run(() => Geolocator.RequestAccessAsync().AsTask());

            switch (status)
            {
                case GeolocationAccessStatus.Allowed: return PermissionResult.Granted;
                case GeolocationAccessStatus.Unspecified: return PermissionResult.Unknown;
                default: return PermissionResult.Denied;
            }
        }

        static PermissionResult RequestBodyMotions()
        {
            switch (DeviceAccessInformation.CreateFromDeviceClassId(SENSOR_CLASS_ID)?.CurrentStatus)
            {
                case DeviceAccessStatus.Allowed: return PermissionResult.Granted;

                case DeviceAccessStatus.DeniedBySystem:
                case DeviceAccessStatus.DeniedByUser: return PermissionResult.Denied;

                default: return PermissionResult.Unknown;
            }
        }

        /// <summary>
        /// Asks the user to grant or deny a permission.
        /// </summary>
        static public Task<PermissionResult> Request(Permission permission) => Check(permission);
    }
}