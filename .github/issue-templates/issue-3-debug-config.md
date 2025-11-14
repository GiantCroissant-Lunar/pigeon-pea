# Issue 3: Add #if DEBUG Build Configuration for DevTools

Add conditional compilation to exclude DevTools from Release builds, ensuring it's physically impossible to enable in production.

## References

- RFC-015: DevTools Security and Deployment (Build Configuration section)

## Acceptance Criteria

- [ ] Wrap DevTools integration in `#if DEBUG` preprocessor directives
- [ ] Conditional project reference in `.csproj` files
- [ ] Release builds verified (no DevTools symbols)
- [ ] Debug builds still work normally
- [ ] Documentation updated with build configuration notes

## Files to Modify

- `console-app/core/PigeonPea.Console/Program.cs`
- `console-app/core/PigeonPea.Console/PigeonPea.Console.csproj`
- `windows-app/core/PigeonPea.Windows/MainWindow.axaml.cs`
- `windows-app/core/PigeonPea.Windows/PigeonPea.Windows.csproj`

## Verification

```bash
dotnet build -c Release
strings bin/Release/net9.0/PigeonPea.Console.dll | grep -i devtools
# Should return nothing
```

## Labels

- `enhancement`
- `dev-tools`
- `security`

## Milestone

DevTools v1.0 (before first release)
