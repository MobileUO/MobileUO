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

using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Utility;
using STB = ClassicUO.Game.UI.Controls.ScriptTextBox;

namespace Assistant
{
	internal enum MessageType
	{
		Regular = 0x00,
		System = 0x01,
		Emote = 0x02,
		Label = 0x06,
		Focus = 0x07,
		Whisper = 0x08,
		Yell = 0x09,
		Spell = 0x0A,
		Guild = 0x0D,
		Alliance = 0x0E,
        Command = 0x0F,
        Chat = 0x10,
        Encoded = 0xC0,

		Special = 0x20
	}

    internal static class ClientPackets
    {
        public static void PRecv_UnicodeMessage(uint serial, int graphic, MessageType type, int hue, int font, string lang, string name, string text)
        {
            const byte ID = 0xAE;

            if (string.IsNullOrEmpty(lang)) lang = "ENU";
            if (name == null) name = string.Empty;
            if (text == null) text = string.Empty;

            if (hue == 0)
                hue = 0x3B2;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(50 + (text.Length * 2));

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE((ushort)graphic);
            writer.WriteUInt8((byte)type);
            writer.WriteUInt16BE((ushort)hue);
            writer.WriteUInt16BE((ushort)font);
            writer.WriteASCII(lang.ToUpper(), 4);
            writer.WriteASCII(name, 30);
            writer.WriteUnicodeBE(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_ClearAbility()
        {
            const byte ID = 0xBF;

            StackDataWriter writer = new StackDataWriter(5);

            writer.WriteUInt8(ID);

            writer.WriteUInt16BE(5);//fixed length = 1 + 2 + 2

            writer.WriteUInt16BE(0x21);

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_MobileIncoming(UOMobile m)
        {
            const byte ID = 0x78;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            int count = m.Contains.Count;
            int ltHue = UOSObjects.Gump.HLTargetHue;
            bool isLT;
            if (ltHue != 0)
                isLT = Targeting.LastTargetInfo != null && Targeting.LastTargetInfo.Serial == m.Serial;
            else
                isLT = false;

            writer.WriteUInt32BE(m.Serial);
            writer.WriteUInt16BE(m.Body);
            writer.WriteUInt16BE((ushort)m.Position.X);
            writer.WriteUInt16BE((ushort)m.Position.Y);
            writer.WriteInt8((sbyte)m.Position.Z);
            writer.WriteUInt8((byte)m.Direction);
            writer.WriteUInt16BE((ushort)(isLT ? ltHue | 0x8000 : m.Hue));
            writer.WriteUInt8((byte)m.GetPacketFlags());
            writer.WriteUInt8(m.Notoriety);

            for (int i = 0; i < count; ++i)
            {
                UOItem item = m.Contains[i];
                ushort itemID = (ushort)(item.ItemID & 0x3FFF);
                bool writeHue = item.Hue != 0;
                if (writeHue || isLT)
                    itemID |= 0x8000;

                writer.WriteUInt32BE(item.Serial);
                writer.WriteUInt16BE(itemID);
                writer.WriteUInt8((byte)item.Layer);
                if (isLT)
                    writer.WriteUInt16BE((ushort)(ltHue & 0x3FFF));
                else if (writeHue)
                    writer.WriteUInt16BE(item.Hue);
            }

            writer.WriteUInt32BE(0); // terminate

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_EquipmentItem(UOItem item, ushort hue, uint owner)
        {
            const byte ID = 0x2E;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(item.Serial);
            writer.WriteUInt16BE(item.ItemID);
            writer.WriteInt8(0);
            writer.WriteUInt8((byte)item.Layer);
            writer.WriteUInt32BE(owner);
            writer.WriteUInt16BE(hue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_SeasonChange(int season, bool playSound)
        {
            const byte ID = 0xBC;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8((byte)season);
            writer.WriteBool(playSound);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_Target(uint targetid, bool ground, byte targetflags = 0)
        {
            const byte ID = 0x6C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteBool(ground);
            writer.WriteUInt32BE(targetid);
            writer.WriteUInt8(targetflags);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_SetWeather(int type, int num)
        {
            const byte ID = 0x65;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8((byte)type); //types: 0x00 - "It starts to rain", 0x01 - "A fierce storm approaches.", 0x02 - "It begins to snow", 0x03 - "A storm is brewing.", 0xFF - None (turns off sound effects), 0xFE (no effect?? Set temperature?) 
            writer.WriteUInt8((byte)num); //number of weather effects on screen
            writer.WriteUInt8(0xFE);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_CancelTarget(uint targetid)
        {
            const byte ID = 0x6C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0);
            writer.WriteUInt32BE(targetid);
            writer.WriteUInt8(3);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_ContainerItem(UOItem item)
        {
            const byte ID = 0x25;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(item.Serial);

            writer.WriteUInt16BE(item.ItemID);
            writer.WriteUInt8(0);
            writer.WriteUInt16BE(item.Amount);
            writer.WriteUInt16BE((ushort)item.Position.X);
            writer.WriteUInt16BE((ushort)item.Position.Y);

            if (Engine.UsePostKRPackets)
                writer.WriteUInt8(item.GridNum);//gridline

            object cont = item.Container;
            if (cont is UOEntity)
                writer.WriteUInt32BE(((UOEntity)item.Container).Serial);
            else if (cont is uint ser)
                writer.WriteUInt32BE(ser);
            else
                writer.WriteUInt32BE(0x7FFFFFFF);

            /*if (SearchExemptionAgent.Contains(item))
				WriteUShort((ushort)Config.GetInt("ExemptColor"));
			else*/

            if (Scripts.Commands.NextUsedOnce == item.Serial)
                writer.WriteUInt16BE((ushort)(STB.RED_HUE & 0x3FFF));
            else if (Scripts.Commands.UsedOnce.Contains(item.Serial))
                writer.WriteUInt16BE((ushort)(STB.GRAY_HUE & 0x3FFF));
            else
                writer.WriteUInt16BE(item.Hue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_LiftRej(byte reason = 5)//5 == unspecified
        {
            const byte ID = 0x27;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(reason);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_ChangeCombatant(uint ser)
        {
            const byte ID = 0xAA;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(ser);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_RemoveObject(uint serial)
        {
            const byte ID = 0x1D;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_WorldItem(UOItem item)
        {
            const byte ID = 0x1A;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            // 14 base length
            // +2 - Amount
            // +2 - Hue
            // +1 - Flags

            uint serial = item.Serial;
            ushort itemID = item.ItemID;
            ushort amount = item.Amount;
            int x = item.Position.X;
            int y = item.Position.Y;
            ushort hue = item.Hue;
            byte flags = item.GetPacketFlags();
            byte direction = (byte)item.Direction;

            if (amount != 0)
                serial |= 0x80000000;
            else
                serial &= 0x7FFFFFFF;
            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE((ushort)(itemID & 0x7FFF));
            if (amount != 0)
                writer.WriteUInt16BE(amount);

            x &= 0x7FFF;
            if (direction != 0)
                x |= 0x8000;
            writer.WriteUInt16BE((ushort)x);

            y &= 0x3FFF;
            if (hue != 0)
                y |= 0x8000;
            if (flags != 0)
                y |= 0x4000;

            writer.WriteUInt16BE((ushort)y);
            if (direction != 0)
                writer.WriteUInt8(direction);
            writer.WriteInt8((sbyte)item.Position.Z);
            if (hue != 0)
                writer.WriteUInt16BE(hue);
            if (flags != 0)
                writer.WriteUInt8(flags);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_AsciiMessage(uint serial, int graphic, MessageType type, int hue, int font, string name, string text)
        {
            const byte ID = 0x1C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (name == null)
            {
                name = string.Empty;
            }

            if (text == null)
            {
                text = string.Empty;
            }

            if (hue == 0)
            {
                hue = 0x3B2;
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE((ushort)graphic);
            writer.WriteUInt8((byte)type);
            writer.WriteUInt16BE((ushort)hue);
            writer.WriteUInt16BE((ushort)font);
            writer.WriteASCII(name, 30);
            writer.WriteASCII(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_PlaySound(int sound)
        {
            const byte ID = 0x54;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x01); //(0x00=quiet, repeating, 0x01=single normally played sound effect)
            writer.WriteUInt16BE((ushort)sound);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE((ushort)UOSObjects.Player.Position.X);
            writer.WriteUInt16BE((ushort)UOSObjects.Player.Position.Y);
            writer.WriteUInt16BE((ushort)UOSObjects.Player.Position.Z);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }

        public static void PRecv_OPLInfo(uint ser, uint hash)
        {
            const byte ID = 0xDC;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(ser);
            writer.WriteUInt32BE(hash);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            ClassicUO.Network.PacketHandlers.Handler.Append(writer.BufferWritten, true);
        }
    }

    internal static class ServerPackets
    {
        public static void PSend_DropRequest(this NetClient socket, uint from, Point3D p, uint dest)
        {
            const byte ID = 0x08;

            if (UOSObjects.Player == null)
            {
                return;
            }

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 20 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(from);
            writer.WriteUInt16BE((ushort)p.X);
            writer.WriteUInt16BE((ushort)p.Y);
            writer.WriteInt8((sbyte)p.Z);
            if (Engine.UsePostKRPackets)
            {
                writer.WriteUInt8(UOSObjects.FindItem(from)?.GridNum ?? 0);
            }
            writer.WriteUInt32BE(dest);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_EquipRequest(this NetClient socket, uint item, uint to, Layer layer)
        {
            const byte ID = 0x13;

            if (UOSObjects.Player == null)
            {
                return;
            }

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 20 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(item);
            writer.WriteUInt8((byte)layer);
            writer.WriteUInt32BE(to);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_AttackRequest(this NetClient socket, uint serial)
        {
            const byte ID = 0x05;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_CastSpell(this NetClient socket, int idx)
        {
            const byte ID = 0xBF;
            const byte ID_OLD = 0x12;

            byte id = ID;

            if (Engine.PreSAPackets)
            {
                id = ID_OLD;
            }

            int length = NetClient.Socket.PacketsTable.GetPacketLength(id);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(id);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (!Engine.PreSAPackets)
            {
                writer.WriteUInt16BE(0x1C);
                writer.WriteUInt16BE(0x02);
                writer.WriteUInt16BE((ushort)idx);
            }
            else
            {
                writer.WriteUInt8(0x56);
                writer.WriteASCII(idx.ToString());
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_LiftRequest(this NetClient socket, uint ser, int amount)
        {
            const byte ID = 0x07;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(ser);
            writer.WriteUInt16BE((ushort)amount);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_UseAbility(this NetClient socket, Ability a)
        {
            const byte ID = 0xD7;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(UOSObjects.Player.Serial);
            writer.WriteUInt16BE(0x19);
            if (a == Ability.None)
            {
                writer.WriteBool(true);
            }
            else
            {
                writer.WriteBool(false);
                writer.WriteUInt32BE((uint)a);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_BandageReq(this NetClient socket, uint bandage, uint target)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x2C);

            writer.WriteUInt32BE(bandage);

            writer.WriteUInt32BE(target);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_StunDisarm(this NetClient socket, bool stun)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (stun)
            {
                writer.WriteUInt16BE(0x0A);
            }
            else
            {
                writer.WriteUInt16BE(0x09);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_ColorPickResponse(this NetClient socket, uint serial, ushort graphic, ushort hue)
        {
            const byte ID = 0x95;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(hue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_SingleClick(this NetClient socket, uint clicked)
        {
            const byte ID = 0x09;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(clicked);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_DoubleClick(this NetClient socket, uint clicked)
        {
            const byte ID = 0x06;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(clicked);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_Resync(this NetClient socket)
        {
            const byte ID = 0x22;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }


            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_SetWarMode(this NetClient socket, bool mode)
        {
            const byte ID = 0x72;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteBool(mode);
            writer.WriteUInt8(0x00);
            writer.WriteUInt8(0x32);
            writer.WriteUInt8(0x00);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_PartyMessage(this NetClient socket, string text, uint serial = 0)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }


            writer.WriteUInt16BE(0x06);

            if (SerialHelper.IsValid(serial))
            {
                writer.WriteUInt8(0x03);
                writer.WriteUInt32BE(serial);
            }
            else
            {
                writer.WriteUInt8(0x04);
            }

            writer.WriteUnicodeBE(text);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_PartyAccept(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x08);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_PartyDecline(this NetClient socket, uint serial)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x06);
            writer.WriteUInt8(0x09);
            writer.WriteUInt32BE(serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_TargetResponse(this NetClient socket, TargetInfo info)
        {
            const byte ID = 0x6C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(info.Type);
            writer.WriteUInt32BE(info.TargID);
            writer.WriteUInt8(info.Flags);
            writer.WriteUInt32BE(info.Serial);
            writer.WriteUInt16BE((ushort)info.X);
            writer.WriteUInt16BE((ushort)info.Y);
            writer.WriteUInt16BE((ushort)info.Z);
            writer.WriteUInt16BE(info.Gfx);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_TargetResponse(this NetClient socket, uint targetid, UOEntity obj)
        {
            const byte ID = 0x6C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x00); // target object
            writer.WriteUInt32BE(targetid);
            writer.WriteUInt8(0); // flags
            writer.WriteUInt32BE(obj.Serial);
            writer.WriteUInt16BE((ushort)obj.Position.X);
            writer.WriteUInt16BE((ushort)obj.Position.Y);
            writer.WriteUInt16BE((ushort)obj.Position.Z);
            writer.WriteUInt16BE(obj.Graphic);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_TargetCancelResponse(this NetClient socket, uint targetid)
        {
            const byte ID = 0x6C;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0);
            writer.WriteUInt32BE(targetid);
            writer.WriteUInt8(0);
            writer.WriteUInt32BE(0);
            writer.WriteUInt16BE(0xFFFF);
            writer.WriteUInt16BE(0xFFFF);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(0);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_UniEncodedCommandMessage(this NetClient socket, MessageType type, int hue, int font, string text, string lang = "ENU")
        {
            const byte ID = 0xAD;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            var entries = ClassicUO.Client.Game.UO.FileManager.Speeches.GetKeywords(text);
            bool encoded = entries != null && entries.Count != 0;
            if (encoded)
                type |= MessageType.Encoded;

            writer.WriteUInt8((byte)type);
            writer.WriteUInt16BE((ushort)hue);
            writer.WriteUInt16BE((ushort)font);
            writer.WriteASCII(lang, 4);

            if (encoded)
            {
                List<byte> codeBytes = new List<byte>();
                byte[] utf8 = Encoding.UTF8.GetBytes(text);
                int elen = entries.Count;
                codeBytes.Add((byte)(elen >> 4));
                int num3 = elen & 15;
                bool flag = false;
                int index = 0;

                while (index < elen)
                {
                    int keywordID = entries[index].KeywordID;

                    if (flag)
                    {
                        codeBytes.Add((byte)(keywordID >> 4));
                        num3 = keywordID & 15;
                    }
                    else
                    {
                        codeBytes.Add((byte)((num3 << 4) | ((keywordID >> 8) & 15)));
                        codeBytes.Add((byte)keywordID);
                    }

                    index++;
                    flag = !flag;
                }

                if (!flag) codeBytes.Add((byte)(num3 << 4));

                for (int i = 0; i < codeBytes.Count; i++)
                    writer.WriteUInt8(codeBytes[i]);

                writer.Write(utf8);
                writer.WriteUInt8(0);
            }
            else
            {
                writer.WriteUnicodeBE(text);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_RenameReq(this NetClient socket, uint target, string name)
        {
            const byte ID = 0x75;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(target);
            writer.WriteASCII(name, 30);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }


            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_InvokeVirtue(this NetClient socket, byte id)
        {
            const byte ID = 0x12;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0xF4);
            writer.WriteASCII(id.ToString());

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_SkillsStatusChange(this NetClient socket, ushort skillindex, byte lockstate)
        {
            const byte ID = 0x3A;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }
            
            writer.WriteUInt16BE(skillindex);
            writer.WriteUInt8(lockstate);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_StatusQuery(this NetClient socket, UOMobile m)
        {
            const byte ID = 0x34;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(0xEDEDEDED);
            writer.WriteUInt8(0x04);
            writer.WriteUInt32BE(m.Serial);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_TradeResponse(this NetClient socket, uint serial, int code, bool state)
        {
            const byte ID = 0x6F;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            if (code == 1)
            {
                writer.WriteUInt8(0x01);
                writer.WriteUInt32BE(serial);
            }
            else if (code == 2)
            {
                writer.WriteUInt8(0x02);
                writer.WriteUInt32BE(serial);
                writer.WriteUInt32BE((uint)(state ? 1 : 0));
            }
            else
            {
                writer.Dispose();

                return;
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_VendorSellResponse(this NetClient socket, UOMobile vendor, List<SellListItem> list)
        {
            const byte ID = 0x9F;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(vendor.Serial);
            writer.WriteUInt16BE((ushort)list.Count);

            for (int i = 0; i < list.Count; ++i)
            {
                SellListItem sli = list[i];
                writer.WriteUInt32BE(sli.Serial);
                writer.WriteUInt16BE(sli.Amount);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_GumpResponse(this NetClient socket, uint serial, uint typeid, int bid, List<int> switches, List<GumpTextEntry> entries)
        {
            const byte ID = 0xB1;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(typeid);

            writer.WriteUInt32BE((uint)bid);

            writer.WriteUInt32BE((uint)switches.Count);

            for (int i = 0; i < switches.Count; i++)
            {
                writer.WriteUInt32BE((uint)switches[i]);
            }

            writer.WriteUInt32BE((uint)entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                GumpTextEntry gte = entries[i];
                
                writer.WriteUInt16BE(gte.EntryID);
                writer.WriteUInt16BE((ushort)gte.Text.Length);
                writer.WriteUnicodeBE(gte.Text, gte.Text.Length);
            }

            if (UOSObjects.Player.OpenedGumps.TryGetValue(typeid, out var list))
            {
                list.Remove(list.First(g => g.ServerID == serial));
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_PingPacket(this NetClient socket, byte seq)
        {
            const byte ID = 0x73;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(seq);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_UseSkill(this NetClient socket, int sk)
        {
            const byte ID = 0x12;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8(0x24);
            writer.WriteASCII($"{sk} 0");

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_VendorBuyResponse(this NetClient socket, uint vendor, List<VendorBuyItem> list)
        {
            const byte ID = 0x3B;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(vendor);
            if (list.Count > 0)
            {
                writer.WriteUInt8(0x02); // flag

                for (int i = 0; i < list.Count; ++i)
                {
                    VendorBuyItem vbi = list[i];
                    writer.WriteUInt8(0x1A); // layer?
                    writer.WriteUInt32BE(vbi.Serial);
                    writer.WriteUInt16BE((ushort)vbi.Amount);
                }
            }
            else
            {
                writer.WriteUInt8(0x00);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_PromptResponse(this NetClient socket, uint serial, uint promptid, uint operation, string text, string lang = "ENU")
        {
            const byte ID = 0xC2;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(promptid);
            writer.WriteUInt32BE(operation);

            if (string.IsNullOrEmpty(lang))
            {
                lang = "ENU";
            }

            writer.WriteASCII(lang.ToUpper(), 4);

            if (text != string.Empty)
            {
                writer.WriteUnicodeBE(text);
            }

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_DesignStateDetailed(this NetClient socket, uint serial, uint revision, int xMin, int yMin, int xMax, int yMax, MultiTileEntry[] tiles)
        {
            static void clear(byte[] buffer, int size)
            {
                for (int i = 0; i < size; ++i)
                    buffer[i] = 0;
            }

            const byte ID = 0xD8;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            const int MAX_ITEMS_PER_STAIR_BUFFER = 750;
            byte[][] planeBuffers = System.Buffers.ArrayPool<byte[]>.Shared.Rent(9);
            for (int i = 0; i < planeBuffers.Length; ++i)
            {
                planeBuffers[i] = System.Buffers.ArrayPool<byte>.Shared.Rent(0x400);
            }

            bool[] planeUsed = System.Buffers.ArrayPool<bool>.Shared.Rent(9);

            byte[][] stairBuffers = System.Buffers.ArrayPool<byte[]>.Shared.Rent(6);
            for (int i = 0; i < stairBuffers.Length; ++i)
            {
                stairBuffers[i] = System.Buffers.ArrayPool<byte>.Shared.Rent(MAX_ITEMS_PER_STAIR_BUFFER * 5);
            }

            byte[] deflatedBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(0x2000);

            writer.WriteUInt8(0x03); // Compression Type
            writer.WriteUInt8(0x00); // Unknown
            writer.WriteUInt32BE(serial);
            writer.WriteUInt32BE(revision);
            writer.WriteUInt16BE((ushort)tiles.Length);
            writer.WriteUInt16BE(0); // Buffer length : reserved
            writer.WriteUInt8(0); // Plane count : reserved

            int totalLength = 1; // includes plane count

            int width = (xMax - xMin) + 1;
            int height = (yMax - yMin) + 1;

            for (int i = 0; i < planeUsed.Length; ++i)
            {
                planeUsed[i] = false;
            }

            clear(planeBuffers[0], width * height * 2);

            for (int i = 0; i < 4; ++i)
            {
                clear(planeBuffers[1 + i], (width - 1) * (height - 2) * 2);
                clear(planeBuffers[5 + i], width * (height - 1) * 2);
            }

            int totalStairsUsed = 0;

            for (int i = 0; i < tiles.Length; ++i)
            {
                MultiTileEntry mte = tiles[i];
                int x = mte._OffsetX - xMin;
                int y = mte._OffsetY - yMin;
                int z = mte._OffsetZ;
                int plane, size;
                bool floor = false;
                try
                {
                    floor = (ClassicUO.Client.Game.UO.FileManager.TileData.StaticData[mte._ItemID & (ClassicUO.Client.Game.UO.FileManager.TileData.StaticData.Length - 1)].Height <= 0);
                }
                catch
                {
                }

                switch (z)
                {
                    case 0: plane = 0; break;
                    case 7: plane = 1; break;
                    case 27: plane = 2; break;
                    case 47: plane = 3; break;
                    case 67: plane = 4; break;
                    default:
                    {
                        int stairBufferIndex = (totalStairsUsed / MAX_ITEMS_PER_STAIR_BUFFER);
                        byte[] stairBuffer = stairBuffers[stairBufferIndex];

                        int byteIndex = (totalStairsUsed % MAX_ITEMS_PER_STAIR_BUFFER) * 5;

                        stairBuffer[byteIndex++] = (byte)((mte._ItemID >> 8) & 0x3F);
                        stairBuffer[byteIndex++] = (byte)mte._ItemID;

                        stairBuffer[byteIndex++] = (byte)mte._OffsetX;
                        stairBuffer[byteIndex++] = (byte)mte._OffsetY;
                        stairBuffer[byteIndex++] = (byte)mte._OffsetZ;

                        ++totalStairsUsed;

                        continue;
                    }
                }

                if (plane == 0)
                {
                    size = height;
                }
                else if (floor)
                {
                    size = height - 2;
                    x -= 1;
                    y -= 1;
                }
                else
                {
                    size = height - 1;
                    plane += 4;
                }

                int index = ((x * size) + y) * 2;

                planeUsed[plane] = true;
                planeBuffers[plane][index] = (byte)((mte._ItemID >> 8) & 0x3F);
                planeBuffers[plane][index + 1] = (byte)mte._ItemID;
            }

            int planeCount = 0;

            for (int i = 0; i < planeBuffers.Length; ++i)
            {
                if (!planeUsed[i])
                    continue;

                ++planeCount;

                int size = 0;

                if (i == 0)
                    size = width * height * 2;
                else if (i < 5)
                    size = (width - 1) * (height - 2) * 2;
                else
                    size = width * (height - 1) * 2;

                byte[] inflatedBuffer = planeBuffers[i];

                int deflatedLength = deflatedBuffer.Length;
                ZLibManaged.Compress(deflatedBuffer, ref deflatedLength, inflatedBuffer);
                writer.WriteUInt8((byte)(0x20 | i));
                writer.WriteUInt8((byte)size);
                writer.WriteUInt8((byte)deflatedLength);
                writer.WriteUInt8((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                writer.Write(deflatedBuffer);

                totalLength += 4 + deflatedLength;
            }

            int totalStairBuffersUsed = (totalStairsUsed + (MAX_ITEMS_PER_STAIR_BUFFER - 1)) / MAX_ITEMS_PER_STAIR_BUFFER;

            for (int i = 0; i < totalStairBuffersUsed; ++i)
            {
                ++planeCount;

                int count = (totalStairsUsed - (i * MAX_ITEMS_PER_STAIR_BUFFER));

                if (count > MAX_ITEMS_PER_STAIR_BUFFER)
                    count = MAX_ITEMS_PER_STAIR_BUFFER;

                int size = count * 5;

                byte[] inflatedBuffer = stairBuffers[i];

                int deflatedLength = deflatedBuffer.Length;
                ZLibManaged.Compress(deflatedBuffer, ref deflatedLength, inflatedBuffer);
                writer.WriteUInt8((byte)(9 + i));
                writer.WriteUInt8((byte)size);
                writer.WriteUInt8((byte)deflatedLength);
                writer.WriteUInt8((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                writer.Write(deflatedBuffer);

                totalLength += 4 + deflatedLength;
            }

            writer.Seek(15, SeekOrigin.Begin);

            writer.WriteUInt16BE((ushort)totalLength); // Buffer length
            writer.WriteUInt8((byte)planeCount); // Plane count

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_HuePicker(this NetClient socket, uint serial, ushort itemid)
        {
            const byte ID = 0x95;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(0);
            writer.WriteUInt16BE(itemid);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_OpenDoorMacro(this NetClient socket)
        {
            const byte ID = 0x12;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt8((byte)0x58);
            writer.WriteUInt8((byte)0);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_MenuResponse(this NetClient socket, uint serial, ushort menuid, ushort index, ushort itemid, ushort hue)
        {
            const byte ID = 0x7D;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt32BE(serial);
            writer.WriteUInt16BE(menuid);
            writer.WriteUInt16BE(index);
            writer.WriteUInt16BE(itemid);
            writer.WriteUInt16BE(hue);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_ContextMenuRequest(this NetClient socket, uint entity)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x13);
            writer.WriteUInt32BE(entity);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }

        public static void PSend_ContextMenuResponse(this NetClient socket, uint entity, ushort idx)
        {
            const byte ID = 0xBF;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x15);
            writer.WriteUInt32BE(entity);
            writer.WriteUInt16BE(idx);

            if (length < 0)
            {
                writer.Seek(1, SeekOrigin.Begin);
                writer.WriteUInt16BE((ushort)writer.BytesWritten);
            }
            else
            {
                writer.WriteZero(length - writer.BytesWritten);
            }

            socket.Send(writer.BufferWritten, true);
            writer.Dispose();
        }
    }

	internal sealed class GumpTextEntry
	{
		internal GumpTextEntry(ushort id, string s)
		{
			EntryID = id;
			Text = s;
		}

		internal ushort EntryID;
		internal string Text;
	}
}
