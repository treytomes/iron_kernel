using System.Drawing;
using IronKernel.Common.ValueObjects;
using Userland.Morphic.Commands;
using Userland.Morphic.Events;
using Userland.Morphic.Layout;
using Userland.Services;

namespace Userland.Morphic;

public sealed class AlertWindowMorph : WindowMorph
{
	#region Fields

	private readonly Action _onClose;

	#endregion

	#region Constructors

	public AlertWindowMorph(string message, Action onClose)
		: base(Point.Empty, new Size(352, 96), "Alert")
	{
		_onClose = onClose;

		var vStack = new VerticalStackMorph();
		vStack.AddMorph(new LabelMorph { Text = message });
		vStack.AddMorph(new ButtonMorph(Point.Empty, new Size(24, 12), "OK")
		{
			Command = new ActionCommand(OnClose),
		});
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
			if (e.Key == Key.Escape || e.Key == Key.Enter)
			{
				OnClose();
			}
		}
	}

	protected override void UpdateLayout()
	{
		base.UpdateLayout();
		Content.Size = Content.Submorphs.First().Size;
		Size = new Size(Content.Size.Width + Padding, Content.Size.Height + Padding + HeaderHeight);
	}

	private void OnClose()
	{
		Close();
		_onClose();
	}

	#endregion
}