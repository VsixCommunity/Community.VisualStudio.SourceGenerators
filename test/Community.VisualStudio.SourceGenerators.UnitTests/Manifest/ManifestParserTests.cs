namespace Community.VisualStudio.SourceGenerators;

public class ManifestParserTests
{
    [Fact]
    public void CanParseTheManifestFile()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
                <Metadata>
                    <Identity Id='my.test.extension' Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>".TrimStart();

        Manifest manifest = ManifestParser.Parse(contents);

        Assert.NotNull(manifest);
        Assert.Equal("The author", manifest.Author);
        Assert.Equal("The description", manifest.Description);
        Assert.Equal("my.test.extension", manifest.Id);
        Assert.Equal("en-US", manifest.Language);
        Assert.Equal("My Test Extension", manifest.Name);
        Assert.Equal("1.2.3", manifest.Version);
    }

    [Fact]
    public void RequiresAuthor()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
                <Metadata>
                    <Identity Id='my.test.extension' Version='1.2.3' Language='en-US' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }

    [Fact]
    public void RequiresIdentity()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
                <Metadata>
                    <Identity Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }

    [Fact]
    public void RequiresLanguage()
    {
        string contents = @"
        <?xml version='1.0' encoding='utf-8'?>
        <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
            <Metadata>
                <Identity Id='my.test.extension' Version='1.2.3' Publisher='The author' />
                <DisplayName>My Test Extension</DisplayName>
                <Description>The description</Description>
            </Metadata>
        </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }

    [Fact]
    public void RequiresVersion()
    {
        string contents = @"
        <?xml version='1.0' encoding='utf-8'?>
        <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
            <Metadata>
                <Identity Id='my.test.extension' Language='en-US' Publisher='The author' />
                <DisplayName>My Test Extension</DisplayName>
                <Description>The description</Description>
            </Metadata>
        </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }

    [Fact]
    public void RequiresName()
    {
        string contents = @"
    <?xml version='1.0' encoding='utf-8'?>
    <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
        <Metadata>
            <Identity Id='my.test.extension' Version='1.2.3' Language='en-US' Publisher='The author' />
            <Description>The description</Description>
        </Metadata>
    </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }

    [Fact]
    public void RequiresDescription()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011' xmlns:d='http://schemas.microsoft.com/developer/vsx-schema-design/2011'>
                <Metadata>
                    <Identity Id='my.test.extension' Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                </Metadata>
            </PackageManifest>".TrimStart();

        Assert.Throws<InvalidManifestException>(() => ManifestParser.Parse(contents));
    }
}