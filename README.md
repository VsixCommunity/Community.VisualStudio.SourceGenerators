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

The `Vsix` class will be generated in the root namespace of the project. If you would like to generate the code into a different namespace, you can specify the namespace by defining the `Namespace` metadata for the `source.extension.vsixmanifest` file like this:

```xml
<ItemGroup>
    <None Include="source.extension.vsixmanifest">
        <Namespace>MyCustomNamespace</Namespace>
    </None>
</ItemGroup>
```

## Command Table Files

The source generator will create a container class that is named after the `.vsct` file. Within that container class, a class will be created for each `<GUIDSymbol>`.

The class for a `<GUIDSymbol>` contains a `Guid` and `GuidString` field that defines the GUID value, and each `<IDSymbol>` is defined as a constant.

For example, a `VSCommandTable.vsct` file that looks like this:

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

Will result in this:

```csharp
internal sealed partial class VSCommandTable
{
    internal sealed partial class MyPackage
    {
        public const string GuidString = "e5d94a98-30f6-47da-88bb-1bdf3b4157ff";
        public static readonly Guid Guid = new Guid(GuidString);
    
        public const int MyFirstCommand = 1;
        public const int MySecondCommand = 2;
    }
}
```

You can then access the `Guid` and IDs like this:

```csharp
[GuidAttribute(VSCommandTable.MyPackage.GuidString)]
```

#### Use a custom namespace

The classes will be generated in the root namespace of the project. If you would like to generate the code into a different namespace, you can specify the namespace by defining the `Namespace` metadata for the `VSCTCompile` item like this:

```xml
<ItemGroup>
    <VSCTCompile Include="MyCommandTable.vsct">
        <ResourceName>Menus.ctmenu</ResourceName>
        <Namespace>MyCustomNamespace</Namespace>
    </VSCTCompile>
</ItemGroup>
```

#### Migrating from the Vsix Synchronizer extension

If you are migrating from the [Vsix Synchronizer](https://github.com/madskristensen/VsixSynchronizer) extension and would like to continue to use the `PackageGuids` and `PackageIds` classes that it generates, you can change the output format by defining the `Format` metadata for the `VSCTCompile` item like this:

```xml
<ItemGroup>
    <VSCTCompile Include="MyCommandTable.vsct">
        <ResourceName>Menus.ctmenu</ResourceName>
        <Format>VsixSynchronizer</Format>
    </VSCTCompile>
</ItemGroup>
```

This will result in classes like this:

```csharp
internal sealed partial class PackageGuids
{
    public const string MyPackageString = "e5d94a98-30f6-47da-88bb-1bdf3b4157ff";
    public static readonly Guid MyPackage = new Guid(MyPackageString);
}

internal sealed partial class PackageIds
{
    public const int MyFirstCommand = 1;
    public const int MySecondCommand = 2;
}
```
