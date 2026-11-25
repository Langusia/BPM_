using BPM.Core;
using BPM.Core.Process;
using BPM.Core.Definition;
using BPM.Core.Attributes;
using BPM.Core.Events;
using BPM.Core.Definition.Interfaces;
using MediatR;
using MyCredo.Common;
using MyCredo.Features.Loan.LoanV9;
using MyCredo.Retail.Loan.Application.Features.IssueLoanProcess.CreditCard.GetLimits;
using MyCredo.Retail.Loan.Application.Features.IssueLoanProcess.CreditCard.Initiating;

namespace MyCredo.Retail.Loan.Application.Features.IssueLoanProcess.CreditCard.Initiating
{
    public record CreditCardInitiated(
        long ApplicationId,
        object LoanApplication,
        object UserContext
    );

    [BpmProducer(typeof(CreditCardInitiated))]
    public record IssueLoanInitiate(int UserId, long ApplicationId, ChannelTypeEnum Channel) : IRequest;
}

namespace MyCredo.Retail.Loan.Application.Features.IssueLoanProcess.CreditCard.GetLimits
{
    public record GetCreditCardLimits(long applicationId, string personalNumber);

    [BpmProducer(typeof(GetCreditCardLimits))]
    public record GetCreditCardLimits1(int UserId, long ApplicationId, ChannelTypeEnum Channel) : IRequest;
}

public class IssueCreditCard : Aggregate
{
}

public class IssueCreditCardDefinition : BpmDefinition<IssueCreditCard>
{
    public override ProcessConfig<IssueCreditCard> DefineProcess(IProcessBuilder<IssueCreditCard> configureProcess)
    {
        return configureProcess.StartWith<IssueLoanInitiate>()
            .Continue<GetCreditCardLimits1>()
            .End();
    }
}