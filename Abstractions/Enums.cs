namespace Zebble
{
    public enum OnError
    {
        /// <summary>
        /// Will throw the error. If you use this, ensure to have a try-catch somewhere in the call chain. 
        /// </summary>
        Throw,
        /// <summary>
        /// Will show a toast (nonobstructive) message to the user.
        /// </summary>
        Toast,
        /// <summary>
        /// Will show a message box to the user, waiting for them to tap OK.
        /// </summary>
        Alert,
        /// <summary>
        /// It will only log the error in the output window for use during development.
        /// </summary>
        Ignore
    }

    public enum ApiResponseCache
    {
        /// <summary>
        /// If a cache is available, that's preferred and there is no need for a fresh Web Api request.
        /// </summary>
        Prefer,

        /// <summary>
        /// If a cache is available, that's returned immediately. But a call will still be made to the server to check for an update, in which case a provided refresher delegate will be invoked.
        /// </summary>
        PreferThenUpdate,

        /// <summary>
        /// Means a new request should be sent. But if it failed and a cache is available, then that's accepted.
        /// </summary>
        Accept,

        /// <summary>
        /// A new request should be sent. But if it failed and a cache is available, then that's accepted. However a warning toast will be displayed to the user in that case to say: The latest data cannot be received from the server right now.
        /// </summary>
        AcceptButWarn,

        /// <summary>
        /// Only a fresh response from the server is acceptable, and any cache should be ignored.
        /// </summary>
        Refuse,

        /// <summary>
        /// Only a cached response will be used and a new request will not be sent.
        /// </summary>
        CacheOrNull
    }

    public enum AnimationEasing
    {
        /// <summary>Starts slow and then gets fast.</summary>
        EaseIn,
        /// <summary>Starts fast and then gets slow.</summary>
        EaseInOut,
        /// <summary>Starts slow and then gets fast, and then again gets slow towards the end.</summary>
        EaseOut,
        /// <summary>Same speed all along.</summary>
        Linear,
        /// <summary>Like a ball hitting the floor: starts slow, then gets fast to hit the end,
        /// then bounces back a bit and finally stop, </summary>
        EaseInBounceOut
    }

    public enum EasingFactor
    {
        /// <summary>Animation should accelerates or decelerates using the formula f(t) = t^2</summary>
        Quadratic = 2,
        /// <summary>Animation should accelerates or decelerates using the formula f(t) = t^3</summary>
        Cubic = 3,
        /// <summary>Animation should accelerates or decelerates using the formula f(t) = t^4</summary>
        Quartic = 4,
        /// <summary>Animation should accelerates or decelerates using the formula f(t) = t^5</summary>
        Quintic = 5
    }

    public enum TimeFormat { AMPM, Twentyfour }

    public enum DeviceConnectionType
    {
        /// <summary>For example 3G, 4G, Edge or LTE</summary>
        Cellular, WiFi, Ethernet,
#if ANDROID
        Bluetooth,
#endif
        Other
    }
}