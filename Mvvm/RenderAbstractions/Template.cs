using System;
using Olive;

namespace Zebble.Mvvm
{
    class Template
    {
        public View View { get; private set; }

        internal Type TemplateType;

        public Template(Type templateType) => TemplateType = templateType;

        internal void Bind(ViewModel model) => GetOrCreate(model);

        internal View GetOrCreate(ViewModel model)
        {
            if (View == null || View.IsDisposing)
                View = (View)TemplateType.CreateInstance();

#if UWP || ANDROID || IOS
            View.SetViewModelValue(model);
            View.RefreshBindings();
#endif
            return View;
        }
    }
}