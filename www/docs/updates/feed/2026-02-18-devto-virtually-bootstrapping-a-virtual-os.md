
# Virtually bootstrapping a virtual OS.

The purpose of a bootloader is to allow an operating system to pick itself up by its bootstraps; to begin building something out of nothing.  It starts with whatever BIOS or UEFI generates, then with as little fanfare as possible makes room for the kernel and passes execution onward, never to be heard from again.

In a C# app, the Main function (typically Program.Main) should act as a bootloader.  Its a static function, typically in a static class, so no long-term state to talk about.  Creating a "new" Program class should feel weird; you never need more than one Program.  In the apps that I build, I use this entry point to pull in whatever details the host OS wants to give us, parse command-line parameters, set up logging and dependency injection, then pass off execution to a higher-level "kernel" object that knows nothing of the command-line or service container.  And never come back.

This is the role of the Program class in the IronKernel system.  It's a rather boring, one-way operation that looks like it could be part of almost any other program.  To a point.

The first responsibility of the bootloader is collecting boot parameters.  In a traditional OS, this comes from firmware, jumpers, or a kernel command line.  In IronKernel, this takes the form of command‑line options: where userland lives, which configuration file to load, and whether debug mode is enabled. These values are gathered once, normalized, and never reinterpreted later.

Next comes environment construction. Using the .NET Generic Host, the bootloader defines:


where configuration comes from (and that it is immutable),
how logging works and how verbose it should be,
and which core services exist at all.


This is the rough equivalent of switching CPU modes, establishing memory maps, and bringing up early I/O. The mechanics are different, but the responsibility is the same: deciding what assumptions the kernel is allowed to make.

With the environment established, the bootloader then assembles the kernel.  Core kernel services—the message bus, kernel state, scheduler, virtual display, and resource manager—are registered. Kernel modules are discovered by reflection and wired in, but not yet executed.  At this point, the kernel exists only as a loaded image, not a running system.

The most important boundary comes next: userland loading. The bootloader explicitly loads the userland assembly, isolates it via a separate load context, discovers the single user application it contains, and registers it behind an interface. This is IronKernel’s equivalent of selecting and launching the init process.  The kernel decides what userland is allowed to be, and userland never gets to see how that decision was made.

Finally, the bootloader defines shutdown semantics. Ctrl‑C does not kill the process; it signals the kernel.  Even teardown is mediated through the kernel rather than imposed from outside.

Only after all of this is complete does the bootloader transfer control:

kernel.StartAsync(ct)

That call is the hand-off point. Everything before it is bootloader logic.  Everything after it belongs to the kernel proper.

And just like a real bootloader, Program should never try to come back.




Stay tuned next time to learn more about IronKernel's kernel!


  
  
  Reference



IronKernel's "Bootloader"

```
date: 2026-02-18
type: devlog
source: dev.to
title: "Virtually bootstrapping a virtual OS."
url: https://dev.to/treytomes/virtually-bootstrapping-a-virtual-os-4158
```
