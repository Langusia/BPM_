import React, { useState, useEffect, useCallback, useMemo } from 'react';
import ReactFlow, {
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  MarkerType,
} from 'reactflow';
import 'reactflow/dist/style.css';

import { fetchGraphDefinition, fetchGraph, fetchSubgraph } from './api';
import { getLayoutedElements } from './layout';
import StandardNode from './components/StandardNode';
import GroupNode from './components/GroupNode';
import JumpToNode from './components/JumpToNode';
import ConditionalEdge from './components/ConditionalEdge';
import FormPanel from './components/FormPanel';

const nodeTypes = {
  standard: StandardNode,
  group: GroupNode,
  jumpTo: JumpToNode,
};

const edgeTypes = {
  conditional: ConditionalEdge,
};

function getAggregateNameFromUrl() {
  // URL pattern: /bpm/ui/{aggregateName}
  const parts = window.location.pathname.split('/');
  const uiIndex = parts.indexOf('ui');
  if (uiIndex >= 0 && parts.length > uiIndex + 1) {
    return parts[uiIndex + 1];
  }
  return null;
}

export default function App() {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [processId, setProcessId] = useState(null);
  const [aggregateName, setAggregateName] = useState(null);
  const [selectedNode, setSelectedNode] = useState(null);
  const [breadcrumbs, setBreadcrumbs] = useState([]);
  const [nodeStatuses, setNodeStatuses] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Load initial graph definition
  useEffect(() => {
    const name = getAggregateNameFromUrl();
    if (!name) {
      setError('No aggregate name found in URL. Navigate to /bpm/ui/{aggregateName}');
      setLoading(false);
      return;
    }
    setAggregateName(name);
    setBreadcrumbs([name]);
    loadDefinition(name);
  }, []);

  async function loadDefinition(name) {
    try {
      setLoading(true);
      const data = await fetchGraphDefinition(name);
      applyGraphData(data);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  async function loadProcessGraph(pid) {
    try {
      const data = await fetchGraph(pid);
      applyGraphData(data);
    } catch (e) {
      setError(e.message);
    }
  }

  function applyGraphData(data) {
    const statuses = {};
    const rfNodes = data.nodes.map((n) => {
      statuses[n.id] = n.status;
      const type = n.nodeType === 'Group' ? 'group'
        : n.nodeType === 'JumpTo' ? 'jumpTo'
        : 'standard';

      return {
        id: n.id,
        type,
        position: { x: 0, y: 0 },
        data: {
          label: n.label,
          status: n.status,
          nodeType: n.nodeType,
          commandName: n.commandName,
          endpoint: n.endpoint,
          httpMethod: n.httpMethod,
          members: n.members,
          subGraphEndpoint: n.subGraphEndpoint,
          memberStatuses: statuses,
          onMemberClick: (memberId) => handleMemberClick(memberId, data.nodes),
          onExpand: n.subGraphEndpoint ? () => handleJumpToExpand(n) : undefined,
        },
      };
    });

    const rfEdges = data.edges.map((e) => ({
      id: e.id,
      source: e.source,
      target: e.target,
      type: e.conditionMet !== null && e.conditionMet !== undefined ? 'conditional' : 'default',
      data: { conditionMet: e.conditionMet },
      markerEnd: { type: MarkerType.ArrowClosed, color: '#9ca3af' },
      style: {
        stroke: e.conditionMet === true ? '#22c55e'
          : e.conditionMet === false ? '#ef4444'
          : '#9ca3af',
        strokeWidth: 2,
      },
      animated: false,
    }));

    const { nodes: layoutedNodes, edges: layoutedEdges } = getLayoutedElements(rfNodes, rfEdges);

    setNodes(layoutedNodes);
    setEdges(layoutedEdges);
    setNodeStatuses(statuses);
  }

  function handleMemberClick(memberId, allNodes) {
    const memberNode = allNodes?.find((n) => n.id === memberId);
    if (!memberNode || nodeStatuses[memberId] === 'Completed' || nodeStatuses[memberId] === 'Locked') return;
    setSelectedNode({
      commandName: memberNode.commandName,
      endpoint: memberNode.endpoint,
      httpMethod: memberNode.httpMethod,
    });
  }

  async function handleJumpToExpand(node) {
    if (!processId || !node.subGraphEndpoint) return;
    try {
      const url = node.subGraphEndpoint.replace('{processId}', processId);
      const parts = url.split('/');
      const guestType = parts[parts.length - 2];
      const data = await fetchSubgraph(guestType, processId);
      setBreadcrumbs((prev) => [...prev, data.aggregateName || guestType]);
      applyGraphData(data);
    } catch (e) {
      setError(e.message);
    }
  }

  const onNodeClick = useCallback((event, node) => {
    const { status, commandName, endpoint, httpMethod, nodeType } = node.data;
    if (status === 'Locked' || status === 'Completed') return;

    if (nodeType === 'JumpTo') {
      node.data.onExpand?.();
      return;
    }

    if (nodeType === 'Group') {
      // Group expansion is handled by the GroupNode component internally
      return;
    }

    setSelectedNode({ commandName, endpoint, httpMethod });
  }, []);

  function handleCommandSuccess(result) {
    // Capture processId from StartWith command
    if (!processId && result.processId) {
      setProcessId(result.processId);
    }

    const pid = result.processId || processId;

    // Update node statuses
    setNodes((prev) =>
      prev.map((n) => {
        // The submitted command's node becomes Completed
        if (n.data.commandName === selectedNode?.commandName && n.data.status === 'Available') {
          return {
            ...n,
            data: { ...n.data, status: 'Completed', memberStatuses: { ...n.data.memberStatuses } },
          };
        }

        const newStatus = result.availableStepIds?.includes(n.id)
          ? 'Available'
          : n.data.status === 'Completed'
            ? 'Completed'
            : 'Locked';

        return {
          ...n,
          data: { ...n.data, status: newStatus, memberStatuses: { ...n.data.memberStatuses } },
        };
      })
    );

    // Update statuses map
    setNodeStatuses((prev) => {
      const updated = { ...prev };
      // Mark submitted node as completed
      for (const key of Object.keys(updated)) {
        const commandPart = key.split('_')[0];
        if (commandPart === selectedNode?.commandName && updated[key] === 'Available') {
          updated[key] = 'Completed';
        }
      }
      // Update available steps
      for (const key of Object.keys(updated)) {
        if (result.availableStepIds?.includes(key)) {
          if (updated[key] !== 'Completed') updated[key] = 'Available';
        } else if (updated[key] === 'Available') {
          // Was available but no longer
          const commandPart = key.split('_')[0];
          if (commandPart !== selectedNode?.commandName) {
            updated[key] = 'Locked';
          }
        }
      }
      // Add any new available step IDs
      for (const stepId of result.availableStepIds || []) {
        if (!updated[stepId] || updated[stepId] === 'Locked') {
          updated[stepId] = 'Available';
        }
      }
      return updated;
    });

    setSelectedNode(null);

    // Optionally reload full graph from server for accuracy
    if (pid) {
      setTimeout(() => loadProcessGraph(pid), 300);
    }
  }

  function handleBreadcrumbClick(index) {
    if (index === 0) {
      // Go back to root
      setBreadcrumbs((prev) => [prev[0]]);
      if (processId) {
        loadProcessGraph(processId);
      } else {
        loadDefinition(aggregateName);
      }
    }
  }

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', color: '#6b7280' }}>
        Loading process graph...
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', color: '#ef4444' }}>
        {error}
      </div>
    );
  }

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      {/* Breadcrumb */}
      {breadcrumbs.length > 0 && (
        <div className="breadcrumb">
          {breadcrumbs.map((name, i) => (
            <React.Fragment key={i}>
              {i > 0 && <span style={{ color: '#d1d5db', margin: '0 2px' }}>&gt;</span>}
              <span
                onClick={() => handleBreadcrumbClick(i)}
                style={i < breadcrumbs.length - 1 ? { cursor: 'pointer', color: '#3b82f6' } : { color: '#1a1a2e' }}
              >
                {name}
              </span>
            </React.Fragment>
          ))}
        </div>
      )}

      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={onNodeClick}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        fitView
        fitViewOptions={{ padding: 0.2 }}
        minZoom={0.2}
        maxZoom={2}
        nodesDraggable={false}
        nodesConnectable={false}
      >
        <Controls />
        <Background variant="dots" gap={16} size={1} color="#e5e7eb" />
      </ReactFlow>

      {/* Slide-in panel */}
      {selectedNode && (
        <FormPanel
          commandName={selectedNode.commandName}
          endpoint={selectedNode.endpoint}
          httpMethod={selectedNode.httpMethod}
          processId={processId}
          onSuccess={handleCommandSuccess}
          onClose={() => setSelectedNode(null)}
        />
      )}
    </div>
  );
}
