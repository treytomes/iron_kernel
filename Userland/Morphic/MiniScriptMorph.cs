using Miniscript;

namespace Userland.Morphic;

public class MiniScriptMorph : Morph
{
	private readonly Dictionary<string, Value> _slots = new();

	public MiniScriptMorph()
		: base()
	{
		IsSelectable = true;
		ShouldClipToBounds = true;
	}

	public bool HasSlot(string key) => _slots.ContainsKey(key);
	public TValue? GetSlot<TValue>(string key) where TValue : Value => (_slots.TryGetValue(key, out var v) ? v : null) as TValue;
	public void SetSlot(string key, Value value) => _slots[key] = value;
	public bool DeleteSlot(string key) => _slots.Remove(key);
}
