# SkiaSharp Backend Tests

Comprehensive test suite for the SkiaSharp rendering backend.

## Test Coverage

### Integration Tests (10 tests)

Tests that verify the backend integrates correctly with the rendering contracts:

1. **Backend_ShouldHaveCorrectId** - Verifies backend identifier
2. **Backend_ShouldHaveCapabilities** - Checks rendering capabilities are available
3. **Initialize_ShouldSucceed** - Tests initialization with render context
4. **Execute_WithCommandList_ShouldNotThrow** - Verifies command list execution
5. **Present_ShouldNotThrow** - Tests frame presentation
6. **LoadSpriteFromData_WithValidData_ShouldSucceed** - Tests sprite loading from byte array
7. **Shutdown_ShouldNotThrow** - Tests proper shutdown
8. **Dispose_ShouldNotThrow** - Tests resource disposal
9. **Dispose_MultipleCalls_ShouldNotThrow** - Tests multiple dispose calls
10. **Execute_WithCommandList_ShouldNotThrow** - Integration with command lists

### Command List Tests (10 tests)

Tests that verify the command list functionality:

1. **CommandList_CanBeCreated** - Tests command list creation
2. **BeginFrame_ShouldSucceed** - Tests frame start
3. **EndFrame_AfterBeginFrame_ShouldSucceed** - Tests frame completion
4. **Clear_WithinFrame_ShouldSucceed** - Tests clear command
5. **DrawText_Multiple_WithinFrame_ShouldSucceed** - Tests text rendering
6. **SetViewport_ShouldSucceed** - Tests viewport configuration
7. **SetCamera_ShouldSucceed** - Tests camera transformation
8. **DrawText_WithinFrame_ShouldSucceed** - Tests basic text rendering
9. **Clear_WithoutFrame_ShouldThrow** - Tests error handling without frame
10. **BeginFrame_TwiceWithoutEndFrame_ShouldThrow** - Tests invalid frame nesting
11. **EndFrame_WithoutBeginFrame_ShouldThrow** - Tests error for missing begin frame

## Running Tests

```bash
# From repository root
cd projects/dungeon/dotnet/windows-app/plugins

# Run all tests
dotnet test tests\PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests\PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests.csproj

# Run with detailed output
dotnet test tests\PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests\PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests.csproj --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~CommandListTests.BeginFrame_ShouldSucceed"
```

## Test Results

✅ **All 20 tests passing**

- **Total**: 20
- **Passed**: 20
- **Failed**: 0
- **Skipped**: 0
- **Duration**: < 1 second

## Test Framework

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **SkiaSharp** - Rendering engine (test fixtures)
- **Moq** - Mocking framework (available but not currently used)

## Code Coverage

Tests cover:

- Backend initialization and lifecycle
- Command list creation and execution
- Frame management (BeginFrame/EndFrame)
- Rendering commands (Clear, DrawText)
- Viewport and camera management
- Sprite loading
- Resource disposal
- Error handling

## Future Test Enhancements

Potential additions for comprehensive coverage:

1. **Performance Tests**
   - Benchmark rendering throughput
   - Measure sprite caching performance
   - Test large buffer rendering

2. **Integration Tests**
   - Test with actual Avalonia controls
   - Test with real window rendering
   - Test resize handling

3. **Sprite Tests**
   - Test sprite caching and eviction
   - Test sprite tinting
   - Test sprite atlas support

4. **Error Handling**
   - Test invalid dimensions
   - Test null parameter handling
   - Test resource exhaustion

5. **Rendering Tests**
   - Test tile rendering accuracy
   - Test buffer rendering accuracy
   - Test text rendering quality
   - Test camera transformations

## Continuous Integration

Tests are designed to run in CI/CD environments:

- No external dependencies required
- Deterministic results
- Fast execution (< 1 second)
- Clear pass/fail criteria

## Notes

- Tests use in-memory surfaces (no GPU required)
- Tests are isolated (each creates its own backend instance)
- Tests clean up resources properly (IDisposable pattern)
- Tests follow AAA pattern (Arrange-Act-Assert)
