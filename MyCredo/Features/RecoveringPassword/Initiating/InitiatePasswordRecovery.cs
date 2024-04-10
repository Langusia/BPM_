﻿using Core.BPM;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MediatR;
using MyCredo.Common;
using Newtonsoft.Json;

namespace MyCredo.Features.RecoveringPassword.Initiating;

[BpmRequest<PasswordRecovery>]
public record InitiatePasswordRecovery(
    string PersonalNumber,
    DateTime BirthDate,
    ChannelTypeEnum ChannelType)
    : IBpmRootCommand<Guid>;

public class InitiatePasswordRecoveryHandler(BpmProcessManager<PasswordRecovery> mgr)
    : ICommandHandler<InitiatePasswordRecovery, Guid>
{
    public async Task<Result<Guid>> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        //var cc  = BpmProcessGraphConfiguration.GetConfig<PasswordRecovery>();
        var config = BProcessGraphConfiguration.GetConfig<PasswordRecovery>();
        var ss = JsonConvert.SerializeObject(config.RootNode, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            MaxDepth = 1,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        //dostuff

        var agg = PasswordRecovery.Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType);
        // await mgr.StartProcess(
        //     agg,
        //     cancellationToken);

        return Result.Success(agg.Id);
    }
}