using Core.BPM.Configuration;

namespace Core.BPM.BCommand;

public class BpmEventConfiguration
{
    public List<BpmProcessEventOptions>? BpmProcessEventOptions { get; set; }
}

public class BpmEventConfigurationBuilder<TAggregate> where TAggregate : Aggregate
{
    public void AddBpmEventOptions<T>(Action<BpmEventOptions> configureBpmOptions) where T : BpmEvent
    {
        var inst = new BpmProcessEventOptions();
        inst.ProcessName = typeof(TAggregate).Name;
        inst.BpmEventOptions = new List<BpmEventOptions>();
        var eventInst = new BpmEventOptions
        {
            BpmEventName = typeof(T).Name
        };
        configureBpmOptions.Invoke(eventInst);
        inst.BpmEventOptions.Add(eventInst);
        BProcessGraphConfiguration.EventOptions.Add(inst);
    }
}