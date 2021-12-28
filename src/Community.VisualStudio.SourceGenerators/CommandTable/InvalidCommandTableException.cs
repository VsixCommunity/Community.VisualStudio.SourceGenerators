using System.Diagnostics.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Exception is only used internally.")]
public class InvalidCommandTableException : Exception
{
    public InvalidCommandTableException(string message) : base(message) { }
}
