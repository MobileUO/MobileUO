#region License
// Copyright (C) 2022-2025 Sascha Puligheddu
// 
// This project is a complete reproduction of AssistUO for MobileUO and ClassicUO.
// Developed as a lightweight, native assistant.
// 
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// 
// SPECIAL PERMISSION: Integration with projects under BSD 2-Clause (like ClassicUO)
// is permitted, provided that the integrated result remains publicly accessible 
// and the AGPL-3.0 terms are respected for this specific module.
//
// This program is distributed WITHOUT ANY WARRANTY. 
// See <https://www.gnu.org> for details.
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant
{
    internal class Region
    {
        private static Dictionary<string, byte> MapNames = new Dictionary<string, byte>() { { "felucca", 0 }, { "trammel", 1 }, { "ilshenar", 2 }, { "dungeon_semi-chiusi", 32 }, { "dungeon_chiusi", 33 }, { "eventi", 34 } };
        internal static Dictionary<string, RegionType> RegionTypes = new Dictionary<string, RegionType>() 
        { 
            { "town", RegionType.Town }, { "faction", RegionType.Town }, { "city", RegionType.Town },
            { "guards", RegionType.Guards }, { "guarded", RegionType.Guards }, { "guard", RegionType.Guards },
            { "dungeon", RegionType.Dungeon }, { "dungeons", RegionType.Dungeon },
            { "forest", RegionType.Forest },
            { "any", RegionType.Any }
        };

        internal enum RegionType
        {
            None,
            Town,
            Guards,
            Dungeon,
            Forest,
            Any
        }

        private static Dictionary<byte, Dictionary<RegionType, List<Region>>> MapRegions = new Dictionary<byte, Dictionary<RegionType, List<Region>>>(MapNames.Count);

        internal static void AddRegion(string map, string name, string type, List<Rectangle3D> area)
        {
            if(!string.IsNullOrEmpty(map) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type) && area != null && area.Count > 0 && MapNames.TryGetValue(map.ToLower(XmlFileParser.Culture), out byte mapid) && RegionTypes.TryGetValue(type.ToLower(XmlFileParser.Culture), out RegionType rtype) && rtype != RegionType.Any)
            {
                if(!MapRegions.TryGetValue(mapid, out var pairs))
                {
                    MapRegions[mapid] = pairs = new Dictionary<RegionType, List<Region>>();
                }
                if(!pairs.TryGetValue(rtype, out List<Region> regs))
                {
                    pairs[rtype] = regs = new List<Region>();
                }
                regs.Add(new Region(name, area));
            }
        }

        internal static string Contains(Point3D p, string type, int range = 24)
        {
            if (!string.IsNullOrEmpty(type))
            {
                type = type.ToLower(XmlFileParser.Culture);
                if (UOSObjects.Player == null || string.IsNullOrEmpty(type) || !RegionTypes.TryGetValue(type, out RegionType rtype))
                    return null;

                if (MapRegions.TryGetValue(UOSObjects.Player.MapIndex, out var dict))
                {
                    if (rtype == RegionType.Any)
                    {
                        foreach (var kvp in dict)
                        {
                            foreach (var reg in kvp.Value)
                            {
                                if (reg.IsInside(p) && Utility.InRange(UOSObjects.Player.Position, p, range))
                                    return reg.Name;
                            }
                        }
                    }
                    else if (dict.TryGetValue(rtype, out var list))
                    {
                        foreach (var reg in list)
                        {
                            if (reg.IsInside(p))
                                return reg.Name;
                        }
                    }
                }
            }
            return null;
        }

        private string Name { get; }
        private List<Rectangle3D> Area { get; }
        private Region(string name, List<Rectangle3D> area)
        {
            Name = name;
            Area = area;
        }

        private bool IsInside(Point3D p)
        {
            for(int i = Area.Count - 1; i >= 0; --i)
            {
                Rectangle3D rect = Area[i];
                if (rect.Contains(p))
                    return true;
            }
            return false;
        }
    }
}
