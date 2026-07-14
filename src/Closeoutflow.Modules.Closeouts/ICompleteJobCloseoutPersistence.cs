using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Modules.Closeouts.Application;

public interface ICompleteJobCloseoutPersistence
{
    Task SaveAsync(
        Job job,
        CloseoutRecord closeoutRecord,
        CancellationToken cancellationToken = default);
}
