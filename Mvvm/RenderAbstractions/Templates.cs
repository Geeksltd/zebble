using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Olive;

namespace Zebble.Mvvm
{
    public static partial class Templates
    {
        static readonly Dictionary<Type, Template> Mappings = new Dictionary<Type, Template>();

        public static void Register(Assembly assembly)
        {
            var viewTypes = assembly.DefinedTypes
                  .Where(x => typeof(View).GetTypeInfo().IsAssignableFrom(x))
                  .Where(x => x.ImplementedInterfaces.OrEmpty().Contains(typeof(ITemplate)))
                  .ToArray();

            foreach (var type in viewTypes)
            {
                var interfaces = type.ImplementedInterfaces.OrEmpty()
                      .Except(typeof(ITemplate))
                      .Where(i => i.Name.StartsWith("ITemplate`") || i.Name.StartsWith("IDynamicTemplate`"))
                      .ToArray();

                if (interfaces.None())
                    throw new Exception(type.FullName + " should implement ITemplate<Model> rather than ITemplate.");

                if (interfaces.HasMany())
                    throw new Exception(type.FullName + " should implement a single ITemplate<Model>.");

                var modelType = interfaces.Single().GetGenericArguments().Single();

                if (modelType.IsA<FullScreen>() || modelType.IsA<ModalScreen>())
                {
                    if (Mappings.TryGetValue(modelType, out var existing))
                        throw new Exception("More than one template is defined for " + modelType.FullName
                            + Environment.NewLine + existing.TemplateType.FullName +
                            Environment.NewLine + type.FullName);

                    Mappings.Add(modelType, new Template(type.AsType()));
                }
            }
        }
    }
}