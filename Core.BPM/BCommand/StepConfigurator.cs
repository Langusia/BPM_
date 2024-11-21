using Core.BPM.Configuration;
using MediatR;

namespace Core.BPM.BCommand;

public class StepConfigurator
{
    public List<BpmProcessEventOptions>? BpmProcessEventOptions { get; set; }
}

public class StepConfigurator<TAggregate> where TAggregate : Aggregate
{
    public StepBuilder<TAggregate, T> Configure<T>() where T : IBaseRequest
    {
        var stepOptions = new StepOptions<TAggregate>(typeof(TAggregate), typeof(T));
        BProcessStepConfiguration.AddStepConfig(stepOptions);
        return new StepBuilder<TAggregate, T>(stepOptions);
    }
}

public class StepBuilder<TProcess, TStep>(StepOptions<TProcess> options) where TStep : IBaseRequest where TProcess : Aggregate
{
    private StepOptions<TProcess> _options = options;

    public StepBuilder<TProcess, TStep> SetMaxCount(int? count)
    {
        _options.MaxCount = count;
        return this;
    }


    public StepBuilder<TProcess, TStep> SetProcessPreCondition(Predicate<TProcess> processPredicate)
    {
        _options.AggregateCondition = processPredicate;
        return this;
    }
}

public class BpmEventConfigurationBuilder<TAggregate> where TAggregate : Aggregate
{
    public void AddCommandOptions<T>(Action<StepOptions> configureBpmOptions)
    {
        var inst = new BpmProcessEventOptions();
        inst.ProcessName = typeof(TAggregate).Name;
        inst.BpmCommandtOptions = new List<StepOptions>();
        //var eventInst = new BpmEventOptions
        //{
        //    BpmEventName = typeof(T).Name,
        //    BpmCommandName = typeof(T).Name
        //};
        //configureBpmOptions.Invoke(eventInst);
        //inst.BpmCommandtOptions.Add(eventInst);
        //BProcessGraphConfiguration.EventOptions.Add(inst);
    }
}