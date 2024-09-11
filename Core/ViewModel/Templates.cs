using System;
using Olive;

namespace Zebble.Mvvm
{
    partial class Templates
    {
        public static View GetOrCreate(ViewModel target)
        {
            var template = Mappings.GetOrDefault(target.GetType());

            if (template != null)
                return template.GetOrCreate(target);
            else
                return new AutoPage(target);
        }
    }
}