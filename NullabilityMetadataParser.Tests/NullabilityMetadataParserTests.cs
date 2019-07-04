using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NUnit.Framework;

namespace NullabilityMetadataParser.Tests
{
    public class NullabilityMetadataParserTests
    {
        [TestCaseSource(typeof(NullabilityData), nameof(NullabilityData.TestCases))]
        public Nullability TestNullability(MemberInfo memberInfo)
            => new NullabilityMetadataParser().ParseNullability(memberInfo);

        /*
        public void Foo()
        {
            var x = new MixedType();
            string s = x.NullableWithNotNull;
            string s2 = x.NonNullableWithMaybeNull;
        }*/
    }

    static class NullabilityData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(typeof(MixedType).GetProperty(nameof(MixedType.NonNullable)))
                    .Returns(Nullability.NonNullable).SetName("NonNullableInMixedType");

                yield return new TestCaseData(typeof(MixedType).GetProperty(nameof(MixedType.Nullable)))
                    .Returns(Nullability.Nullable).SetName("NullableInMixedType");

                yield return new TestCaseData(typeof(MostlyNullableType).GetProperty(nameof(MostlyNullableType.NonNullable)))
                    .Returns(Nullability.NonNullable).SetName("NonNullableInMostlyNullableType");

                yield return new TestCaseData(typeof(ObliviousType).GetProperty(nameof(ObliviousType.Oblivious)))
                    .Returns(Nullability.Oblivious).SetName("Oblivious");

                // [MaybeNull]
                yield return new TestCaseData(typeof(MixedType).GetProperty(nameof(MixedType.NonNullableWithMaybeNull)))
                    .Returns(Nullability.Nullable).SetName("NonNullableWithMaybeNull");
                yield return new TestCaseData(typeof(GenericType<string>).GetProperty(nameof(GenericType<string>.MaybeNullable)))
                    .Returns(Nullability.Nullable).SetName("MaybeNullableProperty");

                // [NotNull]
                yield return new TestCaseData(typeof(MixedType).GetProperty(nameof(MixedType.NullableWithNotNull)))
                    .Returns(Nullability.NonNullable).SetName("GenericNullableWithNotNull");
                yield return new TestCaseData(typeof(GenericType<string?>).GetProperty(nameof(GenericType<string?>.NotNullable)))
                    .Returns(Nullability.NonNullable).SetName("GenericNotNullProperty");
            }
        }
    }

    public class MixedType
    {
        public string NonNullable { get; set; } = "";
        public string? Nullable { get; set; }

        [MaybeNull] [DisplayName("WHAT")] public string NonNullableWithMaybeNull { get; set; } = "";
        [NotNull] public string? NullableWithNotNull { get; set; }
    }

    public class MostlyNullableType
    {
        public string NonNullable { get; set; } = "";

        public string? Nullable1 { get; set; }
        public string? Nullable2 { get; set; }
        public string? Nullable3 { get; set; }
        public string? Nullable4 { get; set; }
        public string? Nullable5 { get; set; }
        public string? Nullable6 { get; set; }
        public string? Nullable7 { get; set; }
        public string? Nullable8 { get; set; }
        public string? Nullable9 { get; set; }
    }

    public class GenericType<T>
    {
        [MaybeNull] public T MaybeNullable { get; set; } = default!;
        [NotNull] public T NotNullable { get; set; } = default!;
    }

#nullable disable

    public class ObliviousType
    {
        public string Oblivious { get; set; }
    }

#nullable enable
}
