using IronKernel.Kernel.Bus;

namespace IronKernel.Modules.Clock;

public sealed record Tick(DateTime UtcNow) : IMessage;
