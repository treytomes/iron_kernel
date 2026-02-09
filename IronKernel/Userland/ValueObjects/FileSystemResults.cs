namespace IronKernel.Userland.ValueObjects;

public sealed record FileReadResult(
	byte[] Data,
	string? MimeType
);

public sealed record FileWriteResult(
	bool Success,
	string? Error
);

public sealed record FileDeleteResult(
	bool Success,
	string? Error
);