# IronKernel

### Working description

We are building a **living retro computing platform**: a small, self-contained computer system implemented in C#, designed to feel closer to an early home computer than a modern desktop app.

At its core is a **microkernel-style architecture**. The kernel runs a set of **supervised modules**, each responsible for a narrow hardware-like concern—window hosting, framebuffer output, input devices, timing, and lifecycle management. These modules communicate exclusively through a typed message bus, enforcing strong boundaries and explicit causality.

The primary display device exposed by the kernel is a **960x480 pixel framebuffer** backed by a **fixed 256-color palette**, composed of six discrete intensity levels each of red, green, and blue (in the spirit of the MiniCraft palette). This constraint is intentional: it defines the aesthetic, performance envelope, and programming model of the system.

Above the kernel sits a **single userland application**. Userland does not directly touch OpenGL, OpenTK, or the host OS. Instead, it interacts with the system through a **bus bridge**, making explicit requests (syscalls) to kernel services and receiving forwarded kernel events.

Inside userland, we are building a **Morphic UI system**—a live, object-centric interface inspired by Smalltalk. Morphs are interactive, inspectable, and modifiable at runtime. This UI layer becomes the primary way the system is explored, programmed, and evolved while it is running.

On top of Morphic, we will construct a **simple operating environment**: windows, inspectors, tools, and workflows that feel more like a living workspace than an application shell. From within that environment, we will ultimately build a **roguelike game**, developed *inside* the system it runs on.
