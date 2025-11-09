#!/usr/bin/env node

/**
 * Simple PTY spawn test to verify node-pty installation
 * This script spawns a simple command in a pseudoterminal and captures output
 */

const pty = require('node-pty');
const os = require('os');

function runSimpleTest() {
  console.log('Starting simple PTY spawn test...');

  // Determine shell based on platform
  const shell = os.platform() === 'win32' ? 'powershell.exe' : 'bash';

  // Spawn a simple echo command in a PTY
  const ptyProcess = pty.spawn(shell, [], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    cwd: process.cwd(),
    env: process.env
  });

  let output = '';

  // Capture output
  ptyProcess.onData((data) => {
    output += data;
    process.stdout.write(data);
  });

  // Safety timeout to prevent the test from running indefinitely.
  const safetyTimeout = setTimeout(() => {
    console.log('\nTimeout reached, killing PTY process...');
    ptyProcess.kill(); // This will trigger the onExit handler, centralizing the exit logic.
  }, 2000);

  // Handle exit
  ptyProcess.onExit(({ exitCode, signal }) => {
    clearTimeout(safetyTimeout); // Clear the safety timeout to prevent the script from hanging.
    console.log(`\nPTY process exited with code ${exitCode}, signal ${signal}`);
    
    // A more robust check for specific output.
    if (output.includes("Hello from PTY test")) {
      console.log('✓ PTY spawn test passed: Received expected output from spawned process');
      process.exit(0);
    } else {
      console.error('✗ PTY spawn test failed: Did not receive expected output.');
      console.error('--- Received output: ---\n' + output + '\n------------------------');
      process.exit(1);
    }
  });

  // Send a simple command and exit
  setTimeout(() => {
    ptyProcess.write('echo "Hello from PTY test"\r');
  }, 100);

  setTimeout(() => {
    ptyProcess.write('exit\r');
  }, 500);
}

// Run the test
try {
  runSimpleTest();
} catch (error) {
  console.error('Error running PTY test:', error);
  process.exit(1);
}
