using System;
using System.Collections.Generic;
using System.Linq;
using BPM.Contracts;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Identity;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Projection;
using MediatR;
using Xunit;

namespace BPM.Tests.Application;

// ---- probe commands ----

public enum Severity
{
    Low,
    Medium,
    High
}

public sealed record NestedThing(string Label, int Weight);

public sealed record SchemaProbe(
    string Name,
    string? Nickname,
    int Count,
    double? Score,
    Severity Severity,
    Guid ProcessId,
    TestIdentity? Identity,
    [property: BpmDerived] decimal? Rate,
    [property: BpmServerPopulated] string? Stamp,
    [property: BpmPattern("^[A-Z]+$")] [property: BpmDescription("Uppercase code")] string? Code,
    [property: BpmRange(1, 10)] int? Level,
    [property: BpmRequired] string? ForcedRequired,
    List<string> Tags,
    NestedThing? Nested) : IRequest;

[BpmDescription("attribute description")]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public sealed record PrecedenceProbe(
    [property: BpmDescription("attribute field description")]
    [property: BpmRange(0, 100)]
    decimal Amount,
    string? Note) : IRequest;

public sealed class PrecedenceProbeAgentSpec : IAgentSpec<PrecedenceProbe>
{
    public void Configure(AgentSpecBuilder<PrecedenceProbe> spec)
    {
        spec.Describe("spec description")
            .Policy(ExecutionPolicy.HumanOnly)
            .SuccessCriteria("spec success criteria");

        spec.Field(x => x.Amount)
            .Describe("spec field description")
            .Source("spec source hint")
            .Range(5, 50);

        spec.Field(x => x.Note).Derived();
    }
}

public class SchemaProjectionTests
{
    private readonly BpmIdentityOptions _identity = new();
    private readonly AgentSpecRegistry _specs = new();

    private CommandSchemaModel Project<T>()
    {
        var resolver = new CommandMetadataResolver(_specs, _identity);
        var projector = new CommandSchemaProjector(resolver);
        var descriptor = new CatalogCommandDescriptor(
            typeof(T).Name, typeof(T), "TestAggregate", typeof(object), IsInitial: false, []);
        return projector.Project(descriptor);
    }

    private void RegisterIdentityType() =>
        _identity.RegisterGlobal(typeof(TestIdentity), (_, _) => System.Threading.Tasks.Task.FromResult<object?>(null));

    // ---- bucket classification ----

    [Fact]
    public void IdentityTypedProperty_IsExcluded_ByTypeConvention()
    {
        RegisterIdentityType();

        var schema = Project<SchemaProbe>();

        Assert.DoesNotContain(schema.Fields, f => f.Name == "Identity");
    }

    [Fact]
    public void WithoutIdentityRegistration_IdentityPropertyWouldBeInput()
    {
        // The convention is type-based: unconfigured types are not magic.
        var schema = Project<SchemaProbe>();

        Assert.Contains(schema.Fields, f => f.Name == "Identity");
    }

    [Fact]
    public void ServerPopulatedAndDerivedAttributes_AreExcluded()
    {
        var schema = Project<SchemaProbe>();

        Assert.DoesNotContain(schema.Fields, f => f.Name == "Stamp");
        Assert.DoesNotContain(schema.Fields, f => f.Name == "Rate");
    }

    [Fact]
    public void ProcessId_IsExcluded_ItComesFromTheRoute()
    {
        var schema = Project<SchemaProbe>();

        Assert.DoesNotContain(schema.Fields, f => f.Name == "ProcessId");
    }

    [Fact]
    public void PlainProperties_AreAgentInput()
    {
        var schema = Project<SchemaProbe>();

        Assert.Contains(schema.Fields, f => f.Name == "Name");
        Assert.Contains(schema.Fields, f => f.Name == "Count");
    }

    // ---- type mapping ----

    [Fact]
    public void Enum_MapsToEnumKind_WithAllNames()
    {
        var schema = Project<SchemaProbe>();

        var severity = schema.Fields.Single(f => f.Name == "Severity");
        Assert.Equal(SchemaFieldKind.Enum, severity.Kind);
        Assert.Equal(["Low", "Medium", "High"], severity.EnumValues!.ToArray());
    }

