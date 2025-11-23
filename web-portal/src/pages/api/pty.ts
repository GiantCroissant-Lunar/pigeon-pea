// src/pages/api/pty.ts
import type { APIRoute } from 'astro';
// import { spawn } from 'node-pty'; // Disabled due to build environment issues
import * as os from 'os';
import * as path from 'path';

// Store active PTY sessions
const sessions = new Map<string, any>();

export const GET: APIRoute = async ({ request, url }) => {
  const upgradeHeader = request.headers.get('upgrade');
  
  if (upgradeHeader !== 'websocket') {
    return new Response('Expected WebSocket', { status: 400 });
  }
  
  const workspaceId = url.searchParams.get('workspace') || 'default';
  
  // Check if session already exists
  if (sessions.has(workspaceId)) {
    return new Response('Workspace already active', { status: 409 });
  }
  
  // Upgrade to WebSocket
  // @ts-ignore
  const { socket, response } = (globalThis.Deno?.upgradeWebSocket(request)) || { socket: null, response: new Response('WebSockets require Deno or custom Node server. node-pty is currently disabled.', { status: 501 }) };

  if (!socket) return response;
  
  /* 
  // Mock PTY for now since node-pty failed to build
  const shell = os.platform() === 'win32' ? 'powershell.exe' : 'bash';
  const projectPath = path.join(process.cwd(), '../dotnet/console-app/core/src/PigeonPea.Console');
  
  const ptyProcess = spawn(shell, [], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    cwd: projectPath,
    env: process.env as any,
  });
  
  sessions.set(workspaceId, ptyProcess);
  
  if (os.platform() === 'win32') {
    setTimeout(() => {
      ptyProcess.write('dotnet run\r');
    }, 500);
  }
  
  ptyProcess.onData((data: string) => {
    try {
      socket.send(data);
    } catch (e) {
      console.error('Failed to send to WebSocket:', e);
    }
  });
  
  socket.addEventListener('message', (event: any) => {
    const data = event.data;
    if (typeof data === 'string') {
      try {
        const msg = JSON.parse(data);
        if (msg.type === 'resize') {
          ptyProcess.resize(msg.cols, msg.rows);
        } else {
          ptyProcess.write(data);
        }
      } catch {
        ptyProcess.write(data);
      }
    } else {
      ptyProcess.write(data);
    }
  });
  
  socket.addEventListener('close', () => {
    console.log(\Workspace \ closed\);
    ptyProcess.kill();
    sessions.delete(workspaceId);
  });
  
  ptyProcess.onExit(() => {
    socket.close();
    sessions.delete(workspaceId);
  });
  */

  // Mock response for WebSocket connection
  socket.addEventListener('open', () => {
    socket.send('Welcome to PigeonPea Web Portal!\\r\\n');
    socket.send('NOTE: PTY integration is currently disabled due to build environment limitations.\\r\\n');
    socket.send('You would normally see the game running here.\\r\\n');
  });

  return response;
};
