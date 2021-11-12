using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Olive;

namespace Zebble
{
    internal class CssReference : IEqualityComparer<CssReference>
    {
        public Type Type;
        public string Id, Class, Pseudo;
        int HashCode;
        WeakReference<CssReference> Parent;

        public CssReference(Type type)
        {
            Type = type;
            HashCode = type.GetHashCode();
        }

        public override int GetHashCode() => HashCode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CssReference one, CssReference other)
        {
            if (one is null && other is null) return true;
            if (one is null || other is null) return false;

            if (one.Type != other.Type) return false;
            if (one.Id != other.Id) return false;
            if (one.Class != other.Class) return false;
            if (one.Pseudo != other.Pseudo) return false;

            return Equals(one.Parent.GetTargetOrDefault(), other.Parent.GetTargetOrDefault());
        }

        internal CssReference CloneForCacheKey()
        {
            var parent = Parent.GetTargetOrDefault()?.CloneForCacheKey();

            return new CssReference(Type)
            {
                Class = Class,
                HashCode = HashCode,
                Id = Id,
                Pseudo = Pseudo,
                Parent = parent?.GetWeakReference()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(CssReference obj) => GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => Equals(this, obj as CssReference);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetPseudo(string value)
        {
            Pseudo = value.OrNullIfEmpty();
            UpdateHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetId(string value)
        {
            Id = value.OrNullIfEmpty();
            UpdateHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetClass(string value)
        {
            Class = value.OrNullIfEmpty();
            UpdateHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateHashCode() => HashCode = System.HashCode.Combine(Type, Id, Class, Pseudo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetParent(CssReference cssReference) => Parent = cssReference.GetWeakReference();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveParent() => Parent = null;

        public override string ToString() => Type.Name + Id.WithPrefix("#") + Class.WithPrefix(".") + Pseudo.WithPrefix(":");

        public static bool operator !=(CssReference @this, CssReference another) => !(@this == another);

        public static bool operator ==(CssReference @this, CssReference another) => @this?.Equals(another) ?? (another is null);
    }
}