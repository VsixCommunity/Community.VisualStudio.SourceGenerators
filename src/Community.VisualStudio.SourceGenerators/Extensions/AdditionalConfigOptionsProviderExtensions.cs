using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Community.VisualStudio.SourceGenerators;

internal static class AdditionalConfigOptionsProviderExtensions
{
    public static string GetNamespace(this AnalyzerConfigOptionsProvider options, AdditionalText file)
    {
        // Check if a namespace was specified in the metadata for the additional file.
        if (options.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.Namespace", out string? fileNamespace))
        {
            if (!string.IsNullOrEmpty(fileNamespace))
            {
                return fileNamespace;
            }
        }

        // Fall back to using the root namespace from the project.
        if (options.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? rootNamespace))
        {
            if (!string.IsNullOrEmpty(rootNamespace))
            {
                return rootNamespace;
            }
        }

        return "";
    }

    public static string GetLangVersion(this AnalyzerConfigOptionsProvider options)
    {
        if (options.GlobalOptions.TryGetValue("build_property.LangVersion", out string? version))
        {
            return version;
        }

        // Extensions are built against the .NET Framework,
        // which has a default language version of 7.3.
        return "7.3";
    }
}
