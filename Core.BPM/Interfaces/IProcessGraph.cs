namespace Core.BPM.Interfaces;

public interface IProcessGraph<TProcess>
{
    public INode RootNode { get; }
}