namespace Community.VisualStudio.SourceGenerators;

internal class IDSymbol
{
    public IDSymbol(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public int Value { get; }

    public override string ToString()
    {
        return $"{Name}={Value}";
    }
}
