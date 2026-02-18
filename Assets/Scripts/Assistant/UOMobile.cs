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
using System.IO;
using System.Collections.Generic;
using System.Text;

//using Assistant.Agents;
//using Assistant.UI;
using ClassicUO.Configuration;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.Game.Data;

using UOScript;

namespace Assistant
{
    [Flags]
    internal enum AssistDirection : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    internal class UOMobile : UOEntity
    {
        private ushort _Body;
        private AssistDirection _Direction;
        private string _Name;

        private byte _Notoriety;

        private bool _Visible;
        private bool _Female;
        private bool _Poisoned;
        private bool _Blessed;
        private bool _Warmode;
        private bool _Flying;
        private bool _Paralyzed;
        private bool _IgnoreMobiles;
        private bool _Unknown3;

        private bool _CanRename;
        //end new

        private ushort _HitsMax, _Hits;
        protected ushort _StamMax, _Stam, _ManaMax, _Mana;

        private List<UOItem> _Items = new List<UOItem>();

        private byte _Map;

        public override string ToString()
        {
            return $"{Name} 0x{Serial:X8}";
        }

        internal override string GetName()
        {
            return $"{Name} 0x{Serial:X}";
        }

        internal UOMobile(uint serial) : base(serial)
        {
            _Map = UOSObjects.Player == null ? (byte)0 : UOSObjects.Player.Map;
            _Visible = true;

            //Agent.InvokeMobileCreated(this);
        }

        internal string Name
        {
            get
            {
                if (_Name == null)
                    return "";
                else
                    return _Name;
            }
            set
            {
                if (!string.IsNullOrEmpty(value) && value != _Name)
                {
                    string trim = ClilocConversion(value);
                    if (trim.Length > 0)
                    {
                        _Name = trim;
                    }
                }
            }
        }

        private static StringBuilder _InternalSB = new StringBuilder(32);

        private static string ClilocConversion(string old)
        {
            _InternalSB.Clear();
            string[] arr = old.Split(' ');
            for (int i = 0; i < arr.Length; i++)
            {
                string ss = arr[i];
                if (ss.Length > 1 && ss.StartsWith("#"))
                {
                    if (int.TryParse(ss.Substring(1), out int x))
                    {
                        ss = ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(x);
                        if (string.IsNullOrEmpty(ss))
                        {
                            ss = arr[i];
                        }
                    }
                }

                _InternalSB.Append(ss);
                _InternalSB.Append(' ');
            }

            return _InternalSB.ToString().Trim();
        }

        internal ushort Body
        {
            get { return _Body; }
            set { _Body = value; }
        }

        internal AssistDirection Direction
        {
            get { return _Direction; }
            set
            {
                if (value != _Direction)
                {
                    var oldDir = _Direction;
                    _Direction = value;
                    OnDirectionChanging(oldDir);
                }
            }
        }

        internal bool Visible
        {
            get { return _Visible; }
            set { _Visible = value; }
        }

        internal bool Poisoned
        {
            get { return _Poisoned; }
            set { _Poisoned = value; }
        }

        internal bool Blessed
        {
            get { return _Blessed; }
            set { _Blessed = value; }
        }

        public bool Paralyzed
        {
            get { return _Paralyzed; }
            set { _Paralyzed = value; }
        }

        public bool Flying
        {
            get { return _Flying; }
            set { _Flying = value; }
        }

        internal bool IsGhost
        {
            get
            {
                return _Body == 402
                       || _Body == 403
                       || _Body == 607
                       || _Body == 608
                       || _Body == 970;
            }
        }

        internal bool IsHuman
        {
            get
            {
                return _Body == 400
                        || _Body == 401
                        || _Body == 402
                        || _Body == 403
                        || _Body == 605
                        || _Body == 606
                        || _Body == 607
                        || _Body == 608
                        || _Body == 970; //player ghost
            }
        }

        internal bool IsMonster
        {
            get { return !IsHuman; }
        }

        internal bool Unknown2
        {
            get { return _IgnoreMobiles; }
            set { _IgnoreMobiles = value; }
        }

        internal bool Unknown3
        {
            get { return _Unknown3; }
            set { _Unknown3 = value; }
        }

        internal bool CanRename //A pet! (where the health bar is open, we can add this to an arraylist of mobiles...
        {
            get { return _CanRename; }
            set { _CanRename = value; }
        }
        //end new

        internal override ushort Graphic => Body;

        internal bool Warmode
        {
            get { return _Warmode; }
            set { _Warmode = value; }
        }

        internal bool Female
        {
            get { return _Female; }
            set { _Female = value; }
        }

