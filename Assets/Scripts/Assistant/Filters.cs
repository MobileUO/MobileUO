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

using System.Xml;
using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Game.UI.Controls;

namespace Assistant
{
	internal abstract class Filter
	{
		protected Filter(string name)
		{
			Name = name;
			XmlName = $"Filter{Name.Replace(" ", "").Replace("'", "")}";
			_Callback = new PacketViewerCallback(OnFilter);
		}

        internal static void Initialize()
        {
            SoundFilter.Configure();
			TextMessageFilter.Configure();
			LocMessageFilter.Configure();
			DeathFilter.Configure();
			StaffItemFilter.Configure();
			SnoopMessageFilter.Configure();
			TradeFilter.Configure();
        }

		internal static List<Filter> List { get; } = new List<Filter>();
        internal bool Enabled { get; private set; }

		internal static void Register(Filter filter)
		{
			List.Add(filter);
		}

        internal abstract void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args);
        internal abstract byte[] PacketIDs { get; }
        internal string Name { get; }
		internal string XmlName { get; }

		private PacketViewerCallback _Callback { get; }

		public override string ToString()
		{
			return Name;
		}

        internal virtual void OnCheckChanged(bool enabled)
		{
			if (Enabled != enabled)
			{
				Enabled = enabled;
				if (Enabled)
				{
					for (int i = 0; i < PacketIDs.Length; i++)
						PacketHandler.RegisterServerToClientViewer(PacketIDs[i], _Callback);
				}
				else
				{
					for (int i = 0; i < PacketIDs.Length; i++)
						PacketHandler.RemoveServerToClientViewer(PacketIDs[i], _Callback);
				}
			}
		}

