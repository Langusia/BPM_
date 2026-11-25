using System;
using BPM.Core.Attributes;
using MediatR;

namespace BPM.Tests.Application.AmbiguousDomain;

// Same simple name as BPM.Tests.Application.OpenTicket, different CLR type —
// used to prove ambiguous command names surface a structured error.
[BpmProducer(typeof(PingReceived))]
public record OpenTicket(string Reason) : IRequest<Guid>;
