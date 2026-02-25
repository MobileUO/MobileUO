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

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Assets;
using System;
using System.Collections.Generic;

namespace Assistant
{
    internal class UOItem : UOEntity
    {
        private ushort _ItemID;
        private ushort _Amount;
        private byte _Direction;

        private bool _Visible;
        private bool _Movable;

        private Layer _Layer;
        private string _Name;
        private object _Parent;
        private int _Price;
        private string _BuyDesc;
        private List<UOItem> _Items;
        internal int ItemCount => _Items.Count;

        private bool _IsNew;
        private bool _AutoStack;

        private byte _GridNum;

        internal UOItem(uint serial) : base(serial)
        {
            _Items = new List<UOItem>();

            _Visible = true;
            _Movable = true;

            OnItemCreated?.Invoke(this);
        }

        public delegate void ItemCreatedEventHandler(UOItem item);
        public static event ItemCreatedEventHandler OnItemCreated;

        internal ushort ItemID
        {
            get { return _ItemID; }
            set { _ItemID = value; }
        }

        internal ushort Amount
        {
            get { return _Amount; }
            set { _Amount = value; }
        }

        internal byte Direction
        {
            get { return _Direction; }
            set { _Direction = value; }
        }

        internal bool Visible
        {
            get { return _Visible; }
            set { _Visible = value; }
        }

        internal bool Movable
        {
            get { return _Movable; }
            set { _Movable = value; }
        }

