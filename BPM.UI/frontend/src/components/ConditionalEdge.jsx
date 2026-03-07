import React from 'react';
import { BaseEdge, EdgeLabelRenderer, getBezierPath } from 'reactflow';

export default function ConditionalEdge({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  data,
  markerEnd,
}) {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
  });

  const conditionMet = data?.conditionMet;

  return (
    <>
      <BaseEdge
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          stroke: conditionMet === true ? '#22c55e' : conditionMet === false ? '#ef4444' : '#9ca3af',
          strokeWidth: 2,
        }}
      />
      {conditionMet !== null && conditionMet !== undefined && (
        <EdgeLabelRenderer>
          <div
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${labelX}px,${labelY}px)`,
              pointerEvents: 'all',
            }}
          >
            <div
              className={`edge-badge ${conditionMet}`}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                width: 22,
                height: 22,
                borderRadius: '50%',
                fontSize: 12,
                fontWeight: 'bold',
                color: 'white',
                background: conditionMet ? '#22c55e' : '#ef4444',
              }}
            >
              {conditionMet ? '✓' : '✗'}
            </div>
          </div>
        </EdgeLabelRenderer>
      )}
    </>
  );
}
