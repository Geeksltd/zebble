namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class DependencyManager
    {
        static bool Initialized;

        static readonly List<Type> DependencyTypes = new();
        static readonly Dictionary<Type, DependencyData> DependencyImplementations = new();

        public static T Get<T>(DependencyFetchTarget fetchTarget = DependencyFetchTarget.GlobalInstance) where T : class
        {
            Initialize();

            var targetType = typeof(T);

            if (!DependencyImplementations.ContainsKey(targetType))
            {
                var implementor = FindImplementor(targetType);
                DependencyImplementations[targetType] = implementor != null ? new DependencyData { ImplementorType = implementor } : null;
            }

            var dependencyImplementation = DependencyImplementations[targetType];
            if (dependencyImplementation is null) return null;

            if (fetchTarget == DependencyFetchTarget.GlobalInstance)
            {
                if (dependencyImplementation.GlobalInstance is null)
                {
                    dependencyImplementation.GlobalInstance = Activator.CreateInstance(dependencyImplementation.ImplementorType);
                }

                return (T)dependencyImplementation.GlobalInstance;
            }

            return (T)Activator.CreateInstance(dependencyImplementation.ImplementorType);
        }

        public static void Register<T>() where T : class
        {
            var type = typeof(T);
            if (!DependencyTypes.Contains(type)) DependencyTypes.Add(type);
        }

        public static void Register<T, TImpl>() where T : class where TImpl : class, T
        {
            var targetType = typeof(T);
            var implementorType = typeof(TImpl);
            if (!DependencyTypes.Contains(targetType)) DependencyTypes.Add(targetType);

            DependencyImplementations[targetType] = new DependencyData { ImplementorType = implementorType };
        }

        static Type FindImplementor(Type target) => DependencyTypes.FirstOrDefault(t => target.IsAssignableFrom(t));

        static void Initialize()
        {
            if (Initialized) return;
            Initialize(AssemblyInfo.GetAssemblies());
        }

        internal static void Initialize(Assembly[] assemblies)
        {
            if (Initialized) return;

            foreach (var assembly in assemblies)
            {
                var attributes = assembly.GetCustomAttributes<DependencyAttribute>().ToArray();
                if (attributes.Length == 0) continue;

                foreach (var attribute in attributes)
                {
                    if (!DependencyTypes.Contains(attribute.Implementor))
                    {
                        DependencyTypes.Add(attribute.Implementor);
                    }
                }
            }

            Initialized = true;
        }

        class DependencyData
        {
            public object GlobalInstance { get; set; }

            public Type ImplementorType { get; set; }
        }
    }

    public enum DependencyFetchTarget
    {
        GlobalInstance,
        NewInstance
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class DependencyAttribute : Attribute
    {
        public DependencyAttribute(Type implementorType) => Implementor = implementorType;

        internal Type Implementor { get; private set; }
    }
}