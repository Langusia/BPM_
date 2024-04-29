using Core.BPM.Configuration;

namespace Core.BPM.BCommand;

public class BpmEventConfiguration
{
    public List<BpmProcessEventOptions>? BpmProcessEventOptions { get; set; }
}

public class BpmEventConfigurationBuilder<TAggregate> where TAggregate : Aggregate
{
    public void AddCommandOptions<T>(Action<BpmEventOptions> configureBpmOptions)
    {
        var inst = new BpmProcessEventOptions();
        inst.ProcessName = typeof(TAggregate).Name;
        inst.BpmCommandtOptions = new List<BpmEventOptions>();
        var eventInst = new BpmEventOptions
        {
            BpmEventName = typeof(T).Name,
            BpmCommandName = typeof(T).Name
        };
        configureBpmOptions.Invoke(eventInst);
        inst.BpmCommandtOptions.Add(eventInst);
        BProcessGraphConfiguration.EventOptions.Add(inst);
    }
}