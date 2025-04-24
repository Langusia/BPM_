using Core.BPM;
using Core.BPM.Application;
using Core.BPM.DefinitionBuilder;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Trash;
using MyCredo.Common;
using MyCredo.Features.Loan;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Features.RecoveringPassword.Finishing;
using MyCredo.Features.RecoveringPassword.IdentifyingFace;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.RecoveringPassword.RequestingPhoneChange;
using MyCredo.Features.TwoFactor;
using ValidateOtp = MyCredo.Features.TwoFactor;

namespace MyCredo.Features.RecoveringPassword;

public class PasswordRecoveryDefinition : BpmDefinition<PasswordRecovery>
{
    public override ProcessConfig<PasswordRecovery> DefineProcess(IProcessBuilder<PasswordRecovery> configure) =>
        configure.StartWith<InitiatePasswordRecovery>()
            .Continue<SendOtp>()
            .Continue<Loan.OtpValidate.ValidateOtp>()
            .If(x => !x.IsOtpValid, x =>
                x.If(x => !x.IsOtpValid, z =>
                        z.Continue<A>())
                    .If(x => x.IsOtpValid, z =>
                        z.Continue<B>()))
            .Else(z => z.Continue<Z>())
            .End();


    public override void ConfigureSteps(StepConfigurator<PasswordRecovery> stepConfigurator)
    {
        stepConfigurator.Configure<GenerateOtp>()
            .SetProcessPreCondition(x => x.ChannelType == ChannelTypeEnum.Unclassified)
            .SetMaxCount(3);
    }
}

public class PasswordRecovery : Aggregate
{
    public PasswordRecovery()
    {
    }

    public PasswordRecovery(string personalNumber, DateTime birthDate, ChannelTypeEnum channelType)
    {
        Id = Guid.NewGuid();
        var @event = new PasswordRecoveryInitiated(personalNumber, birthDate, channelType);
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(PasswordRecoveryInitiated @event)
    {
        PersonalNumber = @event.PersonalNumber;
        BirthDate = @event.BirthDate;
        ChannelType = @event.ChannelType;
    }

    public PasswordRecovery Initiate(string personalNumber, DateTime birthDate, ChannelTypeEnum channelType)
    {
        var @event = new PasswordRecoveryInitiated(personalNumber, birthDate, channelType);
        Enqueue(@event);
        Apply(@event);
        return new(personalNumber, birthDate, channelType);
    }

    public void ValidateSecurityQuestion()
    {
        var @event = new SecurityQuestionValidated();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(ValidateOtp.OtpSent @event)
    {
    }

    public void Apply(SecurityQuestionValidated @event)
    {
    }

    public void Apply(OtpValidated @event)
    {
        IsOtpValid = @event.ValidOtp;
    }

    public void CheckCardComplete()
    {
        var @event = new CheckCardCompleted();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(CheckCardCompleted @event)
    {
    }

    public void CheckCardInitiate(long userId, int paymentId, string hash)
    {
        var @event = new CheckCardInitiated(userId, paymentId, hash);
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(CheckCardInitiated @event)
    {
    }

    public void FinishPasswordRecovery()
    {
        var @event = new FinishedPasswordRecovery();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(FinishedPasswordRecovery @event)
    {
    }

    public void IdentifyFace()
    {
        var @event = new IdentifiedFace();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(IdentifiedFace @event)
    {
    }


    public void PhoneChangeComplete()
    {
        var @event = new PhoneChangeCompleted();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(PhoneChangeCompleted @event)
    {
    }

    public void PhoneChangeInitiate()
    {
        var @event = new PhoneChangeInitiated();
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(PhoneChangeInitiated @event)
    {
    }

    public void Apply(OtpSubmited @event)
    {
        IsOtpValid = @event.IsValid;
    }


    public string PersonalNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public ChannelTypeEnum ChannelType { get; set; }
    public bool IsOtpValid { get; set; }
    public int? KycParameters { get; set; }
}