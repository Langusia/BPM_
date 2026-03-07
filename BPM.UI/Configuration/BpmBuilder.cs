using BPM.UI.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace BPM.UI.Configuration;

public class BpmBuilder
{
    private readonly IServiceCollection _services;

    public BpmBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public BpmBuilder UseUi(Action<BpmUiConfiguration>? configure = null)
    {
        var uiConfig = new BpmUiConfiguration();
        configure?.Invoke(uiConfig);

        _services.AddSingleton(uiConfig);

        var registry = new BpmSchemaRegistry();
        _services.AddSingleton(registry);

        _services.AddSingleton<BpmUiMarker>();

        return this;
    }
}

internal class BpmUiMarker;
