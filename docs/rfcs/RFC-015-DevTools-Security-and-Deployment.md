# RFC-015: DevTools Security and Deployment

Status: Implemented
Date: 2025-11-13
Author: Claude (Anthropic)

## Summary

Define the security model and deployment guidelines for PigeonPea DevTools, ensuring it remains a safe development tool that never poses risks in production environments.

## Motivation

DevTools provides powerful runtime control over the game, including:
- Spawning arbitrary entities
- Teleporting the player
- Modifying game state
- Querying internal ECS data

If exposed incorrectly, DevTools could:
- Allow remote code execution vulnerabilities
- Expose sensitive game data
- Enable cheating in multiplayer scenarios (future)
- Create attack vectors for malicious actors

A clear security model ensures DevTools is **safe for development** and **never enabled in production**.

## Goals

1. **Localhost-only binding** - Never expose DevTools to network
2. **Explicit opt-in** - Disabled by default, requires explicit flag
3. **No authentication** - Simple for development, but only safe because localhost-only
4. **Build-time separation** - Easy to exclude from production builds
5. **Clear documentation** - Developers understand security implications

## Non-Goals

1. **Authentication/authorization** - Unnecessary for localhost-only tool
2. **Encryption** - Not needed for localhost communication
3. **Rate limiting** - Development tool, not a production API
4. **Audit logging** - Overkill for dev tools (use game logs if needed)

## Security Model

### Threat Model

**Assumptions:**
1. DevTools is only used during development
2. Developer's machine is trusted
3. No untrusted code runs on developer's machine during game dev
4. DevTools is never enabled on player machines

**Threats We Protect Against:**
- ❌ Remote attackers (network exposure)
- ❌ Accidental production deployment with DevTools enabled

**Threats We Do NOT Protect Against:**
- ✅ Local malicious processes (if your machine is compromised, game dev tools are the least of your worries)
- ✅ Developer abuse (developers can modify game code anyway)
- ✅ Physical access attacks (if someone has physical access, game is not the attack vector)

### Defense Layers

```text
┌────────────────────────────────────────────────────────────┐
│ Layer 1: Localhost-Only Binding                           │
│ - Server binds to 127.0.0.1 (not 0.0.0.0)                 │
│ - Prevents remote connections over network                │
│ - OS-level firewall protects loopback interface           │
└────────────────────────────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ Layer 2: Explicit Opt-In                                  │
│ - Disabled by default (no flag = no server)               │
│ - Requires --enable-dev-tools flag (Console)               │
│ - Requires PIGEONPEA_DEV_TOOLS=1 env var (Windows)        │
│ - User must intentionally enable                          │
└────────────────────────────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ Layer 3: Build Configuration (Future)                     │
│ - #if DEBUG preprocessor directives                       │
│ - Conditional compilation excludes DevTools from Release  │
│ - NuGet package excluded from Release builds              │
└────────────────────────────────────────────────────────────┘
                        │
                        ▼
┌────────────────────────────────────────────────────────────┐
│ Layer 4: Documentation & Training                         │
│ - Clear warnings in docs                                  │
│ - Security section in README                              │
│ - Code comments explaining risks                          │
└────────────────────────────────────────────────────────────┘
```

## Implementation

### 1. Localhost-Only Binding

**Console App (`Program.cs`):**
```csharp
// Correct - binds to localhost only
devToolsServer = new DevToolsServer(gameApp.GameWorld, devToolsPort);
```

**DevToolsServer.cs:**
```csharp
_httpListener = new HttpListener();
_httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");  // NOT 0.0.0.0
_httpListener.Start();
```

**Verification:**
```bash
# Server running on correct interface
netstat -an | grep 5007
# Should show: 127.0.0.1:5007  (NOT 0.0.0.0:5007)
```

**Why This Matters:**
- `127.0.0.1` - Only accessible from localhost
- `0.0.0.0` - Accessible from any network interface ⚠️ **NEVER USE THIS**
- `192.168.x.x` - Accessible from LAN ⚠️ **AVOID**

