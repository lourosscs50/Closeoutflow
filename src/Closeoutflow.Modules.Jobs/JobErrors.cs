using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Jobs;

public static class JobErrors
{
    public static readonly Error AlreadyStarted =
        new("Jobs.AlreadyStarted", "The job has already been started.");

    public static readonly Error CannotMarkPendingFromCurrentStatus =
        new("Jobs.CannotMarkPendingFromCurrentStatus", "Only in-progress jobs can be marked as pending closeout.");

    public static readonly Error CannotCloseFromCurrentStatus =
        new("Jobs.CannotCloseFromCurrentStatus", "Only jobs pending closeout can be closed.");
}