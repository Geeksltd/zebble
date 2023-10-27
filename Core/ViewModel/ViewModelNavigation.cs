namespace Zebble.Mvvm
{
    partial class ViewModelNavigation
    {
        Page realPage;
        Page RealPage => realPage ??= (Page)Templates.GetOrCreate(Target);

        Page realModal;
        Page RealModal => realModal ??= (Page)Templates.GetOrCreate(Target);

        partial void Configure()
        {
            RealGo = () => Nav.Go(RealPage, Transition);
            RealForward = () => Nav.Forward(RealPage, Transition);
            RealReplace = () => Nav.Replace(RealPage, Transition);
            RealReload = () => Nav.Reload();
            RealBack = () => Nav.Back();
            RealHidePopup = () => Nav.HidePopUp();
            RealShowPopup = () => Nav.ShowPopUp(RealModal, Transition);
        }
    }
}