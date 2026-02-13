namespace Userland.Morphic;

public sealed class MiniScriptMorphHandle
{
	public MiniScriptMorph Morph { get; }

	public MiniScriptMorphHandle(MiniScriptMorph morph)
	{
		Morph = morph;
	}
}