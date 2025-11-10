# Security Scanning - Detailed Procedure

## Overview

This guide provides step-by-step instructions for running security scans on the PigeonPea project using gitleaks, detect-secrets, and .NET security analyzers.

## Prerequisites

- **Pre-commit installed** (check: `pre-commit --version`)
- **gitleaks** configured in `.pre-commit-config.yaml`
- **detect-secrets** configured in `.pre-commit-config.yaml`
- **.NET SDK 9.0** for security analyzers

## Security Scanning Tools

### 1. Gitleaks (Secret Detection)

Scans Git history and files for hardcoded secrets, API keys, passwords, tokens.

**What it detects:**
- API keys (AWS, Azure, GitHub, etc.)
- Private keys and certificates
- Database connection strings
- OAuth tokens and secrets
- Passwords and credentials

**Configuration:** `.gitleaksignore` for false positives

### 2. Detect-Secrets (Secret Baseline)

Scans files for potential secrets using heuristics and maintains a baseline of known false positives.

**What it detects:**
- High entropy strings (potential passwords/keys)
- Base64-encoded secrets
- Hex-encoded secrets
- Private key headers

**Configuration:** `.secrets.baseline` for false positive baseline

### 3. .NET Security Analyzers (CA5xxx Rules)

Built-in Roslyn analyzers that detect security vulnerabilities in C# code.

**What it detects:**
- Insecure cryptography usage
- SQL injection vulnerabilities
- Path traversal issues
- XML external entity (XXE) attacks
- Insecure deserialization
- CSRF vulnerabilities

## Standard Security Scan Flow

### Step 1: Run Pre-commit Secret Detection

```bash
pre-commit run gitleaks --all-files
pre-commit run detect-secrets --all-files
```

This scans all files in the repository for secrets.

### Step 2: Run .NET Security Analysis

```bash
cd ./dotnet
dotnet build PigeonPea.sln /p:RunAnalyzers=true /p:TreatWarningsAsErrors=true
```

Security rules (CA5xxx) are enabled by default and run during build.

### Step 3: Review Findings

**Gitleaks output:**
```
INFO[0000] 7 commits scanned.
INFO[0000] scan completed in 142ms
INFO[0000] No leaks found
```

**Detect-secrets output:**
```
detect-secrets...........Passed
```

**Security analyzer output:**
```
console-app/Program.cs(42,15): warning CA5351: Do Not Use Broken Cryptographic Algorithms
```

## Gitleaks Configuration

### Configuration File

Gitleaks uses `.gitleaksignore` to exclude false positives:

```
# .gitleaksignore - False positives to ignore

# Example connection strings in documentation
docs/examples/connection-string.md:line42

# Test fixtures with dummy credentials
tests/fixtures/sample-config.json:*

# Known safe patterns
**/*.md:password
```

### Running Gitleaks Manually

```bash
# Scan all files
gitleaks detect --source . --verbose

# Scan staged files only
gitleaks protect --staged --verbose

# Scan specific files
gitleaks detect --source ./dotnet/console-app --verbose

# Generate report
gitleaks detect --source . --report-path gitleaks-report.json
```

### Gitleaks Exit Codes

- **0**: No leaks found
- **1**: Leaks detected
- **2**: Configuration error

## Detect-Secrets Configuration

### Baseline File

`.secrets.baseline` stores known false positives:

```json
{
  "version": "1.4.0",
  "filters_used": [
    {
      "path": "detect_secrets.filters.heuristic.is_potential_uuid"
    }
  ],
  "results": {
    "tests/fixtures/sample.config": [
      {
        "type": "Base64 High Entropy String",
        "filename": "tests/fixtures/sample.config",
        "hashed_secret": "abc123...",
        "line_number": 42
      }
    ]
  }
}
```

### Running Detect-Secrets Manually

```bash
# Scan all files
detect-secrets scan

# Update baseline (after adding false positives)
detect-secrets scan --baseline .secrets.baseline

# Audit baseline interactively
detect-secrets audit .secrets.baseline

# Scan specific directory
detect-secrets scan dotnet/
```

### Detect-Secrets Exit Codes

- **0**: No new secrets found
- **1**: New secrets detected or baseline outdated

## .NET Security Analyzers

### Security Rule Categories

**CA5xxx Series** - Cryptography and Security

**CA5350/CA5351**: Do not use weak/broken cryptographic algorithms
```csharp
// Bad
using (var md5 = MD5.Create()) { }  // CA5351

// Good
using (var sha256 = SHA256.Create()) { }
```

**CA5359**: Do not disable certificate validation
```csharp
// Bad
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;  // CA5359

// Good
// Use proper certificate validation
```

**CA5363**: Do not disable request validation
```csharp
// Bad
[ValidateInput(false)]  // CA5363
public ActionResult Submit(string input) { }

// Good
public ActionResult Submit(string input) { }  // Validation enabled
```

**CA5364**: Do not use deprecated security protocols
```csharp
// Bad
ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;  // CA5364

// Good
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
```

**CA5379/CA5380**: Ensure key derivation function algorithm is strong
```csharp
// Bad
using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, 1000)) { }  // CA5379 (iterations too low)

// Good
using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256)) { }
```

**CA5384**: Do not use DSA
```csharp
// Bad
using (var dsa = DSA.Create()) { }  // CA5384

// Good
using (var rsa = RSA.Create()) { }
```

