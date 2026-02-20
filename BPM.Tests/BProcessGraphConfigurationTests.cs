using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPM.Core.Attributes;
using BPM.Core.Configuration;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Events;
using BPM.Core.Nodes;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Persistence;
using BPM.Core.Process;
using MediatR;
using NSubstitute;
using Xunit;

namespace BPM.Tests;

#region Test Aggregates, Events, and Commands

public record Step1Completed(string Value) : BpmEvent;
public record Step2Completed(bool Flag) : BpmEvent;
public record Step3Completed() : BpmEvent;
public record Step4Completed() : BpmEvent;
public record Step5Completed() : BpmEvent;

[BpmProducer(typeof(Step1Completed))]
public record Step1Command(Guid ProcessId) : IRequest;

[BpmProducer(typeof(Step2Completed))]
public record Step2Command(Guid ProcessId) : IRequest;

[BpmProducer(typeof(Step3Completed))]
public record Step3Command(Guid ProcessId) : IRequest;

[BpmProducer(typeof(Step4Completed))]
public record Step4Command(Guid ProcessId) : IRequest;

[BpmProducer(typeof(Step5Completed))]
public record Step5Command(Guid ProcessId) : IRequest;

public class TestAggregate : Aggregate
{
    public string Value { get; set; } = string.Empty;
    public bool Flag { get; set; }

    public void Apply(Step1Completed e) => Value = e.Value;
    public void Apply(Step2Completed e) => Flag = e.Flag;
    public void Apply(Step3Completed e) { }
    public void Apply(Step4Completed e) { }
    public void Apply(Step5Completed e) { }
}

public class LinearDefinition : BpmDefinition<TestAggregate>
{
    public override ProcessConfig<TestAggregate> DefineProcess(IProcessBuilder<TestAggregate> configureProcess)
    {
        return configureProcess
            .StartWith<Step1Command>()
            .Continue<Step2Command>()
            .Continue<Step3Command>()
            .End();
    }
}

// Second aggregate for multiple-registration test
public record OtherStep1Completed() : BpmEvent;

[BpmProducer(typeof(OtherStep1Completed))]
public record OtherStep1Command(Guid ProcessId) : IRequest;

public class OtherAggregate : Aggregate
{
    public void Apply(OtherStep1Completed e) { }
}

public class OtherDefinition : BpmDefinition<OtherAggregate>
{
    public override ProcessConfig<OtherAggregate> DefineProcess(IProcessBuilder<OtherAggregate> configureProcess)
    {
        return configureProcess
            .StartWith<OtherStep1Command>()
            .End();
    }
}

#endregion

public class BProcessGraphConfigurationTests : IDisposable
{
    private readonly INodeEvaluatorFactory _evaluatorFactory;

    public BProcessGraphConfigurationTests()
    {
        // Reset the static _processes field before each test
        ClearProcesses();

        var repository = Substitute.For<IBpmRepository>();
        _evaluatorFactory = new NodeEvaluatorFactory(repository);
    }

    public void Dispose()
    {
        ClearProcesses();
    }

    private static void ClearProcesses()
    {
        var field = typeof(BProcessGraphConfiguration)
            .GetField("_processes", BindingFlags.Static | BindingFlags.NonPublic)!;
        field.SetValue(null, null);
    }

    private ProcessConfig<T> BuildDefinition<T, TDefinition>()
        where T : Aggregate
        where TDefinition : BpmDefinition<T>, new()
    {
        var builder = new ProcessRootBuilder<T>(_evaluatorFactory);
        var definition = new TDefinition();
        return definition.DefineProcess(builder);
    }

    [Fact]
    public void GetConfig_AfterBuildingLinearDefinition_ReturnsProcessForAggregate()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();

