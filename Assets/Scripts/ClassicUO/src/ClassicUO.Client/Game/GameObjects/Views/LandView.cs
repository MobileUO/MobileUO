// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.GameObjects
{
    public sealed partial class Land
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (!AllowedToDraw || IsDestroyed)
            {
                return false;
            }

            //Engine.DebugInfo.LandsRendered++;

            ushort hue = Hue;
            bool isSelected = SelectedObject.Object == this;

            if (isSelected && _profile.HighlightGameObjects)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            }
            else if (
                _profile.NoColorObjectsOutOfRange
                && Distance > World.ClientViewRange
            )
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && _profile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            if (isSelected)
            {
                SpellVisualRangeManager.Instance.LastCursorTileLoc = new Vector2(X, Y);
            }

            if (SpellVisualRangeManager.Instance.IsTargetingAfterCasting())
            {
                hue = SpellVisualRangeManager.Instance.ProcessHueForTile(hue, this);
            }

            if (_profile.DisplayRadius && Distance == _profile.DisplayRadiusDistance)
                hue = _profile.DisplayRadiusHue;


            Vector3 hueVec;
            if (hue != 0)
            {
                hueVec.X = hue - 1;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND_HUED
                    : ShaderHueTranslator.SHADER_HUED;
            }
            else
            {
                hueVec.X = 0;
                hueVec.Y = IsStretched
                    ? ShaderHueTranslator.SHADER_LAND
                    : ShaderHueTranslator.SHADER_NONE;
            }
            hueVec.Z = 1f;

            if (IsStretched)
            {
                posY += Z << 2;

                ref readonly var texmapInfo = ref Client.Game.UO.Texmaps.GetTexmap(
                    Client.Game.UO.FileManager.TileData.LandData[Graphic].TexID
                );

                if (texmapInfo.Texture != null)
                {
                    batcher.DrawStretchedLand(
                        texmapInfo.Texture,
                        new Vector2(posX, posY),
                        texmapInfo.UV,
                        ref YOffsets,
                        ref NormalTop,
                        ref NormalRight,
                        ref NormalLeft,
                        ref NormalBottom,
                        hueVec,
                        depth + 0.5f
                    );
                }
                else
                {
                    DrawStatic(
                        batcher,
                        Graphic,
                        posX,
                        posY,
                        hueVec,
                        depth,
                        _profile.AnimatedWaterEffect && TileData.IsWet
                    );
                }
            }
            else
            {
                ref readonly var artInfo = ref Client.Game.UO.Arts.GetLand(Graphic);

                // ref readonly var texmapInfo = ref Client.Game.UO.Texmaps.GetTexmap(
                //     Client.Game.UO.FileManager.TileData.LandData[Graphic].TexID
                // );

                if (artInfo.Texture != null)
                {
                    var pos = new Vector2(posX, posY);
                    var scale = Vector2.One;

                    if (_profile.AnimatedWaterEffect && TileData.IsWet)
                    {
                        batcher.Draw(
                            artInfo.Texture,
                            pos,
                            artInfo.UV,
                            hueVec,
                            0f,
                            Vector2.Zero,
                            scale,
                            SpriteEffects.None,
                            depth + 0.5f
                        );

                        var sin = (float)Math.Sin(Time.Ticks / 1000f);
                        var cos = (float)Math.Cos(Time.Ticks / 1000f);
                        scale = new Vector2(1.1f + sin * 0.1f, 1.1f + cos * 0.5f * 0.1f);
                    }

                    batcher.Draw(
                        artInfo.Texture,
                        pos,
                        artInfo.UV,
                        hueVec,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        depth + 0.5f
                    );

                }
            }

            return true;
        }

        public override bool CheckMouseSelection()
        {
            if (IsStretched)
            {
                return SelectedObject.IsPointInStretchedLand(
                    ref YOffsets,
                    RealScreenPosition.X,
                    RealScreenPosition.Y + (Z << 2)
                );
            }

            return SelectedObject.IsPointInLand(RealScreenPosition.X, RealScreenPosition.Y);
        }
    }
}
