using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Olive;

namespace Zebble.Mvvm
{
    public static partial class Templates
    {
        static readonly ConcurrentDictionary<Type, Template> Mappings = new();

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
                    throw new RenderException(type.FullName + " should implement ITemplate<Model> rather than ITemplate.");

                if (interfaces.HasMany())
                    throw new RenderException(type.FullName + " should implement a single ITemplate<Model>.");

                var modelType = interfaces.Single().GetGenericArguments().Single();

                if (modelType.IsA<FullScreen>() || modelType.IsA<ModalScreen>())
                {
                    var tp = type.AsType();

                    if (Mappings.TryGetValue(modelType, out var existing))
                    {
                        if (existing.TemplateType == tp) continue;

                        throw new InvalidStateException("More than one template is defined for " + modelType.FullName
                            + Environment.NewLine + existing.TemplateType.FullName +
                            Environment.NewLine + type.FullName);
                    }

                    Mappings[modelType] = new Template(tp);
                }
            }
        }
    }
}