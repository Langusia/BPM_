using BPM.UI.Configuration;
using BPM.UI.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace BPM.UI;

public static class BpmUiServiceCollectionExtensions
{
    public static IServiceCollection UseUi(this IServiceCollection services, Action<BpmUiConfiguration>? configure = null)
    {
        var uiConfig = new BpmUiConfiguration();
        configure?.Invoke(uiConfig);

        services.AddSingleton(uiConfig);
        services.AddSingleton<BpmSchemaRegistry>();

        return services;
    }
}