### 2. Explicit Opt-In

**Console App - Command Line Flag:**
```csharp
var enableDevToolsOption = new Option<bool>("--enable-dev-tools")
{
    Description = "Enable DevTools WebSocket server for external control"
};

// Default value is false (disabled)
var enableDevTools = parseResult.GetValue(enableDevToolsOption);
```

**Usage:**
```bash
# DevTools disabled (default)
dotnet run --project console-app

# DevTools enabled (explicit)
dotnet run --project console-app -- --enable-dev-tools
```

**Windows App - Environment Variable:**
```csharp
var enableDevTools = Environment.GetEnvironmentVariable("PIGEONPEA_DEV_TOOLS") == "1";
if (enableDevTools)
{
    _devToolsServer = new DevToolsServer(_gameWorld, devToolsPort);
    await _devToolsServer.StartAsync();
}
```

**Usage:**
```bash
# DevTools disabled (default)
dotnet run --project windows-app

# DevTools enabled (explicit)
PIGEONPEA_DEV_TOOLS=1 dotnet run --project windows-app
```

**Why Different Mechanisms?**
- Console app has CLI parser → use flag
- Windows app is GUI → environment variable easier
- Both require **explicit opt-in** (disabled by default)

### 3. Build Configuration (Recommended for Production)

**Current State:** DevTools always compiled, opt-in at runtime

**Future Improvement:**
```csharp
#if DEBUG
using PigeonPea.DevTools.Server;
#endif

public partial class MainWindow : Window
{
#if DEBUG
    private readonly DevToolsServer? _devToolsServer;
#endif

    public MainWindow()
    {
#if DEBUG
        var enableDevTools = Environment.GetEnvironmentVariable("PIGEONPEA_DEV_TOOLS") == "1";
        if (enableDevTools)
        {
            _devToolsServer = new DevToolsServer(_gameWorld, 5007);
            _ = _devToolsServer.StartAsync();
        }
#endif
    }
}
```

**.csproj Conditional Reference:**
```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <ProjectReference Include="..\..\..\dev-tools\core\PigeonPea.DevTools\PigeonPea.DevTools.csproj" />
</ItemGroup>
```

**Why This Helps:**
- Release builds **physically cannot** start DevTools (code not compiled)
- Smaller binary size (DevTools excluded)
- Impossible to accidentally enable in production

**Current Decision:** Not implemented yet (low priority, runtime opt-in is sufficient for now)

### 4. Port Configuration

**Default Port:** 5007

**Why 5007?**
- Not a well-known port (reduces chance of conflict)
- Above 1024 (no root/admin required)
- Easy to remember

**Configurable Port:**
```bash
# Console app
dotnet run --project console-app -- --enable-dev-tools --dev-tools-port 5008

# Windows app
PIGEONPEA_DEV_TOOLS=1 PIGEONPEA_DEV_TOOLS_PORT=5008 dotnet run
```

**Security Consideration:**
- Port number doesn't affect security (still localhost-only)
- Useful if 5007 is already in use

## Deployment Guidelines

### Development Environment (Safe)

✅ **Allowed:**
```bash
# Local development
dotnet run --project console-app -- --enable-dev-tools

# Testing with external tools
pp-dev spawn goblin 10 5

# Automated testing scripts
python test_game.py --connect ws://127.0.0.1:5007
```

### CI/CD Environment (Safe with Caution)

✅ **Allowed (if needed for automated tests):**
```yaml
# GitHub Actions workflow
- name: Run integration tests
  run: |
    dotnet run --project console-app -- --enable-dev-tools &
    sleep 2
    cargo run --manifest-path dev-tools/clients/rust-cli/Cargo.toml -- ping
```

⚠️ **Caution:**
- Only enable in isolated CI containers (GitHub Actions, GitLab CI, etc.)
- Never enable on shared CI servers accessible from network

### Production Environment (NEVER)

