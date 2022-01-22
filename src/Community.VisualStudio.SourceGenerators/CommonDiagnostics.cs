using Microsoft.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

internal static class CommonDiagnostics
{
    internal const string Category = "VSIX";

    internal static readonly DiagnosticDescriptor NoNamespace = new(
        DiagnosticIds.CVSSG001_NoNamespace,
        new LocalizableResourceString(nameof(Resources.CVSSG001_NoNamespace_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG001_NoNamespace_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        Category,
        DiagnosticSeverity.Error,
        true
    );
}
