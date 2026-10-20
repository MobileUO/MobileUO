using ClassicUO.Assets;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace ClassicUO.IO
{
    public class MMFileReader : FileReader
    {
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly MemoryMappedFile _mmf;
        private readonly BinaryReader _file;
        // MobileUO: added ptr
        private unsafe byte* _ptr;

        public MMFileReader(FileStream stream) : base(stream)
        {
            if (Length <= 0)
                return;

            _mmf = MemoryMappedFile.CreateFromFile
            (
                stream,
                null,
                0,
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false
            );

            _accessor = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);

            try
            {
                unsafe
                {
                    // MobileUO: use local ptr
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
                    _file = new BinaryReader(new UnmanagedMemoryStream(_ptr, Length));
                }
            }
            catch (Exception ex)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                _accessor?.Dispose();
                _mmf?.Dispose();

                throw new InvalidOperationException("Failed to acquire memory-mapped file pointer.", ex);
            }
        }

        public override BinaryReader Reader => _file;

        // MobileUO: added methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe T ReadAt<T>(long offset) => Unsafe.ReadUnaligned<T>(_ptr + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void ReadAt(long offset, Span<byte> buffer) => new ReadOnlySpan<byte>(_ptr + offset, buffer.Length).CopyTo(buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe MapBlock ReadMapBlockAt(long offset)
        {
            if (offset < 0 || offset + 4 + (64 * sizeof(MapCells)) > Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            byte* ptr = _ptr + offset;
            var mapBlock = new MapBlock();
            mapBlock.Header = Unsafe.ReadUnaligned<uint>(ptr);

            ptr += 4;
            mapBlock.Cells = new MapCells[64];

            for (int i = 0; i < 64; i++)
            {
                mapBlock.Cells[i] = Unsafe.ReadUnaligned<MapCells>(ptr);
                ptr += sizeof(MapCells);
            }

            return mapBlock;
        }

        public override void Dispose()
        {
            // MobileUO: added dispose
            if (_accessor != null && _accessor.SafeMemoryMappedViewHandle != null && !_accessor.SafeMemoryMappedViewHandle.IsClosed)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }

            _file?.Dispose();
            _accessor?.Dispose();
            _mmf?.Dispose();

            base.Dispose();
        }
    }
}
