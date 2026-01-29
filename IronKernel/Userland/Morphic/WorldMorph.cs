using System.Drawing;
using IronKernel.Common.ValueObjects;
using IronKernel.Userland.Gfx;
using IronKernel.Userland.Morphic.Commands;
using IronKernel.Userland.Morphic.Events;

namespace IronKernel.Userland.Morphic;

public sealed class WorldMorph : Morph
{
	#region Fields

	private HaloMorph? _halo;
	private readonly WorldCommandManager _commandManager = new();

	#endregion

	#region Constructors

	public WorldMorph(Size screenSize, IAssetService assets)
	{
		Assets = assets;
		Position = Point.Empty;
		Size = screenSize;

		Hand = new HandMorph();
		AddMorph(Hand);
	}

	#endregion

	#region Properties

	public WorldCommandManager Commands => _commandManager;
	public IAssetService Assets { get; }
	public new MorphicStyle Style { get; } = MorphicStyles.Default;
	public Morph? PointerFocus { get; private set; }
	public Morph? KeyboardFocus { get; private set; }
	public HandMorph Hand { get; }
	public Morph? SelectedMorph { get; private set; }
	public Morph? PointerCapture { get; private set; }
	public Morph? HoveredMorph { get; private set; }

	#endregion

	#region Methods

	public void CapturePointer(Morph morph)
	{
		PointerCapture = morph;
	}

	public void ReleasePointer(Morph morph)
	{
		if (PointerCapture == morph)
			PointerCapture = null;
	}

	public override void Draw(IRenderingContext rc)
	{
		base.Draw(rc);
		// TODO: Hand will be drawn twice.
		Hand.Draw(rc);
	}

	public override void Update(double deltaTime)
	{
		// Execute all deferred mutation intents.
		_commandManager.Flush();

		base.Update(deltaTime);

		CommitDeletions();
	}

	public void PointerButton(MouseButton button, InputAction action)
	{
		if (button != MouseButton.Left)
			return;

		var position = Hand.Position;
		var target = FindMorphAt(position) ?? this;

		if (action == InputAction.Press)
		{
			Commands.BeginTransaction();

			var e = new PointerDownEvent(button, position)
			{
				Target = target
			};

			// World first (selection, halos)
			OnPointerDown(e);

			if (!e.Handled)
			{
				PointerFocus = target;

				if (target.WantsKeyboardFocus)
					KeyboardFocus = target;

				target.DispatchPointerDown(e);
			}

			// Grab AFTER dispatch so halos win
			if (!e.Handled && target != this && target != Hand && target.IsGrabbable)
			{
				Hand.Grab(target, position);
			}
		}
		else if (action == InputAction.Release)
		{
			var e = new PointerUpEvent(position);

			if (PointerCapture != null)
			{
				PointerCapture.DispatchPointerUp(e);
			}
			else if (PointerFocus != null)
			{
				PointerFocus.DispatchPointerUp(e);
			}

			PointerFocus = null;
			Hand.Release();

			Commands.CommitTransaction();
		}
	}

	public void PointerMove(Point p)
	{
		Hand.MoveTo(p);
		Hand.Update();

		// --- HOVER RESOLUTION ---
		var newHover = FindMorphAt(p);

		if (newHover != HoveredMorph)
		{
			HoveredMorph?.SetHovered(false);
			HoveredMorph = newHover;
			HoveredMorph?.SetHovered(true);
		}

		var e = new PointerMoveEvent(p);

		// --- DISPATCH ---
		if (PointerCapture != null)
		{
			PointerCapture.DispatchPointerMove(e);
		}
		else if (PointerFocus != null)
		{
			PointerFocus.DispatchPointerMove(e);
		}
		else
		{
			Hand.DispatchPointerMove(e);
		}
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		if (e.Target == null) return;

		// Clicking a halo should NOT affect selection.
		if (!e.Target.IsSelectable)
		{
			// e.MarkHandled();
			return;
		}

		if (e.Target == this)
		{
			ClearSelection();
			e.MarkHandled();
			return;
		}

		SelectMorph(e.Target);
	}

	public void SelectMorph(Morph? morph)
	{
		if (SelectedMorph == morph)
			return;

		ClearSelection();

		if (morph == null || morph == this)
			return;

		SelectedMorph = morph;

		_halo = new HaloMorph(morph);
		morph.Owner?.AddMorph(_halo);
	}

	public void ClearSelection()
	{
		if (_halo != null)
		{
			_halo.Owner?.RemoveMorph(_halo);
			_halo = null;
		}

		SelectedMorph = null;
	}

	public void CommitDeletions()
	{
		Sweep(this);
	}

	private void Sweep(Morph parent)
	{
		for (var i = parent.Submorphs.Count - 1; i >= 0; i--)
		{
			var child = parent.Submorphs[i];

			if (child.IsMarkedForDeletion)
			{
				if (IsAncestorOrSelf(child, SelectedMorph)) ClearSelection();
				parent.RemoveMorph(child);
				continue;
			}

			Sweep(child);
		}
	}

	private bool IsAncestorOrSelf(Morph root, Morph? target)
	{
		while (target != null)
		{
			if (target == root) return true;
			target = target.Owner;
		}
		return false;
	}

	#endregion
}
