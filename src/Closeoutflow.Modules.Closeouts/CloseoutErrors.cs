using Closeoutflow.Shared;

namespace Closeoutflow.Modules.Closeouts;

public static class CloseoutErrors
{
    public static readonly Error JobIdRequired =
        new(
            "Closeouts.JobIdRequired",
            "A job ID is required to create a closeout.");

    public static readonly Error ProofRequired =
        new(
            "Closeouts.ProofRequired",
            "At least one proof item is required to create a closeout.");

    public static readonly Error ProofValueRequired =
        new(
            "Closeouts.ProofValueRequired",
            "A value is required.");

    public static readonly Error AlreadyExistsForJob =
        new(
            "Closeouts.AlreadyExistsForJob",
            "A closeout record already exists for this job.");
}
