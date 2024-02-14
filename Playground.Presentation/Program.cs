using Core.BPM;
using Core.BPM.DependencyInjection;
using Core.BPM.Interfaces;
using Marten;
using Marten.Events.Projections;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Playground.Application;
using Playground.Application.Documents;
using Playground.Application.Documents.Onboarding;
using Playground.Application.Documents.Registration;
using Playground.Presentation;
using Weasel.Core;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<TwoFactorValidator>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterUser).Assembly));
builder.Services.AddMarten(options =>
{
    options.Connection("Server=localhost;Port=5432;User Id=zenki;Password=123asdASD;Database=zenki;");
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
    options.DatabaseSchemaName = "bpm";
    options.Projections.Add<RegistrationProjection>(ProjectionLifecycle.Live);

    // If we're running in development mode, let Marten just take care
    // of all necessary schema building and patching behind the scenes
    //if (builder.Environment.IsDevelopment())
    //{
    //    options.AutoCreateSchemaObjects = AutoCreate.All;
    //}
});

builder.Services.AddBpm(x =>
{
    x.AddBpmDefinition<Register, RegisterDefinition>();
    x.AddBpmDefinition<Register, RegisterDefinition>();
    x.AddBpmDefinition<Register, RegisterDefinition>();
    x.AddBpmDefinition<Register, RegisterDefinition>();
    x.AddBpmDefinition<Register, RegisterDefinition>();
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("user/register",
        async (IMediator mediator) => await mediator.Send(new RegisterUser(Guid.NewGuid(), "testman")))
    .WithName("RegisterUser")
    .WithOpenApi();

//app.MapGet("users", async (IMediator mediator) => await mediator.Send(new GetUsers()))
//    .WithName("GetUsers")
//    .WithOpenApi();
//
//
//app.MapPost("registration/check-clientType",
//        (IDocumentSession store) => { return RegistrationHandler.InitiateRegistarion(21, store); })
//    .WithName("clientType")
//    .WithOpenApi();
//
//app.MapPost("registration/check-2fa",
//        ([FromBody] Guid Id, [FromServices] TwoFactorValidator validator,
//            [FromServices] BpmProcessManager<Register, CheckClientType2> bpm, [FromServices] IDocumentSession store) =>
//        {
//            return RegistrationHandler.Check2Fa(Id, validator, store,bpm);
//        })
//    .WithName("Check2FA4Registration")
//    .WithOpenApi();
//
//
//app.MapGet("documents/registration-aggregateProjectionAsync/{DocumentId}",
//        ([FromRoute] Guid DocumentId, IDocumentSession store) =>
//        {
//            return RegistrationHandler.AggregateRegistrationStream(DocumentId, store);
//        })
//    .WithName("AggregateRegistrationDocument")
//    .WithOpenApi();

app.Run();