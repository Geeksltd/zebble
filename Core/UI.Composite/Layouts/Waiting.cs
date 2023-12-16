namespace Zebble
{
    using System;
    using System.Threading.Tasks;

    class Waiting
    {
        const int TOP_MOST = 10000;
        readonly static Overlay Overlay = new Overlay().CssClass("waiting-overlay");

        static View indicator;
        public static View Indicator
        {
            get
            {
                if (indicator is null)
                    Indicator = new TextView { Text = "Loading..." }.Id("Indicator").CssClass("wait-spinner");

                return indicator;
            }
            set => indicator = value;

        }

        static internal Guid ShownVersion;

        public static Task Show(bool block = true)
        {
            return UIWorkBatch.Run(async () =>
            {
                ShownVersion = Guid.NewGuid();
                if (block) await Overlay.Show();

                Indicator.ZIndex(TOP_MOST);

                if (Indicator.Parent is null)
                    await View.Root.Add(Indicator, awaitNative: true);

                Indicator.Visible();
                Indicator.Opacity(1);
                await Indicator.BringToFront();
            });
        }

        public static async Task Hide(Guid? version = null)
        {
            if (version is not null && ShownVersion != version) return;

            await Overlay.Hide();

            if (Indicator.Opacity != 0) Indicator.Opacity(0);

            await Indicator.Hide().SendToBack();
        }
    }
}