namespace Core.BPM.Interfaces;

public interface INodeState
{
    bool DefinitionValidated { get; set; }
    bool CanAppend { get; set; }
}