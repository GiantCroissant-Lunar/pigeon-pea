# PTY Testing Infrastructure

This directory contains the pseudoterminal (PTY) testing infrastructure for the pigeon-pea console application, as specified in [RFC-003: Testing and Verification](../../docs/rfcs/003-testing-verification.md).

## Purpose

The PTY infrastructure enables automated testing of console applications by:

- Spawning console apps in a pseudoterminal environment
- Capturing terminal output including ANSI escape sequences
- Simulating user input programmatically
- Recording terminal sessions for visual regression testing

## Setup

### Prerequisites

- Node.js 20+ (installed via GitHub Actions or locally)
- npm

### Installation

```bash
cd tests/pty
npm install
```

This will install:

- `node-pty` - Node.js library for spawning pseudoterminals

## Usage

### Run Simple Test

```bash
npm test
```

This runs `test-pty.js`, which performs a basic PTY spawn test to verify the setup.

### Test Script

The `test-pty.js` script demonstrates:

- Spawning a shell process in a PTY
- Capturing output from the PTY
- Sending commands to the PTY
- Proper cleanup and exit handling

## CI Integration

The PTY tests are integrated into the CI pipeline via `.github/workflows/console-visual-tests.yml`:

- Tests run on every push and pull request
- Node.js and dependencies are installed automatically
- Test artifacts are uploaded on failure

## Future Enhancements

As outlined in RFC-003, this infrastructure will be extended to support:

1. **asciinema Recording**: Capture terminal sessions as `.cast` files
2. **Test Scenarios**: JSON-defined input sequences for automated testing
3. **Visual Regression**: Compare rendered output against snapshots
4. **Integration with xUnit**: Call PTY tests from C# test projects

## Files

- `package.json` - Node.js package configuration and dependencies
- `.gitignore` - Excludes node_modules and test outputs
- `test-pty.js` - Simple PTY spawn test
- `README.md` - This file

## References

- [RFC-003: Testing and Verification](../../docs/rfcs/003-testing-verification.md)
- [node-pty Repository](https://github.com/microsoft/node-pty)
- [asciinema Documentation](https://docs.asciinema.org/)
