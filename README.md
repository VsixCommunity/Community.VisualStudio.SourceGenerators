# Community source generators for Visual Studio extensions

Part of the [VSIX Community](https://github.com/VsixCommunity)

## Summary

This package contains [C# source generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) that generate code from `.vsixmanfiest` files and `.vsct` files.

These source generators are a replacement for the single-file generators from the [VsixSynchronizer](https://github.com/madskristensen/VsixSynchronizer) extension.

## VSIX Manifest Files

The source generator will create a class called `Vsix` with the following constants:

|Constant     |Source                      |
|-------------|----------------------------|
|`Author`     | `<Identity Publisher=""/>`|
|`Description`| `<Description/>`          |
|`Id`         | `<Identity Id=""/>`       |
|`Language`   | `<Identity Language=""/>` |
|`Name`       | `<DisplayName/>`          |
|`Version`    | `<Identity Version=""/>`  |

#### Use a custom namespace

The `Vsix` class will be generated in the root namespace of the project. If you would like to generate the code into a different namespace, you can specify the namespace by defining the `Namespace` metadata for the `AdditionalFiles` item like this:

```xml
<ItemGroup>
    <AdditionalFiles Update="source.extension.vsixmanifest">
        <Namespace>MyCustomNamespace</Namespace>
    </AdditionalFiles>
</ItemGroup>
```

## Command Table Files

The source generator will create a class called `PackageGuids`. This class will contain a `string` constant and `Guid` field for each `<GUIDSymbol>` in any `.vsct` files. 

A class called `PackageIds` will also be created that contains a constant for each `IDSymbol` in any `.vsct` files.

For example, this `.vsct` file:

```xml
<CommandTable xmlns='http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
    <Symbols>
        <GuidSymbol name='MyPackage' value='{e5d94a98-30f6-47da-88bb-1bdf3b4157ff}'>
            <IDSymbol name='MyFirstCommand' value='0x0001' />
            <IDSymbol name='MySecondCommand' value='0x0002' />
        </GuidSymbol>
    </Symbols>
</CommandTable>
```

Will result in these classes:

```cs
internal sealed class PackageGuids
{
    public const string MyPackageString = "e5d94a98-30f6-47da-88bb-1bdf3b4157ff";
    public static readonly Guid MyPackage = new Guid(MyPackageString);
}

internal sealed class PackageIds
{
    public const int MyFirstCommand = 1;
    public const int MySecondCommand = 2;
}
```

#### Use a custom namespace

The `PackageGuids` and `PackageIds` classes will be generated in the root namespace of the project. If you would like to generate the code into a different namespace, you can specify the namespace by defining the `Namespace` metadata for the `AdditionalFiles` item like this:

```xml
<ItemGroup>
    <AdditionalFiles Update="MyCommandTable.vsct">
        <Namespace>MyCustomNamespace</Namespace>
    </AdditionalFiles>
</ItemGroup>
```
