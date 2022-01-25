using Microsoft.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class ManifestGenerator : IIncrementalGenerator
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

                GeneratedFile file = ManifestCodeWriter.Write(manifest, data.Namespace, data.LangVersion);
                context.AddSource(file.FileName, file.Code);
            }
        );
    }
}
