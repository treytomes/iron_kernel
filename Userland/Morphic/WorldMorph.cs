using System.Collections.Concurrent;
using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;
using Userland.Services;
using Miniscript;
using Userland.Morphic.Halo;

namespace Userland.Morphic;

public sealed class WorldMorph : Morph
{
	#region Fields

	private HaloMorph? _halo;
	private readonly WorldCommandManager _commandManager = new();
	private readonly Interpreter _interpreter = new();
	private readonly ScriptOutputHub _scriptOutput = new();
	private readonly ConcurrentQueue<string> _scriptQueue = new();
	private readonly ConcurrentQueue<Action> _actionQueue = new();
	private bool _scriptBusy = false;
	private readonly WorldScriptContext _scriptContext;

	#endregion

	#region Constructors

	public WorldMorph(Size screenSize, IAssetService assets, IServiceProvider services)
	{
		_scriptContext = new WorldScriptContext(this, services);
		_interpreter.hostData = _scriptContext;
		_scriptOutput.Attach(_interpreter);

		Assets = assets;
		Position = Point.Empty;
		Size = screenSize;

		Hand = new HandMorph();
		AddMorph(Hand);
	}

	#endregion

	#region Properties

	public Interpreter Interpreter => _interpreter;
	public ScriptOutputHub ScriptOutput => _scriptOutput;
	public WorldScriptContext ScriptContext => _scriptContext;

	public WorldCommandManager Commands => _commandManager;
	public IAssetService Assets { get; }
	public new MorphicStyle Style { get; } = MorphicStyles.Default;
	public Morph? KeyboardFocus { get; private set; }
	public HandMorph Hand { get; }
	public Morph? SelectedMorph { get; private set; }
	public Morph? PointerCapture { get; private set; }
	public Morph? HoveredMorph { get; private set; }

	#endregion

	#region Methods

	public void EnqueueScript(string scriptSource)
	{
		_scriptQueue.Enqueue(scriptSource);
	}

	public void EnqueueAction(Action action)
	{
		_actionQueue.Enqueue(action);
	}

	public void CapturePointer(Morph? morph)
	{
		PointerCapture = morph;
	}

	public void ReleasePointer(Morph morph)
	{
		if (PointerCapture == morph)
			PointerCapture = null;
	}

	public void CaptureKeyboard(Morph? morph)
	{
		KeyboardFocus = morph;
		if (morph != null) OnGainedKeyboardFocus(morph);
	}

	public void ReleaseKeyboard(Morph? morph)
	{
		if (KeyboardFocus == morph)
		{
			if (morph != null) OnLostKeyboardFocus(morph);
			KeyboardFocus = null;
		}
	}

	public override void Update(double deltaTime)
	{
		// Advance MiniScript VM
		_interpreter.RunUntilDone(deltaTime);

		// Execute exactly one REPL line per frame (or per update)
		if (!_scriptBusy && _scriptQueue.TryDequeue(out var line))
		{
			try
			{
				_scriptBusy = true;
				_interpreter.REPL(line);
			}
			finally
			{
				_scriptBusy = false;
			}
		}

		while (_actionQueue.TryDequeue(out var action)) action();

		// Execute all deferred mutation intents.
		_commandManager.Flush();

		// Debug.Assert(!Submorphs.Any(m => m == null), "Null sub-morph introduced during command flush");

		base.Update(deltaTime);
		CommitDeletions();
		EnsureHandOnTop();
	}

	private void EnsureHandOnTop()
	{
		if (Submorphs == null || Submorphs.Count == 0) return;

		var count = Submorphs.Count;
		if (count == 0 || Submorphs[count - 1] != Hand)
		{
			RemoveMorph(Hand);
			AddMorph(Hand);
		}
	}

	public void PointerButton(MouseButton button, InputAction action, KeyModifier modifiers)
	{
		var position = Hand.Position;
		var target = FindMorphAt(position) ?? this;

		if (action == InputAction.Press)
		{
			// --- Halo gesture (Ctrl + Right Click) ---
			if (button == MouseButton.Right && modifiers.HasFlag(KeyModifier.Control))
			{
				var e0 = new PointerDownEvent(button, position, modifiers)
				{
					Target = target
				};

				// World-only handling: open halo, do not dispatch
				OnPointerDown(e0);
				return;
			}

			// --- PRIMARY INTERACTION ---
			Commands.BeginTransaction();

			var e = new PointerDownEvent(button, position, modifiers)
			{
				Target = target
			};

			OnPointerDown(e);

			// Resolve focus candidate
			var focusTarget = FindSelectableAncestor(target);

			// --- KEYBOARD FOCUS POLICY ---
			if (focusTarget != null && focusTarget.WantsKeyboardFocus)
			{
				// Clicking on a focusable morph → give it keyboard focus
				CaptureKeyboard(focusTarget);
			}
			else if (KeyboardFocus != null)
			{
				// Clicking elsewhere → release keyboard focus
				ReleaseKeyboard(KeyboardFocus);
			}

			// --- DISPATCH POINTER DOWN ---
			if (!e.Handled)
			{
				target.DispatchPointerDown(e);
			}

			// --- GRAB ---
			if (!e.Handled && target != this && target != Hand && target.IsGrabbable)
			{
				Hand.Grab(target, position);
			}
		}
		else if (action == InputAction.Release)
		{
			var e = new PointerUpEvent(button, position, modifiers);

			if (PointerCapture != null)
			{
				PointerCapture.DispatchPointerUp(e);
			}

			Hand.Release();
			Commands.CommitTransaction();
		}
	}

	public void KeyPress(InputAction action, KeyModifier modifiers, Key key)
	{
		var e = new KeyEvent(action, modifiers, key);
		if (KeyboardFocus != null)
		{
			KeyboardFocus.OnKey(e);
		}
	}

	public void PointerWheel(Point delta)
	{
		var e = new PointerWheelEvent(delta);

		// If a morph has pointer capture, it exclusively receives wheel events
		if (PointerCapture != null)
		{
			PointerCapture.DispatchPointerWheel(e);
			return;
		}

		// Otherwise, deliver to the currently hovered morph
		HoveredMorph?.DispatchPointerWheel(e);
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
		else
		{
			Hand.DispatchPointerMove(e);
		}
	}

	public override void OnPointerDown(PointerDownEvent e)
	{
		base.OnPointerDown(e);

		if (e.Target == null)
			return;

		var selectable = FindSelectableAncestor(e.Target);

		if (SelectedMorph != null)
		{
			if (selectable == null || selectable is HaloMorph || !selectable.IsEffectivelyHovered)
			{
				ClearSelection();
				return;
			}
		}

		if (selectable == null)
		{
			return;
		}

		if (selectable == this && !(_halo?.IsEffectivelyHovered ?? false))
		{
			ClearSelection();
			e.MarkHandled();
			return;
		}

		if (e.Button == MouseButton.Right && e.Modifiers.HasFlag(KeyModifier.Control))
		{
			SelectMorph(selectable);
		}
	}

	public void SelectMorph(Morph? morph)
	{
		if (SelectedMorph == morph)
		{
			return;
		}

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
			if (TryGetWorld(out var world))
				world.ReleasePointer(_halo);

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
			if (child == null) continue;

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

	private Morph? FindSelectableAncestor(Morph? morph)
	{
		while (morph != null && !morph.IsSelectable) morph = morph.Owner;
		return morph;
	}

	#endregion
}
