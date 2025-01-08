#region license

// Copyright (c) 2021, andreakarasho
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

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    internal class UOTexture : Texture2D
    {
        public UOTexture(int width, int height) : base
        (
            Client.Game.GraphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color
        )
        {
            Ticks = Time.Ticks + 3000;
        }

        public long Ticks { get; set; }
        public uint[] Data { get; private set; }

        // MobileUO: added keepData optional parameter
        public void PushData(uint[] data, bool keepData = false)
        {
            if (keepData)
            {
                Data = data;
            }

            SetData(data);
        }

        // MobileUO: logic changes for Unity
        public bool Contains(int x, int y, bool pixelCheck = true)
        {
            // MobileUO: don't keep Data != null or else clicks won't work
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                if (!pixelCheck)
                {
                    return true;
                }

                if (UnityTexture == null)
                    return false;
                
                int pos = y * Width + x;
                return GetDataAtPos(pos) != 0;
            }

            return false;
        }

        // MobileUO: Used for Contains checks in texture using Unity's own texture data, instead of keeping a copy of the data in _data field
        private uint GetDataAtPos(int pos)
        {
            //The index calculation here is the same as in Texture2D.SetData
            var width = Width;
            int x = pos % width;
            int y = pos / width;
            y *= width;
            var index = y + (width - x - 1);
            
            var data = (UnityTexture as UnityEngine.Texture2D).GetRawTextureData<uint>();
            //We reverse the index because we had already reversed it in Texture2D.SetData
            var reversedIndex = data.Length - index - 1;
            if (reversedIndex < data.Length && reversedIndex >= 0)
            {
                return data[reversedIndex];
            }

            return 0;
        }
    }
}