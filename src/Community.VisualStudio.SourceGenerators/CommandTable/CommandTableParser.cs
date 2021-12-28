using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Community.VisualStudio.SourceGenerators;

public class CommandTableParser
{
    private readonly XmlDocument _document;
    private readonly XmlNamespaceManager _namespaceManager;

    public static CommandTable Parse(string contents)
    {
        XmlDocument document = new();

        try
        {
            document.LoadXml(contents);
        }
        catch (XmlException ex)
        {
            throw new InvalidCommandTableException(
                string.Format(CultureInfo.CurrentCulture, Resources.Error_CouldNotParseManifest, ex.Message)
            );
        }

        return new CommandTableParser(document).Parse();
    }

    private CommandTableParser(XmlDocument document)
    {
        _document = document;
        _namespaceManager = new XmlNamespaceManager(_document.NameTable);
        _namespaceManager.AddNamespace("x", document.DocumentElement.NamespaceURI);
    }

    private CommandTable Parse()
    {
        CommandTable commandTable = new();

        XmlNodeList symbols = _document.SelectNodes("/x:CommandTable/x:Symbols/x:GuidSymbol", _namespaceManager);
        foreach (XmlElement symbol in symbols.OfType<XmlElement>())
        {
            string guidName = symbol.GetAttribute("name");

            if (!TryParseGuid(symbol.GetAttribute("value"), out Guid guidValue))
            {
                throw new InvalidCommandTableException(
                    string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidGuidSymbol, guidName)
                );
            }

            commandTable.Guids[guidName] = guidValue;

            IEnumerable<XmlElement> ids = symbol.SelectNodes("x:IDSymbol", _namespaceManager).OfType<XmlElement>();
            foreach (XmlElement id in ids)
            {
                string idName = id.GetAttribute("name");

                if (!TryParseId(id.GetAttribute("value"), out int idValue))
                {
                    throw new InvalidCommandTableException(
                        string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidIdSymbol, idName)
                    );
                }

                commandTable.Ids[idName] = idValue;
            }
        }

        return commandTable;
    }

    private static bool TryParseGuid(string text, out Guid value)
    {
        // According to the VSCT schema file, there are only two supported GUID formats:
        // 
        //  * {6D484634-E53D-4a2c-ADCB-55145C9362C8}
        //  * { 0x6d484634, 0xe53d, 0x4a2c, { 0xad, 0xcb, 0x55, 0x14, 0x5c, 0x93, 0x62, 0xc8 } };
        // 
        // We will try to parse the GUID using only those formats.
        return Guid.TryParseExact(text, "B", out value) ||
               Guid.TryParseExact(text, "X", out value);
    }

    private static bool TryParseId(string text, out int value)
    {
        // The `NumberStyles.AllowHexSpecifier` style (which is part of the `HexNumber` style) does not
        // actually allow a hex specifier at the start of the string, so we need to trim that off ourselves.
        // The following regular expression is based on what the VSCT schema file uses for validation.
        Match match = Regex.Match(text.Trim(), "^0[Xx]([0-9a-fA-F]*)$");

        if (match.Success)
        {
            return int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        value = 0;
        return false;
    }
}
