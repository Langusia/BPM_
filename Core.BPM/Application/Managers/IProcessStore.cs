using System.Threading;
using System.Threading.Tasks;

namespace Core.BPM.Application.Managers;

public interface IProcessStore
{
    Task AppendUncommittedToDb(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}