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
using System.Buffers.Binary;
using System.IO;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Utility;
using UOScript;

namespace Assistant.IO
{
    unsafe ref struct StackDataFixedReadWrite
    {
        private const MethodImplOptions IMPL_OPTION = MethodImplOptions.AggressiveInlining;
        private Span<byte> _buffer;

        public StackDataFixedReadWrite(ref Span<byte> data)
        {
            _buffer = data;
            Length = data.Length;
            Position = 0;
        }

        public int Position { get; private set; }
        public long Length { get; }
        public int Remaining => (int)(Length - Position);

        [MethodImpl(IMPL_OPTION)]
        public void Skip(int count)
        {
            Position += count;
        }

        [MethodImpl(IMPL_OPTION)]
        public byte ReadUInt8()
        {
            if (Position + 1 > Length)
            {
                return 0;
            }

            return _buffer[Position++];
        }

        [MethodImpl(IMPL_OPTION)]
        public sbyte ReadInt8()
        {
            if (Position + 1 > Length)
            {
                return 0;
            }

            return (sbyte)_buffer[Position++];
        }

        public bool ReadBool() => ReadUInt8() != 0;

        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16LE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt16LittleEndian(_buffer.Slice(Position, 2), out ushort v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16LE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt16LittleEndian(_buffer.Slice(Position, 2), out short v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32LE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt32LittleEndian(_buffer.Slice(Position, 4), out uint v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32LE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt32LittleEndian(_buffer.Slice(Position, 4), out int v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64LE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt64LittleEndian(_buffer.Slice(Position, 8), out ulong v);

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64LE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt64LittleEndian(_buffer.Slice(Position, 8), out long v);

            Skip(8);

            return v;
        }





        [MethodImpl(IMPL_OPTION)]
        public ushort ReadUInt16BE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt16BigEndian(_buffer.Slice(Position, 2), out ushort v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public short ReadInt16BE()
        {
            if (Position + 2 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt16BigEndian(_buffer.Slice(Position, 2), out short v);

            Skip(2);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32BE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt32BigEndian(_buffer.Slice(Position, 4), out uint v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public int ReadInt32BE()
        {
            if (Position + 4 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt32BigEndian(_buffer.Slice(Position, 4), out int v);

            Skip(4);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public ulong ReadUInt64BE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt64BigEndian(_buffer.Slice(Position, 8), out ulong v);

            Skip(8);

            return v;
        }

        [MethodImpl(IMPL_OPTION)]
        public long ReadInt64BE()
        {
            if (Position + 8 > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadInt64BigEndian(_buffer.Slice(Position, 8), out long v);

            Skip(8);

            return v;
        }

        private string ReadRawString(int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _buffer.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            if (size <= 0)
            {
                result = string.Empty;
            }
            else
            {
                result = StringHelper.Cp1252ToString(slice.Slice(0, size));

                if (safe)
                {
                    Span<char> buff = stackalloc char[256];
                    ReadOnlySpan<char> chars = result.AsSpan();

                    ValueStringBuilder sb = new ValueStringBuilder(buff);

                    bool hasDoneAnyReplacements = false;
                    int last = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        if (!StringHelper.IsSafeChar(chars[i]))
                        {
                            hasDoneAnyReplacements = true;
                            sb.Append(chars.Slice(last, i - last));
                            last = i + 1; // Skip the unsafe char
                        }
                    }

                    if (hasDoneAnyReplacements)
                    {
                        // append the rest of the string
                        if (last < chars.Length)
                        {
                            sb.Append(chars.Slice(last, chars.Length - last));
                        }

                        result = sb.ToString();
                    }

                    sb.Dispose();
                }
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        public string ReadASCII(bool safe = false)
        {
            return ReadRawString(-1, 1, safe);
        }

        public string ReadASCII(int length, bool safe = false)
        {
            return ReadRawString(length, 1, safe);
        }

        public string ReadUnicodeBE(bool safe = false)
        {
            return ReadString(Encoding.BigEndianUnicode, -1, 2, safe);
        }

        public string ReadUnicodeBE(int length, bool safe = false)
        {
            return ReadString(Encoding.BigEndianUnicode, length, 2, safe);
        }

        public string ReadUnicodeLE(bool safe = false)
        {
            return ReadString(Encoding.Unicode, -1, 2, safe);
        }

        public string ReadUnicodeLE(int length, bool safe = false)
        {
            return ReadString(Encoding.Unicode, length, 2, safe);
        }

        public string ReadUTF8(bool safe = false)
        {
            return ReadString(Encoding.UTF8, -1, 1, safe);
        }

        public string ReadUTF8(int length, bool safe = false)
        {
            return ReadString(Encoding.UTF8, length, 1, safe);
        }

        public void Read(ref Span<byte> data, int offset, int count)
        {
            _buffer.Slice(Position + offset, count);
        }

        // from modernuo <3
        private string ReadString(Encoding encoding, int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _buffer.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            fixed (byte* ptr = slice)
            {
                result = encoding.GetString(ptr, size);
            }

            if (safe)
            {
                Span<char> buff = stackalloc char[256];
                ReadOnlySpan<char> chars = result.AsSpan();

                ValueStringBuilder sb = new ValueStringBuilder(buff);

                bool hasDoneAnyReplacements = false;
                int last = 0;
                for (int i = 0; i < chars.Length; i++)
                {
                    if (!StringHelper.IsSafeChar(chars[i]))
                    {
                        hasDoneAnyReplacements = true;
                        sb.Append(chars.Slice(last, i - last));
                        last = i + 1; // Skip the unsafe char
                    }
                }

                if (hasDoneAnyReplacements)
                {
                    // append the rest of the string
                    if (last < chars.Length)
                    {
                        sb.Append(chars.Slice(last, chars.Length - last));
                    }

                    result = sb.ToString();
                }

                sb.Dispose();
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        [MethodImpl(IMPL_OPTION)]
        private static int GetIndexOfZero(ReadOnlySpan<byte> span, int sizeT)
        {
            switch (sizeT)
            {
                case 2: return MemoryMarshal.Cast<byte, char>(span).IndexOf('\0') * 2;
                case 4: return MemoryMarshal.Cast<byte, uint>(span).IndexOf((uint)0) * 4;
                default: return span.IndexOf((byte)0);
            }
        }

        [MethodImpl(IMPL_OPTION)]
        public void Seek(int position, SeekOrigin origin = SeekOrigin.Begin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:

                    Position = Math.Min(position, _buffer.Length);

                    break;

                case SeekOrigin.Current:

                    Position = Math.Min(Position + position, _buffer.Length);

                    break;

                case SeekOrigin.End:

                    Position = Math.Min(_buffer.Length + position, _buffer.Length);

                    break;
            }
        }


        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt8(byte b)
        {
            EnsureSize(1);

            _buffer[Position] = b;

            Position += 1;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt8(sbyte b)
        {
            EnsureSize(1);

            _buffer[Position] = (byte)b;

            Position += 1;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteBool(bool b)
        {
            WriteUInt8(b ? (byte)0x01 : (byte)0x00);
        }



        /* Little Endian */

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt16LE(ushort b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16LE(short b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteInt16LittleEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32LE(uint b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32LE(int b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteInt32LittleEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt64LE(ulong b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteUInt64LittleEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt64LE(long b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteInt64LittleEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str)
        {
            WriteString<char>(Encoding.Unicode, str, -1);
            WriteUInt16LE(0x0000);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeLE(string str, int length)
        {
            WriteString<char>(Encoding.Unicode, str, length);
        }

        /* Big Endian */

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt16BE(ushort b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt16BE(short b)
        {
            EnsureSize(2);

            BinaryPrimitives.WriteInt16BigEndian(_buffer.Slice(Position), b);

            Position += 2;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt32BE(uint b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt32BE(int b)
        {
            EnsureSize(4);

            BinaryPrimitives.WriteInt32BigEndian(_buffer.Slice(Position), b);

            Position += 4;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUInt64BE(ulong b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteUInt64BigEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteInt64BE(long b)
        {
            EnsureSize(8);

            BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(Position), b);

            Position += 8;
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str)
        {
            WriteString<char>(Encoding.BigEndianUnicode, str, -1);
            WriteUInt16BE(0x0000);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteUnicodeBE(string str, int length)
        {
            WriteString<char>(Encoding.BigEndianUnicode, str, length);
        }





        [MethodImpl(IMPL_OPTION)]
        public void WriteUTF8(string str, int len)
        {
            WriteString<byte>(Encoding.UTF8, str, len);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                foreach (var b in StringHelper.StringToCp1252Bytes(str))
                {
                    WriteUInt8(b);
                }
            }

            WriteUInt8(0x00);
        }

        [MethodImpl(IMPL_OPTION)]
        public void WriteASCII(string str, int length)
        {
            int start = Position;

            if (string.IsNullOrEmpty(str))
            {
                WriteZero(sizeof(byte));
            }
            else
            {
                foreach (var b in StringHelper.StringToCp1252Bytes(str, length))
                {
                    WriteUInt8(b);
                }
            }

            if (length > -1 && Position > start)
            {
                WriteZero(length * sizeof(byte) - (Position - start));
            }
        }


        [MethodImpl(IMPL_OPTION)]
        public void WriteZero(int count)
        {
            if (count > 0)
            {
                EnsureSize(count);

                _buffer.Slice(Position, count).Fill(0);

                Position += count;
            }
        }

        private void WriteString<T>(Encoding encoding, string str, int length) where T : struct, IEquatable<T>
        {
            int sizeT = Unsafe.SizeOf<T>();

            if (sizeT > 2)
            {
                throw new InvalidConstraintException("WriteString only accepts byte, sbyte, char, short, and ushort as a constraint");
            }

            if (str == null)
            {
                str = string.Empty;
            }

            int byteCount = length > -1 ? length * sizeT : encoding.GetByteCount(str);

            if (byteCount == 0)
            {
                return;
            }

            EnsureSize(byteCount);

            int charLength = Math.Min(length > -1 ? length : str.Length, str.Length);

            int processed = encoding.GetBytes
            (
                str.AsSpan(0, charLength),
                _buffer.Slice(Position)
            );

            Position += processed;

            if (length > -1)
            {
                WriteZero(length * sizeT - processed);
            }
        }

        [MethodImpl(IMPL_OPTION)]
        private void EnsureSize(int size)
        {
            if (Position + size > _buffer.Length)
            {
                new OverflowException("The written data must not exceed the current size of the packet!");
            }
        }
    }
}
