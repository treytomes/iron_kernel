namespace IronKernel.Common;

public abstract record Query(Guid CorrelationID);
public abstract record Response(Guid CorrelationID);
public abstract record Response<TResponse>(Guid CorrelationID, TResponse Data) : Response(CorrelationID);

