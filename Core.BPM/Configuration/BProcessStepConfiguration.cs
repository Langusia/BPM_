﻿using System.ComponentModel;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

public static class BProcessStepConfiguration
{
    private static List<StepOptions>? _stepOptions = [];

    public static void AddStepConfig(StepOptions stepOptionsToAdd)
    {
        _stepOptions ??= [];
        if (_stepOptions.Any(x => x.AggregateType == stepOptionsToAdd.AggregateType && x.CommandType == stepOptionsToAdd.CommandType))
            throw new InvalidEnumArgumentException($"step already configured.");

        _stepOptions.Add(stepOptionsToAdd);
    }

    public static StepOptions? GetConfig<TProcess>() where TProcess : IAggregate =>
        _stepOptions?.FirstOrDefault(x => x?.AggregateType == typeof(TProcess));

    public static StepOptions? GetConfig(Type processType) =>
        _stepOptions?.FirstOrDefault(x => x?.AggregateType == processType);
}