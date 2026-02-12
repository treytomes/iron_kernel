using IronKernel.Common;

namespace IronKernel.Modules.Clipboard.ValueObjects;

public sealed record ClipboardGetQuery(
	Guid CorrelationID
) : Query(CorrelationID);

public sealed record ClipboardGetResponse(
	Guid CorrelationID,
	string Text
) : Response<string>(CorrelationID, Text);

public sealed record ClipboardSetCommand(
	Guid CorrelationID,
	string Text
) : Command(CorrelationID);
