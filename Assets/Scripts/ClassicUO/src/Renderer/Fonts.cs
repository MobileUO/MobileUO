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

namespace ClassicUO.Renderer
{
    internal static class Fonts
    {
        static Fonts()
        {
            // MobileUO: removed "ClassicUO.Renderer.fonts." and ".xnb" from fonts
            Regular = SpriteFont.Create("regular_font");
            Bold = SpriteFont.Create("bold_font");

            Map1 = SpriteFont.Create("map1_font");
            Map2 = SpriteFont.Create("map2_font");
            Map3 = SpriteFont.Create("map3_font");
            Map4 = SpriteFont.Create("map4_font");
            Map5 = SpriteFont.Create("map5_font");
            Map6 = SpriteFont.Create("map6_font");
        }

        public static SpriteFont Regular { get; }
        public static SpriteFont Bold { get; }
        public static SpriteFont Map1 { get; }
        public static SpriteFont Map2 { get; }
        public static SpriteFont Map3 { get; }
        public static SpriteFont Map4 { get; }
        public static SpriteFont Map5 { get; }
        public static SpriteFont Map6 { get; }
    }
}