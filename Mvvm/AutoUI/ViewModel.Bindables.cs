using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Olive;

namespace Zebble.Mvvm
{
    partial class ViewModel
    {
        internal class BindableMember
        {
            internal MemberInfo Member;
            readonly object Target;
            public string Name;
            public Type Type;

            public BindableMember(MemberInfo member, object target)
            {
                Member = member;
                Target = target;
                Name = member.Name;
                Type = member.GetPropertyOrFieldType().GetGenericArguments().Single();
            }

            internal void SetValue(object value)
            {
                var target = Member.GetValue(Target);
                target.GetType().GetProperty("Value").SetValue(target, value);
            }

            internal object ReadValue()
            {
                try
                {
                    var target = Member.GetValue(Target);
                    return target.GetType().GetProperty("Value").GetValue(target);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }
        }

        internal BindableMember[] GetBindables()
        {
            var properties = GetType()
                    .GetPropertiesAndFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .ToArray();

            var bindables = properties
                    .Where(v => v.GetPropertyOrFieldType().GetInterfaces().Contains(typeof(IBindable)))
                    .ToArray();

            return bindables.Select(v => new BindableMember(v, this)).ToArray();
        }

        internal Dictionary<string, ViewModel> GetNestedViewModels()
        {
            return GetType()
                    .GetPropertiesAndFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(v => v.GetPropertyOrFieldType().IsA<ViewModel>())
                    .ToDictionary(v => v.Name, v => v.GetValue(this) as ViewModel)
                    .Where(v => v.Value != null)
                    .Concat(GetNestedCollectionViewModels())
                    .ToDictionary(x => x.Key, x => x.Value);
        }

        Dictionary<string, ViewModel> GetNestedCollectionViewModels(int max = 4)
        {
            var collections =
                GetType()
                    .GetPropertiesAndFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(v => v.GetPropertyOrFieldType().IsA<ICollectionViewModel>())
                    .ToArray();

            var members = collections
                    .Select(v => new
                    {
                        Key = v.Name,
                        Value = (v.GetValue(this) as ICollectionViewModel)?
                        .Cast<ViewModel>().ExceptNull().Take(max).ToArray()
                    })
                    .Where(v => v.Value != null && v.Value.Any())
                    .ToArray();

            // Flat it out:
            var result = members.SelectMany(v => v.Value.Select((k, i) => new
            {
                Key = v.Key + "[" + (i + 1) + "]",
                Value = k
            })).ToArray();

            return result.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}