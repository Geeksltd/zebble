using System.Collections.Generic;
using System.Linq;
using Olive;
using Zebble.Services;

namespace System
{
    partial class ZebbleExtensions
    {
        /// <summary>
        /// Gets whether this node is a root hierarchy node.
        /// </summary>
        public static bool IsRootNode(this IHierarchy node) => node.GetParent() == null;

        /// <summary>
        /// Gets this node as well as all its children hierarchy.
        /// </summary>
        public static IEnumerable<IHierarchy> WithAllChildren(this IHierarchy parent) =>
            parent.GetAllChildren().Concat(parent).ToArray();

        /// <summary>
        /// Gets all children hierarchy of this node.
        /// </summary>
        public static IEnumerable<IHierarchy> GetAllChildren(this IHierarchy parent) =>
            parent.GetChildren().Except(parent).SelectMany(c => c.WithAllChildren()).ToArray();

        /// <summary>
        /// Gets this node as well as all its parents hierarchy.
        /// </summary>
        public static IEnumerable<IHierarchy> WithAllParents(this IHierarchy child) =>
            child.GetAllParents().Concat(child).ToArray();

        /// <summary>
        /// Gets all parents hierarchy of this node.
        /// </summary>
        public static IEnumerable<IHierarchy> GetAllParents(this IHierarchy child)
        {
            var parent = child.GetParent();

            if (parent == null || parent == child) return new IHierarchy[0];
            else return parent.WithAllParents().ToArray();
        }

        /// <summary>
        /// Gets this node as well as all its parents hierarchy.
        /// </summary>
        public static IEnumerable<T> WithAllParents<T>(this T child) where T : IHierarchy =>
            (child as IHierarchy).WithAllParents().Cast<T>().ToArray();

        /// <summary>
        /// Gets all parents hierarchy of this node.
        /// </summary>
        public static IEnumerable<T> GetAllParents<T>(this IHierarchy child) where T : IHierarchy =>
            child.GetAllParents().Cast<T>().ToArray();
    }
}