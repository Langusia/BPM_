using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public interface INodeBuilder
{
    BProcess GetProcess();
    INode GetCurrent();
}