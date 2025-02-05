using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO
{
    class PixelPicker
    {
        const int InitialDataCount = 0x40000; // 256kb

        Dictionary<ulong, int> m_IDs = new Dictionary<ulong, int>();
        readonly List<byte> m_Data = new List<byte>(InitialDataCount); // list<t> access is 10% slower than t[].

        public bool Get(ulong textureID, int x, int y, int extraRange = 0)
        {
            int index;
            if (!m_IDs.TryGetValue(textureID, out index))
            {
                return false;
            }
            int width = ReadIntegerFromData(ref index);
            if (x < 0 || x >= width)
            {
                return false;
            }
            int height = ReadIntegerFromData(ref index);
            if (y < 0 || y >= height)
            {
                return false;
            }
            int current = 0;
            int target = x + y * width;
            bool inTransparentSpan = true;
            while (current < target)
            {
                int spanLength = ReadIntegerFromData(ref index);
                current += spanLength;
                if (extraRange == 0)
                {
                    if (target < current)
                    {
                        return !inTransparentSpan;
                    }
                }
                else
                {
                    if (!inTransparentSpan)
                    {
                        int y0 = current / width;
                        int x1 = current % width;
                        int x0 = x1 - spanLength;
                        for (int range = -extraRange; range <= extraRange; range++)
                        {
                            if (y + range == y0 && (x + extraRange >= x0) && (x - extraRange <= x1))
                            {
                                return true;
                            }
                        }
                    }
                }
                inTransparentSpan = !inTransparentSpan;
            }
            return false;
        }

        // MobileUO: this logic existed in the old UOTexture class
        // MobileUO: it's probably no longer needed
        public bool Get(ulong textureID, Microsoft.Xna.Framework.Graphics.Texture2D texture, int x, int y, int extraRange = 0, bool pixelCheck = true)
        {
            //return Get(textureID, x, y, extraRange);

            if (x >= 0 && y >= 0 && x < texture.Width && y < texture.Height)
            {
                if (!pixelCheck)
                {
                    return true;
                }

                if (texture.UnityTexture == null)
                    return false;
                
                int pos = y * texture.Width + x;
                return GetDataAtPos(pos, texture) != 0;
            }

            return false;
        }

        public void GetDimensions(ulong textureID, out int width, out int height)
        {
            int index;
            if (!m_IDs.TryGetValue(textureID, out index))
            {
                width = height = 0;
                return;
            }
            width = ReadIntegerFromData(ref index);
            height = ReadIntegerFromData(ref index);
        }

        public void Set(ulong textureID, int width, int height, Span<uint> pixels)
        {
            if (Has(textureID))
            {
                return;
            }

            int begin = m_Data.Count;
            WriteIntegerToData(width);
            WriteIntegerToData(height);
            bool countingTransparent = true;
            int count = 0;
            for (int i = 0, len = width * height; i < len; i++)
            {
                bool isTransparent = pixels[i] == 0;
                if (countingTransparent != isTransparent)
                {
                    WriteIntegerToData(count);
                    countingTransparent = !countingTransparent;
                    count = 0;
                }
                count += 1;
            }
            WriteIntegerToData(count);
            m_IDs[textureID] = begin;
        }

        bool Has(ulong textureID)
        {
            return m_IDs.ContainsKey(textureID);
        }

        void WriteIntegerToData(int value)
        {
            while (value > 0x7f)
            {
                m_Data.Add((byte)((value & 0x7f) | 0x80));
                value >>= 7;
            }
            m_Data.Add((byte)value);
        }

        int ReadIntegerFromData(ref int index)
        {
            int value = 0;
            int shift = 0;
            while (true)
            {
                byte data = m_Data[index++];
                value += (data & 0x7f) << shift;
                if ((data & 0x80) == 0x00)
                {
                    return value;
                }
                shift += 7;
            }
        }

        // MobileUO: Used for Contains checks in texture using Unity's own texture data, instead of keeping a copy of the data in _data field
        private uint GetDataAtPos(int pos, Microsoft.Xna.Framework.Graphics.Texture2D texture)
        {
            //The index calculation here is the same as in Texture2D.SetData
            var width = texture.Width;
            int x = pos % width;
            int y = pos / width;
            y *= width;
            var index = y + (width - x - 1);
            
            var data = (texture.UnityTexture as UnityEngine.Texture2D).GetRawTextureData<uint>();
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
