namespace Community.VisualStudio.SourceGenerators;

internal class CommandTable
{
    public CommandTable(string name, IEnumerable<GUIDSymbol> gUIDSymbols)
    {
        Name = name;
        GUIDSymbols = gUIDSymbols;
    }

    public string Name { get; } 

    public IEnumerable<GUIDSymbol> GUIDSymbols { get; } 
}