        internal string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_Name))
                {
                    return _Name;
                }
                else
                {
                    return DisplayName;
                }
            }
            set
            {
                if (value != null)
                    _Name = value.Trim();
                else
                    _Name = null;
            }
        }

        internal string DisplayName
        {
            get
            {
                return TileDataInfo.Name.Replace("%", ""); 
            }
        }

        internal StaticTiles TileDataInfo => _ItemID < ClassicUO.Client.Game.UO.FileManager.TileData.StaticData.Length ? ClassicUO.Client.Game.UO.FileManager.TileData.StaticData[_ItemID] : ClassicUO.Client.Game.UO.FileManager.TileData.StaticData[0];

        internal Layer Layer
        {
            get
            {
                
                if ((_Layer < Layer.OneHanded || _Layer > Layer.Bank) &&
                    ((TileDataInfo.Flags & TileFlag.Wearable) != 0 ||
                     (TileDataInfo.Flags & TileFlag.Armor) != 0 ||
                     (TileDataInfo.Flags & TileFlag.Weapon) != 0
                    ))
                {
                    _Layer = (Layer)TileDataInfo.Layer;
                }

                return _Layer;
            }
            set { _Layer = value; }
        }

        internal UOItem FindItemByID(ushort id, bool recurse = true, int hue = -1, Layer layer = Layer.Invalid, bool movable = false)
        {
            return RecurseFindItemByID(this, id, recurse, hue, layer, movable);
        }

        private static UOItem RecurseFindItemByID(UOItem current, ushort id, bool recurse, int hue, Layer layer, bool movable = false)
        {
            if (current != null && current._Items.Count > 0)
            {
                List<UOItem> list = current._Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    UOItem item = list[i];

                    if (item.ItemID == id && (hue == -1 || hue == item.Hue) && (layer == Layer.Invalid || layer == item.Layer) && (!movable || item.Movable))
                    {
                        return item;
                    }
                    else if (recurse && item.IsContainer)
                    {
                        UOItem check = RecurseFindItemByID(item, id, recurse, hue, layer, movable);

                        if (check != null)
                        {
                            return check;
                        }
                    }
                }
            }

            return null;
        }

        internal List<UOItem> FindItemsByID(ushort id, bool recurse = true, int hue = -1, bool movable = false)
        {
            List<UOItem> items = new List<UOItem>();
            return RecurseFindItemsByID(this, items, id, recurse, hue, movable);
        }

        private static List<UOItem> RecurseFindItemsByID(UOItem current, List<UOItem> items, ushort id, bool recurse, int hue, bool movable = false)
        {
            if (current != null && current._Items.Count > 0)
            {
                List<UOItem> list = current._Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    UOItem item = list[i];

                    if (item.ItemID == id && (hue == -1 || hue == item.Hue) && (!movable || item.Movable))
                    {
                        items.Add(item);
                    }
                    else if (recurse && item.IsContainer)
                    {
                        RecurseFindItemsByID(item, items, id, recurse, hue, movable);
                    }
                }
            }

            return items;
        }

        internal UOItem FindItemByID(HashSet<ushort> itemset, bool recurse = true, int hue = -1)
        {
            List<UOItem> items = new List<UOItem>();
            RecurseFindItemsByID(this, items, itemset, recurse, hue);
            if (items.Count > 0)
                return items[0];
            return null;
        }

        internal List<UOItem> FindItemsByID(HashSet<ushort> itemset, bool recurse = true, int hue = -1)
        {
            return RecurseFindItemsByID(this, new List<UOItem>(), itemset, recurse, hue);
        }

        private static List<UOItem> RecurseFindItemsByID(UOItem current, List<UOItem> items, HashSet<ushort> ids, bool recurse, int hue)
        {
            if (current != null && current._Items.Count > 0 && ids.Count > 0)
            {
                List<UOItem> list = current._Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    UOItem item = list[i];

                    if (ids.Contains(item.ItemID) && (hue == -1 || item.Hue == hue))
                    {
                        items.Add(item);
                    }
                    else if (recurse && item.IsContainer)
                    {
                        RecurseFindItemsByID(item, items, ids, recurse, hue);
                    }
                }
            }

            return items;
        }

        internal UOItem FindItemByName(string name, bool recurse = true)
        {
            return RecurseFindItemByName(this, name, recurse);
        }

        private static UOItem RecurseFindItemByName(UOItem current, string name, bool recurse)
        {
            if (current != null && current._Items.Count > 0)
            {
                List<UOItem> list = current._Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    UOItem item = list[i];

                    if (item.Name == name)
                    {
                        return item;
                    }
                    else if (recurse && item.IsContainer)
                    {
                        UOItem check = RecurseFindItemByName(item, name, recurse);

                        if (check != null)
                        {
                            return check;
                        }
                    }
                }
            }

            return null;
        }

        internal bool ContainsItemBySerial(uint serial, bool recurse = true)
        {
            return RecurseContainsItemBySerial(this, serial, recurse);
        }

        private static bool RecurseContainsItemBySerial(UOItem current, uint serial, bool recurse)
        {
            if (current != null && current._Items.Count > 0)
            {
                List<UOItem> list = current._Items;

                for (int i = 0; i < list.Count; ++i)
                {
                    UOItem item = list[i];

                    if (item.Serial == serial)
                    {
                        return true;
                    }
                    else if (recurse && item.IsContainer)
                    {
                        bool check = RecurseContainsItemBySerial(item, serial, recurse);

                        if (check)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal int GetCount(ushort iid)
        {
            int count = 0;
            for (int i = 0; i < _Items.Count; i++)
            {
                UOItem item = (UOItem)_Items[i];
                if (item.ItemID == iid)
                    count += item.Amount;
                else if ((item.ItemID == 0x0E34 && iid == 0x0EF3) || (item.ItemID == 0x0EF3 && iid == 0x0E34))
                    count += item.Amount;
                count += item.GetCount(iid);
            }

            return count;
        }

        internal object Container
        {
            get
            {
                if (_Parent is uint && UpdateContainer())
                    _NeedContUpdate.Remove(this);
                return _Parent;
            }
            set
            {
                if ((_Parent != null && _Parent.Equals(value))
                    || (value is uint vval && _Parent is UOEntity entity && entity.Serial == vval)
                    || (_Parent is uint parent && value is UOEntity ventity && ventity.Serial == parent))
                {
                    return;
                }

                if (_Parent is UOMobile mobile)
                    mobile.RemoveItem(this);
                else if (_Parent is UOItem item)
                    item.RemoveItem(this);

                if (value is UOMobile vmobile)
                    _Parent = vmobile.Serial;
                else if (value is UOItem vitem)
                    _Parent = vitem.Serial;
                else
                    _Parent = value;

                if (!UpdateContainer() && _NeedContUpdate != null)
                    _NeedContUpdate.Add(this);
            }
        }

        internal uint GetContainerSerial()
        {
            if (Container is uint ser)
            {
                return ser;
            }
            else if (Container is UOEntity cnt)
            {
                return cnt.Serial;
            }
            return 0;
        }

        internal uint GetRootContainerSerial()
        {
            object root = RootContainer;
            if (root is uint ser)
            {
                return ser;
            }
            else if (root is UOEntity cnt)
            {
                return cnt.Serial;
            }
            return 0;
        }

        internal UOItem GetRootContainerItem(out int depth)
        {
            UOItem cont = null;
            object cnt = Container;
            depth = 0;
            while (cnt != null)
            {
                if (cnt is uint ser && UOSObjects.FindItem(ser) is UOItem subx)
                {
                    cont = subx;
                    cnt = subx.Container;
                    ++depth;
                }
                else if (cnt is UOItem subcnt)
                {
                    cont = subcnt;
                    cnt = subcnt.Container;
                    ++depth;
                }
                else
                {
                    cnt = null;//this is to prevent infinite loop on mobile
                }
            }
            return cont;
        }

        internal bool UpdateContainer()
        {
            if (!(_Parent is uint) || Deleted)
                return true;

            object o = null;
            uint contSer = (uint)_Parent;
            if (SerialHelper.IsItem(contSer))
                o = UOSObjects.FindItem(contSer);
            else if (SerialHelper.IsMobile(contSer))
                o = UOSObjects.FindMobile(contSer);

            if (o == null)
                return false;

            _Parent = o;

            if (_Parent is UOItem)
                ((UOItem)_Parent).AddItem(this);
            else if (_Parent is UOMobile)
                ((UOMobile)_Parent).AddItem(this);

            if (UOSObjects.Player != null && (IsChildOf(UOSObjects.Player.Backpack) || IsChildOf(UOSObjects.Player.Quiver)))
            {
                if (_IsNew)
                {
                    if (_AutoStack)// && )
                        AutoStackResource();

                    if (IsContainer && UOSObjects.Gump.AutoSearchContainers)
                    {
                        if (!SearchExemption.IsExempt(Graphic))
                        {
                            PacketHandlers.IgnoreGumps.Add(Serial);
                            PlayerData.DoubleClick(Serial);

                            for (int c = 0; c < Contains.Count; ++c)
                            {
                                UOItem icheck = Contains[c];
                                if (icheck.IsContainer && !SearchExemption.IsExempt(icheck.Graphic))
                                {
                                    PacketHandlers.IgnoreGumps.Add(icheck.Serial);
                                    PlayerData.DoubleClick(icheck.Serial);
                                }
                            }
                        }
                    }
                }
            }

            _AutoStack = _IsNew = false;

            return true;
        }

        private static List<UOItem> _NeedContUpdate = new List<UOItem>();

        internal static void UpdateContainers()
        {
            int i = 0;
            while (i < _NeedContUpdate.Count)
            {
                if (_NeedContUpdate[i].UpdateContainer())
                    _NeedContUpdate.RemoveAt(i);
                else
                    i++;
            }
        }

        private static List<uint> _AutoStackCache = new List<uint>();

        internal void AutoStackResource()
        {
            //do we need to check for autostack? does it have really any utility?
            if (!Scavenger.Stack || !IsResource || _AutoStackCache.Contains(Serial))
                return;

            foreach (UOItem check in UOSObjects.Items.Values)
            {
                if (check.Container == null && check.ItemID == ItemID && check.Hue == Hue &&
                    Utility.InRange(UOSObjects.Player.Position, check.Position, 2))
                {
                    DragDropManager.DragDrop(this, check);
                    _AutoStackCache.Add(Serial);
                    return;
                }
            }
            DragDropManager.DragDrop(this, UOSObjects.Player.Position);
            _AutoStackCache.Add(Serial);
        }

        internal object RootContainer
        {
            get
            {
                int die = 100;
                object cont = this.Container;
                while (cont != null && cont is UOItem item && --die > 0)
                    cont = item.Container;

                return cont;
            }
        }

        internal bool IsChildOf(object parent)
        {
            uint parentSerial;
            if (parent is UOMobile)
                return parent == RootContainer;
            else if (parent is UOItem)
                parentSerial = ((UOItem)parent).Serial;
            else
                return false;

            object check = this;
            int die = 100;
            while (check != null && check is UOItem item && --die > 0)
            {
                if (item.Serial == parentSerial)
                    return true;
                else
                    check = item.Container;
            }

            return false;
        }

        internal override ushort Graphic => ItemID;

        internal override Point3D WorldPosition => GetWorldPosition();

        internal Point3D GetWorldPosition()
        {
            int die = 100;
            object root = this.Container;
            while (root != null && root is UOItem item && item.Container != null && --die > 0)
                root = item.Container;

            if (root is UOEntity entity)
                return entity.Position;
            else
                return Position;
        }

        private void AddItem(UOItem item)
        {
            for (int i = 0; i < _Items.Count; ++i)
            {
                if (_Items[i] == item)
                    return;
            }

            _Items.Add(item);
        }

        private void RemoveItem(UOItem item)
        {
            _Items.Remove(item);
        }

        internal byte GetPacketFlags()
        {
            byte flags = 0;

            if (!_Visible)
            {
                flags |= 0x80;
            }

            if (_Movable)
            {
                flags |= 0x20;
            }

            return flags;
        }

        internal int DistanceTo(UOMobile m)
        {
            int x = Math.Abs(this.Position.X - m.Position.X);
            int y = Math.Abs(this.Position.Y - m.Position.Y);

            return x > y ? x : y;
        }

        internal void ProcessPacketFlags(byte flags)
        {
            _Visible = ((flags & 0x80) == 0);
            _Movable = ((flags & 0x20) != 0);
        }

        private Timer _RemoveTimer = null;

        internal void RemoveRequest()
        {
            if (_RemoveTimer == null)
                _RemoveTimer = Timer.DelayedCallback(TimeSpan.FromMilliseconds(25), new TimerCallback(Remove));
            else if (_RemoveTimer.Running)
                _RemoveTimer.Stop();

            _RemoveTimer.Start();
        }

        internal bool CancelRemove()
        {
            if (_RemoveTimer != null && _RemoveTimer.Running)
            {
                _RemoveTimer.Stop();
                return true;
            }
            else
            {
                return false;
            }
        }

        internal override void Remove()
        {
            List<UOItem> rem = new List<UOItem>(_Items);
            _Items.Clear();
            for (int i = 0; i < rem.Count; ++i)
                (rem[i]).Remove();

            if (_Parent is UOMobile mobile)
                mobile.RemoveItem(this);
            else if (_Parent is UOItem item)
                item.RemoveItem(this);

            UOSObjects.RemoveItem(this);
            base.Remove();
        }

        internal List<UOItem> Contains
        {
            get { return _Items; }
        }

        public IEnumerable<UOItem> Contents(bool recurse = true)
        {
            if (_Items == null)
                yield break;

            foreach (var item in _Items)
            {
                yield return item;

                foreach (var child in item.Contents(recurse))
                    yield return child;
            }
        }

        internal byte GridNum
        {
            get { return _GridNum; }
            set { _GridNum = value; }
        }

        internal bool OnGround
        {
            get { return Container == null; }
        }

        internal bool IsContainer
        {
            get
            {
                ushort iid = _ItemID;
                return (_Items.Count > 0 && !IsCorpse) || iid == 0x990 || (iid >= 0x9A8 && iid <= 0x9AC) ||
                       (iid >= 0x9B0 && iid <= 0x9B2) ||
                       (iid >= 0xA2C && iid <= 0xA53) || (iid >= 0xA97 && iid <= 0xA9E) ||
                       (iid >= 0xE3C && iid <= 0xE43) ||
                       (iid >= 0xE75 && iid <= 0xE80 && iid != 0xE7B) || iid == 0x1E80 || iid == 0x1E81 ||
                       iid == 0x232A || iid == 0x232B || (iid >= 0x2DF1 && iid <= 0x2DF4) ||
                       iid == 0x2B02 || iid == 0x2B03 || iid == 0x2FB7 || iid == 0x3171;
            }
        }

        internal bool IsBagOfSending
        {
            get { return Hue >= 0x0400 && _ItemID == 0xE76; }
        }

        internal bool IsInBank
        {
            get
            {
                if (_Parent is UOItem)
                    return ((UOItem)_Parent).IsInBank;
                else if (_Parent is UOMobile)
                    return this.Layer == Layer.Bank;
                else
                    return false;
            }
        }

        internal bool IsNew
        {
            get { return _IsNew; }
            set { _IsNew = value; }
        }

        internal bool AutoStack
        {
            get { return _AutoStack; }
            set { _AutoStack = value; }
        }

        internal bool IsMulti
        {
            get { return _ItemID >= 0x4000; }
        }

        internal bool IsPouch
        {
            get { return _ItemID == 0x0E79; }
        }

        internal bool IsCorpse
        {
            get { return _ItemID == 0x2006 || (_ItemID >= 0x0ECA && _ItemID <= 0x0ED2); }
        }

        internal bool IsDoor
        {
            get
            {
                ushort iid = _ItemID;
                return (iid >= 0x0675 && iid <= 0x06F6) || (iid >= 0x0821 && iid <= 0x0875) ||
                       (iid >= 0x1FED && iid <= 0x1FFC) ||
                       (iid >= 0x241F && iid <= 0x2424) || (iid >= 0x2A05 && iid <= 0x2A1C);
            }
        }

        internal bool IsResource
        {
            get
            {
                ushort iid = _ItemID;
                return (iid >= 0x19B7 && iid <= 0x19BA) || // ore
                       (iid >= 0x09CC && iid <= 0x09CF) || // fishes
                       (iid >= 0x1BDD && iid <= 0x1BE2) || // logs
                       iid == 0x1779 || // granite / stone
                       iid == 0x11EA || iid == 0x11EB // sand
                    ;
            }
        }

        internal bool IsPotion
        {
            get
            {
                return (_ItemID >= 0x0F06 && _ItemID <= 0x0F0D) ||
                       _ItemID == 0x2790 || _ItemID == 0x27DB; // Ninja belt (works like a potion)
            }
        }

        internal bool IsVirtueShield
        {
            get
            {
                ushort iid = _ItemID;
                return (iid >= 0x1bc3 && iid <= 0x1bc5); // virtue shields
            }
        }

        internal bool IsTwoHanded
        {
            get
            {
                ushort iid = _ItemID;
                return (
                           // everything in layer 2 except shields is 2handed
                           Layer == Layer.TwoHanded &&
                           !((iid >= 0x1b72 && iid <= 0x1b7b) || IsVirtueShield) // shields
                       ) ||

                       // and all of these layer 1 weapons:
                       (iid == 0x13fc || iid == 0x13fd) || // hxbow
                       (iid == 0x13AF || iid == 0x13b2) || // war axe & bow
                       (iid >= 0x0F43 && iid <= 0x0F50) || // axes & xbow
                       (iid == 0x1438 || iid == 0x1439) || // war hammer
                       (iid == 0x1442 || iid == 0x1443) || // 2handed axe
                       (iid == 0x1402 || iid == 0x1403) || // short spear
                       (iid == 0x26c1 || iid == 0x26cb) || // aos gay blade
                       (iid == 0x26c2 || iid == 0x26cc) || // aos gay bow
                       (iid == 0x26c3 || iid == 0x26cd) // aos gay xbow
                    ;
            }
        }

        public override string ToString()
        {
            return $"{Name} 0x{Serial:X8}";
        }

        internal int Price
        {
            get { return _Price; }
            set { _Price = value; }
        }

        internal string BuyDesc
        {
            get { return _BuyDesc; }
            set { _BuyDesc = value; }
        }

        internal override string GetName()
        {
            return $"{Name} 0x{Serial:X8}";
        }
    }
}
