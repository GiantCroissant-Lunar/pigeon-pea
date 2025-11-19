// Temporary shim for init-only properties when targeting netstandard2.1
// This allows use of 'init' setters in contracts without requiring a newer BCL.
namespace System.Runtime.CompilerServices;

internal sealed class IsExternalInit
{
}
