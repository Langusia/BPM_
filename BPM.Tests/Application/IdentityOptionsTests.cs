using System;
using System.Threading.Tasks;
using BPM.Contracts;
using BPM.Core.Application.Identity;
using Xunit;

namespace BPM.Tests.Application;

public class IdentityOptionsTests
{
    private static IdentityResolver Resolver(string tag) =>
        (_, _) => Task.FromResult<object?>(tag);

    private static async Task<string?> Tag(IdentityRegistration? registration) =>
        registration is null ? null : (string?)await registration.Resolver(null!, null!);

    [Fact]
    public async Task ResolutionOrder_ExactCommand_BeatsInterface_BeatsGlobal()
    {
        var options = new BpmIdentityOptions();
        options.RegisterGlobal(typeof(TestIdentity), Resolver("global"));
        options.RegisterFor(typeof(IAuthenticatedRequest<TestIdentity>), typeof(TestIdentity), Resolver("interface"));
        options.RegisterFor(typeof(CloseTicket), typeof(TestIdentity), Resolver("exact"));

        Assert.Equal("exact", await Tag(options.Resolve(typeof(CloseTicket))));
        // OpenTicket implements the marker but has no exact registration.
        Assert.Equal("interface", await Tag(options.Resolve(typeof(OpenTicket))));
        // AddNote implements nothing: falls back to the global default.
        Assert.Equal("global", await Tag(options.Resolve(typeof(AddNote))));
    }

    [Fact]
    public void NoRegistrations_ResolvesToNull()
    {
        var options = new BpmIdentityOptions();

        Assert.Null(options.Resolve(typeof(CloseTicket)));
    }

    [Fact]
    public void IdentityTypes_CollectsEveryRegisteredType()
    {
        var options = new BpmIdentityOptions();
        options.RegisterGlobal(typeof(TestIdentity), Resolver("g"));
        options.RegisterFor(typeof(CloseTicket), typeof(string), Resolver("s"));

        Assert.Contains(typeof(TestIdentity), options.IdentityTypes);
        Assert.Contains(typeof(string), options.IdentityTypes);
    }
}
