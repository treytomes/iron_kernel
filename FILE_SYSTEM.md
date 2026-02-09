# File System Access

## Overview

IronKernel provides a **kernel‑mediated file system** designed to strictly separate **userland code** from direct access to the host operating system. User applications never touch the host file system APIs directly. All file access is performed through a **message bus**, routed across a **bus bridge**, and handled by a kernel module that enforces security and confinement.

This design is consistent with IronKernel’s broader architecture, where userland interacts with system services exclusively via explicit bus messages rather than direct OS calls [3].

---

## Architectural Model

### Layered Access

File access flows through the system as follows:

```
Userland code
   ↓
Application Bus (App* messages)
   ↓
ApplicationBusBridge
   ↓
Kernel Bus (File* messages)
   ↓
FileSystemModule (kernel)
   ↓
Host operating system
```

Key properties of this model:

- **Userland cannot access the host filesystem directly**
- **All access is explicit and inspectable**
- **The kernel is the sole authority** for file I/O
- **Policies and confinement are enforced centrally**

This mirrors how other kernel services (framebuffer, assets, input) are exposed to userland [3].

---

## URL‑Based Addressing

Files are addressed using **logical URLs**, not host paths.

### Supported Schemes

- `file://…`  
  Refers to user‑accessible persistent storage.
- `asset://…`  
  Refers to packaged, read‑only assets handled by the AssetLoaderModule (not the file system).

Userland code never sees real filesystem paths such as `C:\…` or `/home/…`.

Example:

```text
file://documents/readme.txt
```

---

## Where Files Are Stored on the Host

On the host system, all user‑accessible files are confined to a **sandboxed directory**:

```
<AppUserDataRoot>/user/
```

The base user data root is resolved in a **cross‑platform manner** by the kernel:

- Windows: `%AppData%/<AppName>/user`
- Linux: `~/.config/<AppName>/user`
- macOS: `~/Library/Application Support/<AppName>/user`

The kernel ensures:

- The directory exists
- Paths cannot escape this root
- Path traversal (`..`) is rejected

Userland never learns the physical location of this directory.

---

## Kernel: FileSystemModule

The **FileSystemModule** is a kernel module responsible for:

- Resolving `file://` URLs
- Enforcing sandbox boundaries
- Reading and writing raw bytes
- Listing directories
- Deleting files
- Reporting success or failure via responses

The module does **not** interpret file contents. All data is treated as opaque bytes with an optional MIME type.

Supported kernel messages include:

- `FileReadQuery` / `FileReadResponse`
- `FileWriteCommand` / `FileWriteResult`
- `FileDeleteCommand` / `FileDeleteResult`
- `DirectoryListQuery` / `DirectoryListResponse`

---

## Application Bus Messages

Userland communicates using **App‑level messages**, which are bridged to kernel messages by the `ApplicationHostModule`.

Examples:

- `AppFileReadQuery` → `FileReadQuery`
- `FileReadResponse` → `AppFileReadResponse`
- `AppFileWriteCommand` → `FileWriteCommand`

This translation step ensures userland remains decoupled from kernel internals while still using strongly typed messages.

---

## Userland File System Service

In userland, file access is typically performed via an **`IFileSystem` service**, which wraps application‑bus messaging and exposes a simple async API:

```csharp
Task<FileReadResult> ReadAsync(string url);
Task<FileWriteResult> WriteAsync(string url, byte[] data, string? mime);
Task<FileDeleteResult> DeleteAsync(string url);
Task<IReadOnlyList<DirectoryEntry>> ListDirectoryAsync(string url);
```

Internally, this service uses:

- `QueryAsync` for read‑only operations (reads, directory listing)
- `CommandAsync` for mutating operations (write, delete)

This keeps the semantic distinction between *queries* (“ask”) and *commands* (“do”).

---

## Text Files and Higher‑Level Usage

Text editing tools (such as the Morphic text editor) do **not** interact with files directly. Instead:

1. Files are loaded as UTF‑8 text via the file system service.
2. Text is edited entirely in memory (`TextDocument`, `TextEditingCore`).
3. Saving emits a write command back through the bus.

Convenience helpers (e.g. `ReadTextAsync`, `WriteTextAsync`) live purely in userland and layer on top of the byte‑oriented API.

---

## Security and Policy

The file system design enforces several important guarantees:

- **No ambient authority**  
  Userland cannot “accidentally” access files.
- **Explicit intent**  
  Every file operation is an explicit bus message.
- **Central enforcement**  
  All policy lives in the kernel.
- **Auditable behavior**  
  File operations can be logged, filtered, or denied centrally.

This aligns with IronKernel’s overall philosophy of explicit, message‑driven interaction between userland and kernel services [3].

---

## Summary

- Userland never accesses the host filesystem directly.
- All file I/O flows through the application bus and kernel bus.
- Files are confined to a sandboxed `user/` directory under the app’s user data root.
- URLs (`file://…`) abstract away physical paths.
- The design scales cleanly to text files, binary assets, and future storage backends.

This file system model supports IronKernel’s goals of safety, inspectability, and a living, evolvable userland environment built on Morphic principles.
