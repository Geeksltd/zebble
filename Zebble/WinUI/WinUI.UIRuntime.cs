namespace Zebble
{
    using Microsoft.UI.Xaml;
    using Olive;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Windows.ApplicationModel.Activation;

    partial class UIRuntime
    {
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

        /// <summary>
        /// This will be called whenever a new url opens in app
        /// </summary>
        public static readonly AsyncEvent<Tuple<IActivatedEventArgs, Window>> OnActivated = new();

        public static readonly AsyncEvent<Dictionary<string, string>> OnParameterRecieved = new();

        public static bool SkipPageRefresh = false;
    }
}