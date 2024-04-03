using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

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