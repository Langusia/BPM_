using BPM.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using BPM.Client.Features.OrderFulfillment;
using BPM.Client.Features.OrderFulfillment.Initiating;
using BPM.Client.Features.OrderFulfillment.Verification;
using BPM.Client.Features.OrderFulfillment.Payment;
using BPM.Client.Features.OrderFulfillment.Shipping;
using BPM.Client.Features.OrderFulfillment.Completion;
using BPM.Client.Features.UserRegistration;
using BPM.Client.Features.UserRegistration.Initiating;
using BPM.Client.Features.UserRegistration.VerifyEmail;
using BPM.Client.Features.UserRegistration.SetupProfile;
using BPM.Client.Features.UserRegistration.Completion;
using BPM.Client.Features.XProcess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(c => { c.RegisterServicesFromAssembly(typeof(Program).Assembly); });

builder.Services.AddBpm("bpm", builder.Configuration.GetConnectionString("Bpm")!,
    x =>
    {
        x.AddAggregateDefinition<OrderFulfillment, OrderFulfillmentDefinition>();
        x.AddAggregateDefinition<UserRegistration, UserRegistrationDefinition>();
        x.AddAggregateDefinition<XAggregate, XAggregateDefinition>();
    }
);

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Order Fulfillment endpoints
app.MapPost("/orders/initiate",
        async ([FromBody] InitiateOrder request, IMediator mediator) => await mediator.Send(request))
    .WithName("InitiateOrder")
    .WithOpenApi();

app.MapPost("/orders/verify",
        async ([FromBody] VerifyOrder request, IMediator mediator) => await mediator.Send(request))
    .WithName("VerifyOrder")
    .WithOpenApi();

app.MapPost("/orders/pay",
        async ([FromBody] ProcessPayment request, IMediator mediator) => await mediator.Send(request))
    .WithName("ProcessPayment")
    .WithOpenApi();

app.MapPost("/orders/ship",
        async ([FromBody] ShipOrder request, IMediator mediator) => await mediator.Send(request))
    .WithName("ShipOrder")
    .WithOpenApi();

app.MapPost("/orders/complete",
        async ([FromBody] CompleteOrder request, IMediator mediator) => await mediator.Send(request))
    .WithName("CompleteOrder")
    .WithOpenApi();

// User Registration endpoints
app.MapPost("/registration/initiate",
        async ([FromBody] InitiateRegistration request, IMediator mediator) => await mediator.Send(request))
    .WithName("InitiateRegistration")
    .WithOpenApi();

app.MapPost("/registration/verify-email",
        async ([FromBody] VerifyUserEmail request, IMediator mediator) => await mediator.Send(request))
    .WithName("VerifyEmail")
    .WithOpenApi();

app.MapPost("/registration/setup-profile",
        async ([FromBody] SetupUserProfile request, IMediator mediator) => await mediator.Send(request))
    .WithName("SetupProfile")
    .WithOpenApi();

app.MapPost("/registration/complete",
        async ([FromBody] CompleteRegistration request, IMediator mediator) => await mediator.Send(request))
    .WithName("CompleteRegistration")
    .WithOpenApi();

// XProcess endpoint
app.MapPost("/x/run",
        async ([FromBody] S1 request, IMediator mediator) => await mediator.Send(request))
    .WithName("RunXProcess")
    .WithOpenApi();

app.Run();
