using System;
using System.Collections.Concurrent;
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
            var baseViewTypeInfo = typeof(View).GetTypeInfo();
            var viewTypeInfoes = assembly.DefinedTypes
                .Where(baseViewTypeInfo.IsAssignableFrom)
                .Where(x => x.ImplementedInterfaces.OrEmpty().Contains(typeof(ITemplate)))
                .ToArray();

            FromViewsToViewModels();

            void FromViewsToViewModels()
            {
                foreach (var viewTypeInfo in viewTypeInfoes)
                {
                    var templateInterfaces = viewTypeInfo.ImplementedInterfaces.OrEmpty()
                        .Except(typeof(ITemplate))
                        .Where(i => i.Name.StartsWith("ITemplate`") || i.Name.StartsWith("IDynamicTemplate`"))
                        .ToArray();

                    if (templateInterfaces.None())
                        throw new RenderException(viewTypeInfo.FullName + " should implement ITemplate<Model> rather than ITemplate.");

                    if (templateInterfaces.HasMany())
                        throw new RenderException(viewTypeInfo.FullName + " should implement a single ITemplate<Model>.");

                    var modelType = templateInterfaces.Single().GetGenericArguments().Single();

                    TryRegister(modelType, viewTypeInfo);
                }
            }

            FromViewModelsToGenericViews();

            void FromViewModelsToGenericViews()
            {
                var genericViewTypeInfoes = viewTypeInfoes
                    .Where(x => x.IsGenericType)
                    .Select(x => new
                    {
                        ViewType = x,
                        ParentViewModels = x.GetGenericArguments().FirstOrDefault()?.GetParentTypes().Take(2)
                    })
                    .Where(x => x.ParentViewModels.HasAny())
                    .ToArray();

                var baseScreenTypeInfoes = new Type[] {
                    typeof(ModalScreen),
                    typeof(FullScreen)
                }.Select(x => x.GetTypeInfo()).ToArray();

                var screenTypeInfoes = assembly.DefinedTypes
                    .Where(x => baseScreenTypeInfoes.Any(ti => ti.IsAssignableFrom(x)))
                    .Except(x => x.IsAbstract)
                    .ToArray();

                foreach (var screenTypeInfo in screenTypeInfoes)
                {
                    if (Mappings.ContainsKey(screenTypeInfo))
                    {
                        continue;
                    }

                    foreach (var screenBaseType in screenTypeInfo.GetParentTypes())
                    {
                        var matchedTypeInfo = genericViewTypeInfoes.FirstOrDefault(x => x.ParentViewModels.Contains(screenBaseType));
                        if (matchedTypeInfo is not null)
                        {
                            try { TryRegister(screenTypeInfo, matchedTypeInfo.ViewType.MakeGenericType(screenTypeInfo)); }
                            catch (ArgumentException) {/* Ignore */}
                            break;
                        }
                    }
                }
            }

            static void TryRegister(Type modelType, Type viewType)
            {
                if (modelType.IsA<FullScreen>() || modelType.IsA<ModalScreen>())
                {
                    if (Mappings.TryGetValue(modelType, out var existing))
                    {
                        if (existing.TemplateType == viewType) return;

                        throw new InvalidStateException("More than one template is defined for " + modelType.FullName
                            + Environment.NewLine + existing.TemplateType.FullName +
                            Environment.NewLine + viewType.FullName);
                    }

                    Mappings[modelType] = new Template(viewType);
                }
            }
        }
    }
}