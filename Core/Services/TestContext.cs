using System;
using Olive;

namespace Zebble
{
    public class TestContext
    {
        public static DateTime SimulatedTime { get; set; } = "2022/01/01 10:00:00".To<DateTime>();

        public static void Activate(bool skipAnimations = true, bool simulateTime = true)
        {
            if (simulateTime)
                LocalTime.RedefineNow(() => SimulatedTime);

            Animation.SkipAll = skipAnimations;
        }
    }
}