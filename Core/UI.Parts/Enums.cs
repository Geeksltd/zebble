namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Zebble.Device;
    using Olive;

    public enum SpellCheckingType { Default = 0, No = 1, Yes = 2 }

    public enum AutoCorrectionType { Default = 0, No = 1, Yes = 2 }

    public enum AutoCapitalizationType { None = 0, Words = 1, Sentences = 2, AllCharacters = 3 }

    public enum Alignment { TopLeft = 0, TopRight = 1, TopMiddle = 2, Top = 11, Left = 3, Right = 4, Middle = 5, Bottom = 12, BottomLeft = 6, BottomRight = 7, BottomMiddle = 8, None = 9, Justify = 10 }

    public enum RepeatDirection { Horizontal, Vertical }

    public enum TextTransform
    {
        None, Uppercase, Lowercase,
        /// <summary>Transforms the first character of each word to uppercase</summary>
        Capitalize
    }

    public enum DeviceSharingOption
    {
        AssignToContact,
        CopyToPasteboard,
        Mail,
        Message,
        PostToFacebook,
        PostToTwitter,
        PostToWeibo,
        Print,
        SaveToCameraRoll,
        AddToReadingList,
        AirDrop,
        PostToFlickr,
        PostToTencentWeibo,
        PostToVimeo,
        OpenInIBooks,
    }

    public enum HorizontalAlignment { Left, Center, Right };

    public enum VerticalAlignment { Top, Middle, Bottom };

    public enum Direction { Left, Right, Up, Down }

    public enum TextMode { Auto, GeneralText, Email, Password, Telephone, Url, Integer, Decimal, PersonName }

    public enum KeyboardActionType { Default, Done, Go, Next, Search, Send }

    public enum SensorDelay { Realtime = 0, Game = 16, UI = 60, Background = 200 }

    public enum Stretch
    {
        Default = 3,
        /// <summary>
        /// Stretches the image to completely and exactly fill the display area. This may result in the image being distorted.
        /// </summary>
        Fill = 1,
        /// <summary>
        /// Letterboxes the image (if required) so that the entire image fits into the display area. This may result in blank spaces around the image if aspect ratio of the image differs from that of the display area.
        /// </summary>
        Fit = 0,
        /// <summary>
        /// Clips the image so that it fills the display area while preserving the aspect (ie. no distortion).
        /// </summary>
        AspectFill = 2
    }

    public enum DevicePlatform { IOS, Android, Windows }

    public enum BackgroundRepeat { X, Y }

    public enum DeviceOrientation { Portrait, Landscape }

    public enum ButtonLocation { Left, Right }

    public enum MediaSource
    {
        PickPhoto, TakePhoto, PickVideo, TakeVideo,
#if ANDROID
        GoogleDrive
#elif IOS
        ICloud
#endif
    }

    public enum ShareType { Text, Link, Clipboard, Browser }

    public enum IconLocation { Left, Right }

    public enum VideoQuality { High, Medium, Low }

    public enum CameraOption { Rear, Front }

    public enum RevisitMode { NewParams, SameParams }

    public static class EnumExtensions
    {
        public static HorizontalAlignment ToHorizontalAlignment(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.BottomLeft:
                case Alignment.Left:
                case Alignment.TopLeft: return HorizontalAlignment.Left;
                case Alignment.Right:
                case Alignment.BottomRight:
                case Alignment.TopRight: return HorizontalAlignment.Right;
                default: return HorizontalAlignment.Center;
            }
        }

        public static VerticalAlignment ToVerticalAlignment(this Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.TopLeft:
                case Alignment.TopMiddle:
                case Alignment.TopRight: return VerticalAlignment.Top;
                case Alignment.Middle:
                case Alignment.Left:
                case Alignment.Right: return VerticalAlignment.Middle;
                default: return VerticalAlignment.Bottom;
            }
        }

        public static PageTransition GetReverse(this PageTransition transition)
        {
            switch (transition)
            {
                case PageTransition.Fade: return PageTransition.Fade;
                case PageTransition.SlideDown: return PageTransition.SlideUp;
                case PageTransition.SlideUp: return PageTransition.SlideDown;
                case PageTransition.SlideForward: return PageTransition.SlideBack;
                case PageTransition.SlideBack: return PageTransition.SlideForward;
                case PageTransition.DropDown: return PageTransition.DropUp;
                case PageTransition.DropUp: return PageTransition.DropDown;
                default: return PageTransition.None;
            }
        }

        public static string Apply(this TextTransform transform, string text)
        {
            text = text.OrEmpty();
            switch (transform)
            {
                case TextTransform.None: return text;
                case TextTransform.Lowercase: return text.ToLower();
                case TextTransform.Uppercase: return text.ToUpper();
                case TextTransform.Capitalize: return text.CapitaliseFirstLetters();
                default: throw new NotImplementedException("TextTransform of " + transform + " is not implemented.");
            }
        }

        /// <summary>
        /// Determines if this permission is already granted, but will not request the user for permission.
        /// </summary>
        public static async Task<bool> IsGranted(this Permission permission) => PermissionResult.Granted == await Permissions.Check(permission);

        /// <summary>
        /// Determines if this permission is already granted, or else, it will request the user for permission.
        /// </summary>
        public static async Task<bool> IsRequestGranted(this Permission permission) => PermissionResult.Granted == await Permissions.Request(permission);

        public static Task<PermissionResult> Request(this Permission permission) => Permissions.Request(permission);

        public static Task Apply(this OnError strategy, string error) => Apply(strategy, new Exception(error));

        public static Task Apply(this OnError strategy, Exception error, string friendlyMessage = null)
        {
            if (error is null) return Task.CompletedTask;

            Log.For(typeof(EnumExtensions)).Error(error, friendlyMessage.WithSuffix(": " + error));

            switch (strategy)
            {
                case OnError.Ignore: return Task.CompletedTask;
                case OnError.Throw: throw error;
                case OnError.Toast: return Alert.Toast(friendlyMessage.Or(error.Message));
                case OnError.Alert: return Alert.Show(friendlyMessage.Or(error.Message));
                default: throw new NotSupportedException(strategy + " is not implemented.");
            }
        }

        public static bool IsAndroid(this DevicePlatform platform) => platform == DevicePlatform.Android;

        public static bool IsIOS(this DevicePlatform platform) => platform == DevicePlatform.IOS;

        public static bool IsWindows(this DevicePlatform platform) => platform == DevicePlatform.Windows;

        /// <summary>True if this direction is Left or Right.</summary>
        public static bool IsHorizontal(this Direction dir) => dir == Direction.Left || dir == Direction.Right;

        /// <summary>True if this direction is Up or Down.</summary>
        public static bool IsVertical(this Direction dir) => dir == Direction.Up || dir == Direction.Down;
    }
}