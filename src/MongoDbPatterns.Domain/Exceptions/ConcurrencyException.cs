namespace MongoDbPatterns.Domain.Exceptions;

public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid aggregateId, int expectedVersion)
        : base($"Concurrency conflict on aggregate '{aggregateId}' at expected version {expectedVersion}.")
    {
    }
}
