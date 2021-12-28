using Microsoft.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

internal static class AdditionalTextExtensions
{
    public static bool TryGetNamespace(this AdditionalText manifestFile, GeneratorExecutionContext context, out string value)
    {
        // Check if a namespace was specified in the metadata for the additional file.
        if (context.AnalyzerConfigOptions.GetOptions(manifestFile).TryGetValue("build_metadata.AdditionalFiles.Namespace", out string? fileNamespace))
        {
            if (!string.IsNullOrEmpty(fileNamespace))
            {
                value = fileNamespace;
                return true;
            }
        }

        // Fall back to using the root namespace from the project.
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespace))
        {
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                value = rootNamespace;
                return true;
            }
        }

        value = "";
        return false;
    }
}
