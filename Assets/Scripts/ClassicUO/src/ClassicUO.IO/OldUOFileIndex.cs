#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

// MobileUO: TODO: this file is deprecated and will be removed once Unity's C# features are resolved

using System;

namespace ClassicUO.IO
{
    public struct OldUOFileIndex : IEquatable<OldUOFileIndex>
    {
        public OldUOFileIndex
        (
            IntPtr address,
            uint fileSize,
            long offset,
            int length,
            int decompressed,
            int width = 0,
            int height = 0,
            ushort hue = 0
        )
        {
            Address = address;
            FileSize = fileSize;
            Offset = offset;
            Length = length;
            DecompressedLength = decompressed;
            Width = width;
            Height = height;
            Hue = hue;

            AnimOffset = 0;
        }

        public IntPtr Address;
        public uint FileSize;
        public long Offset;
        public int Length;
        public int DecompressedLength;
        public int Width;
        public int Height;
        public ushort Hue;
        public sbyte AnimOffset;



        public static OldUOFileIndex Invalid = new OldUOFileIndex
        (
            IntPtr.Zero,
            0,
            0,
            0,
            0
        );

        public bool Equals(OldUOFileIndex other)
        {
            return (Address, Offset, Length, DecompressedLength) == (other.Address, other.Offset, other.Length, other.DecompressedLength);
        }
    }

    public struct OldUOFileIndex5D
    {
        public OldUOFileIndex5D(uint file, uint index, uint offset, uint length, uint extra = 0)
        {
            FileID = file;
            BlockID = index;
            Position = offset;
            Length = length;
            GumpData = extra;
        }

        public uint FileID;
        public uint BlockID;
        public uint Position;
        public uint Length;
        public uint GumpData;
    }
}