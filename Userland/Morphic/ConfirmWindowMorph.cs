using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;
using Userland.Morphic.Layout;
using Userland.Services;

namespace Userland.Morphic;

public sealed class ConfirmWindowMorph : WindowMorph
{
	#region Fields

	private readonly Action<bool> _onClose;

	#endregion

	#region Constructors

	public ConfirmWindowMorph(
		string message,
		Action<bool> onClose)
		: base(Point.Empty, new Size(352, 96), "Confirm")
	{
		_onClose = onClose;

		var label = new LabelMorph { Text = message };

		var toolbar = new ToolbarMorph();
		toolbar.AddItem("OK", new ActionCommand(() => OnClose(true)));
		toolbar.AddItem("Cancel", new ActionCommand(() => OnClose(false)));

		var vStack = new VerticalStackMorph();
		vStack.AddMorph(label);
		vStack.AddMorph(toolbar);

		Content.AddMorph(vStack);
	}

	#endregion

	#region Properties

	public override bool WantsKeyboardFocus => true;

	#endregion

	#region Methods

	protected override void OnLoad(IAssetService assetService)
	{
		base.OnLoad(assetService);
		CaptureKeyboard();
	}

	public override void OnKey(KeyEvent e)
	{
		if (e.Action == InputAction.Press)
		{
			if (e.Key == Key.Enter)
			{
				OnClose(true);
			}
			else if (e.Key == Key.Escape)
			{
				OnClose(false);
			}
		}
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Content.Size = Content.Submorphs.First().Size;
		Size = new Size(Content.Size.Width + Padding, Content.Size.Height + Padding + HeaderHeight);
	}

	private void OnClose(bool result)
	{
		Close();
		_onClose(result);
	}

	#endregion
}