❌ **NEVER:**
```bash
# Don't do this on player machines
game.exe --enable-dev-tools

# Don't distribute builds with this enabled
set PIGEONPEA_DEV_TOOLS=1 && game.exe
```

❌ **NEVER bind to 0.0.0.0:**
```csharp
// WRONG - exposes to network
_httpListener.Prefixes.Add($"http://0.0.0.0:{_port}/");

// CORRECT - localhost only
_httpListener.Prefixes.Add($"http://127.0.0.1:{_port}/");
```

### Multiplayer Considerations (Future)

⚠️ **If multiplayer is added:**
- DevTools must be **disabled** in multiplayer mode (even for localhost testing)
- Server operators must **never** enable DevTools on public servers
- Clients must **never** connect to servers with DevTools enabled
- Add multiplayer mode detection:

```csharp
if (enableDevTools && isMultiplayerMode)
{
    throw new InvalidOperationException(
        "DevTools cannot be enabled in multiplayer mode for security reasons");
}
```

## Security Best Practices

### For Developers

1. **Never commit enabled DevTools to scripts:**
   ```bash
   # Bad - in run.sh
   dotnet run --enable-dev-tools

   # Good - in run.sh
   dotnet run

   # Good - in run-dev.sh (separate script)
   dotnet run --enable-dev-tools
   ```

2. **Never expose port 5007 through firewall:**
   ```bash
   # Bad
   sudo ufw allow 5007

   # Good (default - port blocked)
   # No firewall rule needed for localhost
   ```

3. **Document when DevTools is enabled:**
   ```bash
   # Good - clear in script
   echo "Starting game with DevTools enabled for testing..."
   dotnet run --enable-dev-tools
   ```

### For Distributors

1. **Exclude DevTools from release builds:**
   - Use `#if DEBUG` preprocessor directives
   - Exclude `PigeonPea.DevTools.csproj` from Release configuration

2. **Verify release builds:**
   ```bash
   # Check release build doesn't reference DevTools
   dotnet build -c Release
   strings bin/Release/net9.0/PigeonPea.Console.dll | grep -i devtools
   # Should return nothing
   ```

3. **Document in release notes:**
   ```markdown
   ## Security Notes
   - This build does not include DevTools
   - No debug/development features enabled
   - Safe for distribution to players
   ```

### For Players

Players should **never** see or interact with DevTools:
- Not compiled into release builds
- No UI for enabling it
- Not documented in player-facing documentation

## Vulnerability Disclosure

### Known Non-Issues

**Q: Is it safe to have DevTools code in the repo?**
A: Yes. The code itself is not a vulnerability. It's only a risk if:
   1. Enabled in production builds (we prevent via opt-in)
   2. Exposed to network (we prevent via localhost binding)

**Q: What if a player enables it manually?**
A: They would need to:
   1. Have a Debug build (not distributed)
   2. Know about the hidden flag
   3. Install Rust CLI or write their own client
   4. Modify their own local game state (which they can do anyway via memory editing)

   This is equivalent to using Cheat Engine - it only affects their local game.

**Q: Can malware on my development machine abuse DevTools?**
A: Theoretically yes, but:
   1. Malware on your dev machine can already do much worse (steal source code, SSH keys, etc.)
   2. Game dev tools are not a valuable attack vector
   3. DevTools only runs while game is running
   4. Standard anti-malware practices protect against this

### Responsible Disclosure

If security issues are found with DevTools:

1. **Email:** [maintainer email]
2. **Include:**
   - Description of vulnerability
   - Steps to reproduce
   - Proposed fix (if any)
3. **Do not:**
   - Publicly disclose before patch
   - Use in malicious manner

## Testing

### Security Tests

**1. Verify Localhost Binding:**
```bash
# Start game with DevTools
dotnet run --project console-app -- --enable-dev-tools &

# Check listening address
netstat -an | grep 5007

# Expected: 127.0.0.1:5007
# Fail if: 0.0.0.0:5007
```

