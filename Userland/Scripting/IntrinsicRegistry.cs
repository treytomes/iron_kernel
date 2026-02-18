namespace Userland.Scripting;

public static class IntrinsicRegistry
{
	public static void Register()
	{
		DialogIntrinsics.Register();
		MorphIntrinsics.Register();
		ColorIntrinsics.Register();
		FileSystemIntrinsics.Register();
		TileMapIntrinsics.Register();
	}
}