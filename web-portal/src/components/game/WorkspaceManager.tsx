// src/components/game/WorkspaceManager.tsx
import { useState, useEffect } from 'react';

interface Workspace {
  id: string;
  name: string;
  active: boolean;
  created: Date;
}

export function WorkspaceManager() {
  const [workspaces, setWorkspaces] = useState<Workspace[]>([
    { id: 'default', name: 'Main Session', active: true, created: new Date() }
  ]);
  
  const createWorkspace = () => {
    const id = `ws-${Date.now()}`;
    const newWorkspace: Workspace = {
      id,
      name: `Session ${workspaces.length + 1}`,
      active: false,
      created: new Date()
    };
    
    setWorkspaces([...workspaces, newWorkspace]);
  };
  
  const switchWorkspace = (id: string) => {
    window.location.href = `/play/${id}`;
  };
  
  const deleteWorkspace = (id: string) => {
    if (id === 'default') {
      alert('Cannot delete main session');
      return;
    }
    
    if (confirm('Delete this workspace?')) {
      setWorkspaces(workspaces.filter(ws => ws.id !== id));
    }
  };
  
  return (
    <div className="workspace-manager">
      <div className="workspace-tabs">
        {workspaces.map(ws => (
          <div 
            key={ws.id}
            className={`workspace-tab ${ws.active ? 'active' : ''}`}
            onClick={() => switchWorkspace(ws.id)}
          >
            <span className="workspace-name">{ws.name}</span>
            {ws.id !== 'default' && (
              <button 
                className="close-btn"
                onClick={(e) => {
                  e.stopPropagation();
                  deleteWorkspace(ws.id);
                }}
              >
                ×
              </button>
            )}
          </div>
        ))}
        <button className="new-workspace-btn" onClick={createWorkspace}>
          + New Session
        </button>
      </div>
    </div>
  );
}