		internal void SaveProfile(XmlTextWriter xml)
		{
			if (xml != null)
			{
				foreach (Filter filter in List)
				{
					xml.WriteStartElement("data");
					xml.WriteAttributeString("name", XmlName);
					xml.WriteString($"{filter.Enabled}");
					xml.WriteEndElement();
				}
			}
		}
	}

    internal class SoundFilter : Filter
    {
        internal static void Configure()
        {
            Register(new SoundFilter("Bard's Music", GetMultiRange(0x2EA, 0x2ED, 0x3CA, 0x3E2, 0x3FF, 0x417, 0x497, 0x4AF, 0x5D3, 0x5D9, 0x5DA, 0x5E0, 0x5ED, 0x605)));
            Register(new SoundFilter("Dog Sounds", GetRange(0x85, 0x89)));
            Register(new SoundFilter("Cat Sounds", GetRange(0x69, 0x6D)));
            Register(new SoundFilter("Horse Sounds", GetRange(0xA8, 0xAC)));
            Register(new SoundFilter("Sheep Sounds", GetRange(0xD6, 0xDA)));
            Register(new SoundFilter("Spirit Speak Sound", 0x24A));
            Register(new SoundFilter("Fizzle Sound", 0x5C));
            Register(new SoundFilter("Backpack Sounds", 0x48));
            Register(new SoundFilter("Deer Sounds", 0x82, 0x83, 0x84, 0x85, 0x2BE, 0x2BF, 0x2C0, 0x4CB, 0x4CC));
            Register(new SoundFilter("Cyclop Titan Sounds", 0x25D, 0x25E, 0x25F, 0x260, 0x261, 0x262, 0x263, 0x264, 0x265, 0x266));
            Register(new SoundFilter("Bull Sounds", 0x065, 0x066, 0x067, 0x068, 0x069));
            Register(new SoundFilter("Dragon Sounds", 0x2C8, 0x2C9, 0x2CA, 0x2CB, 0x2CC, 0x2CD, 0x2CE, 0x2CF, 0x2D0, 0x2D1, 0x2D2, 0x2D3, 0x2D4, 0x2D5, 0x2D6, 0x16B, 0x16C, 0x16D, 0x16E, 0x16F, 0x15F, 0x160, 0x161));
            Register(new SoundFilter("Chicken Sounds", 0x06F, 0x070, 0x071, 0x072, 0x073));
			Register(new SoundFilter("Emote Sounds", GetMultiRange(0x30A, 0x338, 0x419, 0x44A)));
        }
		internal static void CleanUP()
		{
			_Sounds.Clear();
		}

        internal static ushort[] GetRange(ushort min, ushort max)
        {
            if (max < min)
                return new ushort[0];

            ushort[] range = new ushort[max - min + 1];
            for (ushort i = min; i <= max; i++)
                range[i - min] = i;
            return range;
        }

		internal static ushort[] GetMultiRange(params ushort[] multirange)
		{
			if (multirange.Length == 0 || (multirange.Length % 2) != 0)
				return new ushort[0];
			List<ushort> range = new List<ushort>();
			for(int i = 0; i < multirange.Length; i += 2)
			{
				for(ushort x = multirange[i]; x <= multirange[i+1]; x++)
				{
					range.Add(x);
				}
			}
			return range.ToArray();
		}

		private static HashSet<ushort> _Sounds = new HashSet<ushort>();
        private ushort[] _SoundsArr;

        private SoundFilter(string name, params ushort[] blockSounds) : base(name)
        {
            _SoundsArr = blockSounds;
        }

		private static byte[] Instance { get; } = new byte[] { 0x54 };
		internal override byte[] PacketIDs => Instance;

        internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
        {
            p.ReadUInt8(); // flags

            ushort sound = p.ReadUInt16BE();
			if (_Sounds.Contains(sound))
				args.Block = true;
        }

		internal override void OnCheckChanged(bool enabled)
		{
			base.OnCheckChanged(enabled);
			for (int i = 0; i < _SoundsArr.Length; i++)
			{
				if(enabled)
					_Sounds.Add(_SoundsArr[i]);
				else
					_Sounds.Remove(_SoundsArr[i]);
			}
		}
	}

	internal class TextMessageFilter : Filter
	{
		internal static void Configure()
		{
		}

		private string[] _Strings;
		private MessageType _Type;

		private TextMessageFilter(string name, MessageType type, string[] msgs) : base(name)
		{
			_Strings = msgs;
			_Type = type;
		}

		private static byte[] Instance { get; } = new byte[] { 0x1C };
		internal override byte[] PacketIDs => Instance;

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			if (args.Block)
				return;

			// 0, 1, 2
			uint serial = p.ReadUInt32BE(); // 3, 4, 5, 6
			ushort body = p.ReadUInt16BE(); // 7, 8
			MessageType type = (MessageType)p.ReadUInt8(); // 9

			if (type != _Type)
				return;

			ushort hue = p.ReadUInt16BE(); // 10, 11
			ushort font = p.ReadUInt16BE();
			string name = p.ReadASCII(30);
			string text = p.ReadASCII();

			for (int i = 0; i < _Strings.Length; i++)
			{
				if (text.IndexOf(_Strings[i]) != -1)
				{
					args.Block = true;
					return;
				}
			}
		}
	}

	internal class LocMessageFilter : Filter
	{
		internal static void Configure()
		{
		}

		private int[] _Nums;
		private MessageType _Type;

		private LocMessageFilter(string name, MessageType type, int[] msgs) : base(name)
		{
			_Nums = msgs;
			_Type = type;
		}

		private static byte[] Instance { get; } = new byte[] { 0xC1 };
		internal override byte[] PacketIDs => Instance;

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			if (args.Block)
				return;

			uint serial = p.ReadUInt32BE();
			ushort body = p.ReadUInt16BE();
			MessageType type = (MessageType)p.ReadUInt8();
			ushort hue = p.ReadUInt16BE();
			ushort font = p.ReadUInt16BE();
			int num = (int)p.ReadUInt32BE();

			// paladin spells
			if (num >= 1060718 && num <= 1060727)
				type = MessageType.Spell;
			if (type != _Type)
				return;

			for (int i = 0; i < _Nums.Length; i++)
			{
				if (num == _Nums[i])
				{
					args.Block = true;
					return;
				}
			}
		}
	}

	internal class DeathFilter : Filter
	{
		internal static void Configure()
		{
			Register(new DeathFilter());
		}

		internal DeathFilter() : base("Death")
		{
		}

		private static byte[] Instance { get; } = new byte[] { 0x2C };
		internal override byte[] PacketIDs => Instance;

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			args.Block = true;
		}
	}

	internal class StaffItemFilter : Filter
	{
		internal static void Configure()
		{
			Register(new StaffItemFilter());
		}

		internal StaffItemFilter() : base("Staff Items")
		{
		}

		private static byte[] Instance { get; } = new byte[] { 0x1A };
		internal override byte[] PacketIDs => Instance;

		private static bool IsStaffItem(ushort itemID)
		{
			return itemID == 0x36FF || // LOS blocker
				   itemID == 0x1183; // Movement blocker
		}

		private static bool IsStaffItem(UOItem i)
		{
			return i.OnGround && (IsStaffItem(i.ItemID) || !i.Visible);
		}

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			uint serial = p.ReadUInt32BE();
			ushort itemID = p.ReadUInt16BE();

			if ((serial & 0x80000000) != 0)
				p.ReadUInt16BE(); // amount

			if ((itemID & 0x8000) != 0)
				itemID = (ushort)((itemID & 0x7FFF) + p.ReadInt8()); // itemID offset

			ushort x = p.ReadUInt16BE();
			ushort y = p.ReadUInt16BE();

			if ((x & 0x8000) != 0)
				p.ReadUInt8(); // direction

			short z = p.ReadInt8();

			if ((y & 0x8000) != 0)
				p.ReadUInt16BE(); // hue

			bool visible = true;
			if ((y & 0x4000) != 0)
			{
				int flags = p.ReadUInt8();

				visible = ((flags & 0x80) == 0);
			}

			if (IsStaffItem(itemID) || !visible)
				args.Block = true;
		}

		internal override void OnCheckChanged(bool enabled)
		{
			base.OnCheckChanged(enabled);
			if (UOSObjects.Player != null)
			{
				if (Enabled)
				{
					foreach (UOItem i in UOSObjects.Items.Values)
					{
                        if (IsStaffItem(i))
                            ClientPackets.PRecv_RemoveObject(i.Serial);
					}
				}
				else
				{
					foreach (UOItem i in UOSObjects.Items.Values)
					{
                        if (IsStaffItem(i))
                            ClientPackets.PRecv_WorldItem(i);
					}
				}
			}
		}
	}

	internal class SnoopMessageFilter : Filter
	{
		internal static void Configure()
		{
			Register(new SnoopMessageFilter());
		}

		private SnoopMessageFilter() : base("Snooping Messages")
		{
		}

		private static byte[] Instance { get; } = new byte[] { 0x1C, 0xAE };
		internal override byte[] PacketIDs => Instance;

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			if (args.Block)
				return;

			// 0, 1, 2
			uint serial = p.ReadUInt32BE(); // 3, 4, 5, 6
			ushort body = p.ReadUInt16BE(); // 7, 8
			MessageType type = (MessageType)p.ReadUInt8(); // 9

			if (type != MessageType.System)
				return;

			ushort hue = p.ReadUInt16BE(); // 10, 11
			ushort font = p.ReadUInt16BE();
			string text;
			if (p[0] == 0xAE)
			{
				p.ReadASCII(4);
				p.ReadASCII(30);
				text = p.ReadUnicodeBE();
			}
			else
			{
				p.ReadASCII(30);
				text = p.ReadASCII();
			}

			if(!string.IsNullOrEmpty(text) && text.StartsWith("You notice") && text.Contains("peek") && text.EndsWith("belongings!") && text.Contains(UOSObjects.Player.Name))
			{
				args.Block = true;
			}
		}
	}

	internal class TradeFilter : Filter
	{
		internal static void Configure()
		{
			Register(new TradeFilter());
		}

		internal TradeFilter() : base("Trade Window")
		{
		}

		private static byte[] Instance { get; } = new byte[] { 0x6F };
		internal override byte[] PacketIDs => Instance;

		internal override void OnFilter(ref StackDataReader p, PacketHandlerEventArgs args)
		{
			args.Block = true;
			p.Skip(1);
			uint serial = p.ReadUInt32BE();
            NetClient.Socket.PSend_TradeResponse(serial, 1, false);//cancel
		}
	}
}
