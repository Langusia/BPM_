using BPM.Core.Configuration;
using BPM.Core.Nodes;
using BPM.Core.Process;
using BPM.UI.Configuration;

namespace BPM.UI.Endpoints;

public static class GraphBuilder
{
    /// <summary>
    /// Build a pure graph definition with no process state.
    /// All nodes are Locked except the root StartWith node which is Available.
    /// </summary>
    public static GraphResponse BuildDefinition(string aggregateName, BpmUiConfiguration uiConfig)
    {
        var config = BProcessGraphConfiguration.GetConfig(aggregateName);
        if (config is null)
            return new GraphResponse { AggregateName = aggregateName };

        var rootNode = config.RootNode;
        var response = new GraphResponse
        {
            AggregateName = aggregateName,
            ProcessId = null
        };

        var visited = new HashSet<INode>();

        void TraverseNode(INode node, bool isRoot)
        {
            if (visited.Contains(node)) return;
            visited.Add(node);

            if (node.CommandType == typeof(ConditionalNode))
            {
                var condNode = (ConditionalNode)node;
                var parentId = node.PrevSteps?.FirstOrDefault() is { } prev
                    ? GetNodeId(prev) : null;

                foreach (var ifRoot in condNode.IfNodeRoots ?? [])
                {
                    if (parentId is not null)
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{parentId}-{GetNodeId(ifRoot)}",
                            Source = parentId,
                            Target = GetNodeId(ifRoot),
                            ConditionMet = true
                        });
                    }
                    TraverseNode(ifRoot, false);
                }

