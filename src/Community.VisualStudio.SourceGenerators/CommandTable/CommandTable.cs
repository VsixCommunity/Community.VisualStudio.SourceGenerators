namespace Community.VisualStudio.SourceGenerators;

public class CommandTable
{
    public Dictionary<string, Guid> Guids { get; } = new();

    public Dictionary<string, int> Ids { get; } = new();
}
