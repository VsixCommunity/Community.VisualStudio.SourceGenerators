using System.Text;

namespace Community.VisualStudio.SourceGenerators;

/// <summary>
/// Writes the generated code for command table files like the VSIX Synchronizer extension does.
/// </summary>
internal class VsixSynchronizerCommandTableCodeWriter : CommandTableCodeWriter
{
    public override string Format => "VsixSynchronizer";
   
    public override IEnumerable<GeneratedFile> Write(CommandTable commandTable, string codeNamespace, string langVersion)
    {
        yield return GeneratePackageGuids(commandTable, codeNamespace, langVersion);
        yield return GeneratePackageIds(commandTable, codeNamespace, langVersion);
    }

    private static GeneratedFile GeneratePackageGuids(CommandTable commandTable, string containingNamespace, string langVersion)
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
            string guidName = SafeIdentifierName(GetGuidName(symbol.Name));
            builder.AppendLine($"        public const string {guidName}String = \"{symbol.Value:D}\";");
            builder.AppendLine($"        public static readonly System.Guid {guidName} = new System.Guid({guidName}String);");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return new GeneratedFile(SafeHintName($"PackageGuids.{commandTable.Name}.g.cs"), builder.ToString());
    }

    private static GeneratedFile GeneratePackageIds(CommandTable commandTable, string containingNamespace, string langVersion)
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
            builder.AppendLine($"        public const int {SafeIdentifierName(symbol.Name)} = 0x{symbol.Value:X4};");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return new GeneratedFile(SafeHintName($"PackageIds.{commandTable.Name}.g.cs"), builder.ToString());
    }
}
