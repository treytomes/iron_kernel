
# Writing an "operating system"?

What if I wrote my next C# app as if I were writing an operating system?  Travel those same paths of bootloader, kernel initialization, init, userland.


  
  
  Why put yourself through the hassle?


Operating system development has always fascinated me.  I took a class on it in college, which sadly turned into a semester of incredibly boring lectures on the theory behind multi-tasking.  I've tried writing little x86 bootloaders before; the farthest I ever got was while following along on nanobyte's channel:

https://www.youtube.com/@nanobyte-dev
https://github.com/treytomes/cocos

A problem you run into quickly is writing device drivers for all of those little things we take for granted: hard drives, speakers, microphones, keyboard, mouse, etc etc etc.  But it can be fun.


  
  
  The Familiar Boot Sequence Hidden in Plain Sight


There are a series of things that happen every time a C# app that I write starts:


Parse command‑line arguments
Configure logging
Wire up dependency injection
Start hosted services
Hand control over to some central state or application loop


Over time, this hardened into a reusable Bootloader.cs file that I drag from project to project. It’s doing the same job every time: bringing a system to life in a controlled, repeatable way.

That’s when the question changed from “could I write an OS?” to “what if I treated this app like one?”


  
  
  From App Startup to System Boot


Real operating systems follow a well‑understood progression:


A tiny bootloader hands off control to the kernel,
The kernel initializes core subsystems,
The kernel then launches an init process,

init brings up userland.


IronKernel compresses those ideas into a single host process, but the authority boundaries still matter. There is a kernel. There is userland. There is a moment where control is handed off rather than implicitly shared.

That framing is what led to IronKernel’s architecture: a microkernel‑style system written in C#, with narrowly scoped kernel modules, explicit boundaries, and a strict message‑passing model.


  
  
  The Project


All of this eventually became IronKernel:
https://github.com/treytomes/iron_kernel/

It’s a living retro computing platform implemented in C#. The kernel owns the framebuffer, input, timing, and system services. Userland runs as a single application and can only interact with the system through explicit messages, not direct APIs.

I may change the name later. It came from thinking about the old DLR projects—IronPython, IronRuby, IronScheme. If you can have an Iron language, why not an entire Iron kernel?

This project exists because I wanted to see what would happen if I took the discipline of operating system design seriously—without requiring myself to write a PCI driver first.




Stay tuned to learn what it means to build a "bootloader" in a C# app.

```
date: 2026-02-13
type: devlog
source: dev.to
title: "Writing an "operating system"?"
url: https://dev.to/treytomes/writing-an-operating-system-akd
```
