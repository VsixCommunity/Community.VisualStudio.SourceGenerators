using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Community.VisualStudio.SourceGenerators;

internal class TestAdditionalText : AdditionalText
{
    private readonly SourceText _sourceText;

    public TestAdditionalText(string path, SourceText sourceText)
    {
        Path = path;
        _sourceText = sourceText;
    }

    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken = default) => _sourceText;
}
