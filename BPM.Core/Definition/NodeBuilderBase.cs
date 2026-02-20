using System.Collections.Generic;
using System.Linq;
using BPM.Core.Nodes;
using BPM.Core.Process;

namespace BPM.Core.Definition;

public class NodeBuilderBase(INode firstNode, BProcess process)
{
    protected readonly BProcess ProcessConfig = process;
    protected INode CurrentNode = firstNode;
    public List<INode> CurrentBranchInstances = [firstNode];


    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => firstNode;


    protected INode GetConfiguredProcessRootReverse()
    {
        var currentNodeSet = CurrentBranchInstances;
        List<INode> result = [];
        HashSet<INode> visited = [];
        int levelCounter = 0;
        IterateConfiguredProcessRoot(currentNodeSet, result, visited, 0);
        return result.FirstOrDefault()!;
    }

    private void IterateAllPossibles(INode root, List<List<INode>> results)
    {
        int ct = 0;
        var init = new List<INode>(results.Last());
        foreach (var t in root.NextSteps)
        {
            if (ct == 0)
                results.Last().Add(t);
            else
            {
                var nextResultSet = new List<INode>(init) { t };
                results.Add(nextResultSet);
            }

            IterateAllPossibles(t, results);
            ct++;
        }
    }

    protected void IterateConfiguredProcessRoot(List<INode> currentNodeSet, List<INode> res, HashSet<INode> visited, int levelCounter)
    {
        foreach (var currentBranchInstance in currentNodeSet)
        {
            if (currentBranchInstance is null)
                continue;
            currentBranchInstance.NodeLevel = levelCounter;
            if (currentBranchInstance.PrevSteps is null)
            {
                res.Add(currentBranchInstance);
                continue;
            }

            foreach (var currentBranchInstancePrevStep in currentBranchInstance.PrevSteps)
            {
                if (currentBranchInstancePrevStep is null)
                {
                    res.Add(currentBranchInstance);
                    continue;
                }

                if (!currentBranchInstancePrevStep.NextSteps!.Contains(currentBranchInstance))
                {
                    currentBranchInstancePrevStep.AddNextStep(currentBranchInstance);
                }
            }

            IterateConfiguredProcessRoot(currentBranchInstance.PrevSteps!, res, visited, ++levelCounter);
        }
    }
}
