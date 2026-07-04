using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using BPM.Core;
using BPM.Mcp;
using Loan.Api;
using Loan.Contracts;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Same enum-as-string convention as the MCP channel.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the output of GET /dev-token"
    };
    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    return Task.CompletedTask;
}));

builder.Services.AddMediatR(c =>
{
    c.RegisterServicesFromAssembly(typeof(Program).Assembly);
    c.AddOpenBehavior(typeof(CommandAuditBehavior<,>));
});

// Host-level auth: the MCP endpoint sits behind the app's normal JWT auth.
// BPM.Mcp only reads HttpContext.User; it never validates tokens itself.
// The configured secret is hashed so any non-empty string yields a valid
// 256-bit HS256 key (the raw value may be too short for the algorithm).
var signingKey = new SymmetricSecurityKey(
    SHA256.HashData(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)));
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
        .WithIdentity<UserContext>(LoanEndpoints.MapUser)
        .WithAgentSpecsFromAssembly(typeof(UserContext).Assembly));

var app = builder.Build();

// Dev convenience: Marten creates schemas and tables, but never the database
// itself. Provision it so a plain `docker compose up postgres` is enough.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Bpm:EnsureDatabase", true))
{
    await EnsureDatabaseExistsAsync(
        app.Configuration.GetConnectionString("Bpm")!,
        app.Logger);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Loan.Api");
});

app.MapLoanEndpoints();

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

static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
{
    var database = new NpgsqlConnectionStringBuilder(connectionString).Database!;
    var admin = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
    try
    {
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var exists = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", database);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
            await create.ExecuteNonQueryAsync();
            logger.LogInformation("Created database {Database}.", database);
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex,
            "Could not verify that database {Database} exists. " +
            "If Postgres is not running, start it with `docker compose up -d postgres`.", database);
    }
}

public partial class Program;
