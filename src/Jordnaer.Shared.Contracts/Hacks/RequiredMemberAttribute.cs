#pragma warning disable CA1018
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global
public class RequiredMemberAttribute : Attribute { }
public class CompilerFeatureRequiredAttribute : Attribute
{
    // ReSharper disable once UnusedParameter.Local
#pragma warning disable IDE0060
    public CompilerFeatureRequiredAttribute(string name) { }
#pragma warning restore IDE0060
}
