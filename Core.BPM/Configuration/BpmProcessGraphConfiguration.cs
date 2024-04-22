using System.ComponentModel;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

public static class BProcessGraphConfiguration
{
    private static List<BProcess>? _processes;
    public static List<BpmProcessEventOptions> EventOptions = [];

    public static BpmProcessEventOptions GetEventConfig<TProcess>() where TProcess : IAggregate
    {
        var config = EventOptions.FirstOrDefault(x => x?.ProcessName == typeof(TProcess).Name);
        if (config is null)
            throw new InvalidEnumArgumentException($"process named {typeof(TProcess).Name} does not exist in the configuration.");

        return config;
    }

    public static BpmProcessEventOptions GetEventConfig(string processName)
    {
        var config = EventOptions.FirstOrDefault(x => x?.ProcessName == processName);
        if (config is null)
            throw new InvalidEnumArgumentException($"process named {processName} does not exist in the configuration.");

        return config;
    }

    public static BProcess GetConfig<TProcess>() where TProcess : IAggregate
    {
        var bpmProcess = _processes?.FirstOrDefault(x => x?.ProcessType == typeof(TProcess));
        if (bpmProcess is null)
            throw new InvalidEnumArgumentException($"process named {typeof(TProcess).Name} does not exist in the configuration.");

        return bpmProcess;
    }


    public static List<INode> MoveTo<TCommand>(this BProcess process) => MoveTo(process, typeof(TCommand));

    public static List<INode> MoveTo(this BProcess process, Type commandType)
    {
        var result = new List<INode>();
        MoveTo(process.RootNode, commandType, result);

        return result;
    }

    private static void MoveTo(INode currNode, Type commandType, List<INode> result)
    {
        foreach (var step in currNode.NextSteps!)
        {
            if (step.CommandType == commandType)
                result.Add(step);
            else
                MoveTo(step, commandType, result);
        }
    }

    public static BProcess GetConfig(Type processType)
    {
        var bpmProcess = _processes?.FirstOrDefault(x => x?.ProcessType == processType);
        if (bpmProcess is null)
            throw new InvalidEnumArgumentException($"process named {processType.Name} does not exist in the configuration.");

        return bpmProcess;
    }

    public static BProcess GetConfig(string processTypeFullName)
    {
        var bpmProcess = _processes?.FirstOrDefault(x => x?.ProcessType.Name == processTypeFullName);
        if (bpmProcess is null)
            throw new InvalidEnumArgumentException($"process named {processTypeFullName} does not exist in the configuration.");

        return bpmProcess;
    }


    public static void AddProcess(BProcess processToAdd)
    {
        _processes ??= [];
        if (_processes.Any(x => x.ProcessType == processToAdd.ProcessType))
            throw new InvalidEnumArgumentException($"process named {processToAdd.ProcessType.Name} already exists in the configuration.");

        _processes.Add(processToAdd);
    }
}