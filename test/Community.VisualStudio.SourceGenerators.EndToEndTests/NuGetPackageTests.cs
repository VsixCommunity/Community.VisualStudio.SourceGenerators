using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Build.Locator;
using Xunit.Abstractions;

namespace Community.VisualStudio.SourceGenerators;

public sealed class NuGetPackageTests : IClassFixture<NuGetPackageTests.Fixture>
{
    private const string _version = "0.0.1";
    private const string _packageVersion = _version + "-e2e";

    private readonly Fixture _fixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public NuGetPackageTests(Fixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("DefaultFormat")]
    [InlineData("VsixSynchronizerFormat")]
    [InlineData("Wpf")] // A specific test for WPF because of https://github.com/dotnet/wpf/issues/3404.
    [InlineData("CustomNamespace")]
    public async Task NuGetPackageWorksAsync(string projectDirectory)
    {
        await _fixture.BuildProjectAsync(projectDirectory, _testOutputHelper).ConfigureAwait(false);
    }

    public sealed class Fixture : IDisposable
    {
        private readonly VisualStudioInstance _visualStudio;
        private readonly string _rootDirectory;
        private readonly string _nugetRegistry;
        private readonly string _nugetCache;
        private readonly string _solutionDirectory;
        private bool _havePackedSourceGenerators;

        public Fixture()
        {
            _visualStudio = MSBuildLocator
                .QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions { DiscoveryTypes = DiscoveryType.VisualStudioSetup })
                .OrderByDescending((x) => x.Version)
                .First();

            MSBuildLocator.RegisterInstance(_visualStudio);

            _rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            _nugetRegistry = Path.Combine(_rootDirectory, "nuget");
            _nugetCache = Path.Combine(_rootDirectory, "cache");

            Directory.CreateDirectory(_nugetRegistry);
            Directory.CreateDirectory(_nugetCache);

            _solutionDirectory = GetEndToEndSolutionDirectory();

            File.WriteAllText(Path.Combine(_solutionDirectory, "nuget.config"), @$"
                <configuration>
                    <packageSources>
                        <clear/>
                        <add key='e2e' value='{_nugetRegistry}' />
                        <add key='nuget' value='https://api.nuget.org/v3/index.json' />
                    </packageSources>
                </configuration>
            ");
        }

        private static string GetEndToEndSolutionDirectory([CallerFilePath] string thisFilePath = "")
        {
            return Path.GetFullPath(Path.Combine(thisFilePath, "../EndToEndSolution"));
        }

        public async Task BuildProjectAsync(string projectDirectory, ITestOutputHelper testOutputHelper)
        {
            if (!_havePackedSourceGenerators)
            {
                await PackedSourceGeneratorsAsync(testOutputHelper).ConfigureAwait(false);
                _havePackedSourceGenerators = true;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Combine(_visualStudio.MSBuildPath, "MSBuild.exe"),
                Arguments = $"/t:Rebuild /Restore /nr:false /v:m /p:SourceGeneratorsVersion={_packageVersion}",
                WorkingDirectory = Path.Combine(_solutionDirectory, projectDirectory)
            };

            // Override the NuGet cache directory so that the package that
            // we built for these tests isn't put into the normal cache.
            startInfo.Environment["NUGET_PACKAGES"] = _nugetCache;

            await ExecuteAsync(startInfo, testOutputHelper).ConfigureAwait(false);
        }

        private async Task PackedSourceGeneratorsAsync(ITestOutputHelper testOutputHelper)
        {
            string sourceGeneratorsDirectory = GetSourceGeneratorsDirectory();

            // For some reason, running "dotnet pack" won't
            // build the project first, so build it manually.
            await ExecuteAsync(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build -c Release -p:Version={_version}",
                    WorkingDirectory = sourceGeneratorsDirectory
                },
                testOutputHelper
            ).ConfigureAwait(false);

            await ExecuteAsync(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"pack -c Release -o {_nugetRegistry} -p:PackageVersion={_packageVersion}",
                    WorkingDirectory = sourceGeneratorsDirectory
                },
                testOutputHelper
            ).ConfigureAwait(false);
        }

        private static string GetSourceGeneratorsDirectory([CallerFilePath] string thisFilePath = "")
        {
            return Path.GetFullPath(Path.Combine(thisFilePath, "../../../src/Community.VisualStudio.SourceGenerators"));
        }

        private static async Task ExecuteAsync(ProcessStartInfo startInfo, ITestOutputHelper testOutputHelper)
        {
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using (Process process = Process.Start(startInfo))
            {
                await Task.WhenAll(
                    DrainReaderAsync(process.StandardOutput, testOutputHelper),
                    DrainReaderAsync(process.StandardError, testOutputHelper)
                ).ConfigureAwait(false);

                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }
        }

        private static async Task DrainReaderAsync(StreamReader reader, ITestOutputHelper testOutputHelper)
        {
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                testOutputHelper.WriteLine(line);
            }
        }

        public void Dispose()
        {
            File.Delete(Path.Combine(_solutionDirectory, "nuget.config"));
            Directory.Delete(_rootDirectory, true);
        }
    }
}
