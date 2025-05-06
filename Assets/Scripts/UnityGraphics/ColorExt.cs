using UnityEngine;

public static class ColorExt
{

	public static string ToHexString(this Color32 c)
	{
		return string.Format("{0:X2}{1:X2}{2:X2}", c.r, c.g, c.b);
	}

	public static string ToHexString(this Color color)
	{
		Color32 c = color;
		return c.ToHexString();
	}

	public static uint ToHex(this Color32 c)
	{
		return (uint)((c.a << 24) | (c.r << 16) | (c.g << 8) | (c.b));
	}

	public static uint ToHex(this Color color)
	{
		Color32 c = color;
		return c.ToHex();
	}
}
