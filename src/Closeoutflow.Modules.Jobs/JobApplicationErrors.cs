using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Jobs;

public static class JobApplicationErrors
{
    public static readonly Error NotFound =
        new("Jobs.NotFound", "The requested job was not found.");

    public static readonly Error JobMustBePendingCloseout =
        new("Jobs.JobMustBePendingCloseout", "The job must be pending closeout before it can be completed.");
}