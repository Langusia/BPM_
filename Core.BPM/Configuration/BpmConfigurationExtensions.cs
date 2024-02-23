using Core.BPM.Interfaces;

namespace Core.BPM.Configuration;

public static class BpmConfigurationExtensions
{
    public static bool Validate<TProcess>(Type comandType) where TProcess : IProcess
    {
        var config = BpmProcessGraphConfiguration.GetConfig<TProcess>();
        if (config is null)
            return false;

        var current = config.RootNode.TraverseTo(comandType);
        return current is not null && current.PrevSteps.Any(x => x.EventType == comandType);
    }


    public static bool GetValidOpts<TProcess, TCommand>(TProcess aggregate) where TProcess : IProcess
    {
        var config = BpmProcessGraphConfiguration.GetConfig<TProcess>();
        if (config is null)
            return false;

        while (true)
        {
            var current = config.RootNode.TraverseTo<TCommand>();
            current.
        }

        return current is not null && current.PrevSteps.Any(x => x.EventType == comandType);
    }
}