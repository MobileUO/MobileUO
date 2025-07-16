using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ClassicUO.IO
{
    public class MMFileReader : FileReader
    {
        // MobileUO: removed accessor for stream
        private readonly MemoryMappedViewStream _stream;
        private readonly MemoryMappedFile _mmf;
        private readonly BinaryReader _file;

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

            // MobileUO: replaced unsafe call
            _stream = _mmf.CreateViewStream(0, Length, MemoryMappedFileAccess.Read);
            _file = new BinaryReader(_stream);
        }

        public override BinaryReader Reader => _file;

        public override void Dispose()
        {
            // MobileUO: added dispose
            _file?.Dispose();
            _stream?.Dispose();
            _mmf?.Dispose();

            base.Dispose();
        }
    }
}
