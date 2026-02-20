using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BPM.Core.Attributes;
using BPM.Core.Process;

namespace BPM.Core.Configuration;

public static class BProcessGraphConfiguration
{
    private static List<BProcess>? _processes;

    public static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    public static BProcess GetConfig<TProcess>() where TProcess : IAggregate
    {
        var bpmProcess = _processes?.FirstOrDefault(x => x?.ProcessType == typeof(TProcess));
        if (bpmProcess is null)
            throw new InvalidEnumArgumentException($"process named {typeof(TProcess).Name} does not exist in the configuration.");

        return bpmProcess;
    }

    public static BProcess? GetConfig(string processTypeFullName)
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
