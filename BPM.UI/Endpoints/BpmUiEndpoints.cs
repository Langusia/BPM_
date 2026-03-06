using System.Text.Json;
using BPM.Core.Process;
using BPM.UI.Configuration;
using BPM.UI.Schema;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace BPM.UI.Endpoints;

public static class BpmUiEndpoints
{
    public static IEndpointRouteBuilder MapBpmUi(this IEndpointRouteBuilder app)
    {
        var uiConfig = app.ServiceProvider.GetRequiredService<BpmUiConfiguration>();
        var registry = app.ServiceProvider.GetRequiredService<BpmSchemaRegistry>();
        CommandSchemaBuilder.PopulateRegistry(registry, uiConfig);

        // Pure graph definition — no process state
        app.MapGet("/bpm/graph/definition/{aggregateName}", (string aggregateName) =>
        {
            try
            {
                var response = GraphBuilder.BuildDefinition(aggregateName, uiConfig);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Graph with current process state
        app.MapGet("/bpm/graph/{processId:guid}", async (Guid processId, IProcessStore store, CancellationToken ct) =>
        {
            try
            {
                var process = await store.FetchProcessAsync(processId, ct);
                var response = GraphBuilder.Build(process, uiConfig);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Subgraph for JumpTo nodes
        app.MapGet("/bpm/graph/subgraph/{guestProcessType}/{processId:guid}", async (string guestProcessType, Guid processId, IProcessStore store, CancellationToken ct) =>
        {
            try
            {
                var process = await store.FetchProcessAsync(processId, ct);
                var response = GraphBuilder.Build(process, uiConfig);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Schema for a command
        app.MapGet("/bpm/schema/{commandName}", (string commandName) =>
        {
            var schema = registry.GetSchema(commandName);
            if (schema is null)
                return Results.NotFound(new { error = $"Schema not found for command '{commandName}'" });
            return Results.Ok(schema);
        });

        // Generic MediatR dispatch
        app.MapPost("/bpm/execute/{commandName}", async (string commandName, HttpContext context, IMediator mediator, IProcessStore store, CancellationToken ct) =>
        {
            var commandType = FindCommandType(commandName);
            if (commandType is null)
                return Results.NotFound(new { error = $"Command '{commandName}' not found" });

            try
            {
                var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Request.Body, cancellationToken: ct);
                var command = JsonSerializer.Deserialize(body.GetRawText(), commandType, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (command is null)
                    return Results.BadRequest(new { error = "Could not deserialize command" });

                var result = await mediator.Send(command, ct);

                // Extract processId: from command body (continuation) or from result (StartWith returns Guid)
                Guid? processId = null;

                // Try command body first (continuation commands have ProcessId)
                if (body.TryGetProperty("processId", out var pidElem) ||
                    body.TryGetProperty("ProcessId", out pidElem))
                {
                    if (pidElem.TryGetGuid(out var pid) && pid != Guid.Empty)
                        processId = pid;
                }

                // If no processId in body, try extracting from MediatR result (StartWith commands return new Guid)
                if (processId is null && result is Guid guidResult && guidResult != Guid.Empty)
                {
                    processId = guidResult;
                }

                List<string> availableStepIds = [];
                if (processId is not null)
                {
                    try
                    {
                        var process = await store.FetchProcessAsync(processId.Value, ct);
                        var stepsResult = process.GetAvailableStepIds();
                        if (stepsResult.IsSuccess)
                            availableStepIds = stepsResult.Data!;
                    }
                    catch
                    {
                        // Process may not be persisted yet for the current request
                    }
                }

                return Results.Ok(new ExecuteResponse
                {
                    Success = true,
                    ProcessId = processId,
                    AvailableStepIds = availableStepIds
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new ExecuteResponse
                {
                    Success = false,
                    ProcessId = null,
                    Error = ex.Message,
                    AvailableStepIds = []
                });
            }
        });

        // Serve React app — aggregateName passed via URL for frontend to read
        var assembly = typeof(BpmUiEndpoints).Assembly;
        var wwwrootPath = Path.Combine(Path.GetDirectoryName(assembly.Location)!, "wwwroot");
        if (Directory.Exists(wwwrootPath))
        {
            var fileProvider = new PhysicalFileProvider(wwwrootPath);
            app.MapGet("/bpm/ui/{aggregateName}/{**path}", (string aggregateName, string? path) =>
            {
                return ServeStaticFile(fileProvider, path);
            });
            app.MapGet("/bpm/ui/{aggregateName}", (string aggregateName) =>
            {
                return ServeStaticFile(fileProvider, null);
            });
        }
        else
        {
            app.MapGet("/bpm/ui/{aggregateName}/{**path}", (string aggregateName, string? path) => Results.Content(
                "<!DOCTYPE html><html><body><h1>BPM UI</h1><p>Frontend not built. Run <code>npm run build</code> in BPM.UI/frontend.</p></body></html>",
                "text/html"));
            app.MapGet("/bpm/ui/{aggregateName}", (string aggregateName) => Results.Content(
                "<!DOCTYPE html><html><body><h1>BPM UI</h1><p>Frontend not built. Run <code>npm run build</code> in BPM.UI/frontend.</p></body></html>",
                "text/html"));
        }

        return app;
    }

    private static IResult ServeStaticFile(IFileProvider fileProvider, string? path)
    {
        if (string.IsNullOrEmpty(path))
            path = "index.html";

        var fileInfo = fileProvider.GetFileInfo(path);
        if (!fileInfo.Exists)
            fileInfo = fileProvider.GetFileInfo("index.html");

        if (!fileInfo.Exists)
            return Results.NotFound();

        var contentType = path switch
        {
            _ when path.EndsWith(".js") => "application/javascript",
            _ when path.EndsWith(".css") => "text/css",
            _ when path.EndsWith(".html") => "text/html",
            _ when path.EndsWith(".json") => "application/json",
            _ when path.EndsWith(".svg") => "image/svg+xml",
            _ when path.EndsWith(".png") => "image/png",
            _ when path.EndsWith(".ico") => "image/x-icon",
            _ => "application/octet-stream"
        };

        return Results.File(fileInfo.CreateReadStream(), contentType);
    }

    private static Type? FindCommandType(string commandName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == commandName);
                if (type is not null) return type;
            }
            catch
            {
                // Skip assemblies that can't be reflected
            }
        }
        return null;
    }
}
