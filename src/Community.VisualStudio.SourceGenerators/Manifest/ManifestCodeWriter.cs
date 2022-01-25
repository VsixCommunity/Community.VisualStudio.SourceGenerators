using System.Text;

namespace Community.VisualStudio.SourceGenerators;

internal class ManifestCodeWriter : WriterBase
{
    public static GeneratedFile Write(Manifest manifest, string codeNamespace, string langVersion)
    {
        StringBuilder builder = new();
        WritePreamble(builder, langVersion);
        builder.AppendLine($"namespace {codeNamespace}");
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

        return new GeneratedFile("Vsix.g.cs", builder.ToString());
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
