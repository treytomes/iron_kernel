namespace IronKernel.Applications.DemoApp;

public sealed record PingMessage(int Value);
public sealed record PongMessage(int Value);
public sealed record TickMessage(int Tick);
