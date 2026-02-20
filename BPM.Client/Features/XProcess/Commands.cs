using BPM.Core.Attributes;
using MediatR;

namespace BPM.Client.Features.XProcess;

[BpmProducer(typeof(S2Completed))]
public record S2(Guid ProcessId) : IRequest;

[BpmProducer(typeof(S3Completed))]
public record S3(Guid ProcessId) : IRequest;

[BpmProducer(typeof(S4Completed))]
public record S4(Guid ProcessId) : IRequest;

[BpmProducer(typeof(S5Completed))]
public record S5(Guid ProcessId) : IRequest;

[BpmProducer(typeof(S6Completed))]
public record S6(Guid ProcessId) : IRequest;

[BpmProducer(typeof(S7Completed))]
public record S7(Guid ProcessId) : IRequest;
