using System.Drawing;

namespace IronKernel.Morphic;

public abstract class Morph
{
	#region Fields

	private readonly List<Morph> _submorphs = new();

	#endregion

	#region Properties

	public Point Position { get; set; }
	public Size Size { get; set; }
	public bool Visible { get; set; } = true;

	public Morph? Owner { get; private set; }

	public IReadOnlyList<Morph> Submorphs => _submorphs;

	#endregion

	#region Methods

	public void AddMorph(Morph morph)
	{
		if (morph.Owner != null)
			throw new InvalidOperationException("Morph already has an owner");

		morph.Owner = this;
		_submorphs.Add(morph);
	}

	public void RemoveMorph(Morph morph)
	{
		if (_submorphs.Remove(morph))
			morph.Owner = null;
	}

	/// <summary>
	/// Draw this morph. Coordinates are in world space.
	/// </summary>
	public virtual void Draw(IMorphicCanvas canvas)
	{
		foreach (var child in _submorphs)
		{
			if (!child.Visible) continue;
			child.Draw(canvas);
		}
	}

	/// <summary>
	/// Hit testing (later used for mouse / halos).
	/// </summary>
	public virtual bool ContainsPoint(Point p)
	{
		return new Rectangle(Position, Size).Contains(p);
	}

	#endregion
}
