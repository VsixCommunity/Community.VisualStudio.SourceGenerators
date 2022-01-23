using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Community.VisualStudio.SourceGenerators;

[Collection("GeneratorTests")]
public abstract class GeneratorTestBase : IDisposable
{
    private readonly List<string> _projectSegments = new();

    static GeneratorTestBase()
    {
        MSBuildLocator.RegisterDefaults();
    }

    protected GeneratorTestBase()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(TempDirectory);
    }

    protected abstract IIncrementalGenerator CreateGenerator();

    protected string TempDirectory { get; }

    protected async Task WriteFileAsync(string relativeFileName, string contents)
    {
        string fullPath = Path.Combine(TempDirectory, relativeFileName);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, contents).ConfigureAwait(false);
    }

    protected void SetProjectProperty(string name, string value)
    {
        _projectSegments.Add($@"
            <PropertyGroup>
                <{name}>{value}</{name}>
            </PropertyGroup>"
        );
    }

    protected void AddProjectFileFragment(string segment)
    {
        _projectSegments.Add(segment);
    }

    protected async Task<(Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics)> RunGeneratorAsync()
    {
        string projectFileName = Path.Combine(TempDirectory, $"Project.csproj");
        await WriteFileAsync(
            projectFileName,
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <Project ToolsVersion=""15.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
                <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
                <Import Project=""{GetGeneratorsImportPath(".props")}"" />
                <PropertyGroup>
                    <OutputType>Library</OutputType>
                    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
                </PropertyGroup>
                {string.Join(Environment.NewLine, _projectSegments)}
                <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
            </Project>"
        ).ConfigureAwait(false);

        using (MSBuildWorkspace workspace = MSBuildWorkspace.Create())
        {
            Project project = await workspace.OpenProjectAsync(projectFileName).ConfigureAwait(false);

            // Make sure the project loaded successfully.
            Assert.Empty(workspace.Diagnostics);

            Compilation? inputCompilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (inputCompilation is null)
            {
                throw new InvalidOperationException("Compilation is null.");
            }

            // Make sure the project compiles (warnings are OK, but errors are not).
            Assert.Empty(
                inputCompilation.GetDiagnostics().Where((x) => x.IsWarningAsError || x.Severity == DiagnosticSeverity.Error)
            );

            GeneratorDriver driver = CSharpGeneratorDriver
                    .Create(CreateGenerator())
                    .AddAdditionalTexts(ImmutableArray.CreateRange(await GetAdditionalTextsAsync(project).ConfigureAwait(false)))
                    .WithUpdatedAnalyzerConfigOptions(project.AnalyzerOptions.AnalyzerConfigOptionsProvider);

            if (project.ParseOptions is not null)
            {
                driver = driver.WithUpdatedParseOptions(project.ParseOptions);
            }

            driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out Compilation outputCompilation,
                out ImmutableArray<Diagnostic> diagnostics
            );

            return (outputCompilation, diagnostics);
        }
    }

    protected async Task<Compilation> RunGeneratorAndVerifyNoDiagnosticsAsync()
    {
        Compilation compilation;
        ImmutableArray<Diagnostic> diagnostics;
        (compilation, diagnostics) = await RunGeneratorAsync().ConfigureAwait(false);

        Assert.Empty(diagnostics);

        return compilation;
    }

    private static string GetGeneratorsImportPath(string extension, [CallerFilePath] string thisFilePath = "")
    {
        return Path.GetFullPath(
            "../../src/Community.VisualStudio.SourceGenerators/NuGet/build/Community.VisualStudio.SourceGenerators" + extension,
            Path.GetDirectoryName(thisFilePath) ?? ""
        );
    }

    private static async Task<IEnumerable<AdditionalText>> GetAdditionalTextsAsync(Project project)
    {
        List<AdditionalText> additionalTexts = new();

        foreach (TextDocument document in project.AdditionalDocuments)
        {
            additionalTexts.Add(
                new TestAdditionalText(
                    document.FilePath ?? "",
                    await document.GetTextAsync().ConfigureAwait(false)
                )
            );
        }

        return additionalTexts;
    }

    protected static void AssertNotNull([NotNull] object? value)
    {
        Assert.NotNull(value);

        // This check is necessary to keep the compiler happy, because it
        // doesn't know that xUnit will throw an exception if the value is null.
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        Directory.Delete(TempDirectory, true);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
