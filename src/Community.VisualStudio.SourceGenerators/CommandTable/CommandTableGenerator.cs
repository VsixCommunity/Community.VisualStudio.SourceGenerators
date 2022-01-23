using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class CommandTableGenerator : GeneratorBase, IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor _invalidCommandTableFile = new(
        DiagnosticIds.CVSSG003_InvalidCommandTableFile,
        new LocalizableResourceString(nameof(Resources.CVSSG003_InvalidCommandTableFile_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG003_InvalidCommandTableFile_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<(string FilePath, string FileContents, string Namespace, string LangVersion)> values = context
            .AdditionalTextsProvider
            .Where(static (file) => Path.GetExtension(file.Path).Equals(".vsct", StringComparison.OrdinalIgnoreCase))
            .Where(static (file) => Path.GetExtension(file.Path).Equals(".vsct", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (item, cancellationToken) => (
                FilePath: item.Left.Path,
                FileContents: item.Left.GetText(cancellationToken)?.ToString() ?? "",
                Namespace: item.Right.GetNamespace(item.Left),
                LangVersion: item.Right.GetLangVersion()
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

                GeneratePackageGuids(commandTable, data.Namespace, data.LangVersion, context);
                GeneratePackageIds(commandTable, data.Namespace, data.LangVersion, context);
            }
        );
    }

    private static void GeneratePackageGuids(CommandTable commandTable, string containingNamespace, string langVersion, SourceProductionContext context)
    {
        StringBuilder builder = new();
        WritePreamble(builder, langVersion);
        builder.AppendLine($"namespace {containingNamespace}");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Defines GUIDs from VSCT files.</summary>");
        builder.AppendLine("    internal sealed partial class PackageGuids");
        builder.AppendLine("    {");

        foreach (GUIDSymbol symbol in commandTable.GUIDSymbols.OrderBy((x) => x.Name))
        {
            string guidName = GetGuidName(symbol.Name);
            builder.AppendLine($"        public const string {guidName}String = \"{symbol.Value:D}\";");
            builder.AppendLine($"        public static readonly System.Guid {guidName} = new System.Guid({guidName}String);");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource($"PackageGuids.{commandTable.Name}.g.cs", builder.ToString());
    }

    private static void GeneratePackageIds(CommandTable commandTable, string containingNamespace, string langVersion, SourceProductionContext context)
    {
        StringBuilder builder = new();
        WritePreamble(builder, langVersion);
        builder.AppendLine($"namespace {containingNamespace}");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>Defines IDs from VSCT files.</summary>");
        builder.AppendLine("    internal sealed partial class PackageIds");
        builder.AppendLine("    {");

        foreach (IDSymbol symbol in commandTable.GUIDSymbols.SelectMany((x) => x.IDSymbols).OrderBy((x) => x.Name))
        {
            builder.AppendLine($"        public const int {symbol.Name} = 0x{symbol.Value:X4};");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource($"PackageIds.{commandTable.Name}.g.cs", builder.ToString());
    }

    private static string GetGuidName(string name)
    {
        // If the name starts with "guid", then trim it off because
        // that prefix doesn't provide any additional information
        // since all symbols defined in the class are GUIDs.
        if (Regex.IsMatch(name, "^guid[A-Z]"))
        {
            name = name.Substring(4);
        }

        return name;
    }
}
