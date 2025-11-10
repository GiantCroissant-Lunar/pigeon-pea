#!/usr/bin/env node

/**
 * PTY Test Runner for pigeon-pea console app
 * This script spawns the console app in a pseudoterminal, executes test scenarios,
 * and optionally records the session with asciinema.
 *
 * Usage:
 *   node test-pty.js [scenario-name] [options]
 *
 * Options:
 *   --no-record    Disable asciinema recording
 *   --output-dir   Directory for recordings (default: recordings/)
 */

const pty = require('node-pty');
const fs = require('fs');
const path = require('path');
const os = require('os');
const { spawn } = require('child_process');

/**
 * Load test scenario from JSON file
 * @param {string} scenarioName - Name of the scenario (without .json extension)
 * @returns {object} Test scenario object
 */
function loadTestScenario(scenarioName) {
  const scenarioPath = path.join(__dirname, 'scenarios', `${scenarioName}.json`);

  if (!fs.existsSync(scenarioPath)) {
    throw new Error(`Scenario file not found: ${scenarioPath}`);
  }

  const data = fs.readFileSync(scenarioPath, 'utf8');
  return JSON.parse(data);
}

/**
 * Sleep for specified milliseconds
 * @param {number} ms - Milliseconds to sleep
 * @returns {Promise} Promise that resolves after the delay
 */
function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Check if asciinema is available
 * @returns {Promise<boolean>} True if asciinema is available
 */
function isAsciinemaAvailable() {
  return new Promise((resolve) => {
    // Use 'where' on Windows and 'which' on other platforms to find the command.
    const command = os.platform() === 'win32' ? 'where' : 'which';
    const checkProcess = spawn(command, ['asciinema']);

    // Handle cases where the check command (e.g., 'which') is not on the system PATH.
    checkProcess.on('error', () => resolve(false));

    checkProcess.on('exit', (code) => {
      resolve(code === 0);
    });
  });
}

/**
 * Run the game in a PTY with the given test scenario
 * @param {object} scenario - Test scenario object
 * @param {object} options - Test options
 * @returns {Promise<object>} Test result with output and recording path
 */
async function runGameInPTY(scenario, options = {}) {
  const { record = true, outputDir = 'recordings', scenarioName = 'test' } = options;

  console.log(`\n=== Running PTY Test: ${scenario.name} ===`);
  console.log(`Description: ${scenario.description}`);

  // Create output directory
  if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
  }

  // Check if asciinema is available for recording
  let recordingFile = null;
  const hasAsciinema = await isAsciinemaAvailable();

  if (record && hasAsciinema) {
    recordingFile = path.join(outputDir, `${scenarioName}.cast`);
    console.log(`Recording enabled: ${recordingFile}`);
  } else if (record && !hasAsciinema) {
    console.log('⚠ asciinema not found, recording disabled');
  }

  // Spawn the console app in a PTY
  const consoleAppPath = path.join(__dirname, '../../dotnet/console-app');
  console.log(`\nSpawning console app from: ${consoleAppPath}`);

  const game = pty.spawn('dotnet', ['run', '--', '--renderer', 'ascii'], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    cwd: consoleAppPath,
    env: process.env,
  });

  let output = '';
  const outputFile = path.join(outputDir, `${scenarioName}.output.txt`);

  // If recording with asciinema, create the recording file writer
  let castFileStream = null;
  let recordingStartTime = null;

  if (recordingFile) {
    recordingStartTime = Date.now();
    castFileStream = fs.createWriteStream(recordingFile);

    // Write asciinema v2 header
    const header = {
      version: 2,
      width: 80,
      height: 24,
      timestamp: Math.floor(recordingStartTime / 1000),
      env: {
        TERM: 'xterm-256color',
        SHELL: process.env.SHELL || (os.platform() === 'win32' ? 'powershell.exe' : '/bin/bash'),
      },
    };
    castFileStream.write(JSON.stringify(header) + '\n');
  }

  // Capture output
  game.onData((data) => {
    output += data;

    // Write to asciinema cast file if recording
    if (castFileStream && recordingStartTime) {
      const timestamp = (Date.now() - recordingStartTime) / 1000;
      const event = [timestamp, 'o', data];
      castFileStream.write(JSON.stringify(event) + '\n');
    }

    // Only echo output if verbose or debugging
    if (process.env.VERBOSE) {
      process.stdout.write(data);
    }
  });

  // Handle game exit
  const gameExitPromise = new Promise((resolve, reject) => {
    game.onExit(({ exitCode, signal }) => {
      console.log(`\nGame exited with code ${exitCode}, signal ${signal}`);
      resolve({ exitCode, signal });
    });
  });

  // Wait for initial startup
  console.log('\nWaiting for game to start...');
  await sleep(2000);

  // Send test inputs according to scenario
  console.log(`\nExecuting ${scenario.inputs.length} test inputs:\n`);
  for (const input of scenario.inputs) {
    await sleep(input.delay);
    console.log(`  [${input.delay}ms] ${input.description} (key: '${input.key}')`);

    // Write input to cast file if recording
    if (castFileStream && recordingStartTime) {
      const timestamp = (Date.now() - recordingStartTime) / 1000;
      const event = [timestamp, 'i', input.key];
      castFileStream.write(JSON.stringify(event) + '\n');
    }

    game.write(input.key);
  }

  // Wait a bit more for the last action to complete
  // Note: Using a fixed delay can lead to flaky tests if the application takes longer
  // to process commands under high system load. A more robust approach would be to
  // wait for specific output from the application signaling completion.
  console.log('\nWaiting for test to complete...');
  await sleep(1000);

  // Cleanup
  game.kill();

  await gameExitPromise;

  // Close cast file stream if recording
  if (castFileStream) {
    castFileStream.end();
    console.log(`\n✓ Recording saved: ${recordingFile}`);
  }

  // Save captured output to file
  fs.writeFileSync(outputFile, output);
  console.log(`Output saved to: ${outputFile}`);

  console.log('\n=== Test Execution Complete ===');
  console.log(`Total output captured: ${output.length} characters`);

  return {
    output,
    recording: recordingFile,
    outputFile,
    success: true,
  };
}

/**
 * Main test execution function
 */
async function main() {
  // Parse command line arguments
  const args = process.argv.slice(2);
  const scenarioName = args.find((arg) => !arg.startsWith('--')) || 'basic-movement';
  const record = !args.includes('--no-record');
  const outputDirArg = args.find((arg) => arg.startsWith('--output-dir='));
  const outputDir = outputDirArg ? outputDirArg.split('=')[1] : 'recordings';

  // Validate output directory argument
  if (outputDirArg && !outputDir) {
    throw new Error('--output-dir cannot be empty. Please provide a valid directory path.');
  }

  try {
    // Load test scenario
    console.log(`Loading scenario: ${scenarioName}`);
    const scenario = loadTestScenario(scenarioName);

    // Run test
    const result = await runGameInPTY(scenario, {
      record,
      outputDir,
      scenarioName,
    });

    // Report results
    if (result.recording) {
      console.log(`\n✓ Recording saved: ${result.recording}`);
    }

    console.log('\n✓ Test completed successfully');
    process.exit(0);
  } catch (error) {
    console.error('\n✗ Test failed:', error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

// Run main function
if (require.main === module) {
  main();
}

// Export for testing
module.exports = {
  loadTestScenario,
  runGameInPTY,
  sleep,
};
