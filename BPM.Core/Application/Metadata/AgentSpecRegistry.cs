using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPM.Contracts;

namespace BPM.Core.Application.Metadata;

/// <summary>
/// Holds the fluent agent-spec data collected from <see cref="IAgentSpec{TCommand}"/>
/// implementations, keyed by command type. Populated by assembly scan at startup.
/// </summary>
public sealed class AgentSpecRegistry
{
    private readonly Dictionary<Type, AgentSpecData> _specs = new();

    public AgentSpecData? Find(Type commandType) =>
        _specs.TryGetValue(commandType, out var data) ? data : null;

    public void Add(Type commandType, AgentSpecData data)
    {
        if (_specs.ContainsKey(commandType))
            throw new InvalidOperationException(
                $"An agent spec for command '{commandType.Name}' is already registered. " +
                "Keep exactly one IAgentSpec implementation per command.");
        _specs[commandType] = data;
    }

    /// <summary>
    /// Scans an assembly for <see cref="IAgentSpec{TCommand}"/> implementations,
    /// runs each spec's Configure and stores the collected data
    /// (FluentValidation / EF IEntityTypeConfiguration discovery pattern).
    /// </summary>
    public void AddFromAssembly(Assembly assembly)
    {
        var specTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAgentSpec<>))
                .Select(i => (SpecType: t, CommandType: i.GetGenericArguments()[0])));

        foreach (var (specType, commandType) in specTypes)
        {
            var spec = Activator.CreateInstance(specType)
                       ?? throw new InvalidOperationException(
                           $"Agent spec '{specType.Name}' must have a public parameterless constructor.");

            var builderType = typeof(AgentSpecBuilder<>).MakeGenericType(commandType);
            var builder = Activator.CreateInstance(builderType)!;

            var configure = specType.GetMethod(
                nameof(IAgentSpec<object>.Configure),
                [builderType])!;
            configure.Invoke(spec, [builder]);

            var data = (AgentSpecData)builderType
                .GetProperty(nameof(AgentSpecBuilder<object>.Data))!
                .GetValue(builder)!;

            Add(commandType, data);
        }
    }
}