        Assert.NotNull(config);
        Assert.Equal(typeof(TestAggregate), config.ProcessType);
    }

    [Fact]
    public void GetConfig_AfterBuildingLinearDefinition_RootNodeIsFirstCommand()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();

        Assert.Equal(typeof(Step1Command), config.RootNode.CommandType);
    }

    [Fact]
    public void GetConfig_LinearDefinition_HasCorrectNodeChain()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();

        // Linear: Step1 -> Step2 -> Step3
        var commandTypes = allNodes.Select(n => n.CommandType).ToList();
        Assert.Contains(typeof(Step1Command), commandTypes);
        Assert.Contains(typeof(Step2Command), commandTypes);
        Assert.Contains(typeof(Step3Command), commandTypes);
        Assert.Equal(3, allNodes.Count);
    }

    [Fact]
    public void GetConfig_LinearDefinition_NodesAreLinkedSequentially()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var root = config.RootNode;

        // Root (Step1) -> Step2
        Assert.Single(root.NextSteps!);
        var step2 = root.NextSteps![0];
        Assert.Equal(typeof(Step2Command), step2.CommandType);

        // Step2 -> Step3
        Assert.Single(step2.NextSteps!);
        var step3 = step2.NextSteps![0];
        Assert.Equal(typeof(Step3Command), step3.CommandType);

        // Step3 is terminal
        Assert.Empty(step3.NextSteps!);
    }

    [Fact]
    public void GetConfig_LinearDefinition_EachNodeProducesCorrectEvent()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();

        var step1Node = allNodes.First(n => n.CommandType == typeof(Step1Command));
        Assert.Contains(typeof(Step1Completed), step1Node.ProducingEvents);

        var step2Node = allNodes.First(n => n.CommandType == typeof(Step2Command));
        Assert.Contains(typeof(Step2Completed), step2Node.ProducingEvents);

        var step3Node = allNodes.First(n => n.CommandType == typeof(Step3Command));
        Assert.Contains(typeof(Step3Completed), step3Node.ProducingEvents);
    }

    [Fact]
    public void GetConfig_ByString_ReturnsMatchingProcess()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig(nameof(TestAggregate));

        Assert.NotNull(config);
        Assert.Equal(typeof(TestAggregate), config!.ProcessType);
    }

    [Fact]
    public void GetConfig_UnregisteredProcess_ThrowsException()
    {
        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(
            () => BProcessGraphConfiguration.GetConfig<TestAggregate>());
    }

    [Fact]
    public void GetConfig_ByString_UnregisteredProcess_ThrowsException()
    {
        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(
            () => BProcessGraphConfiguration.GetConfig("NonExistent"));
    }

    [Fact]
    public void AddProcess_DuplicateProcess_ThrowsException()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(
            () => BuildDefinition<TestAggregate, LinearDefinition>());
    }

    [Fact]
    public void GetConfig_MultipleProcesses_ReturnsCorrectOne()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();
        BuildDefinition<OtherAggregate, OtherDefinition>();

        var testConfig = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var otherConfig = BProcessGraphConfiguration.GetConfig<OtherAggregate>();

        Assert.Equal(typeof(TestAggregate), testConfig.ProcessType);
        Assert.Equal(typeof(OtherAggregate), otherConfig.ProcessType);
        Assert.NotEqual(testConfig.ProcessType, otherConfig.ProcessType);
    }

    [Fact]
    public void GetConfig_WithConditionalBranch_ContainsConditionalNode()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .If(x => x.Flag,
                branch => branch.Continue<Step2Command>())
            .Else(branch => branch.Continue<Step3Command>())
            .Continue<Step4Command>()
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();

        // Should contain a ConditionalNode
        Assert.Contains(allNodes, n => n is ConditionalNode);

        var conditionalNode = (ConditionalNode)allNodes.First(n => n is ConditionalNode);
        Assert.NotNull(conditionalNode.IfNodeRoots);
        Assert.NotNull(conditionalNode.ElseNodeRoots);
    }

    [Fact]
    public void GetConfig_WithConditionalBranch_IfBranchContainsCorrectCommand()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .If(x => x.Flag,
                branch => branch.Continue<Step2Command>())
            .Else(branch => branch.Continue<Step3Command>())
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();
        var conditionalNode = (ConditionalNode)allNodes.First(n => n is ConditionalNode);

        var ifNodeTypes = conditionalNode.IfNodeRoots
            .SelectMany(n => n.GetAllNodes())
            .Select(n => n.CommandType)
            .ToList();

        Assert.Contains(typeof(Step2Command), ifNodeTypes);
    }

    [Fact]
    public void GetConfig_WithConditionalBranch_ElseBranchContainsCorrectCommand()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .If(x => x.Flag,
                branch => branch.Continue<Step2Command>())
            .Else(branch => branch.Continue<Step3Command>())
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();
        var conditionalNode = (ConditionalNode)allNodes.First(n => n is ConditionalNode);

        var elseNodeTypes = conditionalNode.ElseNodeRoots!
            .SelectMany(n => n.GetAllNodes())
            .Select(n => n.CommandType)
            .ToList();

        Assert.Contains(typeof(Step3Command), elseNodeTypes);
    }

    [Fact]
    public void GetConfig_WithGroupNode_ContainsGroupNode()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .Group(g =>
            {
                g.AddStep<Step2Command>();
                g.AddStep<Step3Command>();
            })
            .Continue<Step4Command>()
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();

        Assert.Contains(allNodes, n => n is GroupNode);
        var groupNode = (GroupNode)allNodes.First(n => n is GroupNode);
        var subRootTypes = groupNode.SubRootNodes.Select(n => n.CommandType).ToList();
        Assert.Contains(typeof(Step2Command), subRootTypes);
        Assert.Contains(typeof(Step3Command), subRootTypes);
    }

    [Fact]
    public void GetConfig_WithOptionalNode_ContainsOptionalNode()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .If(x => x.Flag,
                branch => branch.UnlockOptional<Step2Command>())
            .Continue<Step3Command>()
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var allNodes = config.RootNode.GetAllNodes();

        // The conditional branch should contain an OptionalNode
        var conditionalNode = (ConditionalNode)allNodes.First(n => n is ConditionalNode);
        var ifBranchNodes = conditionalNode.IfNodeRoots.SelectMany(n => n.GetAllNodes()).ToList();
        Assert.Contains(ifBranchNodes, n => n is OptionalNode);
        Assert.Contains(ifBranchNodes, n => n.CommandType == typeof(Step2Command));
    }

    [Fact]
    public void GetConfig_WithOrBranch_RootHasMultipleNextSteps()
    {
        var builder = new ProcessRootBuilder<TestAggregate>(_evaluatorFactory);
        builder
            .StartWith<Step1Command>()
            .Continue<Step2Command>()
                .Or<Step3Command>()
            .Continue<Step4Command>()
            .End();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();
        var root = config.RootNode;

        // After Step1, there should be two alternatives: Step2 and Step3
        Assert.True(root.NextSteps!.Count >= 2);
        var nextCommandTypes = root.NextSteps!.Select(n => n.CommandType).ToList();
        Assert.Contains(typeof(Step2Command), nextCommandTypes);
        Assert.Contains(typeof(Step3Command), nextCommandTypes);
    }

    [Fact]
    public void GetConfig_RootNodeContainsEvent_RecognizesProducedEvent()
    {
        BuildDefinition<TestAggregate, LinearDefinition>();

        var config = BProcessGraphConfiguration.GetConfig<TestAggregate>();

        Assert.True(config.RootNode.ContainsEvent(new Step1Completed("test")));
        Assert.False(config.RootNode.ContainsEvent(new Step2Completed(true)));
    }

    [Fact]
    public void GetCommandProducer_ReturnsCorrectProducer()
    {
        var producer = BProcessGraphConfiguration.GetCommandProducer(typeof(Step1Command));

        Assert.NotNull(producer);
        Assert.Contains(typeof(Step1Completed), producer.EventTypes);
    }
}
