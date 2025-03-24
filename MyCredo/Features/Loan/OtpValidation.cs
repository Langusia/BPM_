using Core.BPM;
using Core.BPM.Application;
using Core.BPM.DefinitionBuilder;
using Core.BPM.DefinitionBuilder.Interfaces;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.Loan.OtpValidate;

namespace MyCredo.Features.Loan;

public class OtpValidationDefinition : BpmDefinition<OtpValidation>
{
    public override ProcessConfig<OtpValidation> DefineProcess(IProcessBuilder<OtpValidation> configureProcess) =>
        configureProcess
            .StartWithAnyTime<SendOtp>()
            .ContinueAnyTime<ValidateOtp>()
            .End();
}

public class OtpValidation : Aggregate
{
    public int ValidationCount { get; set; }
    public int SendCount { get; set; }
    public bool IsValidOtp { get; set; }

    public override bool? IsCompleted()
    {
        return IsValidOtp;
    }

    public void Apply(OtpSent @event)
    {
        SendCount++;
    }

    public void Apply(OtpValidated @event)
    {
        ValidationCount++;
        IsValidOtp = @event.ValidOtp;
    }
}