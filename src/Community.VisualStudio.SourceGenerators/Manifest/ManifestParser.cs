using System.Globalization;
using System.Xml;

namespace Community.VisualStudio.SourceGenerators;

public class ManifestParser
{
    private static readonly Version _supportedManifestVersion = new(2, 0, 0);

    private readonly XmlDocument _document;
    private readonly XmlNamespaceManager _namespaceManager;

    public static Manifest Parse(string xml)
    {
        XmlDocument document = new();
        try
        {
            document.LoadXml(xml);
        }
        catch (XmlException ex)
        {
            throw new InvalidManifestException(string.Format(CultureInfo.CurrentCulture, Resources.Error_CouldNotParseManifest, ex.Message));
        }

        return new ManifestParser(document).Parse();
    }

    private ManifestParser(XmlDocument document)
    {
        _document = document;
        _namespaceManager = new XmlNamespaceManager(_document.NameTable);
        _namespaceManager.AddNamespace("x", document.DocumentElement.NamespaceURI);

        VerifyManifestVersion();
    }

    private void VerifyManifestVersion()
    {
        string value = GetNodeValue("/x:PackageManifest/@Version", false);
        if (!string.IsNullOrEmpty(value) && Version.TryParse(value, out Version version))
        {
            if (version == _supportedManifestVersion)
            {
                return;
            }
        }

        throw new InvalidManifestException($"Only version {_supportedManifestVersion} VSIX manifest files are supported.");
    }

    private Manifest Parse()
    {
        Manifest manifest = new();

        manifest.Author = GetMetadataValue("x:Identity/@Publisher", true);
        manifest.Description = GetMetadataValue("x:Description", true);
        manifest.Id = GetMetadataValue("x:Identity/@Id", true);
        manifest.Language = GetMetadataValue("x:Identity/@Language", true);
        manifest.Name = GetMetadataValue("x:DisplayName", true);
        manifest.Version = GetMetadataValue("x:Identity/@Version", true);

        return manifest;
    }

    private string GetMetadataValue(string xpath, bool required)
    {
        return GetNodeValue("/x:PackageManifest/x:Metadata/" + xpath, required);
    }

    private string GetNodeValue(string xpath, bool required)
    {
        XmlNode? node = _document.SelectSingleNode(xpath, _namespaceManager);

        if (node is XmlElement element)
        {
            return element.InnerText.Trim();
        }
        else if (node is not null)
        {
            return node.Value;
        }
        else if (required)
        {
            throw new InvalidManifestException(
                string.Format(CultureInfo.CurrentCulture, Resources.Error_ManifestNodeNotFound, xpath.Replace("x:", ""))
            );
        }

        return "";
    }
}
