using Core.BPM.Interfaces;

namespace Core.BPM;

public abstract class BpmProcessGraphDefinition<TProcess>
    where TProcess : IProcess
{
    public abstract void Define(BpmProcessGraphConfigurator<TProcess> configure);
}

public class BpmProcessGraphConfigurator<TProcess> where TProcess : IProcess
{
    public BpmNode<TProcess, T> SetRootNode<T>() where T : IEvent
    {
        var inst = new BpmNode<TProcess, T>();
        BpmProcessGraphConfiguration.AddProcess(new BpmProcess(typeof(TProcess), inst));
        return inst;
    }

    public BpmProcess<TProcess> ProcessSetRootNode<T>() where T : IEvent => new(new BpmNode<TProcess, T>());
}

public static class BpmProcessGraphConfiguration
{
    private static List<BpmProcess?> _processes;

    public static BpmProcess? GetConfig<TProcess>()
    {
        return _processes.FirstOrDefault(x => x?.ProcessType == typeof(TProcess));
    }

    public static void AddProcess(BpmProcess? processToAdd)
    {
        _processes ??= new List<BpmProcess?>();
        _processes.Add(processToAdd);
    }
}