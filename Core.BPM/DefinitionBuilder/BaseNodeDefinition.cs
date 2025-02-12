using Core.BPM.Exceptions;
using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class BaseNodeDefinition(INode firstNode, BProcess process)
{
    protected readonly BProcess ProcessConfig = process;
    protected INode CurrentNode = firstNode;
    public List<INode> CurrentBranchInstances = [firstNode];


    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => firstNode;

    protected List<List<INode>> GetAllPossibles(INode rootNode)
    {
        List<List<INode>> result = [[rootNode]];
        IterateAllPossibles(rootNode, result);
        return result;
    }


    protected Tuple<INode, List<INode>> GetConfiguredProcessRootReverse()
    {
        var currentNodeSet = CurrentBranchInstances;
        List<INode> result = [];
        List<INode> distresult = [];
        IterateConfiguredProcessRoot(currentNodeSet, result, distresult);
        return new Tuple<INode, List<INode>>(result.FirstOrDefault()!, distresult);
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

    protected void IterateConfiguredProcessRoot(List<INode> currentNodeSet, List<INode> res, List<INode> distinctNodeSet)
    {
        foreach (var currentBranchInstance in currentNodeSet)
        {
            if (currentBranchInstance.PrevSteps is null)
            {
                res.Add(currentBranchInstance);
                if (!distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() == currentBranchInstance.GetType()))
                    distinctNodeSet.Add(currentBranchInstance);
                continue;
            }

            foreach (var currentBranchInstancePrevStep in currentBranchInstance.PrevSteps)
            {
                if (!currentBranchInstancePrevStep.NextSteps!.Contains(currentBranchInstance))
                {
                    if (currentBranchInstancePrevStep.NextSteps.Select(x => x.CommandType).Contains(currentBranchInstance.CommandType))
                        throw new SameCommandOnSameLevelDiffBranchFoundException(currentBranchInstance.CommandType.Name);

                    if (!distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() == currentBranchInstance.GetType()))
                        distinctNodeSet.Add(currentBranchInstance);
                    if (distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() != currentBranchInstance.GetType()))
                        throw new SameCommandDiffNodeTypeException(currentBranchInstance.CommandType.Name);

                    currentBranchInstancePrevStep.AddNextStep(currentBranchInstance);
                }
            }

            IterateConfiguredProcessRoot(currentBranchInstance.PrevSteps!, res, distinctNodeSet);
        }
    }
}