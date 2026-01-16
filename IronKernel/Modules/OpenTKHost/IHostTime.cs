namespace IronKernel.Modules.OpenTKHost;

public interface IHostTime
{
	event Action<double> UpdateTick;
	event Action<double> RenderTick;
}
