using IronKernel.Kernel.Bus;

namespace IronKernel.Kernel.Messages;

public sealed record KernelStarting : IMessage;
public sealed record KernelStarted : IMessage;
public sealed record KernelStopping : IMessage;
public sealed record KernelStopped : IMessage;

public sealed record ModuleStarted(Type Module) : IMessage;
public sealed record ModuleStopped(Type Module) : IMessage;
public sealed record ModuleFaulted(Type ModuleType, string TaskName, Exception Exception) : IMessage;

public sealed record ChaosTrigger(string Mode);
