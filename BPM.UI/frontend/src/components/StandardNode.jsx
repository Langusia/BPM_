import React from 'react';
import { Handle, Position } from 'reactflow';

const statusColors = {
  Completed: { bg: '#f0fdf4', border: '#22c55e', text: '#15803d' },
  Available: { bg: '#eff6ff', border: '#3b82f6', text: '#1d4ed8' },
  Locked: { bg: '#f3f4f6', border: '#9ca3af', text: '#6b7280' },
};

export default function StandardNode({ data }) {
  const { label, status, nodeType } = data;
  const colors = statusColors[status] || statusColors.Locked;
  const isLocked = status === 'Locked';

  const borderStyle = nodeType === 'Optional' ? '2px dashed' : '2px solid';
  const accentColor = nodeType === 'AnyTime' ? '#8b5cf6' : colors.border;

  return (
    <div
      className={`${status === 'Available' ? 'node-available' : ''} ${nodeType === 'Optional' ? 'node-fade-in' : ''}`}
      style={{
        background: colors.bg,
        border: `${borderStyle} ${accentColor}`,
        borderRadius: 10,
        padding: '10px 16px',
        minWidth: 170,
        opacity: isLocked ? 0.5 : 1,
        cursor: isLocked ? 'not-allowed' : 'pointer',
        textAlign: 'center',
        position: 'relative',
      }}
    >
      <Handle type="target" position={Position.Top} style={{ background: accentColor }} />
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
        {status === 'Completed' && <span style={{ color: '#22c55e', fontSize: 16 }}>✓</span>}
        <span style={{ fontWeight: 600, color: colors.text, fontSize: 13 }}>{label}</span>
      </div>
      {nodeType !== 'Standard' && (
        <div style={{ fontSize: 10, color: '#9ca3af', marginTop: 2 }}>{nodeType}</div>
      )}
      <Handle type="source" position={Position.Bottom} style={{ background: accentColor }} />
    </div>
  );
}
