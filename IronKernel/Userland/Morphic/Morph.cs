using System.Drawing;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

public abstract class Morph
{
	#region Fields

	private readonly List<Morph> _submorphs = new();

	#endregion

	#region Properties

	public Point Position { get; set; }
	public Size Size { get; set; }
	public bool Visible { get; set; } = true;
	public bool IsHovered { get; private set; } = false;

	public Morph? Owner { get; private set; }

	public IReadOnlyList<Morph> Submorphs => _submorphs;
	public virtual bool WantsKeyboardFocus => false;
	public virtual bool IsSelectable => true;
	public virtual bool IsGrabbable => false;
	public bool IsMarkedForDeletion { get; private set; } = false;

	protected MorphicStyle? Style => GetWorld()?.Style;

	#endregion

	#region Methods

	public void MarkForDeletion()
	{
		IsMarkedForDeletion = true;
	}

	internal void ClearDeletionMark()
	{
		IsMarkedForDeletion = false;
	}

	public void AddMorph(Morph morph)
	{
		if (morph == null)
			throw new ArgumentNullException(nameof(morph));
		if (morph.Owner != null)
			throw new InvalidOperationException("Morph already has an owner");

		morph.Owner = this;
		_submorphs.Add(morph);

		// If we're already in a world, load immediately.
		if (TryGetWorld(out var world))
			morph.NotifyAddedToWorld(world);
	}

	public void RemoveMorph(Morph morph)
	{
		if (_submorphs.Remove(morph))
			morph.Owner = null;
	}

	/// <summary>
	/// Draw this morph. Coordinates are in world space.
	/// </summary>
	public virtual void Draw(IRenderingContext rc)
	{
		foreach (var child in _submorphs)
		{
			if (!child.Visible) continue;
			child.Draw(rc);
		}
	}

	public virtual void Update(double deltaTime)
	{
		// Default: do nothing.
		foreach (var child in Submorphs) child.Update(deltaTime);
	}

	/// <summary>
	/// Hit testing (later used for mouse / halos).
	/// </summary>
	public virtual bool ContainsPoint(Point p)
	{
		return new Rectangle(Position, Size).Contains(p);
	}

	public virtual Morph? FindMorphAt(Point p)
	{
		// Traverse top-down so last added is "on top"
		for (int i = Submorphs.Count - 1; i >= 0; i--)
		{
			var child = Submorphs[i];
			if (!child.Visible) continue;

			var found = child.FindMorphAt(p);
			if (found != null)
				return found;
		}

		return ContainsPoint(p) ? this : null;
	}

	internal void SetHovered(bool value)
	{
		if (IsHovered == value)
			return;

		IsHovered = value;

		if (value)
			OnPointerEnter();
		else
			OnPointerLeave();
	}

	protected virtual void OnLoad(IAssetService assetService) { }

	protected virtual void OnPointerEnter() { }
	protected virtual void OnPointerLeave() { }

	public virtual void OnPointerDown(PointerDownEvent e) { }
	public virtual void OnPointerUp(PointerUpEvent e) { }
	public virtual void OnPointerMove(PointerMoveEvent e) { }
	public virtual void OnKeyDown(AppKeyboardEvent e) { }

	public void DispatchPointerDown(PointerDownEvent e)
	{
		OnPointerDown(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerDown(e);
	}

	public void DispatchPointerUp(PointerUpEvent e)
	{
		OnPointerUp(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerUp(e);
	}

	public void DispatchPointerMove(PointerMoveEvent e)
	{
		OnPointerMove(e);
		if (!e.Handled && Owner != null)
			Owner.DispatchPointerMove(e);
	}

	/// <summary>
	/// Mark this Morph's rendering area as needing a redraw.
	/// </summary>
	public virtual void Invalidate()
	{
		// no-op for now
	}

	protected bool TryGetWorld(out WorldMorph world)
	{
		if (this is WorldMorph w)
		{
			world = w;
			return true;
		}

		if (Owner != null)
			return Owner.TryGetWorld(out world);

		world = null!;
		return false;
	}

	protected WorldMorph GetWorld()
	{
		if (this is WorldMorph) return (this as WorldMorph)!;
		return (Owner ?? throw new InvalidOperationException("World is missing.")).GetWorld();
	}

	internal void NotifyAddedToWorld(WorldMorph world)
	{
		OnLoad(world.Assets);

		foreach (var child in _submorphs)
			child.NotifyAddedToWorld(world);
	}

	#endregion
}
