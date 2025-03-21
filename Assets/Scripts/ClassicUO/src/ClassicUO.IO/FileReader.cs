using ClassicUO.Assets;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    public abstract class FileReader : IDisposable
    {
        private long _position;
        private readonly FileStream _stream;

        protected FileReader(FileStream stream)
        {
            _stream = stream;
        }

        public string FilePath => _stream.Name;
        public long Length => _stream.Length;
        public long Position => _position;

        public abstract BinaryReader Reader { get; }

        public virtual void Dispose()
        {
            Reader?.Dispose();
            _stream?.Dispose();
        }

        public void Seek(long index, SeekOrigin origin) => _position = Reader.BaseStream.Seek(index, origin);
        public sbyte ReadInt8() { _position += sizeof(sbyte); return Reader.ReadSByte(); }
        public byte ReadUInt8() { _position += sizeof(byte); return Reader.ReadByte(); }
        public short ReadInt16() { _position += sizeof(short); return Reader.ReadInt16(); }
        public ushort ReadUInt16() { _position += sizeof(ushort); return Reader.ReadUInt16(); }
        public int ReadInt32() { _position += sizeof(int); return Reader.ReadInt32(); }
        public uint ReadUInt32() { _position += sizeof(uint); return Reader.ReadUInt32(); }
        public long ReadInt64() { _position += sizeof(long); return Reader.ReadInt64(); }
        public ulong ReadUInt64() { _position += sizeof(ulong); return Reader.ReadUInt64(); }
        public int Read(Span<byte> buffer) { _position += buffer.Length; return Reader.Read(buffer); }
        public unsafe T Read<T>() where T : unmanaged
        {
            Unsafe.SkipInit<T>(out var v);
            var p = new Span<byte>(&v, sizeof(T));
            Read(p);
            return v;
        }

        // MobileUO: TODO: InlineArray feature is not available in Unity's C#
        public MapBlock ReadMapBlock()
        {
            var mapBlock = new MapBlock();
            mapBlock.Header = ReadUInt32();
            mapBlock.Cells = new MapCells[64];
            for (int i = 0; i < mapBlock.Cells.Length; i++)
            {
                mapBlock.Cells[i] = Read<MapCells>();
            }

            return mapBlock;
        }

        public HuesGroup ReadHuesGroup()
        {
            var huesGroup = new HuesGroup();
            huesGroup.Header = ReadUInt32();
            huesGroup.Entries = new HuesBlock[8];
            for (int i = 0; i < huesGroup.Entries.Length; i++)
            {
                huesGroup.Entries[i] = ReadHuesBlock();
            }

            return huesGroup;
        }

        public unsafe HuesBlock ReadHuesBlock()
        {
            var huesBlock = new HuesBlock();
            huesBlock.ColorTable = new ushort[32];
            for (int i = 0; i < huesBlock.ColorTable.Length; i++)
            {
                huesBlock.ColorTable[i] = ReadUInt16();
            }

            huesBlock.TableStart = ReadUInt16();
            huesBlock.TableEnd = ReadUInt16();
            var buf = new byte[20];
            /*huesBlock.Name =*/ Read(buf);

            return huesBlock;
        }
    }
}
