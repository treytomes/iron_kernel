using System.Drawing;
using IronKernel.Modules.ApplicationHost;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;
using IronKernel.Userland.Morphic.Handles;

namespace IronKernel.Userland.Morphic;

public abstract class Morph : ICommandTarget
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
	public bool IsSelectable { get; set; } = true;
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

	public void AddMorph(Morph morph, int index = -1)
	{
		if (morph == null)
			throw new ArgumentNullException(nameof(morph));
		if (morph.Owner != null)
			throw new InvalidOperationException("Morph already has an owner");

		morph.Owner = this;
		if (index >= 0)
		{
			_submorphs.Insert(index, morph);
		}
		else
		{
			_submorphs.Add(morph);
		}

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

	#region ICommandTarget Implementation

	/// <summary>
	/// Determines whether this morph is willing to execute the given command.
	/// Subclasses should override to restrict behavior.
	/// </summary>
	public virtual bool CanExecute(ICommand command)
	{
		return command switch
		{
			MoveCommand => true,
			ResizeCommand => true,
			DeleteCommand => Owner != null,
			_ => false
		};
	}

	/// <summary>
	/// Executes the given command, applying this morph's rules.
	/// </summary>
	public virtual void Execute(ICommand command)
	{
		switch (command)
		{
			case MoveCommand move:
				ExecuteMove(move);
				break;

			case ResizeCommand resize:
				ExecuteResize(resize);
				break;

			case DeleteCommand delete:
				delete.Execute(); // deletion owns its own logic
				break;
		}
	}

	/// <summary>
	/// Undoes the effects of a previously executed command.
	/// </summary>
	public virtual void Undo(ICommand command)
	{
		switch (command)
		{
			case MoveCommand move:
				UndoMove(move);
				break;

			case ResizeCommand resize:
				UndoResize(resize);
				break;

			case DeleteCommand delete:
				delete.Undo();
				break;
		}
	}

	#endregion

	#region Move handling

	private Point _previousPosition;

	/// <summary>
	/// Applies a move command.
	/// </summary>
	protected virtual void ExecuteMove(MoveCommand command)
	{
		_previousPosition = Position;
		Position = new Point(
			Position.X + command.DeltaX,
			Position.Y + command.DeltaY);

		Invalidate();
	}

	/// <summary>
	/// Reverts a move command.
	/// </summary>
	protected virtual void UndoMove(MoveCommand command)
	{
		Position = _previousPosition;
		Invalidate();
	}

	#endregion

	#region Resize handling

	private Size _previousSize;
	private Point _previousResizePosition;

	/// <summary>
	/// Applies a resize command.
	/// Default behavior is free resize.
	/// </summary>
	protected virtual void ExecuteResize(ResizeCommand command)
	{
		_previousSize = Size;
		_previousResizePosition = Position;

		var newSize = Size;
		var newPosition = Position;

		switch (command.Handle)
		{
			case ResizeHandle.TopLeft:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height - command.DeltaY);
				newPosition = new Point(Position.X + command.DeltaX, Position.Y + command.DeltaY);
				break;

			case ResizeHandle.TopRight:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height - command.DeltaY);
				newPosition = new Point(Position.X, Position.Y + command.DeltaY);
				break;

			case ResizeHandle.BottomLeft:
				newSize = new Size(Size.Width - command.DeltaX, Size.Height + command.DeltaY);
				newPosition = new Point(Position.X + command.DeltaX, Position.Y);
				break;

			case ResizeHandle.BottomRight:
				newSize = new Size(Size.Width + command.DeltaX, Size.Height + command.DeltaY);
				break;
		}

		Size = ClampSize(newSize);
		Position = newPosition;

		Invalidate();
	}

	/// <summary>
	/// Reverts a resize command.
	/// </summary>
	protected virtual void UndoResize(ResizeCommand command)
	{
		Size = _previousSize;
		Position = _previousResizePosition;
		Invalidate();
	}

	/// <summary>
	/// Ensures the size remains valid.
	/// Subclasses may override.
	/// </summary>
	protected virtual Size ClampSize(Size size)
	{
		return new Size(
			Math.Max(1, size.Width),
			Math.Max(1, size.Height));
	}

	#endregion

	#endregion
}
