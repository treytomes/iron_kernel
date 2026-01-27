namespace IronKernel.Common;

public static class IApplicationBusExtensions
{
	public static async Task<TResponse> QueryAsync<TQuery, TResponse>(
		this IApplicationBus @this,
		Func<Guid, TQuery> queryFactory,
		CancellationToken cancellationToken = default)
		where TQuery : Query
		where TResponse : Response
	{
		var correlationId = Guid.NewGuid();
		var tcs = new TaskCompletionSource<TResponse>(
			TaskCreationOptions.RunContinuationsAsynchronously);

		IDisposable? sub = null;

		sub = @this.Subscribe<TResponse>(
			$"QueryAsync<{typeof(TResponse).Name}>",
			(msg, ct) =>
			{
				if (msg.CorrelationID != correlationId)
					return Task.CompletedTask;

				sub?.Dispose();
				tcs.TrySetResult(msg);
				return Task.CompletedTask;
			});

		@this.Publish(queryFactory(correlationId));

		using (cancellationToken.Register(() =>
		{
			sub?.Dispose();
			tcs.TrySetCanceled(cancellationToken);
		}))
		{
			return await tcs.Task.ConfigureAwait(false);
		}
	}
}
