using Core.BPM.Process;
using Core.BPM.Attributes;
using Core.BPM.Events;
using MediatR;

namespace BPM.Client.Features.XProcess;

[BpmProducer(typeof(S1Completed))]
public record S1(string Name) : IRequest<Guid>;

public class S1Handler(IProcessStore store) : IRequestHandler<S1, Guid>
{
    public async Task<Guid> Handle(S1 request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<XAggregate>(new S1Completed(request.Name));
        process.AppendEvent(new S2Completed(true));
        process.AppendEvent(new S4Completed(42));
        process.AppendEvent(new S5Completed());
        process.AppendEvent(new S7Completed());
        await store.SaveChangesAsync(cancellationToken);
        return process.Id;
    }
}
