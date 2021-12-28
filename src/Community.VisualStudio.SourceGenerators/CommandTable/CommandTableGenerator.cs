using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Community.VisualStudio.SourceGenerators;

[Generator]
public class CommandTableGenerator : GeneratorBase, ISourceGenerator
{
    private static readonly DiagnosticDescriptor _invalidCommandTableFile = new(
        DiagnosticIds.CVSSG004_InvalidCommandTableFile,
        new LocalizableResourceString(nameof(Resources.CVSSG004_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG004_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        CommonDiagnostics.Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor _duplicateSymbol = new(
        DiagnosticIds.CVSSG005_DuplicateSymbol,
        new LocalizableResourceString(nameof(Resources.CVSSG005_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.CVSSG005_MessageFormat), Resources.ResourceManager, typeof(Resources)),
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
        IEnumerable<AdditionalText> commandTableFiles = context.AdditionalFiles.Where(
            (file) => Path.GetExtension(file.Path).Equals(".vsct", StringComparison.OrdinalIgnoreCase)
        );

        Dictionary<string, List<CommandTable>> commandTablesByNamespace = new();

        foreach (AdditionalText file in commandTableFiles)
        {
            if (!file.TryGetNamespace(context, out string commandTableNamespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(CommonDiagnostics.NoNamespace, Location.None, Path.GetFileName(file.Path)));
                continue;
            }

            CommandTable commandTable;
            try
            {
                commandTable = CommandTableParser.Parse(file.GetText(context.CancellationToken)?.ToString() ?? "");
            }
            catch (InvalidCommandTableException ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(_invalidCommandTableFile, Location.None, file.Path, ex.Message));
                continue;
            }

            if (!commandTablesByNamespace.TryGetValue(commandTableNamespace, out List<CommandTable>? commandTables))
            {
                commandTables = new List<CommandTable>();
                commandTablesByNamespace[commandTableNamespace] = commandTables;
            }

            commandTables.Add(commandTable);
        }

        if (commandTablesByNamespace.Count > 0)
        {
            GeneratePackageGuids(commandTablesByNamespace, context);
            GeneratePackageIds(commandTablesByNamespace, context);
        }
    }

    private static void GeneratePackageGuids(Dictionary<string, List<CommandTable>> commandTablesByNamespace, GeneratorExecutionContext context)
    {
        StringBuilder builder = new();

        WritePreamble(builder);

        foreach (KeyValuePair<string, List<CommandTable>> group in commandTablesByNamespace)
        {
            Dictionary<string, Guid> symbolsSeen = new();

            WriteClassStart(builder, group.Key, "Defines GUIDs from VSCT files.", "PackageGuids");

            foreach (KeyValuePair<string, Guid> symbol in GetUniqueSymbolsAndReportDuplicates(group.Value.SelectMany((x) => x.Guids), GetGuidName, context))
            {
                builder.AppendLine($"        public const string {symbol.Key}String = \"{symbol.Value:B}\";");
                builder.AppendLine($"        public static readonly System.Guid {symbol.Key} = new System.Guid({symbol.Key}String);");
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        context.AddSource("PackageGuids.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void GeneratePackageIds(Dictionary<string, List<CommandTable>> commandTablesByNamespace, GeneratorExecutionContext context)
    {
        StringBuilder builder = new();

        WritePreamble(builder);

        foreach (KeyValuePair<string, List<CommandTable>> group in commandTablesByNamespace)
        {
            Dictionary<string, int> symbolsSeen = new();

            WriteClassStart(builder, group.Key, "Defines IDs from VSCT files.", "PackageIds");

            foreach (KeyValuePair<string, int> symbol in GetUniqueSymbolsAndReportDuplicates(group.Value.SelectMany((x) => x.Ids), null, context))
            {
                builder.AppendLine($"        public const int {symbol.Key} = 0x{symbol.Value:X4};");
            }

            WriteClassEnd(builder);
        }

        context.AddSource("PackageIds.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static IEnumerable<KeyValuePair<string, T>> GetUniqueSymbolsAndReportDuplicates<T>(IEnumerable<KeyValuePair<string, T>> symbols, Func<string, string>? nameModifier, GeneratorExecutionContext context) where T : struct
    {
        Dictionary<string, T> symbolsSeen = new();

        foreach (KeyValuePair<string, T> symbol in symbols.OrderBy((x) => x.Key))
        {
            string name = symbol.Key;
            if (nameModifier is not null)
            {
                name = nameModifier(name);
            }

            if (symbolsSeen.TryGetValue(name, out T existing))
            {
                // We've already seen a symbol with this name. If this symbol has the same
                // value as the other one, then we'll just ignore it; otherwise, we'll
                // report a diagnostic so that we don't silently ignore the symbol.
                if (!Equals(existing, symbol.Value))
                {
                    // Use the original name instead than the modified name,
                    // since the original name is the one that needs to be fixed.
                    context.ReportDiagnostic(Diagnostic.Create(_duplicateSymbol, Location.None, symbol.Key));
                }
            }
            else
            {
                symbolsSeen.Add(name, symbol.Value);
                yield return new KeyValuePair<string, T>(name, symbol.Value);
            }
        }
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