**CA5397**: Do not use deprecated SslProtocols values
**CA3001**: Review code for SQL injection vulnerabilities
**CA3002**: Review code for XSS vulnerabilities
**CA3003**: Review code for file path injection vulnerabilities
**CA3004**: Review code for information disclosure vulnerabilities
**CA3006**: Review code for process command injection vulnerabilities
**CA3007**: Review code for open redirect vulnerabilities
**CA3008**: Review code for XPath injection vulnerabilities
**CA3009**: Review code for XML injection vulnerabilities
**CA3010**: Review code for XAML injection vulnerabilities
**CA3011**: Review code for DLL injection vulnerabilities
**CA3012**: Review code for regex injection vulnerabilities

### Enabling Security Analysis

Security rules are enabled by default. To enforce strict mode:

```bash
cd ./dotnet
dotnet build PigeonPea.sln /p:AnalysisMode=AllEnabledByDefault /p:TreatWarningsAsErrors=true
```

### Configuring Security Rules

In `.editorconfig`:

```ini
[*.cs]
# Security rules - treat as errors
dotnet_diagnostic.CA5350.severity = error
dotnet_diagnostic.CA5351.severity = error
dotnet_diagnostic.CA5359.severity = error
dotnet_diagnostic.CA5364.severity = error

# SQL injection - warning level
dotnet_diagnostic.CA3001.severity = warning
```

## Interpreting Security Scan Results

### Clean Scan (No Issues)

```
gitleaks................Passed
detect-secrets...........Passed
Build succeeded. 0 Warning(s) 0 Error(s)
```

✅ No security issues detected.

### Secrets Detected

```
gitleaks................Failed
- hook id: gitleaks
- exit code: 1

Finding:     AWS Access Key
Secret:      AKIAIOSFODNN7EXAMPLE
File:        config/aws.json
Line:        12
```

❌ **Action Required:** Remove secret, rotate credentials, add to `.gitleaksignore` if false positive.

### Security Vulnerabilities

```
console-app/Crypto.cs(42,15): warning CA5351: Do Not Use Broken Cryptographic Algorithms. MD5 is cryptographically broken.
```

❌ **Action Required:** Replace MD5 with SHA256 or stronger algorithm.

## Handling False Positives

### Gitleaks False Positives

Add to `.gitleaksignore`:

```
# Example API key in documentation
docs/api-guide.md:example_api_key_12345

# Test fixtures
tests/fixtures/*.json:*
```

### Detect-Secrets False Positives

Update baseline interactively:

```bash
# Audit baseline and mark false positives
detect-secrets audit .secrets.baseline

# Update baseline with new scan
detect-secrets scan --baseline .secrets.baseline
```

### Security Analyzer False Positives

Suppress with justification:

```csharp
// Justification: MD5 used only for non-cryptographic checksums
[SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms")]
private string ComputeChecksum(byte[] data)
{
    using (var md5 = MD5.Create())
        return Convert.ToBase64String(md5.ComputeHash(data));
}
```

## Advanced Scenarios

### Scan Git History for Secrets

```bash
# Full history scan
gitleaks detect --source . --log-opts="--all" --verbose

# Scan specific commit range
gitleaks detect --source . --log-opts="main..feature-branch"

# Scan and generate report
gitleaks detect --source . --report-path security-report.json --report-format json
```

### Pre-commit Hook Integration

Hooks run automatically on `git commit`:

```bash
# Test hooks
pre-commit run --all-files

# Bypass hooks (emergency only)
git commit --no-verify
```

### CI/CD Integration

```yaml
# GitHub Actions example
- name: Run Security Scans
  run: |
    pre-commit run gitleaks --all-files
    pre-commit run detect-secrets --all-files
    cd dotnet && dotnet build PigeonPea.sln /p:TreatWarningsAsErrors=true
```

## Best Practices

1. **Never commit secrets**: Use environment variables or secret management services
2. **Rotate compromised secrets immediately**: Assume any committed secret is compromised
3. **Run scans before commit**: Use pre-commit hooks or IDE integrations
4. **Review baseline regularly**: Audit `.secrets.baseline` for stale entries
5. **Document suppressions**: Justify all security rule suppressions
6. **Use strong cryptography**: Always use SHA256 or stronger, TLS 1.2+
7. **Enable security rules in CI/CD**: Fail builds on security issues
8. **Keep tools updated**: Update gitleaks and detect-secrets regularly

## Emergency: Secret Committed

If a secret is accidentally committed:

1. **Rotate the secret immediately** (revoke/regenerate credentials)
2. **Remove from Git history**:
   ```bash
   # Use a tool like git-filter-repo (recommended) or BFG Repo-Cleaner.
   # git filter-branch is not recommended.
   # Example with git-filter-repo:
   git-filter-repo --invert-paths --path path/to/file
   ```
3. **Force push** (if safe): `git push --force`
4. **Notify team** about credential rotation
5. **Add to `.gitleaksignore`** to prevent future issues

**Important:** Once a secret is committed to a public repo, assume it's compromised forever.

## Related Procedures

- **Static analysis:** See [`static-analysis.md`](static-analysis.md)
- **Dependency checks:** See [`dependency-check.md`](dependency-check.md)
- **Pre-commit setup:** See `../../../.pre-commit-config.yaml`

## Quick Reference

```bash
# Run all security scans
pre-commit run gitleaks --all-files
pre-commit run detect-secrets --all-files
cd dotnet && dotnet build PigeonPea.sln /p:TreatWarningsAsErrors=true

# Update secret baseline
detect-secrets scan --baseline .secrets.baseline

# Manual gitleaks scan
gitleaks detect --source . --verbose

# Security analysis only
cd dotnet
dotnet build PigeonPea.sln /p:RunAnalyzers=true /warnaserror:CA5350,CA5351,CA5359
```