**2. Verify Remote Connection Rejected:**
```bash
# From another machine on network
wscat -c ws://192.168.1.100:5007
# Expected: Connection refused
```

**3. Verify Default Disabled:**
```bash
# Run without flag
dotnet run --project console-app &

# Attempt connection
wscat -c ws://127.0.0.1:5007
# Expected: Connection refused
```

**4. Verify Release Build Exclusion (Future):**
```bash
dotnet build -c Release
strings bin/Release/net9.0/PigeonPea.Console.dll | grep DevToolsServer
# Expected: No output (symbol not found)
```

## Documentation Requirements

### Developer Documentation

**README.md:**
```markdown
## DevTools (Development Only)

⚠️ **Security Warning:** DevTools is for development only. Never enable in production.

### Usage
# Console
dotnet run --project console-app -- --enable-dev-tools

# Windows
PIGEONPEA_DEV_TOOLS=1 dotnet run --project windows-app
```

**ARCHITECTURE.md:**
```markdown
## DevTools System

⚠️ **DevTools binds to 127.0.0.1 only** (localhost). It should never be exposed to external networks.

- Only enabled via explicit flag/environment variable
- No authentication (designed for local development only)
- Should never be used in production builds
```

### Code Documentation

**DevToolsServer.cs:**
```csharp
/// <summary>
/// WebSocket server for dev tools that allows external clients to connect
/// and send commands to the running game.
///
/// ⚠️ SECURITY: This server binds to 127.0.0.1 (localhost only) and should
/// NEVER be exposed to external networks. It has no authentication and is
/// intended ONLY for development/testing environments.
/// </summary>
public class DevToolsServer : IDisposable
{
    // ...
}
```

## Future Enhancements

### 1. Authentication (If Network Exposure Needed)

**If** we ever need to expose DevTools over network (e.g., remote testing):

```csharp
// Add token-based auth
var authToken = Environment.GetEnvironmentVariable("PIGEONPEA_DEV_TOKEN");
if (request.Headers["Authorization"] != $"Bearer {authToken}")
{
    response.StatusCode = 401;
    return;
}
```

**But:** This should be avoided. Better to use SSH tunneling:
```bash
# Tunnel remote port to localhost
ssh -L 5007:localhost:5007 developer@remote-machine

# Connect to localhost (which tunnels to remote)
pp-dev --connect ws://127.0.0.1:5007
```

### 2. Rate Limiting

**If** needed to prevent accidental DoS during automated testing:

```csharp
private readonly RateLimiter _rateLimiter = new(
    maxCommands: 100,
    perTimeSpan: TimeSpan.FromSeconds(1)
);
```

**But:** Unnecessary for localhost development tool.

### 3. Command Whitelisting

**If** some commands are too dangerous even for development:

```csharp
private readonly HashSet<string> _allowedCommands = new()
{
    "spawn", "tp", "query", "heal", "kill", "give"
};

if (!_allowedCommands.Contains(command.Cmd))
{
    return CommandResult(false, $"Command '{command.Cmd}' is not allowed");
}
```

**But:** If we don't trust the developer, they can just modify the code anyway.

## References

- RFC-013: DevTools System Architecture
- RFC-014: DevTools Protocol Specification
- OWASP Top 10: https://owasp.org/www-project-top-ten/
- CWE-200: Exposure of Sensitive Information
- CWE-284: Improper Access Control

## Conclusion

PigeonPea DevTools follows a **defense-in-depth** security model:

1. **Localhost binding** - Never exposed to network
2. **Explicit opt-in** - Disabled by default
3. **Build separation** - Excluded from release builds (future)
4. **Clear documentation** - Developers understand risks

This makes DevTools safe for development while preventing accidental production exposure. The "no authentication" design is acceptable because the tool only runs on trusted developer machines and is never network-accessible.

**Key Principle:** DevTools is a **power tool for developers**, not a feature for players. It should be as easy to use for developers as possible, while being impossible to accidentally enable in production.
