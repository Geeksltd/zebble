namespace Zebble.Mvvm
{
    using System;
    using System.Threading.Tasks;

    partial class ViewModel
    {
        public static Task Go<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.SlideForward)
            where TDestination : FullScreen
        {
            configure?.Invoke(The<TDestination>());
            return Go<TDestination>(transition);
        }

        public static Task Forward<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            configure?.Invoke(The<TDestination>());
            return Forward<TDestination>(transition);
        }

        public static Task Replace<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            configure?.Invoke(The<TDestination>());
            return Replace<TDestination>(transition);
        }

        public static Task ShowPopUp<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.DropUp)
            where TDestination : ModalScreen
        {
            configure?.Invoke(The<TDestination>());
            return ShowPopUp<TDestination>(transition);
        }
    }
}