                foreach (var elseRoot in condNode.ElseNodeRoots ?? [])
                {
                    if (parentId is not null)
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{parentId}-{GetNodeId(elseRoot)}",
                            Source = parentId,
                            Target = GetNodeId(elseRoot),
                            ConditionMet = false
                        });
                    }
                    TraverseNode(elseRoot, false);
                }

                foreach (var next in node.NextSteps ?? [])
                    TraverseNode(next, false);
                return;
            }

            if (node.CommandType == typeof(GroupNode))
            {
                var groupNode = (GroupNode)node;
                var nodeId = $"Group_{node.NodeLevel}";

                response.Nodes.Add(new GraphNode
                {
                    Id = nodeId,
                    CommandName = "Group",
                    Label = "Group",
                    Status = "Locked",
                    NodeType = "Group",
                    Members = groupNode.SubRootNodes.Select(m => GetNodeId(m)).ToList()
                });

                foreach (var member in groupNode.SubRootNodes)
                    TraverseNode(member, false);

                foreach (var next in node.NextSteps ?? [])
                {
                    if (next.CommandType != typeof(ConditionalNode))
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{nodeId}-{GetNodeId(next)}",
                            Source = nodeId,
                            Target = GetNodeId(next)
                        });
                    }
                    TraverseNode(next, false);
                }
                return;
            }

            if (node.CommandType == typeof(GuestProcessNode))
            {
                var guestNode = (GuestProcessNode)node;
                var nodeId = $"{guestNode.GuestProcessType.Name}_{node.NodeLevel}";

                response.Nodes.Add(new GraphNode
                {
                    Id = nodeId,
                    CommandName = guestNode.GuestProcessType.Name,
                    Label = HumanizeName(guestNode.GuestProcessType.Name),
                    Status = "Locked",
                    NodeType = "JumpTo",
                    SubGraphEndpoint = $"/bpm/graph/subgraph/{guestNode.GuestProcessType.Name}/{{processId}}"
                });

                foreach (var next in node.NextSteps ?? [])
                {
                    if (next.CommandType != typeof(ConditionalNode))
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{nodeId}-{GetNodeId(next)}",
                            Source = nodeId,
                            Target = GetNodeId(next)
                        });
                    }
                    TraverseNode(next, false);
                }
                return;
            }

            // Standard, Optional, AnyTime nodes
            var id = GetNodeId(node);
            var nodeType = ResolveNodeType(node);
            var status = isRoot ? "Available" : "Locked";
            var endpoint = uiConfig.ResolveEndpoint(node.CommandType.Name, config.ProcessType);

            response.Nodes.Add(new GraphNode
            {
                Id = id,
                CommandName = node.CommandType.Name,
                Label = HumanizeName(node.CommandType.Name),
                Status = status,
                NodeType = nodeType,
                Endpoint = endpoint,
                HttpMethod = "POST"
            });

            foreach (var next in node.NextSteps ?? [])
            {
                if (next.CommandType == typeof(ConditionalNode))
                {
                    TraverseNode(next, false);
                }
                else
                {
                    response.Edges.Add(new GraphEdge
                    {
                        Id = $"{id}-{GetNodeId(next)}",
                        Source = id,
                        Target = GetNodeId(next)
                    });
                    TraverseNode(next, false);
                }
            }
        }

        TraverseNode(rootNode, isRoot: true);

        return response;
    }

    /// <summary>
    /// Build graph with current process state overlaid.
    /// </summary>
    public static GraphResponse Build(IProcess process, BpmUiConfiguration uiConfig)
    {
        var config = BProcessGraphConfiguration.GetConfig(process.AggregateName);
        if (config is null)
            return new GraphResponse { AggregateName = process.AggregateName, ProcessId = process.Id };

        var rootNode = config.RootNode;
        var allNodes = rootNode.GetAllNodes();

        var stream = process.StoredEvents.Union(process.UncommittedEvents.ToList()).ToList();
        var evalResult = rootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        var availableNodeSet = new HashSet<INode>(evalResult.availableNodes);

        var completedNodes = new HashSet<INode>();
        foreach (var node in allNodes)
        {
            if (node.CommandType == typeof(ConditionalNode)) continue;
            if (node.CommandType == typeof(GroupNode)) continue;
            if (node.CommandType == typeof(GuestProcessNode)) continue;

            var evaluator = node.GetEvaluator();
            if (evaluator.IsCompleted(stream))
                completedNodes.Add(node);
        }

        var response = new GraphResponse
        {
            AggregateName = process.AggregateName,
            ProcessId = process.Id
        };
        var visited = new HashSet<INode>();

        void TraverseNode(INode node)
        {
            if (visited.Contains(node)) return;
            visited.Add(node);

            if (node.CommandType == typeof(ConditionalNode))
            {
                var condNode = (ConditionalNode)node;
                var parentId = node.PrevSteps?.FirstOrDefault() is { } prev
                    ? GetNodeId(prev) : null;

                foreach (var ifRoot in condNode.IfNodeRoots ?? [])
                {
                    if (parentId is not null)
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{parentId}-{GetNodeId(ifRoot)}",
                            Source = parentId,
                            Target = GetNodeId(ifRoot),
                            ConditionMet = true
                        });
                    }
                    TraverseNode(ifRoot);
                }

                foreach (var elseRoot in condNode.ElseNodeRoots ?? [])
                {
                    if (parentId is not null)
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{parentId}-{GetNodeId(elseRoot)}",
                            Source = parentId,
                            Target = GetNodeId(elseRoot),
                            ConditionMet = false
                        });
                    }
                    TraverseNode(elseRoot);
                }

                foreach (var next in node.NextSteps ?? [])
                    TraverseNode(next);
                return;
            }

            if (node.CommandType == typeof(GroupNode))
            {
                var groupNode = (GroupNode)node;
                var nodeId = $"Group_{node.NodeLevel}";
                var memberIds = groupNode.SubRootNodes.Select(m => GetNodeId(m)).ToList();

                var status = completedNodes.Contains(node) ? "Completed"
                    : availableNodeSet.Contains(node) ? "Available"
                    : "Locked";

                response.Nodes.Add(new GraphNode
                {
                    Id = nodeId,
                    CommandName = "Group",
                    Label = "Group",
                    Status = status,
                    NodeType = "Group",
                    Members = memberIds
                });

                foreach (var member in groupNode.SubRootNodes)
                    TraverseNode(member);

                foreach (var next in node.NextSteps ?? [])
                {
                    if (next.CommandType != typeof(ConditionalNode))
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{nodeId}-{GetNodeId(next)}",
                            Source = nodeId,
                            Target = GetNodeId(next)
                        });
                    }
                    TraverseNode(next);
                }
                return;
            }

            if (node.CommandType == typeof(GuestProcessNode))
            {
                var guestNode = (GuestProcessNode)node;
                var nodeId = $"{guestNode.GuestProcessType.Name}_{node.NodeLevel}";

                var status = completedNodes.Contains(node) ? "Completed"
                    : availableNodeSet.Contains(node) ? "Available"
                    : "Locked";

                response.Nodes.Add(new GraphNode
                {
                    Id = nodeId,
                    CommandName = guestNode.GuestProcessType.Name,
                    Label = HumanizeName(guestNode.GuestProcessType.Name),
                    Status = status,
                    NodeType = "JumpTo",
                    SubGraphEndpoint = $"/bpm/graph/subgraph/{guestNode.GuestProcessType.Name}/{process.Id}"
                });

                foreach (var next in node.NextSteps ?? [])
                {
                    if (next.CommandType != typeof(ConditionalNode))
                    {
                        response.Edges.Add(new GraphEdge
                        {
                            Id = $"{nodeId}-{GetNodeId(next)}",
                            Source = nodeId,
                            Target = GetNodeId(next)
                        });
                    }
                    TraverseNode(next);
                }
                return;
            }

            // Standard, Optional, AnyTime nodes
            var id = GetNodeId(node);
            var nodeType = ResolveNodeType(node);

            var nodeStatus = completedNodes.Contains(node) ? "Completed"
                : availableNodeSet.Contains(node) ? "Available"
                : "Locked";

            var endpoint = uiConfig.ResolveEndpoint(node.CommandType.Name, config.ProcessType);

            response.Nodes.Add(new GraphNode
            {
                Id = id,
                CommandName = node.CommandType.Name,
                Label = HumanizeName(node.CommandType.Name),
                Status = nodeStatus,
                NodeType = nodeType,
                Endpoint = endpoint,
                HttpMethod = "POST"
            });

            foreach (var next in node.NextSteps ?? [])
            {
                if (next.CommandType == typeof(ConditionalNode))
                {
                    TraverseNode(next);
                }
                else
                {
                    response.Edges.Add(new GraphEdge
                    {
                        Id = $"{id}-{GetNodeId(next)}",
                        Source = id,
                        Target = GetNodeId(next)
                    });
                    TraverseNode(next);
                }
            }
        }

        TraverseNode(rootNode);

        return response;
    }

    private static string GetNodeId(INode node)
    {
        if (node.CommandType == typeof(GroupNode))
            return $"Group_{node.NodeLevel}";
        if (node.CommandType == typeof(GuestProcessNode))
            return $"{((GuestProcessNode)node).GuestProcessType.Name}_{node.NodeLevel}";
        return $"{node.CommandType.Name}_{node.NodeLevel}";
    }

    private static string ResolveNodeType(INode node)
    {
        return node switch
        {
            OptionalNode => "Optional",
            AnyTimeNode => "AnyTime",
            _ => "Standard"
        };
    }

    private static string HumanizeName(string name)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                result.Append(' ');
            result.Append(i == 0 ? char.ToUpper(c) : c);
        }
        return result.ToString();
    }
}
