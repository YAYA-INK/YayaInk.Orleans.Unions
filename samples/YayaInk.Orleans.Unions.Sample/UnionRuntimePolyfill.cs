// The C# 15 union runtime types (System.Runtime.CompilerServices.IUnion and
// UnionAttribute) are not yet shipped by the .NET 11 preview BCL. Until they
// are, every project that declares a `union` needs to provide them. This sample
// embeds a minimal local polyfill so the demo can build standalone; production
// consumers should provide these types via their own shared library or wait
// for the BCL to ship them.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;

    public interface IUnion
    {
        object? Value { get; }
    }
}
