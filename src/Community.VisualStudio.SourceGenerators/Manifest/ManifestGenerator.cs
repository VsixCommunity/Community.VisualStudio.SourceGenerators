using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class ManifestGenerator : GeneratorBase, ISourceGenerator
{
    private const string _manifestFileName = "source.extension.vsixmanifest";

    private static readonly DiagnosticDescriptor _manifestFileNotFound = new(
        DiagnosticIds.CVSSG001_ManifestFileNotFound,
        new LocalizableResourceString(nameof(Resources.CVSSG001_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG001_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor _invalidManifestFile = new(
        DiagnosticIds.CVSSG002_InvalidManifestFile,
        new LocalizableResourceString(nameof(Resources.CVSSG002_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG002_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required.
    }

    public void Execute(GeneratorExecutionContext context)
    {
        AdditionalText? manifestFile = context.AdditionalFiles.FirstOrDefault(
            (file) => Path.GetFileName(file.Path).Equals(_manifestFileName, StringComparison.OrdinalIgnoreCase)
        );

        if (manifestFile is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(_manifestFileNotFound, Location.None));
            return;
        }

        Manifest manifest;
        try
        {
            manifest = ManifestParser.Parse(manifestFile.GetText(context.CancellationToken)?.ToString() ?? "");
        }
        catch (InvalidManifestException ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(_invalidManifestFile, Location.None, ex.Message));
            return;
        }

        if (!manifestFile.TryGetNamespace(context, out string? generatedNamespace))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommonDiagnostics.NoNamespace, Location.None, _manifestFileName));
            return;
        }

        StringBuilder builder = new();
        WritePreamble(builder);
        WriteClassStart(builder, generatedNamespace, "Defines constants from the <c>source.extension.vsixmanifest</c> file.", "Vsix");
        builder.AppendLine("        /// <summary>The author of the extension.</summary>");
        builder.AppendLine($"        public const string Author = \"{EscapeStringLiteral(manifest.Author)}\";");
        builder.AppendLine("");
        builder.AppendLine("        /// <summary>The description of the extension.</summary>");
        builder.AppendLine($"        public const string Description = \"{EscapeStringLiteral(manifest.Description)}\";");
        builder.AppendLine("");
        builder.AppendLine("        /// <summary>The extension identifier.</summary>");
        builder.AppendLine($"        public const string Id = \"{EscapeStringLiteral(manifest.Id)}\";");
        builder.AppendLine("");
        builder.AppendLine("        /// <summary>The default language for the extension.</summary>");
        builder.AppendLine($"        public const string Language = \"{EscapeStringLiteral(manifest.Language)}\";");
        builder.AppendLine("");
        builder.AppendLine("        /// <summary>The name of the extension.</summary>");
        builder.AppendLine($"        public const string Name = \"{EscapeStringLiteral(manifest.Name)}\";");
        builder.AppendLine("");
        builder.AppendLine("        /// <summary>The verison of the extension.</summary>");
        builder.AppendLine($"        public const string Version = \"{EscapeStringLiteral(manifest.Version)}\";");
        WriteClassEnd(builder);

        context.AddSource("Vsix.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static object EscapeStringLiteral(string value)
    {
        return value
            // Backslashes need to be replaced with two backslashes.
            .Replace("\\", "\\\\")
            // Quotes need to be escaped with a backslash.
            .Replace("\"", "\\\"");
    }
}
