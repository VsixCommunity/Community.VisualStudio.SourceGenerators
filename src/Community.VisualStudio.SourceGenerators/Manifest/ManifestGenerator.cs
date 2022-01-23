using System.Text;
using Microsoft.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class ManifestGenerator : GeneratorBase, IIncrementalGenerator
{
    private const string _manifestFileName = "source.extension.vsixmanifest";

    private static readonly DiagnosticDescriptor _invalidManifestFile = new(
        DiagnosticIds.CVSSG002_InvalidManifestFile,
        new LocalizableResourceString(nameof(Resources.CVSSG002_InvalidManifestFile_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG002_InvalidManifestFile_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string ManifestContents, string Namespace, string LangVersion)> values = context
            .AdditionalTextsProvider
            .Where(static (file) => Path.GetFileName(file.Path).Equals(_manifestFileName, StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select((x, cancellationToken) => (
                File: x.Left.GetText(cancellationToken)?.ToString() ?? "",
                Namespace: x.Right.GetNamespace(x.Left),
                LangVersion: x.Right.GetLangVersion()
            ));

        context.RegisterSourceOutput(
            values,
            (context, data) =>
            {
                if (string.IsNullOrEmpty(data.Namespace))
                {
                    context.ReportDiagnostic(Diagnostic.Create(CommonDiagnostics.NoNamespace, Location.None, _manifestFileName));
                    return;
                }

                Manifest manifest;
                try
                {
                    manifest = ManifestParser.Parse(data.ManifestContents);
                }
                catch (InvalidManifestException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_invalidManifestFile, Location.None, ex.Message));
                    return;
                }

                StringBuilder builder = new();
                WritePreamble(builder, data.LangVersion);
                builder.AppendLine($"namespace {data.Namespace}");
                builder.AppendLine("{");
                builder.AppendLine("    /// <summary>Defines constants from the <c>source.extension.vsixmanifest</c> file.</summary>");
                builder.AppendLine("    internal sealed partial class Vsix");
                builder.AppendLine("    {");
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
                builder.AppendLine("    }");
                builder.AppendLine("}");

                context.AddSource("Vsix.g.cs", builder.ToString());
            }
        );
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
