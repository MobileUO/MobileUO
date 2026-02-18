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
using Microsoft.Xna.Framework;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility.Collections;

namespace Assistant
{
    internal class SearchExemption : Gump
    {
        public override GumpType GumpType => GumpType.Assistant;

        private static ClassicUO.Utility.Collections.OrderedDictionary<string, List<ushort>> Exemptions = new ClassicUO.Utility.Collections.OrderedDictionary<string, List<ushort>>()
            {
                { "Backpack", new List<ushort>(){0x9B2, 0xE75} },
                { "Bag", new List<ushort>(){0xE76} },
                { "Barrel", new List<ushort>(){0xE77, 0xE7F, 0xE83, 0xFAE} },
                { "Basket", new List<ushort>(){0x990, 0x9AC, 0x9B1, 0x207B} },
                { "Finished Wooden Chest", new List<ushort>(){0x2811, 0x2812} },
                { "Gilded Wooden Chest", new List<ushort>(){0x280F, 0x2810} },
                { "Large Bag Ball", new List<ushort>(){0x2257} },
                { "Large Crate", new List<ushort>(){0xE3D, 0xE3C} },
                { "Medium Crate", new List<ushort>(){0xE3F, 0xE3E} },
                { "Metal Box", new List<ushort>(){0x9A8, 0xE80} },
                { "Metal Chest", new List<ushort>(){0x9AB, 0xE7C} },
                { "Metal Golden Chest", new List<ushort>(){0xe40, 0xe41} },
                { "Ornate Wooden Chest", new List<ushort>(){0x2DF1, 0x2DF2, 0x2DF3,0x2DF4} },
                { "Picnic Basket", new List<ushort>(){0xE7A} },
                { "Plain Wooden Chest", new List<ushort>(){0x280D, 0x280E, 0x2813, 0x2814} },
                { "Pouch", new List<ushort>(){0xE79} },
                { "Small Bag Ball", new List<ushort>(){0x2256} },
                { "Small Crate", new List<ushort>(){0x9A9, 0xE7E, 0x1E80, 0x1E81} },
                { "Wooden Box", new List<ushort>(){0x9AA, 0xE7D} },
                { "Wooden Chest", new List<ushort>(){0xE42, 0xE43} },
                { "Wooden Foot Locker", new List<ushort>(){0x280B, 0x280C, 0x2815, 0x2816, 0x2817, 0x2818} },
                { "Armoires and Drawers", new List<ushort>(){0xA2C, 0xA2D, 0xA2E, 0xA2F, 0xA30, 0xA31, 0xA32, 0xA33, 0xA34, 0xA35, 0xA36, 0xA37, 0xA38, 0xA39, 0xA3A, 0xA3B, 0xA3C, 0xA3D, 0xA3E, 0xA3F, 0xA40, 0xA41, 0xA42, 0xA43, 0xA44, 0xA45, 0xA46, 0xA47, 0xA48, 0xA49, 0xA4A, 0xA4B, 0xA4C, 0xA4D, 0xA4E, 0xA4F, 0xA50, 0xA51, 0xA52, 0xA53, 0x1e70, 0x1e71, 0x1e79, 0x1e7a, 0x2857, 0x2858, 0x2859, 0x285A, 0x285B, 0x285C, 0x285D, 0x285E, 0x28d7, 0x28d8, 0x2DEF, 0x2DF0} },
                { "Bookcase and Shelves", new List<ushort>(){0xA97, 0xA98, 0xA99, 0xA9A, 0xA9B, 0xA9C, 0xA9D, 0xA9E, 0x1E7E, 0x3084, 0x3085, 0x3086, 0x3087} }
            };

        internal SearchExemption() : base(ClassicUO.Client.Game.UO.World, 0, 0)
        {
            AssistantGump gump = UOSObjects.Gump;
            if (gump == null || gump.IsDisposed)
            {
                Dispose();
                return;
            }
            AcceptMouseInput = true;
            CanMove = true;
            IsModal = true;
            CanCloseWithRightClick = true;
            CanCloseWithEsc = false;
            X = gump.X + (gump.Width >> 2);
            Y = gump.Y + (gump.Height >> 3);
            int w = 310;
            int h = 300;
            Width = w;
            Height = h;
            Add(new AlphaBlendControl(gump.Alpha) { X = 1, Y = 1, Width = w - 2, Height = h - 2 });
            AssistantGump.CreateRectangleArea(this, 10, 10, w - 20, h - 40, 0, Color.Gray.PackedValue, 2, "Search Exemption");
            AssistScrollArea area = new AssistScrollArea(15, 15, w - 40, h - 50);
            for (int i = 0; i < Exemptions.Count; ++i)
            {
                var cb = AssistantGump.CreateCheckBox(area, Exemptions.GetItem(i).Key, SearchExemptionSelected[i], 0, 2);
                cb.ValueChanged += Cb_ValueChanged;
            }
            Add(area);
            Add(new AssistNiceButton((w >> 1) - 29, h - 25, 60, 20, ButtonAction.Activate, "OKAY") { ButtonParameter = 555, IsSelectable = false });
        }

        public override void OnButtonClick(int buttonID)
        {
            Dispose();
        }

        private void Cb_ValueChanged(object sender, EventArgs e)
        {
            if (sender is AssistCheckbox cb)
            {
                if (!string.IsNullOrEmpty(cb.Text))
                {
                    int idx = Exemptions.IndexOf(cb.Text);
                    if (idx >= 0)
                    {
                        SearchExemptionSelected[idx] = !SearchExemptionSelected[idx];
                        if (SearchExemptionSelected[idx])
                        {
                            ExemptGraphics.UnionWith(Exemptions[idx]);
                        }
                        else
                        {
                            ExemptGraphics.ExceptWith(Exemptions[idx]);
                        }
                        XmlFileParser.SaveData();
                    }
                }
            }
        }

        internal static void AddExemptions(List<string> conts)
        {
            foreach (string conttype in conts)
            {
                if (!string.IsNullOrEmpty(conttype))
                {
                    int num = Exemptions.IndexOf(conttype);
                    if (num >= 0)
                    {
                        ExemptGraphics.UnionWith(Exemptions[num]);
                        SearchExemptionSelected[num] = true;
                        XmlFileParser.SaveData();
                    }
                }
            }
        }

        internal static void ClearAll()
        {
            ExemptGraphics.Clear();
            SearchExemptionSelected = new bool[Exemptions.Count];
        }

        internal static List<string> ActivatedExemptions()
        {
            List<string> list = new List<string>();
            for(int i = 0; i < SearchExemptionSelected.Length; ++i)
            {
                if(SearchExemptionSelected[i])
                {
                    list.Add(Exemptions.GetItem(i).Key);
                }
            }
            return list;
        }

        private static bool[] SearchExemptionSelected = new bool[Exemptions.Count];
        private static HashSet<ushort> ExemptGraphics = new HashSet<ushort>();
        internal static bool IsExempt(ushort graphic)
        {
            return ExemptGraphics.Contains(graphic);
        }

        internal static bool IsExempt(UOItem item)
        {
            while(item != null)
            {
                if (item.IsContainer && IsExempt(item.Graphic))
                {
                    if (item.Container == UOSObjects.Player || (item.Container is uint cser && cser == UOSObjects.Player.Serial))
                        return false;
                    return true;
                }
                if (item.Container is UOItem cont)
                    item = cont;
                else if (item.Container is uint ser)
                    item = UOSObjects.FindItem(ser);
            }
            return false;
        }
    }
}
