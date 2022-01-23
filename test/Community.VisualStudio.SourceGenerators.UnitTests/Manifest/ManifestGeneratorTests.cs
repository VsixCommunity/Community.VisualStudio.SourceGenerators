using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Community.VisualStudio.SourceGenerators;

public class ManifestGeneratorTests : GeneratorTestBase
{
    protected override IIncrementalGenerator CreateGenerator() => new ManifestGenerator();

    [Fact]
    public async Task ShouldReportDiagnosticWhenManifestFileIsInvalidAsync()
    {
        SetProjectProperty("RootNamespace", "Root");

        await WriteManifestAsync(@"
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011'>
                <Metadata/>
            </PackageManifest>"
        ).ConfigureAwait(false);

        ImmutableArray<Diagnostic> diagnostics;
        (_, diagnostics) = await RunGeneratorAsync().ConfigureAwait(false);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CVSSG002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task ShouldReportDiagnosticWhenNamespaceCannotBeDeterminedAsync()
    {
        SetProjectProperty("RootNamespace", "");

        await WriteManifestAsync(@"
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011'>
                <Metadata>
                    <Identity Id='My.Extension' Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>"
        ).ConfigureAwait(false);

        ImmutableArray<Diagnostic> diagnostics;
        (_, diagnostics) = await RunGeneratorAsync().ConfigureAwait(false);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CVSSG001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task ShouldGenerateTheCodeIntoTheRootNamespaceByDefaultAsync()
    {
        SetProjectProperty("RootNamespace", "Foo.Bar");

        await WriteManifestAsync(@"
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011'>
                <Metadata>
                    <Identity Id='My.Extension' Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>"
        ).ConfigureAwait(false);

        Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

        INamedTypeSymbol? vsixType = compilation.GetTypeByMetadataName("Foo.Bar.Vsix");
        await VerifyVsixTypeAsync(vsixType, new Manifest
        {
            Author = "The author",
            Description = "The description",
            Id = "My.Extension",
            Language = "en-US",
            Name = "My Test Extension",
            Version = "1.2.3"
        }).ConfigureAwait(false);
    }

    [Fact]
    public async Task ShouldGenerateTheCodeIntoTheNamespaceSpecifiedOnTheAdditionalFilesItemForTheManifestFileAsync()
    {
        AddProjectFileFragment(@"
            <ItemGroup>
                <AdditionalFiles Update='source.extension.vsixmanifest'>
                    <Namespace>Manifest.Props</Namespace>
                </AdditionalFiles>
            </ItemGroup>"
        );

        await WriteManifestAsync(@"
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011'>
                <Metadata>
                    <Identity Id='My.Extension' Version='1.2.3' Language='en-US' Publisher='The author' />
                    <DisplayName>My Test Extension</DisplayName>
                    <Description>The description</Description>
                </Metadata>
            </PackageManifest>"
        ).ConfigureAwait(false);

        Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

        INamedTypeSymbol? vsixType = compilation.GetTypeByMetadataName("Manifest.Props.Vsix");
        await VerifyVsixTypeAsync(vsixType, new Manifest
        {
            Author = "The author",
            Description = "The description",
            Id = "My.Extension",
            Language = "en-US",
            Name = "My Test Extension",
            Version = "1.2.3"
        }).ConfigureAwait(false);
    }

    [Fact]
    public async Task EscapesBackslashesAndQuotesAsync()
    {
        SetProjectProperty("RootNamespace", "MyExtension");

        await WriteManifestAsync(@"
            <PackageManifest Version='2.0.0' xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011'>
                <Metadata>
                    <Identity Id='My ""Awesome"" Extension' Version='1.2.3' Language='en-US' Publisher='The ""best"" author' />
                    <DisplayName>My ""Test"" Extension</DisplayName>
                    <Description>The description with ""quotes"" and \backslashes\.</Description>
                </Metadata>
            </PackageManifest>"
        ).ConfigureAwait(false);

        Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

        INamedTypeSymbol? vsixType = compilation.GetTypeByMetadataName($"MyExtension.Vsix");
        await VerifyVsixTypeAsync(vsixType, new Manifest
        {
            Author = "The \"best\" author",
            Description = "The description with \"quotes\" and \\backslashes\\.",
            Id = "My \"Awesome\" Extension",
            Language = "en-US",
            Name = "My \"Test\" Extension",
            Version = "1.2.3"
        }).ConfigureAwait(false);
    }

    private async Task WriteManifestAsync(string contents)
    {
        await WriteFileAsync("source.extension.vsixmanifest", contents).ConfigureAwait(false);

        AddProjectFileFragment(@"
            <ItemGroup>
                <None Include='source.extension.vsixmanifest'>
                    <SubType>Designer</SubType>
                    <Generator>VsixManifestGenerator</Generator>
                    <LastGenOutput>source.extension.cs</LastGenOutput>
                </None>
            </ItemGroup>"
        );
    }

    private static async Task VerifyVsixTypeAsync(INamedTypeSymbol? vsixType, Manifest expected)
    {
        AssertNotNull(vsixType);

        Assert.Equal(Accessibility.Internal, vsixType.DeclaredAccessibility);

        SyntaxReference syntaxReference = Assert.Single(vsixType.DeclaringSyntaxReferences);
        ClassDeclarationSyntax classDeclaration = Assert.IsAssignableFrom<ClassDeclarationSyntax>(
            await syntaxReference.GetSyntaxAsync().ConfigureAwait(false)
        );

        Assert.Contains(classDeclaration.Modifiers, (x) => x.IsKind(SyntaxKind.PartialKeyword));

        VerifyConstant(vsixType, "Author", expected.Author);
        VerifyConstant(vsixType, "Description", expected.Description);
        VerifyConstant(vsixType, "Id", expected.Id);
        VerifyConstant(vsixType, "Language", expected.Language);
        VerifyConstant(vsixType, "Name", expected.Name);
        VerifyConstant(vsixType, "Version", expected.Version);
    }

    private static void VerifyConstant(INamedTypeSymbol containingType, string name, object value)
    {
        ISymbol member = Assert.Single(containingType.GetMembers(name));
        IFieldSymbol field = Assert.IsAssignableFrom<IFieldSymbol>(member);

        Assert.Equal(SpecialType.System_String, field.Type.SpecialType);
        Assert.True(field.IsConst);
        Assert.Equal(value, field.ConstantValue);
    }
}
