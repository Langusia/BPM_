using Core.BPM;
using Core.BPM.Application;
using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Core.BPM.DefinitionBuilder;
using MediatR;
using MyCredo.Common;
using MyCredo.Features.Loan.Finish;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.TwoFactor;

namespace MyCredo.Features.Loan.LoanV9;

public class IssueLoan : Aggregate
{
    public bool Initiated;
    public bool Contracted;
    public bool Scheduled;

    public void Initiate()
    {
        var @event = new IssueLoanInitiated(0, 0, ChannelTypeEnum.Unclassified);
        Enqueue(@event);
        Apply(@event);
    }


    public void Apply(IssueLoanInitiated @event)
    {
        Initiated = true;
    }

    public void Apply(GeneratedSchedule @event)
    {
        Scheduled = true;
    }

    public void Apply(GeneratedContract @event)
    {
        Contracted = true;
    }
}

public class LoanV9AggregateDefinition : BpmDefinition<IssueLoan>
{
    public override MyClass<IssueLoan> DefineProcess(IProcessBuilder<IssueLoan> configureProcess)
    {
        return configureProcess
            .StartWith<InitiateIssueLoanProcess>()
            .Continue<GenerateContract>()
            .Or<GenerateSchedule>(x => x.Continue<GenerateContract>())
            .ContinueAnyTime<SendOtp>()
            .ContinueAnyTime<ValidateOtp>()
            .Continue<FinishCarPawnshop>()
            .End();
        //configureProcess
        //    .StartWith<InitiateIssueLoanProcess>()
        //    .ThenContinue<GenerateContract>(x => 
        //        x.ThenContinue<GenerateSchedule>())
        //    .Or<GenerateSchedule>(x => 
        //        x.ThenContinue<GenerateContract>())
        //    .ThenContinueAnyTime<SendOtp>()
        //    .ThenContinueAnyTime<ValidateOtp>()
        //    .Continue<MarkDocumentsAsAssigned>()
        //    .Continue<FinishIssueLoan>();
    }
}

[BpmProducer(typeof(GeneratedSchedule))]
public record GenerateSchedule : IRequest<bool>
{
}

public class GenerateScheduleHandler : IRequestHandler<GenerateSchedule, bool>
{
    private readonly BpmStore<IssueLoan, GenerateSchedule> _bpm;

    public GenerateScheduleHandler(BpmStore<IssueLoan, GenerateSchedule> bpm)
    {
        _bpm = bpm;
    }

    public async Task<bool> Handle(GenerateSchedule request, CancellationToken cancellationToken)
    {
        var id = Guid.Parse("36f3284a-bd03-4d90-b30a-4281cdd5be38");
        var s = await _bpm.AggregateProcessStateAsync(id, cancellationToken);
        throw new NotImplementedException();
    }
}

[BpmProducer(typeof(GeneratedContract))]
public record GenerateContract(Guid Id) : IRequest<ProcessStateDto>;

public class GenerateContractHandler : IRequestHandler<GenerateContract, ProcessStateDto>
{
    private readonly BpmStore<IssueLoan, GenerateContract> _bs;

    public GenerateContractHandler(BpmStore<IssueLoan, GenerateContract> bs)
    {
        _bs = bs;
    }

    public async Task<ProcessStateDto> Handle(GenerateContract request, CancellationToken cancellationToken)
    {
        var s = await _bs.AggregateProcessStateAsync(request.Id, cancellationToken);
        //s.AppendEvent()
        //await _bs.SaveChangesAsync(cancellationToken);
        return null;
    }
}

[BpmProducer(typeof(IssueLoanInitiated))]
public record InitiateIssueLoanProcess : IRequest<ProcessStateDto>;

public class InitiateIssueLoanProcessHandler : IRequestHandler<InitiateIssueLoanProcess, ProcessStateDto>
{
    private readonly BpmStore<IssueLoan, InitiateIssueLoanProcess> _bs;

    public InitiateIssueLoanProcessHandler(BpmStore<IssueLoan, InitiateIssueLoanProcess> bs)
    {
        _bs = bs;
    }

    public async Task<ProcessStateDto> Handle(InitiateIssueLoanProcess request, CancellationToken cancellationToken)
    {
        var s = _bs.StartProcess(x => x.Initiate());
        await _bs.SaveChangesAsync(cancellationToken);
        return new ProcessStateDto()
        {
            Id = s.Aggregate.Id,
            NextNodes = s.CurrentStep.NextSteps.Select(x => x.CommandType.ToString()).ToList()
        };
    }
}

public class ProcessStateDto
{
    public Guid Id { get; set; }
    public List<string> NextNodes { get; set; }
}