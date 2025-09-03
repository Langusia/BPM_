using Core.BPM.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCredo.Common;
using MyCredo.Features.Loan;
using MyCredo.Features.Loan.ConfirmLoanRequest;
using MyCredo.Features.Loan.LoanV9;
using MyCredo.Features.Loan.UploadImage;
using MyCredo.Features.Loann;
using MyCredo.Features.RecoveringPassword;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.TwoFactor;
using ValidateOtp = MyCredo.Features.TwoFactor.ValidateOtp;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(c => { c.RegisterServicesFromAssembly(typeof(Program).Assembly); });

builder.Services.AddBpm("bpm", "Host = 10.195.105.11;Database = MyCredoRetailLoan;Username = MyCredoRetailLoanUser;Password = Fgr$fdsf#fSF",
    x =>
    {
        x.AddAggregateDefinition<PasswordRecovery, PasswordRecoveryDefinition>();
        x.AddAggregateDefinition<OtpValidation, OtpValidationDefinition>();
        x.AddAggregate<TwoFactor>();
        x.AddAggregateDefinition<RequestDigitalLoan, RequestDigitalLoanDefinition>();
        //x.AddAggregateDefinition<IssueCreditCard, IssueCreditCardDefinition>();
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


app.MapPost("playground/query",
        async ([FromBody] Q requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("aaa")
    .WithOpenApi();

app.MapPost("playground/query2",
        async ([FromBody] Q2 requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("aaa2")
    .WithOpenApi();

app.MapPost("playground/do/init-dl",
        async ([FromBody] B requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("confirmLbbbboanRequest")
    .WithOpenApi();

app.MapPost("car/loan/confirm",
        async ([FromBody] ConfirmLoanRequest requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("confirmcccLoanRequest")
    .WithOpenApi();


app.MapPost("car/loan/upload-image",
        async ([FromBody] UploadImage requestLoan, IMediator mediator) => await mediator.Send(requestLoan))
    .WithName("uploadImage")
    .WithOpenApi();

app.MapPost("/password-recovery/initiate",
        async (IMediator mediator) => await mediator.Send(new InitiatePasswordRecovery(
            "01010102020",
            new DateTime(1995, 9, 9),
            ChannelTypeEnum.MOBILE_CIB)))
    .WithName("InitiatePasswordRecovery")
    .WithOpenApi();

app.MapPost("loan/init",
        async (IMediator mediator) => await mediator.Send(new InitiateIssueLoanProcess()))
    .WithName("initiateLoanRequest")
    .WithOpenApi();

app.MapPost("/loan/schedule",
        async (IMediator mediator) => await mediator.Send(new GenerateSchedule()))
    .WithName("generate-schedule")
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