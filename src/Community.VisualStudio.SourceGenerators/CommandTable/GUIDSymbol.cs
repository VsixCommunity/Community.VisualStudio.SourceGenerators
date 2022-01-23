namespace Community.VisualStudio.SourceGenerators;

internal class GUIDSymbol
{
    public GUIDSymbol(string name, Guid value, IEnumerable<IDSymbol> idSymbols)
    {
        Name = name;
        Value = value;
        IDSymbols = idSymbols;
    }

    public string Name { get; }

    public Guid Value { get; }

    public IEnumerable<IDSymbol> IDSymbols { get; }

    public override string ToString()
    {
        return $"{Name}={Value} [{string.Join(", ", IDSymbols)}]";
    }
}
