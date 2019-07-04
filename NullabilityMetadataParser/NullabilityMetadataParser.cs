using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace NullabilityMetadataParser
{
    public class NullabilityMetadataParser
    {
        // For the interpretation of nullability metadata, see
        // https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md

        Type? _nullableAttrType;
        Type? _nullableContextAttrType;
        FieldInfo? _nullableFlagsFieldInfo;
        FieldInfo? _nullableContextFlagFieldInfo;
        readonly Dictionary<Type, Nullability> _typeNullabilityContextCache = new Dictionary<Type, Nullability>();
        readonly Dictionary<Module, Nullability> _moduleNullabilityContextCache = new Dictionary<Module, Nullability>();

        const string NullableAttributeFullName = "System.Runtime.CompilerServices.NullableAttribute";
        const string NullableContextAttributeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";

        public Nullability ParseNullability(MemberInfo memberInfo)
        {
            // First, check if we have [MaybeNull] or [NotNull]
            foreach (var attr in Attribute.GetCustomAttributes(memberInfo, true))
            {
                switch (attr)
                {
                    case MaybeNullAttribute _:
                        return Nullability.Nullable;
                    case NotNullAttribute _:
                        return Nullability.NonNullable;
                }
            }

            // For C# 8.0 nullable types, the C# currently synthesizes a NullableAttribute that expresses nullability into assemblies
            // it produces. If the model is spread across more than one assembly, there will be multiple versions of this attribute,
            // so look for it by name, caching to avoid reflection on every check.
            // Note that this may change - if https://github.com/dotnet/corefx/issues/36222 is done we can remove all of this.

            // First look for NullableAttribute on the member itself
            if (Attribute.GetCustomAttributes(memberInfo, true)
                    .FirstOrDefault(a => a.GetType().FullName == NullableAttributeFullName) is Attribute nullableAttr)
            {
                var attrType = nullableAttr.GetType();

                if (attrType != _nullableAttrType)
                {
                    _nullableFlagsFieldInfo = attrType.GetField("NullableFlags");
                    _nullableAttrType = attrType;
                }

                if (_nullableFlagsFieldInfo?.GetValue(nullableAttr) is byte[] flags)
                    return (Nullability)flags[0];

                // TODO: If NullablePublicOnly is on, return oblivious immediately...?
            }

            // No attribute on the member, try to find a NullableContextAttribute on the declaring type
            var type = memberInfo.DeclaringType;
            if (type != null)
            {
                if (_typeNullabilityContextCache.TryGetValue(type, out var typeContext))
                {
                    return typeContext;
                }

                if (TryGetNullabilityContextFlag(Attribute.GetCustomAttributes(type), out typeContext))
                {
                    return _typeNullabilityContextCache[type] = typeContext;
                }
            }

            // Not found at the type level, try at the module level
            var module = memberInfo.Module;
            if (!_moduleNullabilityContextCache.TryGetValue(module, out var moduleContext))
            {
                moduleContext = TryGetNullabilityContextFlag(Attribute.GetCustomAttributes(memberInfo.Module), out var x)
                    ? x
                    : Nullability.Oblivious;
            }

            if (type != null)
            {
                _typeNullabilityContextCache[type] = moduleContext;
            }

            return moduleContext;
        }

        bool TryGetNullabilityContextFlag(Attribute[] attributes, out Nullability contextFlag)
        {
            if (attributes.FirstOrDefault(a => a.GetType().FullName == NullableContextAttributeFullName) is Attribute attr)
            {
                var attrType = attr.GetType();

                if (attrType != _nullableContextAttrType)
                {
                    _nullableContextFlagFieldInfo = attrType.GetField("Flag");
                    _nullableContextAttrType = attrType;
                }

                if (_nullableContextFlagFieldInfo?.GetValue(attr) is byte flag)
                {
                    contextFlag = (Nullability)flag;
                    return true;
                }
            }

            contextFlag = default;
            return false;
        }

        /// <summary>
        /// Resets all internal caches, preparing the parser for usage with new types or assemblies.
        /// </summary>
        public void Reset()
        {
            (_nullableAttrType, _nullableContextAttrType, _nullableFlagsFieldInfo, _nullableContextFlagFieldInfo) =
                (null, null, null, null);

            _typeNullabilityContextCache.Clear();
            _moduleNullabilityContextCache.Clear();
        }
    }
}
