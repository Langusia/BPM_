using System.ComponentModel;
using System.Xml.Linq;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;
using Core.BPM.MediatR.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Core.BPM.Configuration;

public static class BProcessGraphConfiguration
{
    private static List<BProcess>? _processes;
    public static List<BpmProcessEventOptions> EventOptions = [];

    public static BpmProcessEventOptions? GetEventConfig<TProcess>() where TProcess : IAggregate
        => EventOptions.FirstOrDefault(x => x?.ProcessName == typeof(TProcess).Name);

    public static bool CheckTryCount<TCommand>(this BpmProcessEventOptions opts, Aggregate aggregate)
    {
        var currentCommandConfig = opts.BpmCommandtOptions.FirstOrDefault(x => x.BpmCommandName == typeof(TCommand).Name);
        if (currentCommandConfig is null || currentCommandConfig.PermittedTryCount is null || currentCommandConfig.PermittedTryCount == 0)
            return true;
        if (!aggregate.EventCounters.ContainsKey(typeof(TCommand).Name))
            return true;

        var possibleCommandEventNames = GetCommandProducer<TCommand>().EventTypes.Select(x => x.Name);
        var currentCount = aggregate.EventCounters.Count(x => possibleCommandEventNames.Contains(x.Key));
        return currentCommandConfig.PermittedTryCount != currentCount;
    }

    public static bool CheckTryCount<TCommand>(this BpmProcessEventOptions opts, List<string> persistedEvents)
    {
        var currentCommandConfig = opts.BpmCommandtOptions.FirstOrDefault(x => x.BpmCommandName == typeof(TCommand).Name);
        if (currentCommandConfig is null || currentCommandConfig.PermittedTryCount is null || currentCommandConfig.PermittedTryCount == 0)
            return true;

        var currentCount = persistedEvents.Count(x => x == typeof(TCommand).Name);
        return currentCommandConfig.PermittedTryCount != currentCount;
    }

    public static bool CheckPathValid<TCommand>(this BProcess config, List<string> persistedEvents)
    {
        var currentCommandType = typeof(TCommand);

        var currentNodeConfig = config.MoveTo(currentCommandType);
        var incomingPrevCommandPossibleEvents = currentNodeConfig.SelectMany(x => x.PrevSteps?.Select(z => GetCommandProducer(z.CommandType))).SelectMany(x => x.EventTypes).ToList();
        var lastPersistedEventName = persistedEvents.Last();

        return incomingPrevCommandPossibleEvents.Any(x => persistedEvents.Contains(x.Name));
    }

    public static bool CheckPathValid<TCommand>(Aggregate aggregate)
    {
        var currentCommandType = typeof(TCommand);
        var config = GetConfig(aggregate.GetType());
        if (config is null)
            throw new InvalidEnumArgumentException($"process named {aggregate.GetType().Name} does not exist in the configuration.");

        var currentNodeConfig = config.MoveTo(currentCommandType);
        var incomingPrevCommandPossibleEvents = currentNodeConfig.SelectMany(x => x.PrevSteps?.Select(z => GetCommandProducer(z.CommandType))).SelectMany(x => x.EventTypes).ToList();
        var lastPersistedEventName = aggregate.PersistedEvents.Last();

        return incomingPrevCommandPossibleEvents.All(x => x.Name != lastPersistedEventName);
    }

    private static BpmProducer GetCommandProducer<TCommand>()
    {
        return (BpmProducer)typeof(TCommand).GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }


    public static BpmProcessEventOptions? GetEventConfig(string processName)
    {
        var config = EventOptions.FirstOrDefault(x => x?.ProcessName == processName);
        //if (config is null)
        //    throw new InvalidEnumArgumentException($"process named {processName} does not exist in the configuration.");

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

    public static INode? MoveTo(this BProcess process, List<string> persistedEvents)
    {
        persistedEvents = persistedEvents.Distinct().ToList();
        var result = new List<INode>();
        if (GetCommandProducer(process.RootNode.CommandType).EventTypes.All(x => x.Name != persistedEvents.FirstOrDefault()))
            return null;
        persistedEvents.RemoveAt(0);
        if (persistedEvents.Count == 0)
            return process.RootNode;

        return MoveTo(process.RootNode, persistedEvents);
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

    private static INode? MoveTo(INode currNode, List<string> persistedEvents)
    {
        var nextCurrNode = currNode.NextSteps?.FirstOrDefault(x=>GetCommandProducer(x.CommandType).EventTypes.Any(y => y.Name == persistedEvents.FirstOrDefault()));
        if (nextCurrNode is null)
            return null;
        
        persistedEvents.RemoveAt(0);
        if(persistedEvents.Count == 0)
            return nextCurrNode;

        return MoveTo(nextCurrNode, persistedEvents);
    }

    public static BProcess? GetConfig(Type processType)
    {
        var bpmProcess = _processes?.FirstOrDefault(x => x?.ProcessType == processType);
        //if (bpmProcess is null)
        //    throw new InvalidEnumArgumentException($"process named {processType.Name} does not exist in the configuration.");

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