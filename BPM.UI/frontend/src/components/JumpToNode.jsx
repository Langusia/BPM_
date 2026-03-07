import React from 'react';
import { Handle, Position } from 'reactflow';

export default function JumpToNode({ data }) {
  const { label, status, onExpand } = data;
  const isLocked = status === 'Locked';
  const isCompleted = status === 'Completed';

  const borderColor = isCompleted ? '#22c55e' : isLocked ? '#9ca3af' : '#6366f1';
  const bgColor = isCompleted ? '#f0fdf4' : isLocked ? '#f3f4f6' : '#eef2ff';

  return (
    <div
      onClick={() => !isLocked && onExpand?.()}
      style={{
        background: bgColor,
        border: `2px solid ${borderColor}`,
        borderRadius: 10,
        padding: '10px 16px',
        minWidth: 170,
        opacity: isLocked ? 0.5 : 1,
        cursor: isLocked ? 'not-allowed' : 'pointer',
        textAlign: 'center',
      }}
    >
      <Handle type="target" position={Position.Top} style={{ background: borderColor }} />
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
        {isCompleted && <span style={{ color: '#22c55e', fontSize: 16 }}>✓</span>}
        <span style={{ fontWeight: 600, fontSize: 13 }}>{label}</span>
        {!isCompleted && <span style={{ fontSize: 14, color: '#6366f1' }}>▶</span>}
      </div>
      <div style={{ fontSize: 10, color: '#6b7280', marginTop: 2 }}>Subgraph</div>
      <Handle type="source" position={Position.Bottom} style={{ background: borderColor }} />
    </div>
  );
}
