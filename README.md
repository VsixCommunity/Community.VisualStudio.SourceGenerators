# Community source generators for Visual Studio extensions

Part of the [VSIX Community](https://github.com/VsixCommunity)

## Summary

This package contains [C# source generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) that generate code from `.vsixmanfiest` files.

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
