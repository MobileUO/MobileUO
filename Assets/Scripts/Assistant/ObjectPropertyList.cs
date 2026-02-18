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

using System.IO;
using System.Diagnostics.Contracts;
using System.Text;
using System.Collections.Generic;

using ClassicUO.Network;
using ClassicUO.IO;
using ClassicUO.Assets;

namespace Assistant
{
    internal class ObjectPropertyList
    {
        internal class OPLEntry
        {
            internal int Number = 0;
            internal string Args = "";

            internal OPLEntry(int num) : this(num, "")
            {
            }

            internal OPLEntry(int num, string args)
            {
                Number = num;
                Args = args;
            }
        }

        private List<int> _StringNums { get; } = new List<int>();
        internal List<OPLEntry> Content { get; } = new List<OPLEntry>();

        private uint _CustomHash;
        private uint _Hash;
        private List<OPLEntry> _CustomContent { get; } = new List<OPLEntry>();
        
        internal uint Hash
        {
            get { return _Hash ^ _CustomHash; }
            set { _Hash = value; }
        }

        internal bool Customized
        {
            get { return _CustomHash != 0; }
        }

        internal ObjectPropertyList(UOEntity owner)
        {
            Owner = owner;

            _StringNums.AddRange(_DefaultStringNums);
        }

        internal UOEntity Owner { get; } = null;

        internal void Read(StackDataReader reader, out string name)
        {
            Content.Clear();
            name = "";

            reader.Seek(11); // seek to packet data

            //p.ReadUInt(); // serial from 5 to 9
            //p.ReadByte(); // 10
            //p.ReadByte(); // 11
            _Hash = reader.ReadUInt32BE();

            _StringNums.Clear();
            _StringNums.AddRange(_DefaultStringNums);
            int cliloc;
            List<(int, string)> list = new List<(int, string)>();
            while ((cliloc = reader.ReadInt32BE()) != 0)
            {
                ushort length = reader.ReadUInt16BE();

                string argument = string.Empty;

                if(length != 0)
                {
                    argument = reader.ReadUnicodeLE(length / 2);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var temp = list[i];

                    if (temp.Item1 == cliloc && temp.Item2 == argument)
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }

                list.Add((cliloc, argument));
            }
            for(int i = 0; i < list.Count; i++)
            {
                if(i == 0)
                    name = ClassicUO.Client.Game.UO.FileManager.Clilocs.Translate(list[i].Item1, list[i].Item2, true);
                Content.Add(new OPLEntry(list[i].Item1, list[i].Item2));
            }
 
            for (int i = 0; i < _CustomContent.Count; i++)
            {
                OPLEntry ent = _CustomContent[i];
                if (_StringNums.Contains(ent.Number))
                {
                    _StringNums.Remove(ent.Number);
                }
                else
                {
                    for (int s = 0; s < _DefaultStringNums.Length; s++)
                    {
                        if (ent.Number == _DefaultStringNums[s])
                        {
                            ent.Number = GetStringNumber();
                            break;
                        }
                    }
                }
            }
        }

        internal void Add(int number)
        {
            if (number == 0)
                return;

            AddHash((uint)number);

            _CustomContent.Add(new OPLEntry(number));
        }

        internal void AddHash(uint val)
        {
            _CustomHash ^= (val & 0x3FFFFFF);
            _CustomHash ^= (val >> 26) & 0x3F;
        }

        static int GetHashCode32(string s)
        {
            unsafe
            {
                fixed (char* src = s)
                {
                    Contract.Assert(src[s.Length] == '\0', "src[this.Length] == '\\0'");
                    Contract.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                    int hash1 = (5381 << 16) + 5381;
                    int hash2 = hash1;

                    // 32 bit machines. 
                    int* pint = (int*)src;
                    int len = s.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        internal void Add(int number, string arguments)
        {
            if (number == 0)
                return;

            AddHash((uint)number);
            AddHash((uint)GetHashCode32(arguments));
            _CustomContent.Add(new OPLEntry(number, arguments));
        }

        internal void Add(int number, string format, object arg0)
        {
            Add(number, string.Format(format, arg0));
        }

        internal void Add(int number, string format, object arg0, object arg1)
        {
            Add(number, string.Format(format, arg0, arg1));
        }

        internal void Add(int number, string format, object arg0, object arg1, object arg2)
        {
            Add(number, string.Format(format, arg0, arg1, arg2));
        }

        internal void Add(int number, string format, params object[] args)
        {
            Add(number, string.Format(format, args));
        }

        private static int[] _DefaultStringNums = new int[]
        {
            1042971,
            1070722,
            1114057,
            1114778,
            1114779,
            1149934
        };

        private int GetStringNumber()
        {
            if (_StringNums.Count > 0)
            {
                int num = _StringNums[0];
                _StringNums.RemoveAt(0);
                return num;
            }
            else
            {
                return 1049644;
            }
        }

        private const string HTMLFormat = " <CENTER><BASEFONT COLOR=#FF0000>{0}</BASEFONT></CENTER> ";

        internal void Add(string text)
        {
            Add(GetStringNumber(), string.Format(HTMLFormat, text));
        }

        internal void Add(string format, string arg0)
        {
            Add(GetStringNumber(), string.Format(format, arg0));
        }

        internal void Add(string format, string arg0, string arg1)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1));
        }

