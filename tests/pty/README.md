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

### Run PTY Tests

Run the default test scenario (basic-movement) with recording:

```bash
npm test
```

Run without asciinema recording:

```bash
npm run test:no-record
```

Run a specific test scenario:

```bash
npm run test:scenario basic-movement
# or
node test-pty.js basic-movement
```

Run with custom output directory:

```bash
node test-pty.js basic-movement --output-dir=./my-recordings
```

### Test Script Features

The `test-pty.js` script provides:

- **PTY Spawning**: Spawns the console app in a pseudoterminal with proper dimensions
- **Scenario Loading**: Loads test scenarios from JSON files in `scenarios/` directory
- **Input Simulation**: Sends keyboard inputs with configurable delays
- **Output Capture**: Captures all terminal output including ANSI escape sequences
- **asciinema Recording**: Records terminal sessions to `.cast` files (if asciinema is installed)
- **Clean Exit**: Properly handles cleanup and returns appropriate exit codes
- **Exit Code 0 on Success**: Returns 0 when test completes successfully

### Test Scenarios

Test scenarios are defined in JSON files in the `scenarios/` directory:

- `basic-movement.json` - Tests player movement in all directions

Each scenario defines:
- Name and description
- Array of inputs with delays and descriptions
- Expected frames (for future validation)

## CI Integration

The PTY tests are integrated into the CI pipeline via `.github/workflows/console-visual-tests.yml`:

- Tests run on every push and pull request
- Node.js and dependencies are installed automatically
- Test artifacts are uploaded on failure

### Creating New Test Scenarios

To create a new test scenario:

1. Create a JSON file in `scenarios/` directory (e.g., `my-test.json`)
2. Define the scenario structure:

```json
{
  "name": "My Test Scenario",
  "description": "Description of what this test does",
  "inputs": [
    { "delay": 500, "key": "\r", "description": "Press Enter" },
    { "delay": 200, "key": "w", "description": "Move up" },
    { "delay": 200, "key": "q", "description": "Quit" }
  ],
  "expectedFrames": []
}
```

3. Run the test:

```bash
node test-pty.js my-test
```

## Future Enhancements

As outlined in RFC-003, this infrastructure can be extended to support:

1. ✅ **asciinema Recording**: Capture terminal sessions as `.cast` files (implemented)
2. ✅ **Test Scenarios**: JSON-defined input sequences for automated testing (implemented)
3. **Visual Regression**: Compare rendered output against snapshots
4. **Integration with xUnit**: Call PTY tests from C# test projects
5. **Frame Validation**: Verify expected frames from scenario definitions

## Files

- `package.json` - Node.js package configuration and dependencies
- `.gitignore` - Excludes node_modules and test outputs
- `test-pty.js` - Simple PTY spawn test
- `README.md` - This file

## References

- [RFC-003: Testing and Verification](../../docs/rfcs/003-testing-verification.md)
- [node-pty Repository](https://github.com/microsoft/node-pty)
- [asciinema Documentation](https://docs.asciinema.org/)
