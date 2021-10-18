using System.Threading.Tasks;

namespace Zebble
{
    public interface IBaseUITest
    {
        TView[] AllVisible<TView>() where TView : View;

        T Find<T>() where T : View;

        void Delay(int delay = 1000);

        Task Swipe(Direction direction);
    }
}
