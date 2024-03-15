namespace Core.BPM.MediatR;

public interface IBpmValidationManager
{
    Task ValidateAsync<TCommand>(Guid documentId, CancellationToken cancellationToken);
    bool ValidateConfig<TCommand>();
}