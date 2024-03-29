﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Community.VisualStudio.SourceGenerators;

public static class CommandTableGeneratorTests
{
    public class DefaultFormat : TestBase
    {
        [Fact]
        public async Task ShouldGenerateClassForGuidSymbolAsync()
        {
            SetProjectProperty("RootNamespace", "Foo");

            await WriteCommandTableAsync(
                "Commands.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='MyPackage' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='MyCommand' value='0x0001' />
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName("Foo.Commands")).ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Foo.Commands+MyPackage"),
                new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}"),
                ("MyCommand", 1)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreatesClassesInTheCorrectNamespacesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Alpha.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='One' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "Beta.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Two' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'/>
                    </Symbols>
                </CommandTable>",
                itemNamespace: "Second"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "Gamma.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'/>
                    </Symbols>
                </CommandTable>",
                itemNamespace: "Third"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName("Root.Alpha")).ConfigureAwait(false);
            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.Alpha+One"),
                new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}")
            ).ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName("Second.Beta")).ConfigureAwait(false);
            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Second.Beta+Two"),
                new Guid("{9e1526aa-35df-4e91-af17-bbcc6122ccfe}")
            ).ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName("Third.Gamma")).ConfigureAwait(false);
            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Third.Gamma+Three"),
                new Guid("{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}")
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CanDefineMultipleGuidClassesInSameNamespaceAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "VSCommandTable.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='One' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='First' value='10'/>
                            <IDSymbol name='Second' value='20'/>
                        </GuidSymbol>

                        <GuidSymbol name='Two' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'>
                            <IDSymbol name='Third' value='30'/>
                            <IDSymbol name='Fourth' value='40'/>
                        </GuidSymbol>

                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'>
                            <!-- Test IDSymbols with the same name as symbols in a different GUIDSymbol. -->
                            <IDSymbol name='First' value='50'/>
                            <IDSymbol name='Second' value='60'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName("Root.VSCommandTable")).ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.VSCommandTable+One"),
                new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}"),
                ("First", 10),
                ("Second", 20)
            ).ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.VSCommandTable+Two"),
                new Guid("{9e1526aa-35df-4e91-af17-bbcc6122ccfe}"),
                ("Third", 30),
                ("Fourth", 40)
            ).ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.VSCommandTable+Three"),
                new Guid("{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}"),
                ("First", 50),
                ("Second", 60)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task RemovesGuidPrefixFromGuidNamesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='guidFoo' value='{005c838c-22d2-4ee4-8bc6-e18e3aa5fa47}'/>
                        <GuidSymbol name='guidBar' value='{c717cf33-bc1f-47d4-a173-0efaacaad63c}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.Symbols+Foo"),
                new Guid("{005c838c-22d2-4ee4-8bc6-e18e3aa5fa47}")
            ).ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.Symbols+Bar"),
                new Guid("{c717cf33-bc1f-47d4-a173-0efaacaad63c}")
            ).ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierTestData))]
        public async Task EnsuresContainerTypeNameIsValidAsync(string originalName, string escapedName)
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                $"{originalName}.vsct",
                $@"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Foo' value='{{26891d9b-0896-402f-a59b-693a3ea72962}}'/>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyContainerTypeAsync(compilation.GetTypeByMetadataName($"Root.{escapedName}")).ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierTestData))]
        public async Task EnsuresGuidNameIsValidAsync(string originalName, string escapedName)
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                $@"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='{originalName}' value='{{26891d9b-0896-402f-a59b-693a3ea72962}}'/>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName($"Root.Symbols+{escapedName}"),
                new Guid("{26891d9b-0896-402f-a59b-693a3ea72962}")
            ).ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierTestData), MemberType = typeof(TestBase))]
        public async Task EnsuresIdNameIsValidAsync(string originalName, string escapedName)
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                $@"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Foo' value='{{26891d9b-0896-402f-a59b-693a3ea72962}}'>
                            <IDSymbol name='{originalName}' value='42'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyGuidSymbolTypeAsync(
                compilation.GetTypeByMetadataName("Root.Symbols+Foo"),
                new Guid("{26891d9b-0896-402f-a59b-693a3ea72962}"),
                (escapedName, 42)
            ).ConfigureAwait(false);
        }

        protected override async Task WriteCommandTableAsync(string fileName, string contents, string? itemNamespace = null)
        {
            await WriteFileAsync(fileName, contents).ConfigureAwait(false);

            AddProjectFileFragment($@"
                <ItemGroup>
                    <VSCTCompile Include='{fileName}'>
                        <ResourceName>Menus.ctmenu</ResourceName>
                        {(itemNamespace is not null ? $"<Namespace>{itemNamespace}</Namespace>" : "")}
                    </VSCTCompile>
                </ItemGroup>"
            );
        }

        private static async Task VerifyContainerTypeAsync(INamedTypeSymbol? type)
        {
            AssertNotNull(type);
            Assert.Equal(Accessibility.Internal, type.DeclaredAccessibility);
            await AssertPartialClassAsync(type).ConfigureAwait(false);
        }

        private static async Task VerifyGuidSymbolTypeAsync(INamedTypeSymbol? type, Guid guidValue, params (string Name, int Value)[] ids)
        {
            AssertNotNull(type);

            Assert.Equal(Accessibility.Internal, type.DeclaredAccessibility);
            await AssertPartialClassAsync(type).ConfigureAwait(false);

            await VerifyGuidMemberAsync(type, "Guid", guidValue).ConfigureAwait(false);

            foreach ((string name, int value) in ids)
            {
                VerifyIdMember(type, name, value);
            }
        }
    }

    public class VsixSynchronizerFormat : TestBase
    {
        [Fact]
        public async Task ShouldGeneratePackageGuidsClassForGuidSymbolsAsync()
        {
            SetProjectProperty("RootNamespace", "Foo");

            await WriteCommandTableAsync(
                "Commands.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='MyPackage' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='MyCommand' value='0x0001' />
                        </GuidSymbol>

                        <GuidSymbol name='SomeOtherGuid' value='{bade4e81-c798-4bff-be43-ac22a3f64d70}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Foo.PackageGuids"),
                ("MyPackage", new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}")),
                ("SomeOtherGuid", new Guid("{bade4e81-c798-4bff-be43-ac22a3f64d70}"))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreatesPackageGuidsClassesInTheCorrectNamespacesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "1.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='One' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "2.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Two' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'/>
                    </Symbols>
                </CommandTable>",
                itemNamespace: "Second"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "3.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'/>
                    </Symbols>
                </CommandTable>",
                itemNamespace: "Third"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageGuids"),
                ("One", new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}"))
            ).ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Second.PackageGuids"),
                ("Two", new Guid("{9e1526aa-35df-4e91-af17-bbcc6122ccfe}"))
            ).ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Third.PackageGuids"),
                ("Three", new Guid("{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}"))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CanDefineGuidsFromMultipleVsctFilesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "1.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='One' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "2.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Two' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "3.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageGuids"),
                ("One", new Guid("{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}")),
                ("Two", new Guid("{9e1526aa-35df-4e91-af17-bbcc6122ccfe}")),
                ("Three", new Guid("{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}"))
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task RemovesGuidPrefixFromGuidNamesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='guidFoo' value='{005c838c-22d2-4ee4-8bc6-e18e3aa5fa47}'/>
                        <GuidSymbol name='guidBar' value='{c717cf33-bc1f-47d4-a173-0efaacaad63c}'/>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageGuids"),
                ("Foo", new Guid("{005c838c-22d2-4ee4-8bc6-e18e3aa5fa47}")),
                ("Bar", new Guid("{c717cf33-bc1f-47d4-a173-0efaacaad63c}"))
            ).ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierTestData))]
        public async Task EnsuresGuidNameIsValidAsync(string originalName, string escapedName)
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                $@"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='{originalName}' value='{{26891d9b-0896-402f-a59b-693a3ea72962}}'/>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageGuidsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageGuids"),
                (escapedName, new Guid("{26891d9b-0896-402f-a59b-693a3ea72962}"))
            ).ConfigureAwait(false);
        }

        [Theory]
        [MemberData(nameof(InvalidIdentifierTestData))]
        public async Task EnsuresIdNameIsValidAsync(string originalName, string escapedName)
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "Symbols.vsct",
                $@"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Foo' value='{{26891d9b-0896-402f-a59b-693a3ea72962}}'>
                            <IDSymbol name='{originalName}' value='42'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageIds"),
                (escapedName, 42)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldGeneratePackageIdsClassForIDSymbolsAsync()
        {
            SetProjectProperty("RootNamespace", "Foo");

            await WriteCommandTableAsync(
                "Commands.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='MyPackage' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='One' value='0x0001' />
                            <IDSymbol name='Two' value='0x0002' />
                        </GuidSymbol>
    
                        <GuidSymbol name='SomeOtherGuid' value='{bade4e81-c798-4bff-be43-ac22a3f64d70}'>
                            <IDSymbol name='Three' value='0x0003' />
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Foo.PackageIds"),
                ("One", 1),
                ("Two", 2),
                ("Three", 3)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreatesPackageIdsClassesInTheCorrectNamespacesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "1.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='First' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='One' value='0x01'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "2.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Second' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'>
                            <IDSymbol name='Two' value='0x02'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>",
                itemNamespace: "Second"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "3.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'>
                            <IDSymbol name='Three' value='0x03'/>
                        </GuidSymbol>
                    </Symbols>
             </CommandTable>",
                itemNamespace: "Third"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageIds"),
                ("One", 1)
            ).ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Second.PackageIds"),
                ("Two", 2)
            ).ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Third.PackageIds"),
                ("Three", 3)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task CanDefineIdsFromMultipleVsctFilesAsync()
        {
            SetProjectProperty("RootNamespace", "Root");

            await WriteCommandTableAsync(
                "1.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='One' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
                            <IDSymbol name='First' value='0x01'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "2.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Two' value='{9e1526aa-35df-4e91-af17-bbcc6122ccfe}'>
                            <IDSymbol name='Second' value='0x02'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            await WriteCommandTableAsync(
                "3.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Three' value='{21b17ebd-1dab-462c-a44f-3b2bab6c4ff1}'>
                            <IDSymbol name='Third' value='0x03'/>
                        </GuidSymbol>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            await VerifyPackageIdsTypeAsync(
                compilation.GetTypeByMetadataName("Root.PackageIds"),
                ("First", 1),
                ("Second", 2),
                ("Third", 3)
            ).ConfigureAwait(false);
        }

        protected override async Task WriteCommandTableAsync(string fileName, string contents, string? itemNamespace = null)
        {
            await WriteFileAsync(fileName, contents).ConfigureAwait(false);

            AddProjectFileFragment($@"
                <ItemGroup>
                    <VSCTCompile Include='{fileName}'>
                        <ResourceName>Menus.ctmenu</ResourceName>
                        <Format>VsixSynchronizer</Format>
                        {(itemNamespace is not null ? $"<Namespace>{itemNamespace}</Namespace>" : "")}
                    </VSCTCompile>
                </ItemGroup>"
            );
        }

        private static async Task VerifyPackageGuidsTypeAsync(INamedTypeSymbol? packageGuidsType, params (string Name, Guid Value)[] expected)
        {
            AssertNotNull(packageGuidsType);

            Assert.Equal(Accessibility.Internal, packageGuidsType.DeclaredAccessibility);
            await AssertPartialClassAsync(packageGuidsType).ConfigureAwait(false);

            Assert.Equal(
                expected.SelectMany((x) => new[] { x.Name, $"{x.Name}String" }).OrderBy((x) => x).ToArray(),
                packageGuidsType.GetMembers().OfType<IFieldSymbol>().Select((x) => x.Name).OrderBy((x) => x).ToArray()
            );

            foreach ((string name, Guid value) in expected)
            {
                await VerifyGuidMemberAsync(packageGuidsType, name, value).ConfigureAwait(false);
            }
        }

        private static async Task VerifyPackageIdsTypeAsync(INamedTypeSymbol? packageIdsType, params (string Name, int Value)[] expected)
        {
            AssertNotNull(packageIdsType);

            Assert.Equal(Accessibility.Internal, packageIdsType.DeclaredAccessibility);
            await AssertPartialClassAsync(packageIdsType).ConfigureAwait(false);

            Assert.Equal(
                expected.Select((x) => x.Name).OrderBy((x) => x).ToArray(),
                packageIdsType.GetMembers().OfType<IFieldSymbol>().Select((x) => x.Name).OrderBy((x) => x).ToArray()
            );

            foreach ((string name, int value) in expected)
            {
                VerifyIdMember(packageIdsType, name, value);
            }
        }
    }

    public abstract class TestBase : GeneratorTestBase
    {
        protected override IIncrementalGenerator CreateGenerator() => new CommandTableGenerator();

        [Fact]
        public async Task ShouldNotReportDiagnosticWhenNoCommandTableFilesAreFoundAsync()
        {
            await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldNotGenerateAnyCodeWhenNoCommandTableFilesAreFoundAsync()
        {
            SetProjectProperty("RootNamespace", "Foo");

            Compilation compilation = await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);

            Assert.Null(compilation.GetTypeByMetadataName("Foo.PackageGuids"));
            Assert.Null(compilation.GetTypeByMetadataName("Foo.PackageIds"));
        }

        [Fact]
        public async Task ShouldReportDiagnosticWhenNamespaceCannotBeDeterminedAsync()
        {
            SetProjectProperty("RootNamespace", "");

            await WriteCommandTableAsync(
                "Commands.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='MyPackage' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'/>
                    </Symbols>
                </CommandTable>"
            ).ConfigureAwait(false);

            ImmutableArray<Diagnostic> diagnostics;
            (_, diagnostics) = await RunGeneratorAsync().ConfigureAwait(false);

            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal("CVSSG001", diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        }

        [Theory]
        [InlineData("Foo+Bar")]
        [InlineData("Foo`Bar")]
        [InlineData("Foo~Bar")]
        [InlineData("Foo/Bar")]
        [InlineData("Foo\\Bar")]
        public async Task EnsuresHintNameIsValidAsync(string hintName)
        {
            SetProjectProperty("RootNamespace", "Root");

            // The name of the `.vsct` file is used in the hint name, so test
            // the validation by using the given hint name in the file name.
            await WriteCommandTableAsync(
                $"{hintName}.vsct",
                @"
                <CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <Symbols>
                        <GuidSymbol name='Foo' value='{26891d9b-0896-402f-a59b-693a3ea72962}'/>
                    </Symbols>
                </CommandTable>"
                ).ConfigureAwait(false);

            // We don't see the hint name in the resulting code, but as long as the
            // generator ran without error, then the hint name that was used was valid.
            await RunGeneratorAndVerifyNoDiagnosticsAsync().ConfigureAwait(false);
        }

        protected abstract Task WriteCommandTableAsync(string fileName, string contents, string? itemNamespace = null);

        public static IEnumerable<object[]> InvalidIdentifierTestData()
        {
            yield return new[] { "Foo.Bar", "Foo_Bar" };
            yield return new[] { "Foo~Bar", "Foo_Bar" };
            yield return new[] { ".Foo", "_Foo" };
            yield return new[] { ".Foo,Bar", "_Foo_Bar" };
            yield return new[] { "123", "_123" };
            yield return new[] { "+", "_" };
        }

        protected static async Task AssertPartialClassAsync(INamedTypeSymbol type)
        {
            List<ClassDeclarationSyntax> classDeclarations = new();
            foreach (SyntaxReference reference in type.DeclaringSyntaxReferences)
            {
                classDeclarations.Add(
                    Assert.IsAssignableFrom<ClassDeclarationSyntax>(await reference.GetSyntaxAsync().ConfigureAwait(false))
                );
            }

            Assert.All(
                classDeclarations,
                (declaration) => Assert.Contains(
                    declaration.Modifiers,
                    (modifier) => modifier.IsKind(SyntaxKind.PartialKeyword)
                )
            );
        }

        protected static async Task VerifyGuidMemberAsync(INamedTypeSymbol containingType, string name, Guid expectedValue)
        {
            // There should be a constant with a "String" suffix on the name.
            ISymbol stringMember = Assert.Single(containingType.GetMembers($"{name}String"));
            IFieldSymbol stringField = Assert.IsAssignableFrom<IFieldSymbol>(stringMember);
            Assert.True(stringField.IsConst);
            Assert.Equal(SpecialType.System_String, stringField.Type.SpecialType);
            Assert.Equal(expectedValue.ToString("D"), stringField.ConstantValue);

            // There should be a static read-only field with the given name.
            ISymbol guidMember = Assert.Single(containingType.GetMembers(name));
            IFieldSymbol guidField = Assert.IsAssignableFrom<IFieldSymbol>(guidMember);
            Assert.Equal("System.Guid", $"{guidField.Type.ContainingNamespace.Name}.{guidField.Type.Name}");

            // Because the field stores a `Guid`, the value won't be a constant. We can find the
            // value that is assigned to the field by looking at its syntax reference.
            SyntaxReference syntaxReference = Assert.Single(guidField.DeclaringSyntaxReferences);
            VariableDeclaratorSyntax variable = Assert.IsAssignableFrom<VariableDeclaratorSyntax>(
                await syntaxReference.GetSyntaxAsync().ConfigureAwait(false)
            );

            // A new Guid should be assigned to the field.
            ObjectCreationExpressionSyntax newExpression = Assert.IsType<ObjectCreationExpressionSyntax>(variable.Initializer?.Value);
            Assert.True(newExpression.Type.IsEquivalentTo(SyntaxFactory.ParseTypeName("System.Guid")));

            // The argument passed to the `Guid` constructor should be the corresponding constant.
            AssertNotNull(newExpression.ArgumentList);
            ArgumentSyntax argument = Assert.Single(newExpression.ArgumentList.Arguments);
            IdentifierNameSyntax identifier = Assert.IsType<IdentifierNameSyntax>(argument.Expression);
            Assert.Equal($"{name}String", identifier.Identifier.Text);
        }

        protected static void VerifyIdMember(INamedTypeSymbol containingType, string name, int value)
        {
            ISymbol member = Assert.Single(containingType.GetMembers(name));
            IFieldSymbol field = Assert.IsAssignableFrom<IFieldSymbol>(member);

            Assert.True(field.IsConst);
            Assert.Equal(value, field.ConstantValue);
        }
    }
}
