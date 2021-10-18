using Olive;
using System.Collections;

namespace Zebble.Mvvm
{
    interface ICollectionViewModel : IEnumerable { }

    public class CollectionViewModel<TViewModel> : BindableCollection<TViewModel>, ICollectionViewModel where TViewModel : ViewModel
    {
    }
}