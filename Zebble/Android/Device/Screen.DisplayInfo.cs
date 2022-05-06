namespace Zebble.Device
{
    partial class Screen
    {
        public class DisplaySettings
        {
            public int HardwareWidth { internal set; get; }
            public int HardwareHeight { internal set; get; }
            public int WindowWidth { internal set; get; }
            public int WindowHeight { internal set; get; }
            public int OutOfWindowStatusBarHeight { internal set; get; }
            public int InWindowStatusBarHeight { internal set; get; }
            public int OutOfWindowNavbarHeight { internal set; get; }
            public int InWindowNavbarHeight { internal set; get; }
            public int TopInset { internal set; get; }
            public int RightInset { internal set; get; }
            public int BottomInset { internal set; get; }
            public int LeftInset { internal set; get; }
            public int RealWidth { internal set; get; }
            public int RealHeight { internal set; get; }
            public int Ime { internal set; get; }

            public override string ToString()
            {
                var result = $"HardwareWidth={HardwareWidth},HardwareHeight={HardwareHeight},WindowWidth={WindowWidth},WindowHeight={WindowHeight},RealWidth={RealWidth},RealHeight={RealHeight},OutOfWindowStatusBarHeight={OutOfWindowStatusBarHeight},OutOfWindowNavbarHeight={OutOfWindowNavbarHeight}InWindowStatusBarHeight={InWindowStatusBarHeight},InWindowNavbarHeight={InWindowNavbarHeight},TopInset={RightInset},TopInset={RightInset},BottomInset={BottomInset},LeftInset={LeftInset},Ime={Ime}";

                return result;
            }
        }

    }
}
