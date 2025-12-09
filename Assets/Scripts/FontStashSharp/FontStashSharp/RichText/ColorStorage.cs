using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

// MobileUO: always use FNA
#if true || MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp.RichText
{
	public static class ColorStorage
	{
		public class ColorInfo
		{
			public Color Color { get; set; }
			public string Name { get; set; }
		}

		public static readonly Dictionary<string, ColorInfo> Colors = new Dictionary<string, ColorInfo>();

		static ColorStorage()
		{
			var type = typeof(Color);

			// MobileUO: TazUO: this reflection doesn't work correctly on Android devices - I think IL2CPP is stripping it
			// so instead we will add them all manually
//#if !STRIDE
//			var colors = type.GetRuntimeProperties();
//			foreach (var c in colors)
//			{
//				if (c.PropertyType != typeof(Color))
//				{
//					continue;
//				}

//				var value = (Color)c.GetValue(null, null);
//				Colors[c.Name.ToLower()] = new ColorInfo
//				{
//					Color = value,
//					Name = c.Name
//				};
//			}
//#else
//			var colors = type.GetRuntimeFields();
//			foreach (var c in colors)
//			{
//				if (c.FieldType != typeof(Color))
//				{
//					continue;
//				}

//				var value = (Color)c.GetValue(null);
//				Colors[c.Name.ToLower()] = new ColorInfo
//				{
//					Color = value,
//					Name = c.Name
//				};
//			}
//#endif
			// Add all the known colors manually
			void Add(string name, Color color)
			{
				Colors[name.ToLower()] = new ColorInfo
				{
					Color = color,
					Name = name
				};
			}

            Add(nameof(Color.Transparent), Color.Transparent);
            Add(nameof(Color.AliceBlue), Color.AliceBlue);
            Add(nameof(Color.AntiqueWhite), Color.AntiqueWhite);
            Add(nameof(Color.Aqua), Color.Aqua);
            Add(nameof(Color.Aquamarine), Color.Aquamarine);
            Add(nameof(Color.Azure), Color.Azure);
            Add(nameof(Color.Beige), Color.Beige);
            Add(nameof(Color.Bisque), Color.Bisque);
            Add(nameof(Color.Black), Color.Black);
            Add(nameof(Color.BlanchedAlmond), Color.BlanchedAlmond);
            Add(nameof(Color.Blue), Color.Blue);
            Add(nameof(Color.BlueViolet), Color.BlueViolet);
            Add(nameof(Color.Brown), Color.Brown);
            Add(nameof(Color.BurlyWood), Color.BurlyWood);
            Add(nameof(Color.CadetBlue), Color.CadetBlue);
            Add(nameof(Color.Chartreuse), Color.Chartreuse);
            Add(nameof(Color.Chocolate), Color.Chocolate);
            Add(nameof(Color.Coral), Color.Coral);
            Add(nameof(Color.CornflowerBlue), Color.CornflowerBlue);
            Add(nameof(Color.Cornsilk), Color.Cornsilk);
            Add(nameof(Color.Crimson), Color.Crimson);
            Add(nameof(Color.Cyan), Color.Cyan);
            Add(nameof(Color.DarkBlue), Color.DarkBlue);
            Add(nameof(Color.DarkCyan), Color.DarkCyan);
            Add(nameof(Color.DarkGoldenrod), Color.DarkGoldenrod);
            Add(nameof(Color.DarkGray), Color.DarkGray);
            Add(nameof(Color.DarkGreen), Color.DarkGreen);
            Add(nameof(Color.DarkKhaki), Color.DarkKhaki);
            Add(nameof(Color.DarkMagenta), Color.DarkMagenta);
            Add(nameof(Color.DarkOliveGreen), Color.DarkOliveGreen);
            Add(nameof(Color.DarkOrange), Color.DarkOrange);
            Add(nameof(Color.DarkOrchid), Color.DarkOrchid);
            Add(nameof(Color.DarkRed), Color.DarkRed);
            Add(nameof(Color.DarkSalmon), Color.DarkSalmon);
            Add(nameof(Color.DarkSeaGreen), Color.DarkSeaGreen);
            Add(nameof(Color.DarkSlateBlue), Color.DarkSlateBlue);
            Add(nameof(Color.DarkSlateGray), Color.DarkSlateGray);
            Add(nameof(Color.DarkTurquoise), Color.DarkTurquoise);
            Add(nameof(Color.DarkViolet), Color.DarkViolet);
            Add(nameof(Color.DeepPink), Color.DeepPink);
            Add(nameof(Color.DeepSkyBlue), Color.DeepSkyBlue);
            Add(nameof(Color.DimGray), Color.DimGray);
            Add(nameof(Color.DodgerBlue), Color.DodgerBlue);
            Add(nameof(Color.Firebrick), Color.Firebrick);
            Add(nameof(Color.FloralWhite), Color.FloralWhite);
            Add(nameof(Color.ForestGreen), Color.ForestGreen);
            Add(nameof(Color.Fuchsia), Color.Fuchsia);
            Add(nameof(Color.Gainsboro), Color.Gainsboro);
            Add(nameof(Color.GhostWhite), Color.GhostWhite);
            Add(nameof(Color.Gold), Color.Gold);
            Add(nameof(Color.Goldenrod), Color.Goldenrod);
            Add(nameof(Color.Gray), Color.Gray);
            Add(nameof(Color.Green), Color.Green);
            Add(nameof(Color.GreenYellow), Color.GreenYellow);
            Add(nameof(Color.Honeydew), Color.Honeydew);
            Add(nameof(Color.HotPink), Color.HotPink);
            Add(nameof(Color.IndianRed), Color.IndianRed);
            Add(nameof(Color.Indigo), Color.Indigo);
            Add(nameof(Color.Ivory), Color.Ivory);
            Add(nameof(Color.Khaki), Color.Khaki);
            Add(nameof(Color.Lavender), Color.Lavender);
            Add(nameof(Color.LavenderBlush), Color.LavenderBlush);
            Add(nameof(Color.LawnGreen), Color.LawnGreen);
            Add(nameof(Color.LemonChiffon), Color.LemonChiffon);
            Add(nameof(Color.LightBlue), Color.LightBlue);
            Add(nameof(Color.LightCoral), Color.LightCoral);
            Add(nameof(Color.LightCyan), Color.LightCyan);
            Add(nameof(Color.LightGoldenrodYellow), Color.LightGoldenrodYellow);
            Add(nameof(Color.LightGray), Color.LightGray);
            Add(nameof(Color.LightGreen), Color.LightGreen);
            Add(nameof(Color.LightPink), Color.LightPink);
            Add(nameof(Color.LightSalmon), Color.LightSalmon);
            Add(nameof(Color.LightSeaGreen), Color.LightSeaGreen);
            Add(nameof(Color.LightSkyBlue), Color.LightSkyBlue);
            Add(nameof(Color.LightSlateGray), Color.LightSlateGray);
            Add(nameof(Color.LightSteelBlue), Color.LightSteelBlue);
            Add(nameof(Color.LightYellow), Color.LightYellow);
            Add(nameof(Color.Lime), Color.Lime);
            Add(nameof(Color.LimeGreen), Color.LimeGreen);
            Add(nameof(Color.Linen), Color.Linen);
            Add(nameof(Color.Magenta), Color.Magenta);
            Add(nameof(Color.Maroon), Color.Maroon);
            Add(nameof(Color.MediumAquamarine), Color.MediumAquamarine);
            Add(nameof(Color.MediumBlue), Color.MediumBlue);
            Add(nameof(Color.MediumOrchid), Color.MediumOrchid);
            Add(nameof(Color.MediumPurple), Color.MediumPurple);
            Add(nameof(Color.MediumSeaGreen), Color.MediumSeaGreen);
            Add(nameof(Color.MediumSlateBlue), Color.MediumSlateBlue);
            Add(nameof(Color.MediumSpringGreen), Color.MediumSpringGreen);
            Add(nameof(Color.MediumTurquoise), Color.MediumTurquoise);
            Add(nameof(Color.MediumVioletRed), Color.MediumVioletRed);
            Add(nameof(Color.MidnightBlue), Color.MidnightBlue);
            Add(nameof(Color.MintCream), Color.MintCream);
            Add(nameof(Color.MistyRose), Color.MistyRose);
            Add(nameof(Color.Moccasin), Color.Moccasin);
            Add(nameof(Color.NavajoWhite), Color.NavajoWhite);
            Add(nameof(Color.Navy), Color.Navy);
            Add(nameof(Color.OldLace), Color.OldLace);
            Add(nameof(Color.Olive), Color.Olive);
            Add(nameof(Color.OliveDrab), Color.OliveDrab);
            Add(nameof(Color.Orange), Color.Orange);
            Add(nameof(Color.OrangeRed), Color.OrangeRed);
            Add(nameof(Color.Orchid), Color.Orchid);
            Add(nameof(Color.PaleGoldenrod), Color.PaleGoldenrod);
            Add(nameof(Color.PaleGreen), Color.PaleGreen);
            Add(nameof(Color.PaleTurquoise), Color.PaleTurquoise);
            Add(nameof(Color.PaleVioletRed), Color.PaleVioletRed);
            Add(nameof(Color.PapayaWhip), Color.PapayaWhip);
            Add(nameof(Color.PeachPuff), Color.PeachPuff);
            Add(nameof(Color.Peru), Color.Peru);
            Add(nameof(Color.Pink), Color.Pink);
            Add(nameof(Color.Plum), Color.Plum);
            Add(nameof(Color.PowderBlue), Color.PowderBlue);
            Add(nameof(Color.Purple), Color.Purple);
            Add(nameof(Color.Red), Color.Red);
            Add(nameof(Color.RosyBrown), Color.RosyBrown);
            Add(nameof(Color.RoyalBlue), Color.RoyalBlue);
            Add(nameof(Color.SaddleBrown), Color.SaddleBrown);
            Add(nameof(Color.Salmon), Color.Salmon);
            Add(nameof(Color.SandyBrown), Color.SandyBrown);
            Add(nameof(Color.SeaGreen), Color.SeaGreen);
            Add(nameof(Color.SeaShell), Color.SeaShell);
            Add(nameof(Color.Sienna), Color.Sienna);
            Add(nameof(Color.Silver), Color.Silver);
            Add(nameof(Color.SkyBlue), Color.SkyBlue);
            Add(nameof(Color.SlateBlue), Color.SlateBlue);
            Add(nameof(Color.SlateGray), Color.SlateGray);
            Add(nameof(Color.Snow), Color.Snow);
            Add(nameof(Color.SpringGreen), Color.SpringGreen);
            Add(nameof(Color.SteelBlue), Color.SteelBlue);
            Add(nameof(Color.Tan), Color.Tan);
            Add(nameof(Color.Teal), Color.Teal);
            Add(nameof(Color.Thistle), Color.Thistle);
            Add(nameof(Color.Tomato), Color.Tomato);
            Add(nameof(Color.Turquoise), Color.Turquoise);
            Add(nameof(Color.Violet), Color.Violet);
            Add(nameof(Color.Wheat), Color.Wheat);
            Add(nameof(Color.White), Color.White);
            Add(nameof(Color.WhiteSmoke), Color.WhiteSmoke);
            Add(nameof(Color.Yellow), Color.Yellow);
            Add(nameof(Color.YellowGreen), Color.YellowGreen);
        }

		public static string ToHexString(this Color c)
		{
			return string.Format("#{0}{1}{2}{3}",
				c.R.ToString("X2"),
				c.G.ToString("X2"),
				c.B.ToString("X2"),
				c.A.ToString("X2"));
		}

		public static string GetColorName(this Color color)
		{
			foreach (var c in Colors)
			{
				if (c.Value.Color == color)
				{
					return c.Value.Name;
				}
			}

			return null;
		}

		public static Color? FromName(string name)
		{
			if (name.StartsWith("#"))
			{
				name = name.Substring(1);
				uint u;
				if (uint.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out u))
				{
					// Parsed value contains color in RGBA form
					// Extract color components

					byte r = 0, g = 0, b = 0, a = 0;

					unchecked
					{
						if (name.Length == 6)
						{
							r = (byte)(u >> 16);
							g = (byte)(u >> 8);
							b = (byte)u;
							a = 255;
						}
						else if (name.Length == 8)
						{
							r = (byte)(u >> 24);
							g = (byte)(u >> 16);
							b = (byte)(u >> 8);
							a = (byte)u;
						}
					}

					return new Color(r, g, b, a);
				}
			}
			else
			{
				ColorInfo result;
				if (Colors.TryGetValue(name.ToLower(), out result))
				{
					return result.Color;
				}
			}

			return null;
		}

		public static Color CreateColor(int r, int g, int b, int a = 255)
		{
			return new Color((byte)r, (byte)g, (byte)b, (byte)a);
		}
	}
}