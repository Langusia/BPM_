namespace Core.BPM.Interfaces.Builder;

public interface INodeBuilder
{
    BProcess GetProcess();
    INode GetCurrent();
}