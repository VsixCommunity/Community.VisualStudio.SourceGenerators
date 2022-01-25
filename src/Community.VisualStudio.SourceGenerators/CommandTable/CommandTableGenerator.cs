using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class CommandTableGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor _invalidCommandTableFile = new(
        DiagnosticIds.CVSSG003_InvalidCommandTableFile,
        new LocalizableResourceString(nameof(Resources.CVSSG003_InvalidCommandTableFile_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG003_InvalidCommandTableFile_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    private readonly DefaultCommandTableCodeWriter _defaultWriter = new();
    private readonly VsixSynchronizerCommandTableCodeWriter _vsxiSynchronizerWriter = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string FilePath, string FileContents, string Namespace, string LangVersion, string Format)> values = context
            .AdditionalTextsProvider
            .Where(static (file) => Path.GetExtension(file.Path).Equals(".vsct", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (item, cancellationToken) => (
                FilePath: item.Left.Path,
                FileContents: item.Left.GetText(cancellationToken)?.ToString() ?? "",
                Namespace: item.Right.GetNamespace(item.Left),
                LangVersion: item.Right.GetLangVersion(),
                Format: GetFormat(item.Right, item.Left)
            ));

        context.RegisterSourceOutput(
            values,
            (context, data) =>
            {
                if (string.IsNullOrEmpty(data.Namespace))
                {
                    context.ReportDiagnostic(Diagnostic.Create(CommonDiagnostics.NoNamespace, Location.None, Path.GetFileName(data.FilePath)));
                    return;
                }

                CommandTable commandTable;
                try
                {
                    commandTable = CommandTableParser.Parse(Path.GetFileNameWithoutExtension(data.FilePath), data.FileContents);
                }
                catch (InvalidCommandTableException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_invalidCommandTableFile, Location.None, data.FilePath, ex.Message));
                    return;
                }

                CommandTableCodeWriter writer;
                if (string.Equals(data.Format, _vsxiSynchronizerWriter.Format, StringComparison.OrdinalIgnoreCase))
                {
                    writer = _vsxiSynchronizerWriter;
                }
                else
                {
                    writer = _defaultWriter;
                }

                foreach (GeneratedFile file in writer.Write(commandTable, data.Namespace, data.LangVersion))
                {
                    context.AddSource(file.FileName, file.Code);
                }
            }
        );
    }

    private static string GetFormat(AnalyzerConfigOptionsProvider options, AdditionalText file)
    {
        options.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.Format", out string? format);
        return format ?? "";
    }
}
