namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    partial class View
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsAddedToNativeParentOnce;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public View parent;
        public View Parent => parent;

        public readonly AsyncEvent ParentSet = new();

        /// <summary>
        /// Returns the root container view on the screen. This is the same as UIRuntime.PageContainer.
        /// </summary>
        public static View Root => UIRuntime.PageContainer;

        /// <summary>
        /// Gets all children whether or not they are ignored.
        /// </summary>
        public readonly ConcurrentList<View> AllChildren = new();

        /// <summary>Gets a copy of the children which are not ignored, so it's thread safe.</summary>
        public IEnumerable<View> CurrentChildren => AllChildren.Except(x => x.ignored);

        /// <summary>
        /// Gets all children and their nested children (recursive) whether or not they are ignored.
        /// </summary>
        public virtual IEnumerable<View> AllDescendents()
        {
            foreach (var c in AllChildren)
            {
                yield return c;
                foreach (var gc in c.AllDescendents())
                    yield return gc;
            }
        }

        /// <summary>
        /// Gets the current children and their nested children (recursive) which are not ignored.
        /// </summary>
        public IEnumerable<View> CurrentDescendants()
        {
            var result = CurrentChildren;
            return result.Concat(result.SelectMany(c => c.CurrentDescendants()));
        }

        /// <summary>
        /// Determines if this view is rendered already (i.e. has its Native property set).
        /// Note: The view may be rendered but not be added to a native parent yet.
        /// Consider using IsShown instead.
        /// </summary>
        [DebuggerStepThrough, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRendered() => Native != null;

        public T FindParent<T>() where T : View
        {
            if (parent is null) return null;
            return parent is T p ? p : parent?.FindParent<T>();
        }

        public TView FindDescendent<TView>() where TView : View => AllDescendents().OfType<TView>().FirstOrDefault();

        public Page Page => this is Page page ? page : parent?.Page;

        /// <summary>
        /// Adds an element to the virtual DOM. It also calls its OnInitializing. 
        /// </summary>
        /// <param name="awaitNative">Determines whether it should wait until  it's fully added to the real native UI.</param>
        public virtual Task<TView> Add<TView>(TView child, bool awaitNative = false) where TView : View
            => AddAt(AllChildren.Count, child, awaitNative);

        public Task<TView> AddBefore<TView>(View sibling, TView child) where TView : View
        {
            if (sibling is null) throw new ArgumentNullException("sibling cannot be null.");

            if (child is null) throw new ArgumentNullException("child cannot be null.");

            var index = AllChildren.IndexOf(sibling);
            if (index == -1) throw new Exception($"'{sibling.GetFullPath()} is not a child of '{GetFullPath()}'");

            return AddAt(index, child);
        }

        public Task<TView> AddBefore<TView>(TView sibling) where TView : View => parent?.AddBefore(this, sibling);

        /// <summary>
        /// Will return all siblings views of this view (whether or not they are Ignored).
        /// </summary>
        public IEnumerable<View> AllSiblings => parent?.AllChildren.Except(this) ?? Enumerable.Empty<View>();

        /// <summary>
        /// Will return all siblings views of this view which are not Ignored.
        /// </summary>
        public IEnumerable<View> CurrentSiblings => parent?.CurrentChildren.Except(this) ?? Enumerable.Empty<View>();

        public Task<TView> AddAfter<TView>(View sibling, TView child) where TView : View
        {
            if (sibling is null) throw new ArgumentNullException("sibling cannot be null.");
            if (child is null) throw new ArgumentNullException("child cannot be null.");

            var index = AllChildren.IndexOf(sibling);
            if (index == -1) throw new Exception($"'{sibling.GetFullPath()} is not a child of '{GetFullPath()}'");

            return AddAt(index + 1, child);
        }

        public Task<TView> AddAfter<TView>(TView sibling) where TView : View => parent?.AddAfter(this, sibling);

        public virtual async Task<TView> AddAt<TView>(int index, TView child, bool awaitNative = false) where TView : View
        {
            if (IsDisposing || child.parent == this) return child;

            if (UIRuntime.IsDevMode) ValidateChildAdding(index, child);

            using (await DomLock.Lock())
            {
                try { AllChildren.Insert(index.LimitMax(AllChildren.Count), child); }
                catch (ArgumentOutOfRangeException)
                {
                    if (IsDisposing) return child;
                    else throw;
                }
            }

            child.parent = this;
            child.CssReference.SetParent(CssReference);
            child.ParentSet?.Raise();

            if (Height.AutoOption == Length.AutoStrategy.Content && child.IsRendered())
                Height.Update();

            if (IsRendered())
            {
                if (child.IsRendered())
                {
                    var addTask = child.DoAddToNativeParent();
                    if (awaitNative) await addTask;
                }
                else
                {
                    await child.InitializeWithAllChildren();
                    await RenderAndAddChild(child, awaitNative);
                }
            }
            else if (IsInitialized && !child.IsInitialized && this != UIRuntime.RenderRoot)
            {
                await child.InitializeWithAllChildren();
            }

            await ChildAdded(child);

            return child;
        }

        void ValidateChildAdding(int index, View child)
        {
            if (child is null) throw new Exception("Cannot add NULL to this view: " + GetFullPath());
            if (child.IsDisposing) throw new ArgumentException("A disposed object cannot be added.");

            if (index < 0 || index > AllChildren.Count)
                throw new ArgumentException($"Index {index} is invalid. I have {AllChildren.Count} children.");

            if (child == this) throw new ArgumentException("You cannot add a view as its own child.");

            if (child.IsAnyOf(GetAllParents()))
                throw new ArgumentException($"The specified child view ({child})  is already a parent of this view ({this}).");
        }

        [DebuggerStepThrough]
        public virtual async Task AddRange<T>(IEnumerable<T> children) where T : View
        {
            foreach (var c in children)
                await Add(c);
        }

        protected virtual Task ChildAdded(View view) => Task.CompletedTask;

        protected virtual void ChildRemoved(View view)
        {
            if (Height.AutoOption == Length.AutoStrategy.Content)
                Height.Update();
        }

        bool IsAddedToDom()
        {
            var parent = this.parent;
            while (true)
            {
                if (parent == UIRuntime.RenderRoot) return true;
                if (parent is null) return false;
                parent = parent.parent;
            }
        }

        public Task RemoveAt(int childIndex, bool awaitNative = false)
        {
            if (childIndex < 0 || childIndex >= AllChildren.Count)
                throw new Exception("Invalid child index specified for removing.");

            return Remove(AllChildren[childIndex], awaitNative);
        }

        public virtual async Task Remove(View view, bool awaitNative = false)
        {
#if UWP
            if (UIRuntime.IsDevMode) UIRuntime.Inspector.DomUpdated(view).RunInParallel();
#endif

            if (view.IsDisposed) return;
            if (AllChildren.Lacks(view)) return;

            using (await DomLock.Lock())
            {
                AllChildren.Remove(view);
                view.parent = null;
                view.CssReference.RemoveParent();
                ChildRemoved(view);
            }

            if (IsRendered())
            {
                if (awaitNative)
                {
                    view.Renderer?.Apply("[REMOVE]", null);
                    if (view.ShouldDisposeWhenRemoved()) view.Dispose();
                }
                else
                {
                    UIWorkBatch.Publish(view, "[REMOVE]", null);
                    if (view.ShouldDisposeWhenRemoved())
                        UIWorkBatch.Publish(view, "[DISPOSE]", null);
                }
            }
            else if (view.ShouldDisposeWhenRemoved()) view.Dispose();
        }

        public async Task InitializeWithAllChildren()
        {
            await Initialize();

            foreach (var view in AllChildren)
                await view.InitializeWithAllChildren();
        }

        Task RenderAndAddChild(View child, bool awaitNativeRender)
        {
            return RenderAndAddChild(child, awaitNativeRender, applyCss: true);
        }

        internal async Task RenderAndAddChild(View child, bool awaitNativeRender, bool applyCss)
        {
            try
            {
                if (applyCss) await child.ApplyCssToBranch();

                await child.OnPreRender();

                foreach (var c in child.AllChildren)
                    await child.RenderAndAddChild(c, awaitNativeRender, applyCss: true);

                if (!child.IsRendered())
                {
                    child.Height.EnsureHeightCalculatedOnce();
                    await UIRuntime.Render(child);

                    foreach (var c in child.AllChildren)
                    {
                        var addChildTask = c.DoAddToNativeParent();
                        if (awaitNativeRender) await addChildTask;
                    }
                }

                if (IsRendered())
                {
                    var addTask = child.DoAddToNativeParent();
                    if (awaitNativeRender) await addTask;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error rendering and adding child view: " + child + ": " + ex, ex);
            }
        }

        async Task DoAddToNativeParent()
        {
            if (IsDisposing || parent?.IsDisposing is null) return; // High concurrency   

            IsAddedToNativeParentOnce = true;

            UIWorkBatch.Publish(this, "[ADD]", null);

#if UWP
            if (UIRuntime.IsDevMode) await UIRuntime.Inspector.DomUpdated(this);
#endif
        }

        public async Task<View> Render()
        {
            var tasks = new List<Task>();

            if (!IsRendered())
            {
                Height.EnsureHeightCalculatedOnce();

                await OnPreRender();
                tasks.Add(UIRuntime.Render(this));
            }

            foreach (var c in AllChildren)
                tasks.Add(RenderAndAddChild(c, awaitNativeRender: false));

            await Task.WhenAll(tasks);

            return this;
        }

        protected virtual bool ShouldDisposeWhenRemoved() => true;

        public int DepthInHierarchy => 1 + (parent?.DepthInHierarchy ?? 0);

        /// <summary>
        /// Remvoes all children without awaiting native action.
        /// </summary>
        public Task ClearChildren() => ClearChildren(awaitNative: false);

        public Task ClearChildren(bool awaitNative)
        {
            return Thread.UI.Run(async () =>
                   {
                       foreach (var child in AllChildren.Reverse().ToArray()) await Remove(child, awaitNative);
                   });
        }

        /// <summary>Gets this view as well as all its parents hierarchy.</summary>
        public IEnumerable<View> WithAllParents()
        {
            for (var node = this; node != null; node = node.parent)
                yield return node;
        }

        public IEnumerable<View> WithAllDescendants()
        {
            yield return this;
            foreach (var child in AllChildren)
                foreach (var item in child.WithAllDescendants())
                    yield return item;
        }

        /// <summary>Gets this view as well as all its parents hierarchy.</summary>
        public IEnumerable<View> GetAllParents() => parent?.WithAllParents() ?? Enumerable.Empty<View>();
    }
}