namespace IronKernel.Morphic;

public abstract class MorphicEvent
{
	public bool Handled { get; private set; }
	public Morph? Target { get; internal set; }

	public void MarkHandled() => Handled = true;
}
