using Closeoutflow.Modules.Jobs;

namespace Closeoutflow.Modules.Closeouts.Application;

public sealed record CompleteJobCloseoutResult(
    Guid CloseoutRecordId,
    Guid JobId,
    JobStatus JobStatus);
    