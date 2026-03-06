import React, { useState } from 'react';
import { Handle, Position } from 'reactflow';

export default function GroupNode({ data }) {
  const { label, status, members, onMemberClick, memberStatuses } = data;
  const [expanded, setExpanded] = useState(false);
  const isLocked = status === 'Locked';
  const isCompleted = status === 'Completed';

  const allMembersCompleted = members?.every((m) => memberStatuses?.[m] === 'Completed');

  if (isCompleted || allMembersCompleted) {
    return (
      <div style={{
        background: '#f0fdf4',
        border: '2px solid #22c55e',
        borderRadius: 10,
        padding: '10px 16px',
        minWidth: 170,
        textAlign: 'center',
      }}>
        <Handle type="target" position={Position.Top} style={{ background: '#22c55e' }} />
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6 }}>
          <span style={{ color: '#22c55e', fontSize: 16 }}>✓</span>
          <span style={{ fontWeight: 600, color: '#15803d', fontSize: 13 }}>{label}</span>
        </div>
        <div style={{ fontSize: 10, color: '#15803d', marginTop: 2 }}>All completed</div>
        <Handle type="source" position={Position.Bottom} style={{ background: '#22c55e' }} />
      </div>
    );
  }

  if (!expanded) {
    return (
      <div
        onClick={() => !isLocked && setExpanded(true)}
        style={{
          background: isLocked ? '#f3f4f6' : '#fefce8',
          border: `2px solid ${isLocked ? '#9ca3af' : '#eab308'}`,
          borderRadius: 10,
          padding: '10px 16px',
          minWidth: 170,
          opacity: isLocked ? 0.5 : 1,
          cursor: isLocked ? 'not-allowed' : 'pointer',
          textAlign: 'center',
        }}
      >
        <Handle type="target" position={Position.Top} style={{ background: isLocked ? '#9ca3af' : '#eab308' }} />
        <span style={{ fontWeight: 600, fontSize: 13 }}>{label}</span>
        <div style={{
          fontSize: 11,
          color: '#92400e',
          marginTop: 4,
          background: '#fef3c7',
          borderRadius: 4,
          padding: '2px 6px',
          display: 'inline-block',
        }}>
          {members?.length || 0} parallel steps
        </div>
        <Handle type="source" position={Position.Bottom} style={{ background: isLocked ? '#9ca3af' : '#eab308' }} />
      </div>
    );
  }

  return (
    <div className="group-expanded" style={{ minWidth: 220 }}>
      <Handle type="target" position={Position.Top} style={{ background: '#eab308' }} />
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <span style={{ fontWeight: 600, fontSize: 13 }}>{label}</span>
        <button
          onClick={(e) => { e.stopPropagation(); setExpanded(false); }}
          style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 14, color: '#6b7280' }}
        >
          ▲
        </button>
      </div>
      {members?.map((memberId) => {
        const memberStatus = memberStatuses?.[memberId] || 'Available';
        const memberCompleted = memberStatus === 'Completed';
        const memberName = memberId.split('_')[0];
        return (
          <div
            key={memberId}
            className={`group-member ${memberCompleted ? 'completed' : ''}`}
            onClick={(e) => { e.stopPropagation(); onMemberClick?.(memberId); }}
          >
            <div className="check">{memberCompleted ? '✓' : ''}</div>
            <span style={{ fontSize: 12 }}>{humanize(memberName)}</span>
          </div>
        );
      })}
      <Handle type="source" position={Position.Bottom} style={{ background: '#eab308' }} />
    </div>
  );
}

function humanize(name) {
  return name.replace(/([A-Z])/g, ' $1').trim();
}
