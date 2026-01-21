using IronKernel.Common;

namespace IronKernel.Modules.AssetLoader.ValueObjects;

public sealed record AssetImageQuery(Guid CorrelationID, string AssetId) : Query(CorrelationID);
public sealed record AssetImageResponse(Guid CorrelationID, string AssetId, Image Image) : Response<Image>(CorrelationID, Image);
