using System.Drawing;

namespace IronKernel.Common.ValueObjects;

public sealed record AppUpdateTick(double TotalTime, double ElapsedTime);

// The render tick is effectively the vsync signal.
public sealed record AppRenderTick(ulong FrameId, double TotalTime, double ElapsedTime);
// public sealed record AppFrameReady(ulong FrameId);

public sealed record AppResizeEvent(int Width, int Height);
public sealed record AppShutdown();
public sealed record AppAcquiredFocus();
public sealed record AppLostFocus();

public sealed record AppMouseWheelEvent(int OffsetX, int OffsetY);
public sealed record AppMouseMoveEvent(int X, int Y, int DeltaX, int DeltaY);
public sealed record AppMouseButtonEvent(InputAction Action, MouseButton Button, KeyModifier Modifiers);
public sealed record AppKeyboardEvent(InputAction Action, KeyModifier Modifiers, Key Key);


public sealed record AppFbWriteSpan(int X, int Y, IReadOnlyList<RadialColor> Data, bool IsComplete);
public sealed record AppFbWriteRect(
	int X,
	int Y,
	int Width,
	int Height,
	RadialColor[] Data,
	bool IsComplete);
public sealed record AppFbSetBorder(RadialColor Color);

public sealed record AppFbInfoQuery(Guid CorrelationID) : Query(CorrelationID);
public sealed record AppFbInfoResponse(Guid CorrelationID, Size Size) : Response<Size>(CorrelationID, Size);

public sealed record AppAssetImageQuery(Guid CorrelationID, string Url) : Query(CorrelationID);
public sealed record AppAssetImageResponse(Guid CorrelationID, string Url, Image Image) : Response<Image>(CorrelationID, Image);

/// <summary>
/// Read a file as raw bytes from user storage.
/// </summary>
public sealed record AppFileReadQuery(
	Guid CorrelationID,
	string Url
) : Query(CorrelationID);

public sealed record AppFileReadResponse(
	Guid CorrelationID,
	string Url,
	byte[] Data,
	string? MimeType
) : Response<byte[]>(CorrelationID, Data);


/// <summary>
/// Write raw bytes to user storage.
/// </summary>
public sealed record AppFileWriteCommand(
	Guid CorrelationID,
	string Url,
	byte[] Data,
	string? MimeType
) : Command(CorrelationID);

public sealed record AppFileWriteResult(
	Guid CorrelationID,
	string Url,
	bool Success,
	string? Error
) : Response<bool>(CorrelationID, Success);


/// <summary>
/// Delete a file from user storage.
/// </summary>
public sealed record AppFileDeleteCommand(
	Guid CorrelationID,
	string Url
) : Command(CorrelationID);

public sealed record AppFileDeleteResult(
	Guid CorrelationID,
	string Url,
	bool Success,
	string? Error
) : Response<bool>(CorrelationID, Success);


/// <summary>
/// List the contents of a directory (one level).
/// </summary>
public sealed record AppDirectoryListQuery(
	Guid CorrelationID,
	string Url
) : Query(CorrelationID);

public sealed record AppDirectoryListResponse(
	Guid CorrelationID,
	string Url,
	IReadOnlyList<DirectoryEntry> Entries
) : Response<IReadOnlyList<DirectoryEntry>>(CorrelationID, Entries);

// Clipboard messages

public sealed record AppClipboardGetQuery(
	Guid CorrelationID
) : Query(CorrelationID);

public sealed record AppClipboardGetResponse(
	Guid CorrelationID,
	string Text
) : Response<string>(CorrelationID, Text);

public sealed record AppClipboardSetCommand(
	Guid CorrelationID,
	string Text
) : Command(CorrelationID);
