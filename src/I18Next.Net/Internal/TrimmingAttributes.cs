#if !NET
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property |
                AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes) : Attribute
{
    public DynamicallyAccessedMemberTypes MemberTypes { get; } = memberTypes;
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    None = 0,
    PublicParameterlessConstructor = 0x0001,
    PublicConstructors = 0x0002 | PublicParameterlessConstructor,
    PublicMethods = 0x0008,
    PublicProperties = 0x0200
}

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
internal sealed class UnconditionalSuppressMessageAttribute(string category, string checkId) : Attribute
{
    public string Category { get; } = category;

    public string CheckId { get; } = checkId;

    public string Justification { get; set; }
}
#endif
