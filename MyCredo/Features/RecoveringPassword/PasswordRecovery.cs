using Core.BPM;
using Core.BPM.Extensions;
using Core.BPM.Interfaces;
using Core.BPM.Interfaces.Builder;
using Core.BPM.MediatR;
using MyCredo.Common;
using MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Features.RecoveringPassword.Finishing;
using MyCredo.Features.RecoveringPassword.IdentifyingFace;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.RecoveringPassword.RequestingPhoneChange;
using MyCredo.Features.RecoveringPassword.ValidatingOtp;

namespace MyCredo.Features.RecoveringPassword;

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

    public static PasswordRecovery Initiate(string personalNumber, DateTime birthDate, ChannelTypeEnum channelType) =>
        new(personalNumber, birthDate, channelType);

    public void GenerateOtp()
    {
        var @event = new GeneratedOtp();
        Enqueue(@event);
        Apply(@event);
    }

    public void ValidateOtp(bool isValid)
    {
        var @event = new ValidatedOtp(isValid);
        Enqueue(@event);
        Apply(@event);
    }

    public void Apply(PasswordRecoveryInitiated @event)
    {
        PersonalNumber = @event.PersonalNumber;
        BirthDate = @event.BirthDate;
        ChannelType = @event.ChannelType;
    }

    public void Apply(GeneratedOtp @event)
    {
    }

    public void Apply(ValidatedOtp @event)
    {
        IsOtpValid = @event.IsValid;
    }

    public string PersonalNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public ChannelTypeEnum ChannelType { get; set; }
    public bool IsOtpValid { get; set; }
    public int? KycParameters { get; set; }
}

public class PasswordRecoveryDefinition
{
    public void Define()
    {
        //onfigure
        //   .StartWith<InitiatePasswordRecovery>()
        //   .ContinueWith<GenerateOtp>(g => g
        //       .ContinueWith<ValidateOtp>(v => v
        //           .ContinueWith<ValidateSecurityQuestion>()
        //           .Or<InitiateCheckCard>(x => x
        //               .ContinueWith<CheckCard>())
        //           .Or<IdentifyFace>())
        //       .Or<ValidatePhoneChange>())
        //   .Or<CheckCard>()
        //   .ContinueWith<FinishPasswordRecovery>()
        //   .ContinueWith<FinishPasswordRecovery>();

        //  configure.SetMap(typeof(GenerateOtp), typeof)(GenerateOtp), typeof(GenerateOtp), typeof(GenerateOtp))
        //      .SetMap(typeof(RecognizeFace), typeof(RecognizeFace), typeof(RecognizeFace), typeof(RecognizeFace));
    }

    public void Define2(IProcessBuilder<PasswordRecovery> configure)
    {
        configure
            .StartWith<InitiatePasswordRecovery>()
            .Continue<ValidateOtp>(x => x
                .Continue<ValidatePhoneChange>()
                .Or1<ValidateSecurityQuestion>())
            .Continue<CheckCard>()
            .Continue<FinishPasswordRecovery>()
            .Or2<ValidateSecurityQuestion>(z => z
                .Continue<InitiatePasswordRecovery>(c => c
                    .Continue<InitiateCheckCard>()
                    .Or1<GenerateOtp>()
                    .Or1<GenerateOtp>())
                .Continue<CheckCard>())
            .Continue<PasswordRecoveryInitiated>();


        //  configure.SetMap(typeof(GenerateOtp), typeof)(GenerateOtp), typeof(GenerateOtp), typeof(GenerateOtp))
        //      .SetMap(typeof(RecognizeFace), typeof(RecognizeFace), typeof(RecognizeFace), typeof(RecognizeFace));
    }
}

//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.OtpValidated,
// PasswordRecoveryStepEnum.CardCheckInitiated,
// PasswordRecoveryStepEnum.CardCheckCompleted,
// PasswordRecoveryStepEnum.Finished
//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.OtpValidated,
// PasswordRecoveryStepEnum.SecurityAnswerValidated,
// PasswordRecoveryStepEnum.Finished
//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.OtpValidated,
// PasswordRecoveryStepEnum.IdentomatCompleted,
// PasswordRecoveryStepEnum.Finished
//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.DidMobileNumberChange,
// PasswordRecoveryStepEnum.Finished
//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.DidMobileNumberChange,
// PasswordRecoveryStepEnum.CardCheckInitiated,
// PasswordRecoveryStepEnum.CardCheckCompleted,
// PasswordRecoveryStepEnum.Finished
//{
// PasswordRecoveryStepEnum.Initiated,
// PasswordRecoveryStepEnum.OtpGenerated,
// PasswordRecoveryStepEnum.DidMobileNumberChange,
// PasswordRecoveryStepEnum.SecurityAnswerValidated,
// PasswordRecoveryStepEnum.Finished
//}