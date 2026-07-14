using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts.Application;

public interface ICompleteJobCloseoutPersistence
{
    Task<Result> SaveAsync(
        Job job,
        CloseoutRecord closeoutRecord,
        CancellationToken cancellationToken = default);
}
