﻿using Core.BPM;
using Core.BPM.Application;
using Core.BPM.Application.Managers;
using Core.BPM.BCommand;
using Core.BPM.Configuration;
using Core.BPM.DefinitionBuilder;
using MyCredo.Common;
using MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Features.RecoveringPassword.Finishing;
using MyCredo.Features.RecoveringPassword.IdentifyingFace;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.RecoveringPassword.RequestingPhoneChange;
using MyCredo.Features.TwoFactor;
using ValidateOtp = MyCredo.Features.TwoFactor;

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

    //public void Initiate(string personalNumber, DateTime birthDate, ChannelTypeEnum channelType)
    //{
    //    Id = Guid.NewGuid();
    //    var @event = new PasswordRecoveryInitiated(personalNumber, birthDate, channelType);
    //    Enqueue(@event);
    //    Apply(@event);
    //}

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
        SetBpmProps(@event);
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

public class PasswordRecoveryDefinition : BpmDefinition<PasswordRecovery>
{
    //public override void DefineProcess(IProcessBuilder<IssueLoan> configureProcess)
    //{
    //    configureProcess
    //        .StartWith<InitiateIssueLoanProcess>()
    //        .ThenContinue<GenerateContract>(x =>
    //            x.ThenContinue<GenerateSchedule>(y =>
    //                y.ThenContinueOptional<GenerateInsuranceDocuments>()))
    //        .Or<GenerateSchedule>(x =>
    //            x.ThenContinue<GenerateContract>(y =>
    //                y.ThenContinueOptional<GenerateInsuranceDocuments>()))
    //        .ContinueAnyTime<SendOtp>()
    //        .ContinueAnyTime<ValidateOtp>()
    //        .Continue<MarkDocumentsAsAssigned>()
    //        .Continue<FinishIssueLoan>();
    //}
    public override MyClass<PasswordRecovery> DefineProcess(IProcessBuilder<PasswordRecovery> configure) =>
        configure.StartWith<InitiatePasswordRecovery>()
            .UnlockOptional<A>()
            .UnlockOptional<B>()
            .Continue<GenerateOtp>()
            .Continue<Z>()
            .End();


    public override void ConfigureSteps(StepConfigurator<PasswordRecovery> stepConfigurator)
    {
        stepConfigurator.Configure<GenerateOtp>()
            .SetProcessPreCondition(x => x.ChannelType == ChannelTypeEnum.Unclassified)
            .SetMaxCount(3);
    }
}