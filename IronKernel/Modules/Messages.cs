namespace IronKernel.Modules;

public abstract record Query(Guid CorrelationID);
public abstract record Response<TResponse>(Guid CorrelationID, TResponse Data);
