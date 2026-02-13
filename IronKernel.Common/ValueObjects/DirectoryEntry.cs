namespace IronKernel.Common.ValueObjects;

public sealed record DirectoryEntry(
	string Name,
	bool IsDirectory,
	long? Size,
	DateTime LastModified
);