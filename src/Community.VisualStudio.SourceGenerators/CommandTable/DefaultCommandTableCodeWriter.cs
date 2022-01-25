using System.Text;

namespace Community.VisualStudio.SourceGenerators;

/// <summary>
/// Writes the generated code for command table in the default format.
/// </summary>
internal class DefaultCommandTableCodeWriter : CommandTableCodeWriter
{
    public override string Format => "Default";

    public override IEnumerable<GeneratedFile> Write(CommandTable commandTable, string codeNamespace, string langVersion)
    {
        StringBuilder builder = new();
        WritePreamble(builder, langVersion);
        builder.AppendLine($"namespace {codeNamespace}");
        builder.AppendLine("{");
        builder.AppendLine($"    /// <summary>Defines symbols from the {commandTable.Name}.vsct file.</summary>");
        builder.AppendLine($"    internal sealed partial class {SafeIdentifierName(commandTable.Name)}");
        builder.AppendLine("    {");

        foreach (GUIDSymbol guidSymbol in commandTable.GUIDSymbols.OrderBy((x) => x.Name))
        {
            string guidName = GetGuidName(guidSymbol.Name);
            builder.AppendLine($"        /// <summary>Defines the \"{guidName}\" GUIDSymbol and its IDSymbols.</summary>");
            builder.AppendLine($"        internal sealed partial class {SafeIdentifierName(guidName)}");
            builder.AppendLine("        {");

            builder.AppendLine($"            public const string GuidString = \"{guidSymbol.Value:D}\";");
            builder.AppendLine($"            public static readonly System.Guid Guid = new System.Guid(GuidString);");

            foreach (IDSymbol idSymbol in guidSymbol.IDSymbols.OrderBy((x) => x.Name))
            {
                builder.AppendLine($"            public const int {SafeIdentifierName(idSymbol.Name)} = 0x{idSymbol.Value:X4};");
            }

            builder.AppendLine("        }");
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        yield return new GeneratedFile(SafeHintName($"{commandTable.Name}.g.cs"), builder.ToString());
    }
}
