namespace IronKernel.Common;

public abstract record Query(Guid CorrelationID);
public abstract record Response(Guid CorrelationID);
public abstract record Response<TResponse>(Guid CorrelationID, TResponse Data) : Response(CorrelationID);

/// <summary>
/// Represents an imperative request to perform an action.
/// Commands do not imply a response.
/// </summary>
public abstract record Command(Guid CorrelationID);
