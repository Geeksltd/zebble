using System.Collections.Generic;

namespace Zebble.Services
{
    public interface IHierarchy
    {
        IHierarchy GetParent();
        IEnumerable<IHierarchy> GetChildren();
    }
}