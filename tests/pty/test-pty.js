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
  let hasReceivedOutput = false;

  // Capture output
  ptyProcess.onData((data) => {
    output += data;
    hasReceivedOutput = true;
    process.stdout.write(data);
  });

  // Handle exit
  ptyProcess.onExit(({ exitCode, signal }) => {
    console.log(`\nPTY process exited with code ${exitCode}, signal ${signal}`);
    
    if (hasReceivedOutput) {
      console.log('✓ PTY spawn test passed: Received output from spawned process');
      process.exit(0);
    } else {
      console.error('✗ PTY spawn test failed: No output received');
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

  // Safety timeout
  setTimeout(() => {
    if (ptyProcess) {
      console.log('\nTimeout reached, killing PTY process...');
      ptyProcess.kill();
      
      if (hasReceivedOutput) {
        console.log('✓ PTY spawn test passed: Received output from spawned process');
        process.exit(0);
      } else {
        console.error('✗ PTY spawn test failed: No output received');
        process.exit(1);
      }
    }
  }, 2000);
}

// Run the test
try {
  runSimpleTest();
} catch (error) {
  console.error('Error running PTY test:', error);
  process.exit(1);
}