        internal byte Notoriety
        {
            get { return _Notoriety; }
            set
            {
                if (value != Notoriety)
                {
                    OnNotoChange(_Notoriety, value);
                    _Notoriety = value;
                }
            }
        }

        protected virtual void OnNotoChange(byte old, byte cur)
        {
        }

        // grey, blue, green, 'canbeattacked'
        private static uint[] _NotoHues = new uint[8]
        {
            // hue color #30
            0x000000, // black		unused 0
            0x30d0e0, // blue		0x0059 1 
            0x60e000, // green		0x003F 2
            0x9090b2, // greyish	0x03b2 3
            0x909090, // grey		   "   4
            0xd88038, // orange		0x0090 5
            0xb01000, // red		0x0022 6
            0xe0e000 // yellow		0x0035 7
        };

        private static int[] _NotoHuesInt = new int[8]
        {
            1, // black		unused 0
            0x059, // blue		0x0059 1
            0x03F, // green		0x003F 2
            0x3B2, // greyish	0x03b2 3
            0x3B2, // grey		   "   4
            0x090, // orange		0x0090 5
            0x022, // red		0x0022 6
            0x035, // yellow		0x0035 7
        };

        internal uint GetNotorietyColor()
        {
            if (_Notoriety < 0 || _Notoriety >= _NotoHues.Length)
                return _NotoHues[0];
            else
                return _NotoHues[_Notoriety];
        }

        internal int GetNotorietyColorInt()
        {
            if (_Notoriety < 0 || _Notoriety >= _NotoHues.Length)
                return _NotoHuesInt[0];
            else
                return _NotoHuesInt[_Notoriety];
        }

        internal byte GetStatusCode()
        {
            if (_Poisoned)
                return 1;
            else
                return 0;
        }

        internal ushort HitsMax
        {
            get { return _HitsMax; }
            set { _HitsMax = value; }
        }

        internal ushort Hits
        {
            get { return _Hits; }
            set { _Hits = value; }
        }

        internal ushort Stam
        {
            get { return _Stam; }
            set { _Stam = value; }
        }

        internal ushort StamMax
        {
            get { return _StamMax; }
            set { _StamMax = value; }
        }

        internal ushort Mana
        {
            get { return _Mana; }
            set { _Mana = value; }
        }

        internal ushort ManaMax
        {
            get { return _ManaMax; }
            set { _ManaMax = value; }
        }


        internal byte Map
        {
            get { return _Map; }
            set
            {
                if (_Map != value)
                {
                    OnMapChange(_Map, value);
                    _Map = value;
                }
            }
        }

        internal byte MapIndex { get; set; }

        internal virtual void OnMapChange(byte old, byte cur)
        {
        }

        internal void AddItem(UOItem item)
        {
            _Items.Add(item);
        }

        internal void RemoveItem(UOItem item)
        {
            _Items.Remove(item);
            if (Engine.Instance.AllowBit(FeatureBit.AutoRemount) && item.Layer == Layer.Mount && Serial == UOSObjects.Player.Serial && UOSObjects.Gump.AutoMount)
            {
                uint serial = Interpreter.GetAlias("mount");
                if(SerialHelper.IsValid(serial))
                {
                    UOEntity e;
                    if((e = UOSObjects.FindEntity(serial)) != null)
                    {
                        NetClient.Socket.PSend_DoubleClick(e.Serial);
                    }
                }
            }
        }

        internal override void Remove()
        {
            List<UOItem> rem = new List<UOItem>(_Items);
            _Items.Clear();

            for (int i = 0; i < rem.Count; i++)
                rem[i].Remove();

            if (!InParty)
            {
                base.Remove();
                UOSObjects.RemoveMobile(this);
            }
            else
            {
                Visible = false;
            }
        }

        internal bool InParty
        {
            get { return PacketHandlers.Party.Count > 0 && (Serial == UOSObjects.Player.Serial || PacketHandlers.Party.Contains(Serial)); }
        }

        internal UOItem GetItemOnLayer(Layer layer)
        {
            for (int i = 0; i < _Items.Count; i++)
            {
                UOItem item = _Items[i];
                if (item.Layer == layer)
                    return item;
            }

            return null;
        }

        internal UOItem Backpack
        {
            get { return GetItemOnLayer(Layer.Backpack); }
        }

        internal UOItem Quiver
        {
            get
            {
                UOItem item = GetItemOnLayer(Layer.Cloak);

                if (item != null && item.IsContainer)
                    return item;
                else
                    return null;
            }
        }

