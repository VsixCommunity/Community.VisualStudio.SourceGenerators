using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;

namespace Community.VisualStudio.SourceGenerators;

internal abstract class CommandTableCodeWriter : WriterBase
{
    public abstract string Format { get; }

    public abstract IEnumerable<GeneratedFile> Write(CommandTable commandTable, string codeNamespace, string langVersion);

    protected private static string GetGuidName(string name)
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

    protected static string SafeIdentifierName(string name)
    {
        // Most of the time the identifier name will be fine,
        // so rather than always copying the name, we'll
        // check if the name needs to be altered first.
        if (!name.All(SyntaxFacts.IsIdentifierPartCharacter))
        {
            StringBuilder buffer = new(name.Length);
            foreach (char ch in name)
            {
                if (SyntaxFacts.IsIdentifierPartCharacter(ch))
                {
                    buffer.Append(ch);
                }
                else
                {
                    buffer.Append('_');
                }
            }

            name = buffer.ToString();
        }

        // Make sure the name starts with a valid character.
        // If it doesn't, then prepend an underscore.
        if (name.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(name[0]))
        {
            name = "_" + name;
        };

        return name;
    }

    protected static string SafeHintName(string name)
    {
        // Most of the time the name will be fine,
        // so rather than always copying the name, we'll
        // check if the name needs to be altered first.
        if (name.All(IsValidHintNameCharacter))
        {
            return name;
        }

        StringBuilder buffer = new(name.Length);
        foreach (char ch in name)
        {
            if (IsValidHintNameCharacter(ch))
            {
                buffer.Append(ch);
            }
            else
            {
                buffer.Append('_');
            }
        }

        return buffer.ToString();
    }

    private static bool IsValidHintNameCharacter(char ch)
    {
        // This check is taken from the validation
        // in Roslyn's `AdditionalSourcesCollection`.
        ///
        // Allow any identifier character or any of these characters:
        //  [.,-_ ()[]{}]
        //
        // Note that the latest version also allows + and `, but we want to be compatible
        // with earlier versions, so we'll consider those two characters to be invalid.
        return SyntaxFacts.IsIdentifierPartCharacter(ch)
            || ch == '.'
            || ch == ','
            || ch == '-'
            || ch == '_'
            || ch == ' '
            || ch == '('
            || ch == ')'
            || ch == '['
            || ch == ']'
            || ch == '{'
            || ch == '}';
    }
}
