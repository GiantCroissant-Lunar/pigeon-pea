# Source Code Generators

This directory contains source code generators integrated from the eco-shared repository.

## DisposePattern Generator

The DisposePattern source generator automatically implements the `IDisposable` pattern for classes, eliminating boilerplate code and ensuring correct disposal implementations.

### Installation

The generator is already installed in the following projects:
- `PigeonPea.Console`
- `PigeonPea.Windows`
- `PigeonPea.Shared`

### Usage

1. **Mark your class with the `[DisposePattern]` attribute**:
   ```csharp
   using Plate.SCG.General.DisposePattern.Attributes;

   [DisposePattern]
   public partial class MyClass
   {
       // Your code here
   }
   ```

2. **Mark fields that need disposal with `[ToBeDisposed]`**:
   ```csharp
   [DisposePattern]
   public partial class MyClass
   {
       [ToBeDisposed]
       private Timer? _timer;

       [ToBeDisposed]
       private SKBitmap? _bitmap;

       // This field will NOT be disposed (no attribute)
       private int _someValue;
   }
   ```

3. **The generator creates**:
   - Public `Dispose()` method implementing `IDisposable`
   - Protected virtual `Dispose(bool disposing)` for inheritance support
   - Automatic null-checking before disposal
   - Automatic nulling of non-readonly reference fields after disposal
   - Four partial method hooks for custom cleanup logic:
     - `BeforeDisposeManagedResources()`
     - `DisposeManagedResources()`
     - `BeforeDisposeUnmanagedResources()`
     - `DisposeUnmanagedResources()`

### Example

**Before (manual disposal)**:
```csharp
public class GameCanvas : Image
{
    private SKBitmap? _bitmap;

    public void Cleanup()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
```

**After (with DisposePattern generator)**:
```csharp
[DisposePattern]
public partial class GameCanvas : Image
{
    [ToBeDisposed]
    private SKBitmap? _bitmap;

    // Dispose() method is auto-generated!
    // No manual cleanup code needed
}
```

### Benefits

- ✅ **Correctness**: Implements the full dispose pattern correctly
- ✅ **Consistency**: All disposal follows the same pattern
- ✅ **Less Boilerplate**: No need to write repetitive disposal code
- ✅ **Inheritance Support**: Generated code supports class hierarchies
- ✅ **Null Safety**: Automatic null-checking
- ✅ **Extensibility**: Partial methods for custom cleanup logic

### Package Source

The DisposePattern generator packages are sourced from the [eco-shared repository](https://github.com/GiantCroissant-Lunar/eco-shared) and stored locally in `../.local-packages/`:
- `Plate.SCG.General.DisposePattern.0.1.0.nupkg`
- `Plate.SCG.Shared.Attributes.0.1.0.nupkg`
- `Plate.SCG.Shared.Abstractions.0.1.0.nupkg`

### NuGet Configuration

The local package source is configured in `dotnet/NuGet.Config`:
```xml
<packageSources>
  <add key="local-eco-shared" value="../.local-packages" />
</packageSources>
```

### Current Usage in Codebase

The DisposePattern is currently applied to:
- **GameCanvas** (`PigeonPea.Windows`): Disposes `SKBitmap` resources properly

### Future Considerations

- Check if `Arch.Core.World` (used in `GameWorld`) implements `IDisposable`
- Apply pattern to other classes with disposable resources as they're identified
- Consider applying to renderer classes if they manage unmanaged resources

## Adding More Generators

To add additional source generators from eco-shared:
1. Download the `.nupkg` file from eco-shared's `build/packages/` directory
2. Copy it to `.local-packages/`
3. Add a `PackageReference` to your `.csproj`:
   ```xml
   <PackageReference Include="Package.Name" Version="0.1.0"
                     PrivateAssets="all"
                     OutputItemType="Analyzer"
                     ReferenceOutputAssembly="false" />
   ```
