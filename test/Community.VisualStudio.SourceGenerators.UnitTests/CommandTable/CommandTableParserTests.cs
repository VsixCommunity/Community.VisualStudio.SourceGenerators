using System.Diagnostics.CodeAnalysis;

namespace Community.VisualStudio.SourceGenerators;

public class CommandTableParserTests
{
    [Fact]
    public void HandlesNoSymbols()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
            </CommandTable>".TrimStart();

        CommandTable commandTable = CommandTableParser.Parse("test", contents);

        Assert.NotNull(commandTable);
        Assert.Equal("test", commandTable.Name);
        Assert.Empty(commandTable.GUIDSymbols);
    }

    [Fact]
    public void HandlesSingleSymbolsElement()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{499b3e29-b8a1-4016-a972-84ad08da139a}'>
                        <IDSymbol name='One' value='0x1' />
                        <IDSymbol name='Two' value='0x2' />
                    </GuidSymbol>
                </Symbols>
            </CommandTable>".TrimStart();

        CommandTable commandTable = CommandTableParser.Parse("test", contents);

        Assert.NotNull(commandTable);
        Assert.Equal("test", commandTable.Name);

        VerifySymbols(
            commandTable.GUIDSymbols,
            new GUIDSymbol(
                "Foo",
                Guid.Parse("{499b3e29-b8a1-4016-a972-84ad08da139a}"),
                new[] { new IDSymbol("One", 1), new IDSymbol("Two", 2) }
            )
        );
    }

    [Fact]
    public void HandlesMultipleSymbolsElements()
    {
        string contents = @"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{499b3e29-b8a1-4016-a972-84ad08da139a}'>
                        <IDSymbol name='One' value='0x1' />
                        <IDSymbol name='Two' value='0x2' />
                    </GuidSymbol>

                    <GuidSymbol name='Bar' value='{66dc4b3b-ce9b-4377-a655-3e80f9b8e828}'>
                        <IDSymbol name='Three' value='0x3' />
                        <IDSymbol name='Four' value='0x4' />
                    </GuidSymbol>
                </Symbols>
            </CommandTable>".TrimStart();

        CommandTable commandTable = CommandTableParser.Parse("test", contents);

        Assert.NotNull(commandTable);
        Assert.Equal("test", commandTable.Name);

        VerifySymbols(
            commandTable.GUIDSymbols,
            new GUIDSymbol(
                "Foo",
                Guid.Parse("{499b3e29-b8a1-4016-a972-84ad08da139a}"),
                new[] { new IDSymbol("One", 1), new IDSymbol("Two", 2) }
            ),
            new GUIDSymbol(
                "Bar",
                Guid.Parse("{66dc4b3b-ce9b-4377-a655-3e80f9b8e828}"),
                new[] { new IDSymbol("Three", 3), new IDSymbol("Four", 4) }
            )
        );
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0b7920ecf3cd415eb2a9c7909a807b59")]
    [InlineData("27ccce41-3796-428b-a805-a292d4192e5b")]
    [InlineData("(87a8f1c3-fbad-45f3-b084-e1bf53587ec3)")]
    public void ThrowsExceptionWhenGuidCannotBeParsed(string guidValue)
    {
        string contents = $@"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{guidValue}'>
                        <IDSymbol name='One' value='0x1' />
                    </GuidSymbol>
                </Symbols>
            </CommandTable>".TrimStart();

        Assert.Throws<InvalidCommandTableException>(() => CommandTableParser.Parse("test", contents));
    }

    [Theory]
    [InlineData("{873f2e80-0fee-4c56-a10b-8406982ca66f}")]
    [InlineData("{0x873f2e80,0x0fee,0x4c56,{0xa1,0x0b,0x84,0x06,0x98,0x2c,0xa6,0x6f}}")]
    public void CanParseValidGuidFormats(string guidValue)
    {
        string contents = $@"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{guidValue}'/>
                </Symbols>
            </CommandTable>".TrimStart();

        CommandTable commandTable = CommandTableParser.Parse("test", contents);
        VerifySymbols(
            commandTable.GUIDSymbols,
            new GUIDSymbol("Foo", new Guid("{873f2e80-0fee-4c56-a10b-8406982ca66f}"), Enumerable.Empty<IDSymbol>())
        );
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0x12gh")]
    public void ThrowsExceptionWhenIdCannotBeParsed(string idValue)
    {
        string contents = $@"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{{77ab4e86-d0fc-4185-929c-f09b25f762b5}}'>
                        <IDSymbol name='One' value='{idValue}' />
                    </GuidSymbol>
                </Symbols>
            </CommandTable>".TrimStart();

        Assert.Throws<InvalidCommandTableException>(() => CommandTableParser.Parse("test", contents));
    }

    [Theory]
    [InlineData("0x12345abc", 305420988)]
    [InlineData("0x12345ABC", 305420988)]
    [InlineData("0x67890def", 1737035247)]
    [InlineData("0x67890DEF", 1737035247)]
    [InlineData("0x1", 1)]
    [InlineData("0X1", 1)]
    [InlineData("0X000001", 1)]
    [InlineData("123", 123)]
    public void CanParseValidIdFormats(string idValue, int id)
    {
        string contents = $@"
            <?xml version='1.0' encoding='utf-8'?>
            <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <Symbols>
                    <GuidSymbol name='Foo' value='{{77ab4e86-d0fc-4185-929c-f09b25f762b5}}'>
                        <IDSymbol name='One' value='{idValue}' />
                    </GuidSymbol>
                </Symbols>
            </CommandTable>".TrimStart();

        CommandTable commandTable = CommandTableParser.Parse("test", contents);
        VerifySymbols(
            commandTable.GUIDSymbols,
            new GUIDSymbol("Foo", Guid.Parse("{77ab4e86-d0fc-4185-929c-f09b25f762b5}"), new[] { new IDSymbol("One", id) })
        );
    }

    private static void VerifySymbols(IEnumerable<GUIDSymbol> actual, params GUIDSymbol[] expected)
    {
        // Convert the expected and actual collections to arrays to make any assertion
        // failure messages easier to read, because the messages include the type name,
        // and that can be quite long if we leave them as sone form of IEnumerable.
        Assert.Equal(
            expected.OrderBy((x) => x.Name).ToArray(),
            actual.OrderBy((x) => x.Name).ToArray(),
            GUIDSymbolComparer.Instance
        );
    }

    private class GUIDSymbolComparer : IEqualityComparer<GUIDSymbol>
    {
        public static GUIDSymbolComparer Instance { get; } = new();

        public bool Equals(GUIDSymbol? x, GUIDSymbol? y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (y is null)
            {
                return false;
            }

            return string.Equals(x.Name, y.Name, StringComparison.Ordinal) &&
                x.Value == y.Value &&
                x.IDSymbols.ToHashSet(IDSymbolComparer.Instance).SetEquals(y.IDSymbols);
        }

        public int GetHashCode([DisallowNull] GUIDSymbol obj)
        {
            HashCode hashCode = new();
            hashCode.Add(obj.Name);
            hashCode.Add(obj.Value);
            foreach (IDSymbol id in obj.IDSymbols)
            {
                hashCode.Add(id, IDSymbolComparer.Instance);
            }
            return hashCode.ToHashCode();
        }
    }

    private class IDSymbolComparer : IEqualityComparer<IDSymbol>
    {
        public static IDSymbolComparer Instance { get; } = new();

        public bool Equals(IDSymbol? x, IDSymbol? y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (y is null)
            {
                return false;
            }

            return string.Equals(x.Name, y.Name, StringComparison.Ordinal) && x.Value == y.Value;
        }

        public int GetHashCode([DisallowNull] IDSymbol obj)
        {
            return HashCode.Combine(obj.Name, obj.Value);
        }
    }
}
