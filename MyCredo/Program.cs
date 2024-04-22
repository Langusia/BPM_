using Core.BPM.MediatR;
using Marten;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCredo.Common;
using MyCredo.Features.RecoveringPassword;
using MyCredo.Features.RecoveringPassword.Initiating;
using MyCredo.Features.TwoFactor;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMarten(options =>
{
    options.Connection("Host=10.195.105.11; Database=CoreStandingOrders; Username=gelkanishvili; Password=fjem$efXc");
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
    options.DatabaseSchemaName = "bpm";
    options.Events.MetadataConfig.HeadersEnabled = true;
    options.Events.MetadataConfig.CausationIdEnabled = true;
    options.Events.MetadataConfig.CorrelationIdEnabled = true;
});
builder.Services.AddMediatR(c => { c.RegisterServicesFromAssembly(typeof(Program).Assembly); });
builder.Services.AddBpm(x => { x.AddAggregateDefinition<PasswordRecovery, PasswordRecoveryDefinition>(); }
);

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/password-recovery/initiate",
        async (IMediator mediator) => await mediator.Send(new InitiatePasswordRecovery(
            "01010102020",
            new DateTime(1995, 9, 9),
            ChannelTypeEnum.Unclassified)))
    .WithName("InitiatePasswordRecovery")
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

app.Run();