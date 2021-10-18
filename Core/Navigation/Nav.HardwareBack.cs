namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class Nav
    {
        static Stack<KeyValuePair<Type, object>> History = new();

        public static bool IsHardwareBackBlocked;

        public static readonly AsyncEvent<HardwareBackEventArgs> HardwareBack =
            new AsyncEvent<HardwareBackEventArgs>(ConcurrentEventRaisePolicy.Ignore);

        static void AddToHistory(Page page)
        {
            if (IsHardwareBackBlocked) return;
            History.Push(Pair.Of<Type, object>(page.GetType(), page.NavParams));
        }

        public static bool CanGoBack() => Stack.Any() || PopUps.Any();

        public static async Task OnHardwareBack()
        {
            if (IsHardwareBackBlocked) return;
            IsHardwareBackBlocked = true;

            try
            {
                if (HardwareBack.IsHandled())
                {
                    var args = new HardwareBackEventArgs(CurrentPage);
                    await HardwareBack.Raise(args);
                    if (args.Cancel) return;
                }

                if (!CanGoBack())
                {
#if ANDROID
                    if (Config.Get("Android.Hardware.Back.Can.Close", defaultValue: false))
                        Device.OS.OpenHomeScreen();
#endif
                    return;
                }

                if (PopUps.Any()) await HidePopUp();
                else if (Stack.Any()) await Back();
            }
            catch (Exception ex)
            {
                Log.For<Nav>().Error(ex, "Hardware back button failed.");
            }
            finally
            {
                IsHardwareBackBlocked = false;
            }
        }
    }
}