        internal void Add(string format, string arg0, string arg1, string arg2)
        {
            Add(GetStringNumber(), string.Format(format, arg0, arg1, arg2));
        }

        internal void Add(string format, params object[] args)
        {
            Add(GetStringNumber(), string.Format(format, args));
        }

        internal bool Remove(int number)
        {
            for (int i = 0; i < Content.Count; i++)
            {
                OPLEntry ent = Content[i];
                if (ent == null)
                    continue;

                if (ent.Number == number)
                {
                    for (int s = 0; s < _DefaultStringNums.Length; s++)
                    {
                        if (_DefaultStringNums[s] == ent.Number)
                        {
                            _StringNums.Insert(0, ent.Number);
                            break;
                        }
                    }

                    Content.RemoveAt(i);
                    AddHash((uint)ent.Number);
                    if (!string.IsNullOrEmpty(ent.Args))
                        AddHash((uint)GetHashCode32(ent.Args));

                    return true;
                }
            }

            for (int i = 0; i < _CustomContent.Count; i++)
            {
                OPLEntry ent = _CustomContent[i];
                if (ent == null)
                    continue;

                if (ent.Number == number)
                {
                    for (int s = 0; s < _DefaultStringNums.Length; s++)
                    {
                        if (_DefaultStringNums[s] == ent.Number)
                        {
                            _StringNums.Insert(0, ent.Number);
                            break;
                        }
                    }

                    _CustomContent.RemoveAt(i);
                    AddHash((uint)ent.Number);
                    if (!string.IsNullOrEmpty(ent.Args))
                        AddHash((uint)GetHashCode32(ent.Args));
                    if (_CustomContent.Count == 0)
                        _CustomHash = 0;
                    return true;
                }
            }

            return false;
        }

        internal bool Remove(string str)
        {
            string htmlStr = string.Format(HTMLFormat, str);

            for (int i = 0; i < _CustomContent.Count; i++)
            {
                OPLEntry ent = _CustomContent[i];
                if (ent == null)
                    continue;

                for (int s = 0; s < _DefaultStringNums.Length; s++)
                {
                    if (ent.Number == _DefaultStringNums[s] && (ent.Args == htmlStr || ent.Args == str))
                    {
                        _StringNums.Insert(0, ent.Number);

                        _CustomContent.RemoveAt(i);

                        AddHash((uint)ent.Number);
                        if (!string.IsNullOrEmpty(ent.Args))
                            AddHash((uint)GetHashCode32(ent.Args));
                        return true;
                    }
                }
            }

            return false;
        }

        public static void PRecv_ObjectPropertyList(ObjectPropertyList opl)
        {
            const byte ID = 0xD6;

            int length = NetClient.Socket.PacketsTable.GetPacketLength(ID);

            StackDataWriter writer = new StackDataWriter(length < 0 ? 64 : length);

            writer.WriteUInt8(ID);

            if (length < 0)
            {
                writer.WriteZero(2);
            }

            writer.WriteUInt16BE(0x01);
            writer.WriteUInt32BE((opl.Owner != null ? opl.Owner.Serial : 0));
            writer.WriteUInt8(0);
            writer.WriteUInt8(0);
            writer.WriteUInt32BE(opl._Hash ^ opl._CustomHash);

            foreach (OPLEntry ent in opl.Content)
            {
                if (ent != null && ent.Number != 0)
                {
                    writer.WriteUInt32BE((uint)ent.Number);
                    if (!string.IsNullOrEmpty(ent.Args))
                    {
                        ushort len = (ushort)Encoding.Unicode.GetByteCount(ent.Args);
                        writer.WriteUInt16BE(len);
                        writer.WriteUnicodeLE(ent.Args, len >> 1);

                        /*int byteCount = Encoding.Unicode.GetByteCount(ent.Args);

                        if (byteCount > _Buffer.Length)
                            _Buffer = new byte[byteCount];

                        byteCount = Encoding.Unicode.GetBytes(ent.Args, 0, ent.Args.Length, _Buffer, 0);
                        writer.WriteUInt16BE((ushort)byteCount);
                        writer.Write(_Buffer);*/
                    }
                    else
                    {
                        writer.WriteUInt16BE(0);
                    }
                }
            }

            foreach (OPLEntry ent in opl._CustomContent)
            {
                if (ent != null && ent.Number != 0)
                {
                    string arguments = ent.Args;

                    if (string.IsNullOrEmpty(arguments))
                        arguments = " ";
                    arguments += "\t ";

                    writer.WriteUInt32BE((uint)ent.Number);
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        ushort len = (ushort)Encoding.Unicode.GetByteCount(arguments);
                        writer.WriteUInt16BE(len);
                        writer.WriteUnicodeLE(arguments, len >> 1);
                        /*int byteCount = Encoding.Unicode.GetByteCount(arguments);

                        if (byteCount > _Buffer.Length)
                            _Buffer = new byte[byteCount];

                        byteCount = Encoding.Unicode.GetBytes(arguments, 0, arguments.Length, _Buffer, 0);

                        writer.WriteUInt16BE((ushort)byteCount);
                        writer.Write(_Buffer);*/
                    }
                    else
                    {
                        writer.WriteUInt16BE(0);
                    }
                }
            }

            writer.WriteUInt32BE(0);

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
}
