namespace Closeoutflow.Shared;
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}