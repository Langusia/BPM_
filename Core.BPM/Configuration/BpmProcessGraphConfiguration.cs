using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

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

    public static void AddProcess<T>(BpmProcess<T> processToAdd) where T : IProcess
    {
        _processes ??= new List<BpmProcess?>();
        _processes.Add(processToAdd);
    }
}