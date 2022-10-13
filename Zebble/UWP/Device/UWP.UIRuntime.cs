namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using Windows.ApplicationModel.Activation;
    using Windows.UI.Xaml;
    using Olive;

    partial class UIRuntime
    {
        /// <summary>
        /// This will be called whenever a new url opens in app
        /// </summary>
        public static readonly AsyncEvent<Tuple<IActivatedEventArgs, Window>> OnActivated = new();

        public static readonly AsyncEvent<Dictionary<string, string>> OnParameterRecieved = new();

        private static bool? isDevMode;

        /// <summary>
        /// Determines if it's running on a Desktop and "Dev.Mode" config is true.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsDevMode
        {
            get
            {

                if (isDevMode is null)
                {
                    try { isDevMode = Device.App.IsDesktop() && Config.Get("Dev.Mode", defaultValue: false); }
                    catch { return false; }
                }

                return isDevMode.Value;
            }
            set
            {
                isDevMode = value;
            }
        }

        public static bool SkipPageRefresh = false;
    }
}