        internal UOItem FindItemByID(ushort id)
        {
            for (int i = 0; i < Contains.Count; i++)
            {
                UOItem item = Contains[i];
                if (item.ItemID == id)
                    return item;
            }

            return null;
        }

        internal override void OnPositionChanging(Point3D oldPos)
        {
            /*if (this != UOSObjects.Player && Engine.MainWindow.MapWindow != null)
                Engine.MainWindow.SafeAction(s => s.MapWindow.CheckLocalUpdate(this));*/

            base.OnPositionChanging(oldPos);
        }

        internal virtual void OnDirectionChanging(AssistDirection oldDir)
        {
        }

        internal int GetPacketFlags()
        {
            int flags = 0x0;

            if (_Paralyzed)
                flags |= 0x01;

            if (_Female)
                flags |= 0x02;

            if (_Poisoned && !PacketHandlers.UseNewStatus)
                flags |= 0x04;

            if (_Flying)
                flags |= 0x04;

            if (_Blessed)
                flags |= 0x08;

            if (_Warmode)
                flags |= 0x40;

            if (!_Visible)
                flags |= 0x80;

            if (_IgnoreMobiles)
                flags |= 0x10;

            if (_Unknown3)
                flags |= 0x20;

            return flags;
        }

        internal void ProcessPacketFlags(byte flags)
        {
            if (!PacketHandlers.UseNewStatus)
                _Poisoned = (flags & 0x04) != 0;
            else
                _Flying = (flags & 0x04) != 0;

            _Paralyzed = (flags & 0x01) != 0; //new
            _Female = (flags & 0x02) != 0;
            _Blessed = (flags & 0x08) != 0;
            _IgnoreMobiles = (flags & 0x10) != 0; //new
            _Unknown3 = (flags & 0x10) != 0; //new
            _Warmode = (flags & 0x40) != 0;
            _Visible = (flags & 0x80) == 0;
        }

        internal List<UOItem> Contains
        {
            get { return _Items; }
        }

        internal void OverheadMessageFrom(int hue, string from, string format, params object[] args)
        {
            OverheadMessageFrom(hue, from, string.Format(format, args));
        }

        internal void OverheadMessageFrom(int hue, string from, string text, bool ascii = false)
        {
            if (ascii)
            {
                ClientPackets.PRecv_AsciiMessage(Serial, _Body, MessageType.Regular, hue, 3, from, text);
            }
            else
            {
                ClientPackets.PRecv_UnicodeMessage(Serial, _Body, MessageType.Regular, hue, 3, Settings.GlobalSettings.Language, from, text);
            }
        }

        internal void OverheadMessage(int hue, string format, params object[] args)
        {
            OverheadMessage(hue, string.Format(format, args));
        }

        internal void OverheadMessage(int hue, string text)
        {
            OverheadMessageFrom(hue, "UOSteam", text);
        }

        private Point2D _ButtonPoint = Point2D.Zero;

        internal Point2D ButtonPoint
        {
            get { return _ButtonPoint; }
            set { _ButtonPoint = value; }
        }

        private static List<Layer> _layers = new List<Layer>
        {
            Layer.Backpack,
            Layer.Invalid,
            Layer.OneHanded,
            Layer.TwoHanded,
            Layer.Shoes,
            Layer.Pants,
            Layer.Shirt,
            Layer.Helmet,
            Layer.Necklace,
            Layer.Gloves,
            Layer.Torso,
            Layer.Tunic,
            Layer.Arms,
            Layer.Cloak,
            Layer.Robe,
            Layer.Skirt,
            Layer.Legs,
            Layer.Mount,
            Layer.Hair
        };

        internal void ResetLayerHue()
        {
            if (IsGhost)
                return;

            foreach (Layer l in _layers)
            {
                UOItem i = GetItemOnLayer(l);

                if (i == null)
                    continue;

                if (i.ItemID == 0x204E && i.Hue == 0x08FD) // death shroud
                    i.ItemID = 0x1F03;

                ClientPackets.PRecv_EquipmentItem(i, i.Hue, Serial);//Engine.Instance.SendToClient(new EquipmentItem(i, i.Hue, Serial));
            }
        }

        internal void SetLayerHue(int hue)
        {
            if (IsGhost)
                return;

            foreach (Layer l in _layers)
            {
                UOItem i = GetItemOnLayer(l);
                if (i == null)
                    continue;

                ClientPackets.PRecv_EquipmentItem(i, (ushort)hue, Serial);//Engine.Instance.SendToClient(new EquipmentItem(i, (ushort)hue, Serial));
            }
        }
    }
}

