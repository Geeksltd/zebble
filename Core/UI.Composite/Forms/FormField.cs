namespace Zebble
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    public abstract class FormField : Stack
    {
        public readonly TextView Label = new() { Id = "Label" };
        public readonly ImageView Icon = new() { Id = "Icon", ignored = true };

        protected FormField() : base(RepeatDirection.Horizontal)
        {
        }

        public abstract View GetControl();

        public bool Mandatory { get; set; }

        public string LabelText { get => Label.Text; set => Label.Text = value; }

        public virtual string IconPath { get => Icon.Path; set { Icon.Path = value; Icon.Style.Ignored = value.IsEmpty(); } }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            await Add(Icon);
            await Add(Label);
            await AddControl();
        }

        protected abstract Task AddControl();

        protected override string GetStringSpecifier() => LabelText;

        public interface IControl { object Value { get; set; } }

        public interface IPlaceHolderControl { string Placeholder { get; set; } }
    }

    public class FormField<TControl> : FormField where TControl : View, FormField.IControl, new()
    {
        string placeholder;
        public TControl Control = new();
        public Action<TControl> PlaceholderChanged;

        public string Text => GetValue<object>().ToStringOrEmpty();

        public virtual string Placeholder
        {
            get => placeholder;
            set { placeholder = value; PlaceholderChanged?.Invoke(Control); }
        }

        protected override Task AddControl() => Add(Control);

        public override View GetControl() => Control;

        public override async Task OnInitializing()
        {
            await base.OnInitializing();

            if (Placeholder.HasValue())
                (Control as IPlaceHolderControl)?.Perform(x => x.Placeholder = Placeholder);
        }

        public override async Task OnPreRender()
        {
            await base.OnPreRender();
            if (Control.id.IsEmpty()) Control.Id = id + "-Control";
        }

        public object Value
        {
            get => Control.Value;
#if ANDROID
            [Android.Runtime.Preserve]
#endif
            set => Control.Value = value;
        }

        public T GetValue<T>() => (T)Convert(Value, typeof(T));

        static object Convert(object value, Type type)
        {
            if (value is null) return type.GetDefaultValue();

            if (value.GetType().IsA(type)) return value;

            if (type.IsA<string>()) return value.ToStringOrEmpty();

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var genericArg = type.GetGenericArguments().Single();
                var result = typeof(List<>).MakeGenericType(genericArg).CreateInstance() as IList;
                if (value.GetType().IsA(genericArg)) result.Add(value);
                else foreach (var item in (IEnumerable)value) result.Add(item);
                return result;
            }

            try { return value.ToString().To(type); }
            catch (Exception ex)
            {
                throw new RenderException($"The value of '{value}' cannot be converted to type '{type.Name}'.", ex);
            }
        }

        public override void Dispose()
        {
            PlaceholderChanged = null;
            base.Dispose();
        }
    }
}