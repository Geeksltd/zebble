namespace Zebble.AndroidOS
{
    using System.Timers;

    interface IPartialPagingScrollView : IScrollView
    {
        int PartialPagingInterval { get; set; }
        int MaxScrollSpeed { get; set; }
        bool PartialPagingEnabled { get; set; }
        float PartialPagingSize { get; set; }
        Timer PartialPagingTimer { get; set; }

        void ConfigurePartialPaging();

        void OnPartialPagingEnded();
    }
}