    [Fact]
    public void PrimitiveKinds_MapCorrectly()
    {
        var schema = Project<SchemaProbe>();

        Assert.Equal(SchemaFieldKind.String, schema.Fields.Single(f => f.Name == "Name").Kind);
        Assert.Equal(SchemaFieldKind.Integer, schema.Fields.Single(f => f.Name == "Count").Kind);
        Assert.Equal(SchemaFieldKind.Number, schema.Fields.Single(f => f.Name == "Score").Kind);
    }

    [Fact]
    public void Collections_MapToArrayWithItemKind()
    {
        var schema = Project<SchemaProbe>();

        var tags = schema.Fields.Single(f => f.Name == "Tags");
        Assert.Equal(SchemaFieldKind.Array, tags.Kind);
        Assert.Equal(SchemaFieldKind.String, tags.Items!.Kind);
    }

    [Fact]
    public void NestedObjects_ProjectTheirProperties()
    {
        var schema = Project<SchemaProbe>();

        var nested = schema.Fields.Single(f => f.Name == "Nested");
        Assert.Equal(SchemaFieldKind.Object, nested.Kind);
        Assert.Equal(["Label", "Weight"], nested.Properties!.Select(p => p.Name).ToArray());
    }

    // ---- validation mapping and requiredness ----

    [Fact]
    public void ValidationAttributes_MapToConstraints()
    {
        var schema = Project<SchemaProbe>();

        var code = schema.Fields.Single(f => f.Name == "Code");
        Assert.Equal("^[A-Z]+$", code.Pattern);
        Assert.Equal("Uppercase code", code.Description);

        var level = schema.Fields.Single(f => f.Name == "Level");
        Assert.Equal(1, level.Minimum);
        Assert.Equal(10, level.Maximum);
    }

    [Fact]
    public void Requiredness_FollowsNullability_UnlessOverridden()
    {
        var schema = Project<SchemaProbe>();

        Assert.True(schema.Fields.Single(f => f.Name == "Name").Required);
        Assert.False(schema.Fields.Single(f => f.Name == "Nickname").Required);
        Assert.True(schema.Fields.Single(f => f.Name == "Count").Required);
        Assert.False(schema.Fields.Single(f => f.Name == "Score").Required);
        // [BpmRequired] beats nullable-optional reflection default.
        Assert.True(schema.Fields.Single(f => f.Name == "ForcedRequired").Required);
    }

    // ---- precedence: fluent spec > attributes > reflection ----

    [Fact]
    public void AttributesAlone_FeedDescriptionAndPolicy()
    {
        var schema = Project<PrecedenceProbe>();

        Assert.Equal("attribute description", schema.Description);
        Assert.Equal(ExecutionPolicy.Autonomous, schema.Policy);
        var amount = schema.Fields.Single(f => f.Name == "Amount");
        Assert.Equal("attribute field description", amount.Description);
        Assert.Equal(0, amount.Minimum);
        Assert.Equal(100, amount.Maximum);
    }

    [Fact]
    public void FluentSpec_WinsOverAttributes()
    {
        _specs.AddFromAssembly(typeof(PrecedenceProbeAgentSpec).Assembly);

        var schema = Project<PrecedenceProbe>();

        Assert.Equal("spec description", schema.Description);
        Assert.Equal(ExecutionPolicy.HumanOnly, schema.Policy);
        Assert.Equal("spec success criteria", schema.SuccessCriteria);

        var amount = schema.Fields.Single(f => f.Name == "Amount");
        Assert.StartsWith("spec field description", amount.Description);
        Assert.Contains("spec source hint", amount.Description);
        Assert.Equal(5, amount.Minimum);
        Assert.Equal(50, amount.Maximum);

        // Fluent .Derived() reclassifies a would-be input field.
        Assert.DoesNotContain(schema.Fields, f => f.Name == "Note");
    }

    [Fact]
    public void UnannotatedCommand_GetsSafeDefaults()
    {
        var resolver = new CommandMetadataResolver(_specs, _identity);
        var metadata = resolver.Resolve(typeof(AddNote));

        Assert.Null(metadata.Description);
        Assert.Equal(ExecutionPolicy.RequiresApproval, metadata.Policy);
    }
}
