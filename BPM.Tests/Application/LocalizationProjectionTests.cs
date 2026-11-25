using BPM.Contracts;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Identity;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Projection;
using MediatR;
using Xunit;

namespace BPM.Tests.Application;

public sealed record LocalizedProbe(decimal Amount) : IRequest;

public sealed class LocalizedProbeAgentSpec : IAgentSpec<LocalizedProbe>
{
    public void Configure(AgentSpecBuilder<LocalizedProbe> spec)
    {
        spec.Describe("English command")
            .Describe("ka", "ქართული ბრძანება")
            .SuccessCriteria("Done")
            .SuccessCriteria("ka", "დასრულდა");

        spec.Field(x => x.Amount)
            .Describe("Amount in GEL")
            .Describe("ka", "თანხა ლარებში")
            .Source("Ask the customer")
            .Source("ka", "ჰკითხე მომხმარებელს");
    }
}

public class LocalizationProjectionTests
{
    private static CommandSchemaModel Project(string? language)
    {
        var specs = new AgentSpecRegistry();
        specs.AddFromAssembly(typeof(LocalizedProbeAgentSpec).Assembly);
        var resolver = new CommandMetadataResolver(specs, new BpmIdentityOptions());
        var projector = new CommandSchemaProjector(resolver);
        var descriptor = new CatalogCommandDescriptor(
            nameof(LocalizedProbe), typeof(LocalizedProbe), "TestAggregate", typeof(object), IsInitial: false, []);
        return projector.Project(descriptor, language);
    }

    [Fact]
    public void Projects_georgian_variants_when_language_is_ka()
    {
        var schema = Project("ka");

        Assert.Equal("ქართული ბრძანება", schema.Description);
        Assert.Equal("დასრულდა", schema.SuccessCriteria);

        var amount = Assert.Single(schema.Fields);
        Assert.StartsWith("თანხა ლარებში", amount.Description);
        Assert.Contains("ჰკითხე მომხმარებელს", amount.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("en")]
    [InlineData("fr")] // no French variant registered -> English default
    public void Falls_back_to_english_for_default_and_unknown_language(string? language)
    {
        var schema = Project(language);

        Assert.Equal("English command", schema.Description);
        Assert.Equal("Done", schema.SuccessCriteria);

        var amount = Assert.Single(schema.Fields);
        Assert.StartsWith("Amount in GEL", amount.Description);
    }
}
