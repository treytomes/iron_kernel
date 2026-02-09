using IronKernel.Common;

namespace IronKernel.Modules.AssetLoader.ValueObjects;

public sealed record AssetImageQuery(Guid CorrelationID, string Url) : Query(CorrelationID);
public sealed record AssetImageResponse(Guid CorrelationID, string Url, Image Image) : Response<Image>(CorrelationID, Image);
