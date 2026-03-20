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
// See <https://www.gnu.org/licenses/agpl-3.0.html> for details.
#endregion

using System.Collections.Generic;
using ClassicUO.Utility.Collections;
using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.Configuration;
using ClassicUO.IO;
using ClassicUO;
using System.Linq;
using System;

namespace Assistant
{
    internal class Scavenger
    {
        private static uint _Bag;
        internal static ClassicUO.Utility.Collections.OrderedDictionary<ushort, List<ItemDisplay>> ItemIDsHues { get; } = new ClassicUO.Utility.Collections.OrderedDictionary<ushort, List<ItemDisplay>>();

        private static UOItem _BagRef;

        public static void Initialize()
        {
            PacketHandler.RegisterClientToServerViewer(0x09, new PacketViewerCallback(OnSingleClick));

            UOItem.OnItemCreated += CheckBagOPL;
        }

        internal static void AddToHotBag()
        {
            UOSObjects.Player.SendMessage(MsgLevel.Force, "Scavenger: Target Item to Scavenge");
            Targeting.OneTimeTarget(false, OnTarget);
        }

        internal static void SetHotBag()
        {
            UOSObjects.Player.SendMessage("Scavenger: Target the Scavenger HotBag");
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTargetBag));
        }

        private static void CheckBagOPL(UOItem item)
        {
            if (item.Serial == _Bag)
            {
                _BagRef = item;
                Timer.DelayedCallback(TimeSpan.FromMilliseconds(200), OnPositiveCheck).Start();
            }
        }

        private static void OnPositiveCheck()
        {
            if(_BagRef != null && _BagRef.ObjPropList != null)
                _BagRef.ObjPropList.Add("Scavenger HotBag");
        }

        private static void OnSingleClick(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            if (_Bag == serial)
            {
                ushort gfx = 0;
                UOItem c = UOSObjects.FindItem(_Bag);
                if (c != null)
                {
                    gfx = c.ItemID;
                }


                ClientPackets.PRecv_UnicodeMessage(_Bag, gfx, MessageType.Label, 0x3B2, 3, Settings.GlobalSettings.Language, "", "Scavenger HotBag");
            }
        }

        internal static void ClearAll()
        {
            Enabled = false;
            ItemIDsHues.Clear();
            Cached.Activator(false);
            _BagRef = null;
            _Bag = 0;
        }

        internal static void OnEnabledChanged()
        {
            Enabled = UOSObjects.Gump.EnabledScavenger.IsChecked;
        }

        internal static void OnStackChanged()
        {
            Stack = UOSObjects.Gump.StackOresAtFeet.IsChecked;
        }

        private static bool _Enabled;
        public static bool Enabled
        {
            get => _Enabled;
            private set
            {
                _Enabled = value;
                Cached.Activator(_Enabled);
            }
        }

        public static bool Stack { get; private set; }

        private class Cached : Timer
        {
            private static byte _Pos = 0;
            private static Cached _Timer { get; } = new Cached();
            private static HashSet<uint>[] _Cached { get; } = new HashSet<uint>[2] { new HashSet<uint>(), new HashSet<uint>() };
            internal Cached() : base(TimeSpan.Zero, TimeSpan.FromSeconds(10))
            {
            }

            internal static void Add(uint s)
            {
                _Cached[_Pos].Add(s);
            }

            internal static void Remove(uint s)
            {
                foreach (var cache in _Cached)
                    cache.Remove(s);
            }

            internal static bool Contains(uint s)
            {
                return _Cached.Any(cache => cache.Contains(s));
            }

            internal static void Activator(bool start)
            {
                if(start)
                {
                    if (!_Timer.Running)
                        _Timer.Start();
                }
                else
                {
                    if (_Timer.Running)
                    {
                        _Timer.Stop();
                        Clear();
                    }
                }
            }

            internal static void Clear()
            {
                foreach (var cache in _Cached)
                    cache.Clear();
            }

            protected override void OnTick()
            {
                if (_Pos > 0)
                    --_Pos;
                else
                    ++_Pos;
                _Cached[_Pos].Clear();
            }
        }

        internal static void ClearCache(bool msg = false)
        {
            Cached.Clear();

            if (msg && UOSObjects.Player != null)
            {
                UOSObjects.Player.SendMessage(MsgLevel.Force, "Scavenger Item cache cleared.");
            }
        }

        private static void OnTarget(bool location, uint serial, Point3D loc, ushort gfx)
        {
            if (location || !SerialHelper.IsItem(serial))
            {
                return;
            }

            UOItem item = UOSObjects.FindItem(serial);
            if (item == null)
            {
                return;
            }

            if (!ItemIDsHues.TryGetValue(item.ItemID, out var hueset))
                ItemIDsHues[item.ItemID] = hueset = new List<ItemDisplay>();
            ItemDisplay id = new ItemDisplay(item.ItemID, item.DisplayName, (short)item.Hue);
            if (hueset.Contains(id))
            {
                UOSObjects.Player.SendMessage(MsgLevel.Error, "Scavenger: Same Item is already in scavenge list");
            }
            else
            {
                hueset.Add(id);
                UOSObjects.Player.SendMessage(MsgLevel.Force, "Scavenger: Item added to scavenge list");
                UOSObjects.Gump.UpdateScavengerItemsGump(id);
                XmlFileParser.SaveData();
            }
        }

        private static void OnTargetBag(bool location, uint serial, Point3D loc, ushort gfx)
        {
            if (location || !SerialHelper.IsItem(serial))
            {
                return;
            }

            if (_BagRef == null)
            {
                _BagRef = UOSObjects.FindItem(_Bag);
            }

            if (_BagRef != null && _BagRef.ObjPropList != null)
            {
                _BagRef.ObjPropList.Remove("Scavenger HotBag");
                _BagRef.OPLChanged();
            }

            _Bag = serial;
            _BagRef = UOSObjects.FindItem(_Bag);
            if (_BagRef != null && _BagRef.ObjPropList != null)
            {
                _BagRef.ObjPropList.Add("Scavenger HotBag");
                _BagRef.OPLChanged();
            }

            UOSObjects.Player.SendMessage(MsgLevel.Force, $"Scavenger: Setting HotBag 0x{_Bag:X}");
            XmlFileParser.SaveData();
        }

        public static void Uncache(uint s)
        {
            Cached.Remove(s);
        }

        internal static ItemDisplay Remove(ItemDisplay id)
        {
            if (ItemIDsHues.TryGetValue(id.Graphic, out var list))
            {
                int pos = list.IndexOf(id);
                if (pos >= 0)
                {
                    list.RemoveAt(pos);
                    if (pos > 0)
                        return list[pos - 1];
                    else if (pos + 1 < list.Count)
                        return list[pos];
                }
                if(list.Count == 0)
                {
                    list = null;
                    pos = ItemIDsHues.IndexOf(id.Graphic);
                    if(pos >= 0)
                    {
                        ItemIDsHues.RemoveAt(pos);
                        if (pos > 0)
                            list = ItemIDsHues[pos - 1];
                        else if (pos + 1 < ItemIDsHues.Count)
                            list = ItemIDsHues[pos];
                    }
                    if(list != null && list.Count > 0)
                    {
                        return list[list.Count - 1];
                    }
                }
            }
            return null;
        }

        internal static void Scavenge(UOItem item)
        {
            if (!Enabled || UOSObjects.Player.IsGhost)
            {
                return;
            }
            else if (UOSObjects.Player.Backpack == null)
            {
                Utility.SendTimedWarning("You don't have any Backpack!");
                return;
            }
            else if (UOSObjects.Player.Weight >= UOSObjects.Player.MaxWeight)
            {
                Utility.SendTimedWarning("You are overloaded, Scavenger will NOT pickup items anymore!");
                return;
            }
            else if (!ItemIDsHues.TryGetValue(item.ItemID, out var list) || !list.Any(id => id.Enabled && (id.Hue == -1 || id.Hue == item.Hue)))
            {
                return;
            }

            if (Cached.Contains(item.Serial))
            {
                return;
            }

            UOItem bag = _BagRef;
            if (bag == null || bag.Deleted)
            {
                bag = _BagRef = UOSObjects.FindItem(_Bag);
            }

            if (bag == null || bag.Deleted || !bag.IsChildOf(UOSObjects.Player.Backpack))
            {
                bag = UOSObjects.Player.Backpack;
            }

            Cached.Add(item.Serial);
            DragDropManager.DragDrop(item, bag);
        }
    }
}
