using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BPM.Core;
using BPM.Mcp;
using Loan.Api;
using Loan.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(c =>
{
    c.RegisterServicesFromAssembly(typeof(Program).Assembly);
    c.AddOpenBehavior(typeof(CommandAuditBehavior<,>));
});

// Host-level auth: the MCP endpoint sits behind the app's normal JWT auth.
// BPM.Mcp only reads HttpContext.User; it never validates tokens itself.
var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "loan-api-dev",
            ValidAudience = "loan-api",
            IssuerSigningKey = signingKey
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddBpm("bpm", builder.Configuration.GetConnectionString("Bpm")!,
        x => x.AddAggregateDefinition<LoanApplication, LoanApplicationDefinition>())
    .UseMcp(mcp => mcp
        .WithIdentity<UserContext>(user => new UserContext(
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "unknown",
            user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("name") ?? "Unknown User",
            user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email")))
        .WithAgentSpecsFromAssembly(typeof(UserContext).Assembly));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    // Dev-token endpoint for MCP Inspector / Claude Desktop testing only.
    app.MapGet("/dev-token", (string? name) =>
    {
        var token = new JwtSecurityToken(
            issuer: "loan-api-dev",
            audience: "loan-api",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "cust-1001"),
                new Claim(ClaimTypes.Name, name ?? "Nino Beridze"),
                new Claim(ClaimTypes.Email, "nino.beridze@example.com")
            ],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    });
}

app.Run();

public partial class Program;
