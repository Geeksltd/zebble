namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Services;
    using Olive;

    partial class View
    {
        internal enum CssReferencePart { Type, Id, Class, Pseudo }

        /// <summary>
        /// A reference based on the hash codes of its type, id, cssClass and psudo css class.
        /// </summary>
        internal readonly CssReference CssReference;
        string cssClass = string.Empty, pseudoCssState = string.Empty;
        internal BatchUIChangeContext StyleApplyingContext;
        internal HashSet<string> CssClassParts;
        public readonly Stylesheet Style, Css;

        public string PseudoCssState
        {
            get => pseudoCssState;
            set => SetPseudoCssState(value).GetAwaiter();
        }

        async Task SetPseudoCssState(string value)
        {
            value = value.OrEmpty();
            if (pseudoCssState == value) return;

            pseudoCssState = value;
            CssReference.SetPseudo(value);

            if (IsRendered()) await ApplyCssToBranch();
        }

        public Task UnsetPseudoCssState(string state) => SetPseudoCssState(state, set: false);

        public async Task SetPseudoCssState(string state, bool set = true)
        {
            if (state.IsEmpty()) throw new ArgumentNullException(nameof(state));

            if (set)
            {
                if (!pseudoCssState.ContainsWholeWord(state))
                    await SetPseudoCssState(pseudoCssState.WithSuffix(" ") + state);
            }
            else if (pseudoCssState.ContainsWholeWord(state)) {
                await SetPseudoCssState(pseudoCssState.OrEmpty().ReplaceWholeWord(state, " ")
                     .KeepReplacing("  ", " ").Trim());
            }
        }

        public string CssClass
        {
            get => cssClass.OrEmpty();
            set
            {
                if (UIRuntime.IsDevMode && IsShown)
                {
                    Log.For(this).Error("Do not set CssClass after the object is shown. Instead call and await SetCssClass().");
                    Log.For(this).Debug(Environment.StackTrace.ToLines().Skip(4).Take(5).ToLinesString());
                }

                SignalSetCssClass(value);
            }
        }

        async void SignalSetCssClass(string value) => await SetCssClass(value);

        /// <summary>
        /// If setting the css class after an object is rendered, use this method and await the result.
        /// </summary>
        public Task SetCssClass(string value)
        {
            value = value.TrimOrEmpty().KeepReplacing("  ", " ");

            if (cssClass == value) return Task.CompletedTask;
            cssClass = value;
            CssReference.SetClass(value);
            CssClassParts = new HashSet<string>(value.Split(' '));

            if (IsRendered())
                if (parent != null || this == Root)
                {
                    return ApplyCssToBranch();
                    // TODO: Remove old css class's styles?
                }

            return Task.CompletedTask;
        }

        public string CurrentlyAppliedCss => CssEngine.Diagnose(this);

        /// <summary>
        /// This is invoked just before PreRender. It applies the applicable CSS rules on the object.
        /// </summary>
        public virtual Task ApplyStyles() => CssEngine.Apply(this);

        public virtual async Task ApplyCssToBranch(bool skipUnrendered = false)
        {
            async Task apply()
            {
                // Apply my own styles.
                if (!skipUnrendered || IsRendered())
                    await ApplyStyles();

                // If it's runtime, then my children's styles should also be reapplied:
                if (IsRendered())
                    foreach (var c in AllChildren)
                        await c.ApplyCssToBranch(skipUnrendered: true);
            }

            Task applyInBatch() => BatchStyleChange(apply);

            await UIWorkBatch.Run(applyInBatch);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use BatchStyleChange() or BatchUpdate() as appropriate.", error: true)]
        public Task ChangeInBatch(Func<Task> change) => BatchStyleChange(change);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task BatchStyleChange(Func<Task> change)
        {
            if (change is null) return;

            if (StyleApplyingContext != null) await change();
            else
            {
                StyleApplyingContext = new BatchUIChangeContext();

                Lengths.Do(b => b.Suspend());
                await change();
                StyleApplyingContext?.ApplyChanges();
                Lengths.Do(x => x.Resume());

                StyleApplyingContext = null;
            }
        }
    }
}