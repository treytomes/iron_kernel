using IronKernel.Common;
using IronKernel.Common.ValueObjects;

namespace IronKernel.Modules.FileSystem.ValueObjects;

public sealed record FileReadQuery(
	Guid CorrelationID,
	string Url
) : Query(CorrelationID);

public sealed record FileReadResponse(
	Guid CorrelationID,
	string Url,
	byte[] Data,
	string? MimeType
) : Response<byte[]>(CorrelationID, Data);



public sealed record FileWriteCommand(
	Guid CorrelationID,
	string Url,
	byte[] Data,
	string? MimeType
) : Command(CorrelationID);

public sealed record FileWriteResult(
	Guid CorrelationID,
	string Url,
	bool Success,
	string? Error
) : Response<bool>(CorrelationID, Success);



public sealed record FileDeleteCommand(
	Guid CorrelationID,
	string Url
) : Command(CorrelationID);

public sealed record FileDeleteResult(
	Guid CorrelationID,
	string Url,
	bool Success,
	string? Error
) : Response<bool>(CorrelationID, Success);



public sealed record DirectoryListQuery(
	Guid CorrelationID,
	string Url
) : Query(CorrelationID);

public sealed record DirectoryListResponse(
	Guid CorrelationID,
	string Url,
	IReadOnlyList<DirectoryEntry> Entries
) : Response<IReadOnlyList<DirectoryEntry>>(CorrelationID, Entries);
