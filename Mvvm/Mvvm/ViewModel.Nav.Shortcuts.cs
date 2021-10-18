namespace Zebble.Mvvm
{
    using System;

    partial class ViewModel
    {
        public static void Go<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.SlideForward)
            where TDestination : FullScreen
        {
            configure?.Invoke(The<TDestination>());
            Go<TDestination>(transition);
        }

        public static void Forward<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            configure?.Invoke(The<TDestination>());
            Forward<TDestination>(transition);
        }

        public static void ShowPopUp<TDestination>(Action<TDestination> configure, PageTransition transition = PageTransition.DropUp)
            where TDestination : ModalScreen
        {
            configure?.Invoke(The<TDestination>());
            ShowPopUp<TDestination>(transition);
        }
    }
}