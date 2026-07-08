using System;
using System.Collections.Generic;
using System.Linq;
using BPM.Contracts;
using BPM.Core.Configuration;
using BPM.Core.Process;

namespace BPM.Core.Application.Catalog;

/// <summary>
/// Immutable index of every registered process graph: aggregate types, their
/// commands, and which commands can start a new instance. Built once from the
/// registered process definitions; shared by all adapters (MCP, future UI).
/// </summary>
public interface ICommandCatalog
{
    IReadOnlyList<ProcessTypeDescriptor> ProcessTypes { get; }

    ProcessTypeDescriptor? FindProcessType(string aggregateTypeName);

    /// <summary>
    /// Resolves a command by name (case-insensitive, optionally qualified by
    /// aggregate type). Returns all matches; more than one means the name is
    /// ambiguous across process graphs.
    /// </summary>
    IReadOnlyList<CatalogCommandDescriptor> ResolveCommand(string commandName, string? aggregateTypeName = null);
}

/// <summary>A registered process graph.</summary>
public sealed record ProcessTypeDescriptor(
    string Name,
    Type AggregateType,
    string? Description,
    IReadOnlyList<CatalogCommandDescriptor> Commands);

/// <summary>A command node in a process graph.</summary>
public sealed record CatalogCommandDescriptor(
    string Name,
    Type CommandType,
    string AggregateTypeName,
    Type AggregateType,
    bool IsInitial,
    IReadOnlyList<string> ProducedEvents);

public sealed class CommandCatalog : ICommandCatalog
{
    private readonly List<ProcessTypeDescriptor> _processTypes;
    private readonly ILookup<string, CatalogCommandDescriptor> _commandsByName;

    public CommandCatalog(IEnumerable<BProcess> processes)
    {
        _processTypes = processes.Select(BuildProcessType).ToList();
        _commandsByName = _processTypes
            .SelectMany(p => p.Commands)
            .ToLookup(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Builds the catalog from the globally registered process graphs.</summary>
    public static CommandCatalog FromRegisteredProcesses() =>
        new(BProcessGraphConfiguration.GetAllProcesses() ?? []);

    public IReadOnlyList<ProcessTypeDescriptor> ProcessTypes => _processTypes;

    public ProcessTypeDescriptor? FindProcessType(string aggregateTypeName) =>
        _processTypes.FirstOrDefault(p =>
            string.Equals(p.Name, aggregateTypeName, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<CatalogCommandDescriptor> ResolveCommand(string commandName, string? aggregateTypeName = null)
    {
        var matches = _commandsByName[commandName];
        if (aggregateTypeName is not null)
            matches = matches.Where(c =>
                string.Equals(c.AggregateTypeName, aggregateTypeName, StringComparison.OrdinalIgnoreCase));
        return matches.ToList();
    }

    private static ProcessTypeDescriptor BuildProcessType(BProcess process)
    {
        var aggregateType = process.ProcessType;
        var description = aggregateType
            .GetCustomAttributes(typeof(BpmDescriptionAttribute), false)
            .Cast<BpmDescriptionAttribute>()
            .FirstOrDefault()?.Description;

        // The command that starts an instance is the graph's single structural entry
        // point: the StartWith / StartWithAnyTime root. The builder guarantees every
        // process begins with exactly one StartWith and unlocks the rest only after it,
        // so this is read straight from the definition — no aggregate, and no empty-stream
        // traversal (which would evaluate conditional predicates against unpopulated state).
        var initialCommandTypes = new HashSet<Type> { process.RootNode.CommandType };

        var commands = process.RootNode.GetAllNodes()
            .Where(n => n.CommandType is not null && HasProducer(n.CommandType))
            .GroupBy(n => n.CommandType)
            .Select(g => new CatalogCommandDescriptor(
                g.Key.Name,
                g.Key,
                aggregateType.Name,
                aggregateType,
                initialCommandTypes.Contains(g.Key),
                g.SelectMany(n => n.ProducingEvents ?? []).Select(e => e.Name).Distinct().ToList()))
            .ToList();

        return new ProcessTypeDescriptor(aggregateType.Name, aggregateType, description, commands);
    }

    private static bool HasProducer(Type commandType) =>
        commandType.GetCustomAttributes(typeof(BPM.Core.Attributes.BpmProducer), false).Length > 0;
}
