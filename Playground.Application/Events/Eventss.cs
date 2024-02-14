using Core.BPM.Interfaces;
using MediatR;

namespace Playground.Application.Events;

public record Registration(int clientId) : IProcess;

public record CheckClientType(Guid DocumentId, int clientId) : IRequest<bool>, IEvent;

public record CheckClientType2(Guid DocumentId, int clientId) : IEvent;

public record CheckClientType3(Guid DocumentId, int clientId) : IEvent;