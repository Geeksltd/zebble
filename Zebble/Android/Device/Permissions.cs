namespace Zebble.Device
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Android;
    using AndroidX.Core.App;
    using AndroidX.Core.Content;
    using Olive;

    partial class Permissions
    {
        internal static readonly AsyncEvent<PermissionRequestArgs> ReceivedRequestPermissionResult = new();

        static Lazy<IList<string>> ManifestPermissions = new Lazy<IList<string>>(ReadManifestPermissions);

        public static Task<bool> ShouldShowDialog(Permission permission)
        {
            var names = GetManifestNames(permission).ToArray();

            // Android specific group?
            if (names is null) return Task.FromResult(result: false);

            var result = names.Any(n => ActivityCompat.ShouldShowRequestPermissionRationale(UIRuntime.CurrentActivity, n));

            return Task.FromResult(result);
        }

        public static Task<PermissionResult> Check(Permission permission)
        {
            var names = GetManifestNames(permission);

            if (names.None()) return Task.FromResult(PermissionResult.Granted);

            foreach (var name in names)
                if (!IsRequestedInManifest(name))
                {
                    Log.For(typeof(Permissions)).Error($"The manifest should specify '{name}' because it's required for permission: " + permission);
                    return Task.FromResult(PermissionResult.Unknown);
                }

            // All names are requested in the manifest
            if (names.Select(x => ContextCompat.CheckSelfPermission(UIRuntime.CurrentActivity, x))
                .Contains(Android.Content.PM.Permission.Denied))
                return Task.FromResult(PermissionResult.Denied);

            return Task.FromResult(PermissionResult.Granted);
        }

        public static async Task<PermissionResult> Request(Permission permission)
        {
            var result = await Check(permission);
            if (result == PermissionResult.Granted) return PermissionResult.Granted;

            var permissionsToRequest = GetManifestNames(permission);
            if (permissionsToRequest.None()) return PermissionResult.Unknown;

            var source = new TaskCompletionSource<PermissionResult>();
            var requestId = (int)permission;

            Task resultReceived(PermissionRequestArgs r)
            {
                if (r.RequestId != requestId) return Task.CompletedTask;

                ReceivedRequestPermissionResult.RemoveHandler(resultReceived);

                if (r.Result == Android.Content.PM.Permission.Granted)
                    source.TrySetResult(PermissionResult.Granted);
                else source.TrySetResult(PermissionResult.Denied);

                return Task.CompletedTask;
            }

            ReceivedRequestPermissionResult.Handle(resultReceived);

            ActivityCompat.RequestPermissions(UIRuntime.CurrentActivity, permissionsToRequest.ToArray(), requestId);

            return await source.Task;
        }

        internal static void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            for (var i = 0; i < permissions.Length; i++)
            {
                ReceivedRequestPermissionResult.Raise(new PermissionRequestArgs
                {
                    RequestId = requestCode,
                    Permission = permissions[i],
                    Result = grantResults[i]
                });
            }
        }

        internal class PermissionRequestArgs
        {
            public int RequestId;
            public string Permission;
            public Android.Content.PM.Permission Result;
        }

        static List<string> GetManifestNames(Permission permission)
        {
            var result = new List<string>();

            void report(string manifestPermission) => result.Add(manifestPermission);

            switch (permission)
            {
                case Permission.Albums:
                    report(Manifest.Permission.WriteExternalStorage); break;
                case Permission.Calendar:
                    report(Manifest.Permission.ReadCalendar);
                    report(Manifest.Permission.WriteCalendar);
                    break;
                case Permission.Camera: report(Manifest.Permission.Camera); break;
                case Permission.Contacts:
                    report(Manifest.Permission.ReadContacts);
                    report(Manifest.Permission.WriteContacts);
                    report(Manifest.Permission.GetAccounts);
                    break;
                case Permission.Location:
                    report(Manifest.Permission.AccessCoarseLocation);
                    report(Manifest.Permission.AccessFineLocation);
                    break;
                case Permission.Speech:
                case Permission.Microphone:
                    report(Manifest.Permission.RecordAudio); break;
                case Permission.PhoneCall: report(Manifest.Permission.CallPhone); break;
                case Permission.ReadPhoneState: report(Manifest.Permission.ReadPhoneState); break;
                case Permission.ReadCallLog: report(Manifest.Permission.ReadCallLog); break;
                case Permission.WriteCallLog: report(Manifest.Permission.WriteCallLog); break;
                case Permission.AddVoicemail: report(Manifest.Permission.AddVoicemail); break;
                case Permission.UseSip: report(Manifest.Permission.UseSip); break;
                case Permission.ProcessOutgoingCalls: report(Manifest.Permission.ProcessOutgoingCalls); break;
                case Permission.BodyMotions: report("BODY_SENSORS"); break;
                case Permission.SendSms: report(Manifest.Permission.SendSms); break;
                case Permission.ReceiveSms: report(Manifest.Permission.ReceiveSms); break;
                case Permission.ReadSms: report(Manifest.Permission.ReadSms); break;
                case Permission.ReceiveWapPush: report(Manifest.Permission.ReceiveWapPush); break;
                case Permission.ReceiveMms: report(Manifest.Permission.ReceiveMms); break;
                case Permission.Vibration: report(Manifest.Permission.Vibrate); break;
                case Permission.Fingerprint: report(Manifest.Permission.UseFingerprint); break;
                case Permission.ExternalStorage:
                    report(Manifest.Permission.ReadExternalStorage);
                    report(Manifest.Permission.WriteExternalStorage);
                    break;
                default: return default;
            }

            return result;
        }

        static IList<string> ReadManifestPermissions()
        {
            try
            {
                var context = UIRuntime.CurrentActivity;

                var info = context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);

                if (info?.RequestedPermissions is null)
                    throw new Exception("Failed to get Package permissions info. Ensure the required permissions are marked in the manifest.");

                return info.RequestedPermissions;
            }
            catch (Exception ex) { throw new Exception("Unable to check manifest for permission: " + ex); }
        }

        static bool IsRequestedInManifest(string permission) => ManifestPermissions.Value.Contains(permission, caseSensitive: false);
    }
}