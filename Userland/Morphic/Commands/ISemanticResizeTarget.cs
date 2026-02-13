namespace IronKernel.Userland.Morphic.Commands;

public interface ISemanticResizeTarget
{
	object CaptureResizeState();
	void RestoreResizeState(object state);
}