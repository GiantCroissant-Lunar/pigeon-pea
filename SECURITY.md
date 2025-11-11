# Security Policy

## Supported Versions

We release patches for security vulnerabilities. Currently supported versions:

| Version | Supported          |
| ------- | ------------------ |
| main    | :white_check_mark: |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please follow these steps:

### Private Disclosure

**DO NOT** create a public GitHub issue for security vulnerabilities. Instead:

1. **Open a security advisory** on GitHub:
   - Go to the repository's Security tab
   - Click "Report a vulnerability"
   - Fill in the details of the vulnerability

2. **Or send an email** to the repository maintainers with:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

### What to Include

When reporting a vulnerability, please include:

- **Type of vulnerability** (e.g., XSS, SQL injection, authentication bypass)
- **Full paths of source files** related to the vulnerability
- **Location of affected code** (tag/branch/commit or direct URL)
- **Step-by-step instructions** to reproduce the issue
- **Proof-of-concept or exploit code** (if possible)
- **Impact assessment** and potential attack scenarios
- **Suggested mitigation** (if you have ideas)

### Response Timeline

- We will acknowledge receipt of your vulnerability report within **48 hours**
- We will provide a detailed response within **7 days**, including:
  - Assessment of the vulnerability
  - Timeline for fixes
  - Any workarounds or mitigations
- We will keep you informed of our progress
- We will notify you when the vulnerability is fixed

### Disclosure Policy

- We follow coordinated disclosure
- We ask that you do not publicly disclose the vulnerability until we've had a chance to address it
- Once fixed, we will publish a security advisory
- We will credit you for the discovery (unless you prefer to remain anonymous)

## Security Best Practices

This project implements several security measures:

### Pre-commit Hooks

All commits are automatically scanned for:

- **Secrets and credentials** using `gitleaks` and `detect-secrets`
- **Code quality issues** that could lead to vulnerabilities
- **Security linting** for supported languages

### Code Review

- All pull requests require review before merging
- Security-sensitive changes receive extra scrutiny
- We follow the principle of least privilege

### Dependencies

- We regularly update dependencies to patch known vulnerabilities
- We use automated tools to monitor for vulnerable dependencies
- Critical security updates are prioritized

### Development Practices

- **Input validation**: All user inputs are validated and sanitized
- **Authentication**: Secure authentication mechanisms
- **Authorization**: Proper access controls
- **Encryption**: Sensitive data is encrypted at rest and in transit
- **Error handling**: Errors don't leak sensitive information
- **Logging**: Security events are logged for audit

## Security Tools in This Repository

This project uses the following security tools:

- **gitleaks**: Detects hardcoded secrets in git repositories
- **detect-secrets**: Prevents secrets from being committed
- **pre-commit hooks**: Automated security checks before commits
- **yamllint**: YAML security and syntax validation
- **Language-specific linters**: Security checks for .NET, Python, JavaScript

## Known Security Gaps and Future Enhancements

We continuously work to improve security. Current planned enhancements:

- Automated dependency vulnerability scanning
- Security-focused CI/CD pipeline
- Regular security audits
- Penetration testing for critical components

## Security Hall of Fame

We appreciate security researchers who help us keep this project secure:

<!-- List of security researchers who have responsibly disclosed vulnerabilities -->

(No vulnerabilities reported yet)

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security)

## Questions?

If you have questions about this security policy, please open a general (non-security) issue on GitHub.

Thank you for helping keep pigeon-pea and its users safe!
