export async function fetchGraphDefinition(aggregateName) {
  const res = await fetch(`/bpm/graph/definition/${aggregateName}`);
  if (!res.ok) throw new Error(`Failed to fetch graph definition: ${res.status}`);
  return res.json();
}

export async function fetchGraph(processId) {
  const res = await fetch(`/bpm/graph/${processId}`);
  if (!res.ok) throw new Error(`Failed to fetch graph: ${res.status}`);
  return res.json();
}

export async function fetchSchema(commandName) {
  const res = await fetch(`/bpm/schema/${commandName}`);
  if (!res.ok) throw new Error(`Failed to fetch schema: ${res.status}`);
  return res.json();
}

export async function executeCommand(commandName, data) {
  const res = await fetch(`/bpm/execute/${commandName}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) throw new Error(`Failed to execute command: ${res.status}`);
  return res.json();
}

export async function fetchSubgraph(guestProcessType, processId) {
  const res = await fetch(`/bpm/graph/subgraph/${guestProcessType}/${processId}`);
  if (!res.ok) throw new Error(`Failed to fetch subgraph: ${res.status}`);
  return res.json();
}
