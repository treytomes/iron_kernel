using Miniscript;
using Userland.Morphic;

namespace Userland.Scripting;

public sealed class MorphHandleRegistry
{
	// id → morph
	private readonly Dictionary<int, MiniScriptMorph> _morphs = new();

	// morph → id (for cleanup)
	private readonly Dictionary<MiniScriptMorph, int> _reverse = new();

	private readonly Dictionary<int, ValMap> _handles = new();

	private int _nextId = 1;

	/// <summary>
	/// Register a morph and return a MiniScript handle.
	/// </summary>
	public ValMap Register(MiniScriptMorph morph)
	{
		var id = _nextId++;
		_morphs[id] = morph;
		_reverse[morph] = id;
		return CreateHandleForId(id);
	}

	/// <summary>
	/// Resolve a handle to a live morph.
	/// Returns null if invalid or dead.
	/// </summary>
	public MiniScriptMorph? ResolveAlive(Value handle)
	{
		if (handle is not ValMap map)
			return null;

		if (IsDead(map))
			return null;

		if (!TryGetId(map, out var id))
			return null;

		if (!_morphs.TryGetValue(id, out var morph))
			return null;

		if (morph.IsMarkedForDeletion || morph.Owner == null)
		{
			Invalidate(map);
			_morphs.Remove(id);
			_reverse.Remove(morph);
			return null;
		}

		return morph;
	}

	public void Destroy(Value handle)
	{
		if (handle is not ValMap map)
			return;

		if (!TryGetId(map, out var id))
			return;

		if (!_morphs.TryGetValue(id, out var morph))
			return;

		// Mark morph
		morph.MarkForDeletion();

		// Invalidate handle
		Invalidate(map);

		// Remove registry entries
		_morphs.Remove(id);
		_reverse.Remove(morph);
	}

	/// <summary>
	/// Called by the world when a morph is destroyed.
	/// </summary>
	public void OnMorphDestroyed(MiniScriptMorph morph)
	{
		if (_reverse.TryGetValue(morph, out var id))
		{
			_reverse.Remove(morph);
			_morphs.Remove(id);
		}
	}

	/// <summary>
	/// Enumerate all live morphs with handles.
	/// </summary>
	public IEnumerable<(int id, MiniScriptMorph morph)> EnumerateAlive()
	{
		foreach (var pair in _morphs)
		{
			var morph = pair.Value;
			if (morph.IsMarkedForDeletion || morph.Owner == null)
				continue;

			yield return (pair.Key, pair.Value);
		}
	}

	public IEnumerable<ValMap> EnumerateAliveHandles()
	{
		foreach (var (id, morph) in _morphs)
		{
			if (morph.IsMarkedForDeletion || morph.Owner == null)
				continue;

			yield return GetOrCreateHandle(id);
		}
	}

	#region Handle helpers

	private ValMap GetOrCreateHandle(int id)
	{
		if (_handles.TryGetValue(id, out var handle))
			return handle;

		handle = CreateHandleForId(id);
		_handles[id] = handle;
		return handle;
	}

	private static ValMap CreateHandleForId(int id)
	{
		var handle = new ValMap
		{
			["__isa"] = new ValString(nameof(Morph)),
			["__id"] = new ValNumber(id)
		};

		AttachMorphMethods(handle);
		return handle;
	}

	private static void AttachMorphMethods(ValMap handle)
	{
		handle["get"] = Intrinsic.GetByName("slot_get")!.GetFunc().BindAndCopy(handle);
		handle["set"] = Intrinsic.GetByName("slot_set")!.GetFunc().BindAndCopy(handle);
		handle["has"] = Intrinsic.GetByName("slot_has")!.GetFunc().BindAndCopy(handle);
		handle["delete"] = Intrinsic.GetByName("slot_delete")!.GetFunc().BindAndCopy(handle);
		handle["destroy"] = Intrinsic.GetByName("morph_destroy")!.GetFunc().BindAndCopy(handle);
		handle["isAlive"] = Intrinsic.GetByName("morph_isAlive")!.GetFunc().BindAndCopy(handle);
	}

	private static bool TryGetId(ValMap map, out int id)
	{
		id = 0;
		if (!map.TryGetValue(new ValString("__id"), out var idVal))
			return false;

		id = idVal.IntValue();
		return true;
	}

	private static bool IsDead(ValMap map)
	{
		return map.TryGetValue(new ValString("__dead"), out var dead)
			&& dead.BoolValue();
	}

	private static void Invalidate(ValMap handle)
	{
		handle["__dead"] = ValNumber.one;
	}

	#endregion
}