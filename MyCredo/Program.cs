using Core.BPM.Application;
using Marten;
using Marten.Events.Projections;
using Marten.Schema.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCredo.Common;
using MyCredo.Features.Loan;
using MyCredo.Features.Loan.ConfirmLoanRequest;
using MyCredo.Features.Loan.Initiating;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.Loan.UploadImage;
using MyCredo.Features.Loann;
using MyCredo.Features.RecoveringPassword;
using MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Features.RecoveringPassword.Finishing;
using MyCredo.Features.RecoveringPassword.GetUserData;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.TwoFactor;
using Weasel.Core;
using OtpSent = MyCredo.Features.TwoFactor.OtpSent;
using ValidateOtp = MyCredo.Features.TwoFactor.ValidateOtp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(c => { c.RegisterServicesFromAssembly(typeof(Program).Assembly); });

builder.Services.AddBpm(options =>
    {
        options.Connection("Host=10.195.105.11; Database=CoreStandingOrders; Username=gelkanishvili; Password=fjem$efXc");
        options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
        options.DatabaseSchemaName = "bpm";
        options.Events.MetadataConfig.HeadersEnabled = true;
        options.Events.MetadataConfig.CausationIdEnabled = true;
        options.Events.MetadataConfig.CorrelationIdEnabled = true;
        options.Projections.Add<CheckCardFlatProjection>(ProjectionLifecycle.Inline);

        options.Events.AddEventType(typeof(RequestLoanInitiated));
        options.Events.AddEventType(typeof(OtpSent));
        options.Events.AddEventType(typeof(OtpValidated));
        options.Events.AddEventType(typeof(PasswordRecoveryInitiated));
        options.Events.AddEventType(typeof(SecurityQuestionValidated));
        options.Events.AddEventType(typeof(CheckCardInitiated));
        options.Events.AddEventType(typeof(FinishedPasswordRecovery));
        options.Events.AddEventType(typeof(CheckCardCompleted));
        options.Events.AddEventType(typeof(UserDataGet));
        options.Events.AddEventType(typeof(UploadedImage));
        options.Events.AddEventType(typeof(ConfirmedCarPawnshop));
        options.Events.AddEventType(typeof(CarPawnshopInitiated));
        //
        options.Events.AddEventType(typeof(FinishedRequestDigitalLoan));
        options.Events.AddEventType(typeof(ConfirmedDigitalLoan));
        options.Events.AddEventType(typeof(DigitalLoanInitiated));
    }, x =>
    {
        x.AddAggregateDefinition<PasswordRecovery, PasswordRecoveryDefinition>();
        x.AddAggregateDefinition<RequestCarPawnshop, RequestCarPawnshopDefinition>();
        x.AddAggregateDefinition<RequestDigitalLoan, RequestDigitalLoanDefinition>();
    }
);

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("car/loan/confirm",
        async ([FromBody] ConfirmLoanRequest requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("confirmLoanRequest")
    .WithOpenApi();

app.MapPost("car/loan/upload-image",
        async ([FromBody] UploadImage requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("uploadImage")
    .WithOpenApi();

app.MapPost("/password-recovery/initiate",
        async (IMediator mediator) => await mediator.Send(new InitiatePasswordRecovery(
            "01010102020",
            new DateTime(1995, 9, 9),
            ChannelTypeEnum.Unclassified)))
    .WithName("InitiatePasswordRecovery")
    .WithOpenApi();

app.MapPost("/password-recovery/check-card",
        async ([FromBody] Guid DocumentId, IMediator mediator) => await mediator.Send(new CheckCardInitiate(DocumentId)))
    .WithName("CheckCard")
    .WithOpenApi();

app.MapPost("/password-recovery/generate-otp",
        async ([FromBody] Guid documentId, IMediator mediator) => { await mediator.Send(new GenerateOtp(documentId)); })
    .WithName("GenerateOtp")
    .WithOpenApi();

app.MapPost("/password-recovery/validate-otp",
        async ([FromBody] Guid documentId, IMediator mediator) => { await mediator.Send(new ValidateOtp(documentId)); })
    .WithName("ValidateOtp")
    .WithOpenApi();

app.MapPost("/load",
        async ([FromBody] Guid documentId, IMediator mediator) => { await mediator.Send(new ValidateOtp(documentId)); })
    .WithName("load")
    .WithOpenApi();

app.MapPost("/digitLoanFinish",
        async ([FromBody] Guid documentId, IMediator mediator) => { await mediator.Send(new FinishDigitalLoan(documentId)); })
    .WithName("digitLoanFinish")
    .WithOpenApi();

app.Run();