using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

public static class BpmProcessGraphConfiguration
{
    public static List<BpmProcess?> _processes;

    public static BpmProcess<TProcess>? GetConfig<TProcess>() where TProcess : IAggregate
    {
        var bpmProcess = _processes.FirstOrDefault(x => x?.ProcessType == typeof(TProcess));
        return bpmProcess as BpmProcess<TProcess>;
    }

    public static void AddProcess(BpmProcess? processToAdd)
    {
        _processes ??= new List<BpmProcess?>();
        _processes.Add(processToAdd);
    }

    public static void AddProcess<T>(BpmProcess<T> processToAdd) where T : IAggregate
    {
        _processes ??= new List<BpmProcess?>();
        _processes.Add(processToAdd);
    }
}

public static class BProcessGraphConfiguration
{
    public static List<BProcess?> _processes;

    public static BProcess? GetConfig<TProcess>() where TProcess : IAggregate
    {
        var bpmProcess = _processes.FirstOrDefault(x => x?.ProcessType == typeof(TProcess));
        return bpmProcess;
    }

    public static void AddProcess(BProcess? processToAdd)
    {
        _processes ??= new List<BProcess?>();
        _processes.Add(processToAdd);
    }
    
}