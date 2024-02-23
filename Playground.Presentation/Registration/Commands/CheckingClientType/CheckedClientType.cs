using Core.BPM.Interfaces;

namespace Playground.Presentation.Registration.Commands.CheckingClientType;

public record CheckedClientType(string PersonId, ClientType ClientType) : IEvent;