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
// See <https://www.gnu.org/licenses/agpl-3.0.html> for details.
#endregion

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Assets;
using ClassicUO.Utility;
using ClassicUO.Configuration;
using Assistant.Core;
using BuffIcon = Assistant.Core.BuffIcon;
using ClassicUO.Game.UI.Gumps;
using STB = ClassicUO.Game.UI.Controls.ScriptTextBox;
using Assistant.IO;

namespace Assistant
{
    internal class PacketHandlers
    {
        internal static HashSet<uint> IgnoreGumps { get; } = new HashSet<uint>();

        internal static void Initialize()
        {
            //Client -> Server handlers
            PacketHandler.RegisterClientToServerViewer(0x00, new PacketViewerCallback(CreateCharacter));
            //PacketHandler.RegisterClientToServerViewer(0x01, new PacketViewerCallback(Disconnect));
            PacketHandler.RegisterClientToServerViewer(0x02, new PacketViewerCallback(MovementRequest));
            PacketHandler.RegisterClientToServerViewer(0x05, new PacketViewerCallback(AttackRequest));
            PacketHandler.RegisterClientToServerViewer(0x06, new PacketViewerCallback(ClientDoubleClick));
            PacketHandler.RegisterClientToServerViewer(0x07, new PacketViewerCallback(LiftRequest));
            PacketHandler.RegisterClientToServerViewer(0x08, new PacketViewerCallback(DropRequest));
            PacketHandler.RegisterClientToServerViewer(0x09, new PacketViewerCallback(ClientSingleClick));
            PacketHandler.RegisterClientToServerViewer(0x12, new PacketViewerCallback(ClientTextCommand));
            PacketHandler.RegisterClientToServerViewer(0x13, new PacketViewerCallback(EquipRequest));
            // 0x29 - UOKR confirm drop.  0 bytes payload (just a single byte, 0x29, no length or data)
            PacketHandler.RegisterClientToServerViewer(0x3A, new PacketViewerCallback(SetSkillLock));
            PacketHandler.RegisterClientToServerViewer(0x5D, new PacketViewerCallback(PlayCharacter));
            PacketHandler.RegisterClientToServerViewer(0x7D, new PacketViewerCallback(MenuResponse));
            //PacketHandler.RegisterClientToServerFilter(0x80, new PacketFilterCallback(ServerListLogin));
            //PacketHandler.RegisterClientToServerFilter(0x91, new PacketFilterCallback(GameLogin));
            PacketHandler.RegisterClientToServerViewer(0x95, new PacketViewerCallback(HueResponse));
            //PacketHandler.RegisterClientToServerViewer(0xA0, new PacketViewerCallback(PlayServer));
            PacketHandler.RegisterClientToServerViewer(0xB1, new PacketViewerCallback(ClientGumpResponse));
            PacketHandler.RegisterClientToServerViewer(0xBF, new PacketViewerCallback(ExtendedClientCommand));
            //PacketHandler.RegisterClientToServerViewer( 0xD6, new PacketViewerCallback( BatchQueryProperties ) );
            PacketHandler.RegisterClientToServerViewer(0xC2, new PacketViewerCallback(UnicodePromptSend));
            PacketHandler.RegisterClientToServerViewer(0xD7, new PacketViewerCallback(ClientEncodedPacket));
            PacketHandler.RegisterClientToServerViewer(0xF8, new PacketViewerCallback(CreateCharacter));

            //Server -> Client handlers
            PacketHandler.RegisterServerToClientViewer(0x0B, new PacketViewerCallback(Damage));
            PacketHandler.RegisterServerToClientViewer(0x11, new PacketViewerCallback(MobileStatus));
            PacketHandler.RegisterServerToClientViewer(0x17, new PacketViewerCallback(NewMobileStatus));
            PacketHandler.RegisterServerToClientViewer(0x1A, new PacketViewerCallback(WorldItem));
            PacketHandler.RegisterServerToClientViewer(0x1B, new PacketViewerCallback(LoginConfirm));
            PacketHandler.RegisterServerToClientViewer(0x1C, new PacketViewerCallback(AsciiSpeech));
            PacketHandler.RegisterServerToClientViewer(0x1D, new PacketViewerCallback(RemoveObject));
            PacketHandler.RegisterServerToClientFilter(0x20, new PacketFilterCallback(MobileUpdate));
            PacketHandler.RegisterServerToClientViewer(0x24, new PacketViewerCallback(BeginContainerContent));
            PacketHandler.RegisterServerToClientFilter(0x25, new PacketFilterCallback(ContainerContentUpdate));
            PacketHandler.RegisterServerToClientViewer(0x27, new PacketViewerCallback(LiftReject));
            PacketHandler.RegisterServerToClientViewer(0x2D, new PacketViewerCallback(MobileStatInfo));
            PacketHandler.RegisterServerToClientViewer(0x3A, new PacketViewerCallback(Skills));
            PacketHandler.RegisterServerToClientFilter(0x3C, new PacketFilterCallback(ContainerContent));
            PacketHandler.RegisterServerToClientViewer(0x4E, new PacketViewerCallback(PersonalLight));
            PacketHandler.RegisterServerToClientViewer(0x4F, new PacketViewerCallback(GlobalLight));
            PacketHandler.RegisterServerToClientViewer(0x72, new PacketViewerCallback(ServerSetWarMode));
            PacketHandler.RegisterServerToClientViewer(0x73, new PacketViewerCallback(PingResponse));
            PacketHandler.RegisterServerToClientViewer(0x76, new PacketViewerCallback(ServerChange));
            PacketHandler.RegisterServerToClientFilter(0x77, new PacketFilterCallback(MobileMoving));
            PacketHandler.RegisterServerToClientFilter(0x78, new PacketFilterCallback(MobileIncoming));
            PacketHandler.RegisterServerToClientViewer(0x7C, new PacketViewerCallback(SendMenu));
            //PacketHandler.RegisterServerToClientFilter(0x8C, new PacketFilterCallback(ServerAddress));
            PacketHandler.RegisterServerToClientViewer(0xA1, new PacketViewerCallback(HitsUpdate));
            PacketHandler.RegisterServerToClientViewer(0xA2, new PacketViewerCallback(ManaUpdate));
            PacketHandler.RegisterServerToClientViewer(0xA3, new PacketViewerCallback(StamUpdate));
            //PacketHandler.RegisterServerToClientViewer(0xA8, new PacketViewerCallback(ServerList));
            PacketHandler.RegisterServerToClientViewer(0xAB, new PacketViewerCallback(DisplayStringQuery));
            PacketHandler.RegisterServerToClientViewer(0xAF, new PacketViewerCallback(DeathAnimation));
            PacketHandler.RegisterServerToClientViewer(0xAE, new PacketViewerCallback(UnicodeSpeech));
            PacketHandler.RegisterServerToClientViewer(0xB0, new PacketViewerCallback(UncompressedGump));
            PacketHandler.RegisterServerToClientViewer(0xB9, new PacketViewerCallback(Features));
            PacketHandler.RegisterServerToClientViewer(0xBC, new PacketViewerCallback(ChangeSeason));
            PacketHandler.RegisterServerToClientViewer(0xBE, new PacketViewerCallback(OnAssistVersion));
            PacketHandler.RegisterServerToClientViewer(0xBF, new PacketViewerCallback(ExtendedPacket));
            PacketHandler.RegisterServerToClientViewer(0xC1, new PacketViewerCallback(OnLocalizedMessage));
            PacketHandler.RegisterServerToClientViewer(0xC2, new PacketViewerCallback(UnicodePromptReceived));
            PacketHandler.RegisterServerToClientViewer(0xC8, new PacketViewerCallback(SetUpdateRange));
            PacketHandler.RegisterServerToClientViewer(0xCC, new PacketViewerCallback(OnLocalizedMessageAffix));
            PacketHandler.RegisterServerToClientViewer(0xD6, new PacketViewerCallback(EncodedPacket));//0xD6 "encoded" packets
            PacketHandler.RegisterServerToClientViewer(0xD8, new PacketViewerCallback(CustomHouseInfo));
            PacketHandler.RegisterServerToClientFilter(0xDC, new PacketFilterCallback(ServOPLHash));
            PacketHandler.RegisterServerToClientViewer(0xDD, new PacketViewerCallback(CompressedGump));
            PacketHandler.RegisterServerToClientViewer(0xF0, new PacketViewerCallback(RunUOProtocolExtention)); // Special RunUO protocol extentions (for KUOC/Razor)

            PacketHandler.RegisterServerToClientViewer(0xF3, new PacketViewerCallback(SAWorldItem));

            PacketHandler.RegisterServerToClientViewer(0x2C, new PacketViewerCallback(ResurrectionGump));

            PacketHandler.RegisterServerToClientViewer(0xDF, new PacketViewerCallback(BuffDebuff));
            
            PacketHandler.RegisterServerToClientViewer(0x95, new PacketViewerCallback(HuePicker));

            
            
            
            
            PacketHandler.RegisterServerToClientFilter(0x2E, new PacketFilterCallback(EquipmentUpdate));
            
        }

        private static void OnAssistVersion(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            args.Block = true;
        }

        private static void DisplayStringQuery(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
        }

        private static void SetUpdateRange(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOSObjects.ClientViewRange = reader.ReadUInt8();
        }

        private static void EncodedPacket(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            ushort id = reader.ReadUInt16BE();

            switch ( id )
            {
                case 1: // object property list
                {
                    uint serial = reader.ReadUInt32BE();
                    if (SerialHelper.IsItem(serial))
                    {
                        UOItem item = UOSObjects.FindItem( serial );
                        if ( item == null )
                            UOSObjects.AddItem( item=new UOItem( serial ) );

                        item.ReadPropertyList(ref reader, out string name );
                        if (!string.IsNullOrEmpty(name))
                            item.Name = name;
                        if ( item.ModifiedOPL )
                        {
                            args.Block = true;
                            ObjectPropertyList.PRecv_ObjectPropertyList(item.ObjPropList);
                        }
                    }
                    else if (SerialHelper.IsMobile(serial))
                    {
                        UOMobile m = UOSObjects.FindMobile( serial );
                        if ( m == null )
                            UOSObjects.AddMobile( m=new UOMobile( serial ) );

                        m.ReadPropertyList(ref reader, out _ );
                        if ( m.ModifiedOPL )
                        {
                            args.Block = true;
                            ObjectPropertyList.PRecv_ObjectPropertyList(m.ObjPropList);
                        }
                    }
                    break;
                }
            }
        }

        private static void ServOPLHash(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            uint s = rw.ReadUInt32BE();
            uint hash = rw.ReadUInt32BE();

            if ( SerialHelper.IsItem(s) )
            {
                 UOItem item = UOSObjects.FindItem( s );
                 if ( item != null && item.OPLHash != hash )
                 {
                      item.OPLHash = hash;
                      rw.Seek(rw.Position - 4);
                      rw.WriteUInt32BE(item.OPLHash);
                }
            }
            else if ( SerialHelper.IsMobile(s) )
            {
                 UOMobile m = UOSObjects.FindMobile( s );
                 if ( m != null && m.OPLHash != hash )
                 {
                      m.OPLHash = hash;
                      rw.Seek( rw.Position - 4 );
                      rw.WriteUInt32BE( m.OPLHash );
                 }
            }
        }

        private static void ClientSingleClick(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint ser = reader.ReadUInt32BE();

            UOMobile m = UOSObjects.FindMobile(ser);

            if (m == null)
                return;

            Targeting.CheckTextFlags(m);

            if (FriendsManager.IsFriend(m.Serial))
            {
                m.OverheadMessage(63, "[Friend]");
            }
        }

        private static void ClientDoubleClick(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint ser = reader.ReadUInt32BE();
            if (UOSObjects.Gump.PreventDismount && UOSObjects.Player != null && ser == UOSObjects.Player.Serial && UOSObjects.Player.Warmode && UOSObjects.Player.GetItemOnLayer(Layer.Mount) != null)
            { // mount layer = 0x19
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "Dismount Blocked");
                args.Block = true;
                return;
            }

            if (UOSObjects.Gump.UseObjectsQueue)
                args.Block = !PlayerData.DoubleClick(ser, false);
            if (SerialHelper.IsItem(ser) && ClassicUO.Client.Game.UO.World.Player != null)
                UOSObjects.Player.LastObject = ser;

            if (ScriptManager.Recording)
            {
                ushort gfx = 0;
                uint cont = 0;
                if (SerialHelper.IsItem(ser))
                {
                    UOItem i = UOSObjects.FindItem(ser);
                    if (i != null)
                    {
                        gfx = i.ItemID;
                        if (i.RootContainer != null)
                        {
                            if (i.RootContainer is UOEntity ent)
                            {
                                cont = ent.Serial;
                            }
                            else if (i.RootContainer is uint cser)
                                cont = cser;

                            if(SerialHelper.IsMobile(cont) && UOSObjects.FindMobile(cont) is UOMobile uom)
                            {
                                if (uom.Backpack != null && i.Container != uom && !(i.Container is uint cnt && cnt == uom.Serial))
                                    cont = uom.Backpack.Serial;
                            }
                        }
                    }
                }
                else if(SerialHelper.IsMobile(ser))
                {
                    UOMobile m = UOSObjects.FindMobile(ser);
                    if (m != null)
                        gfx = m.Body;
                }

                if (gfx != 0)
                {
                    if (ScriptManager.Recording)
                    {
                        if (UOSObjects.Gump.RecordTypeUse)//usetype (graphic) [color] [source] [range]
                        {
                            string contstr = "";
                            if (cont > 0)
                            {
                                if (UOSObjects.Player.Backpack != null && UOSObjects.Player.Backpack.Serial == cont)
                                    contstr = "\"backpack\"";
                                else if (SerialHelper.IsMobile(cont))
                                    contstr = "\"any\"";
                                else
                                    contstr = $"0x{cont:X8}";
                            }
                            else
                                contstr = "'world'";
                            ScriptManager.AddToScript($"usetype 0x{gfx:X4} \"any\" {contstr}");
                        }
                        else
                            ScriptManager.AddToScript($"useobject 0x{ser:X}");
                    }
                }
            }
        }

        private static HashSet<UOMobile> _RecentlyDead = new HashSet<UOMobile>();
        private static void DeathAnimation(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
            
            if (m != null)
            {
                if (_RecentlyDead.Contains(m))
                    return;
                _RecentlyDead.Add(m);
                Timer.DelayedCallbackState(TimeSpan.FromMilliseconds(150), OnAfterMobileDeath, m).Start();
            }
        }
        
        private static void OnAfterMobileDeath(UOMobile m)
        {
            if (m.IsGhost && ((m == UOSObjects.Player && UOSObjects.Gump.SnapOwnDeath) || UOSObjects.Gump.SnapOtherDeath))
            {
                UOSObjects.SnapShot();
            }
            _RecentlyDead.Remove(m);
        }

        private static void ExtendedClientCommand(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            ushort ext = reader.ReadUInt16BE();
            switch (ext)
            {
                case 0x10: // query object properties
                {
                    break;
                }
                case 0x13:
                {
                    ClientSingleClick(ref reader, args);
                    break;
                }
                case 0x15: // context menu response
                {
                    if (ScriptManager.Recording)
                    {
                        UOEntity ent = null;
                        uint ser = reader.ReadUInt32BE();
                        ushort idx = reader.ReadUInt16BE();

                        if (SerialHelper.IsMobile(ser))
                            ent = UOSObjects.FindMobile(ser);
                        else if (SerialHelper.IsItem(ser))
                            ent = UOSObjects.FindItem(ser);

                        if (ent != null && ent.ContextMenu != null)
                        {
                            ScriptManager.AddToScript($"contextmenu {(ser == ClassicUO.Client.Game.UO.World.Player.Serial ? "\"self\"" : $"0x{ser:X}")} {idx}");
                        }
                    }
                    break;
                }
                case 0x1C:// cast spell
                {
                    //uint ser = uint.MaxValue;
                    if (reader.ReadUInt16BE() == 1)
                        reader.ReadUInt32BE();//ser = reader.ReadUInt32BE();
                    ushort sid = reader.ReadUInt16BE();
                    Spell s = Spell.Get(sid);
                    if (s != null)
                    {
                        Spell.FullCast(sid);
                        args.Block = true;
                        if (ScriptManager.Recording)
                        {
                            ScriptManager.AddToScript($"cast \"{Spell.GetName(sid)}\"");
                        }
                    }
                    break;
                }
            }
        }

        private static void ClientTextCommand(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            int type = reader.ReadUInt8();
            string command = reader.ReadASCII();
            if (UOSObjects.Player != null && !string.IsNullOrEmpty(command))
            {
                switch (type)
                {
                    case 0x24: // Use skill
                    {
                        if (!int.TryParse(command.Split(' ')[0], out int skillIndex) || skillIndex >= ClassicUO.Client.Game.UO.FileManager.Skills.SkillsCount)
                            break;

                        UOSObjects.Player.LastSkill = skillIndex;
                        if (ScriptManager.Recording)
                            ScriptManager.AddToScript($"useskill \"{ClassicUO.Client.Game.UO.FileManager.Skills.Skills[skillIndex].Name}\"");
                        if (skillIndex == (int)SkillName.Stealth && !UOSObjects.Player.Visible)
                            StealthSteps.Hide();
                        SkillTimer.Start();
                        break;
                    }
                    case 0x27: // Cast spell from book
                    {
                        string[] split = command.Split(' ');
                        if (split.Length > 0)
                        {
                            if (ushort.TryParse(split[0], out ushort spellID))
                            {
                                uint serial = 0;
                                if (split.Length > 1)
                                    serial = Utility.ToUInt32(split[1], uint.MaxValue);
                                if (Spell.Get(spellID) != null)
                                {
                                    Spell.FullCast(spellID);
                                    args.Block = true;
                                    if (ScriptManager.Recording)
                                    {
                                        if (SerialHelper.IsValid(serial))
                                            ScriptManager.AddToScript($"cast \"{Spell.GetName(spellID)}\" 0x{serial:X}");
                                        else
                                            ScriptManager.AddToScript($"cast \"{Spell.GetName(spellID)}\"");
                                    }
                                }
                            }
                        }

                        break;
                    }

                    case 0x56: // Cast spell from macro
                    {
                        if(ushort.TryParse(command, out ushort spellID))
                        {
                            //Spell s = Spell.Get(spellID);
                            if (Spell.Get(spellID) != null)
                            {
                                Spell.FullCast(spellID);//s.OnCast(reader);
                                args.Block = true;
                                if (ScriptManager.Recording)
                                    ScriptManager.AddToScript($"cast \"{Spell.GetName(spellID)}\"");
                            }
                        }

                        break;
                    }
                }
            }
        }

        internal static DateTime PlayCharTime = DateTime.MinValue;

        private static void CreateCharacter(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            reader.Seek(10); // skip begining crap
            UOSObjects.OrigPlayerName = reader.ReadASCII(30);

            PlayCharTime = DateTime.UtcNow;
        }

        private static void PlayCharacter(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            reader.ReadUInt32BE(); //0xedededed
            UOSObjects.OrigPlayerName = reader.ReadASCII(30);

            PlayCharTime = DateTime.UtcNow;
        }

        private static object _ParentLifted;
        private static void LiftRequest(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            ushort amount = reader.ReadUInt16BE();

            UOItem item = UOSObjects.FindItem(serial);
            _ParentLifted = item?.Container ?? null;

            if (UOSObjects.Gump.UseObjectsQueue)
            {
                if (item == null)
                {
                    UOSObjects.AddItem(item = new UOItem(serial));
                    item.Amount = amount;
                }
                DragDropManager.Drag(item, amount, true);
                args.Block = true;
            }
        }

        private static void LiftReject(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            int reason = reader.ReadUInt8();
            if (!DragDropManager.LiftReject())
                args.Block = true;
            _ParentLifted = null;
        }

        private static void EquipRequest(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint iser = reader.ReadUInt32BE(); // item being dropped serial
            Layer layer = (Layer)reader.ReadUInt8();
            uint mser = reader.ReadUInt32BE();//mobile dropped to

            UOItem item = UOSObjects.FindItem(iser);

            if (layer == Layer.Invalid || layer > Layer.Bank)
            {
                if (item != null)
                {
                    layer = item.Layer;
                    if (layer == Layer.Invalid || layer > Layer.Bank)
                        layer = (Layer)item.TileDataInfo.Layer;
                }
            }

            if (item == null)
                return;

            UOMobile m = UOSObjects.FindMobile(mser);
            if (m == null)
                return;
            if (UOSObjects.Gump.UseObjectsQueue)
                args.Block = DragDropManager.Drop(item, m, layer);
            if (ScriptManager.Recording && layer > Layer.Invalid && layer < Layer.Mount)
            {
                if(layer == Layer.OneHanded && Scripts.Commands.WandTypes.Contains(item.Graphic) && item.ObjPropList != null && item.ObjPropList.Content.Count > 0 && item.ObjPropList.Content[0].Number == 505617 && int.TryParse(item.ObjPropList.Content[0].Args.Substring(1), out int arg)) // Wand Of ~1_val~
                {
                    ScriptManager.AddToScript($"equipwand \"{ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(arg).ToLower(XmlFileParser.Culture)}\"");
                }
                else if (UOSObjects.Gump.RecordTypeUse)
                {
                    ScriptManager.AddToScript($"equipitem 0x{item.Graphic:X4} {(byte)layer}");
                }
                else
                {
                    ScriptManager.AddToScript($"equipitem 0x{item.Serial:X8} {(byte)layer}");
                }
            }
            _ParentLifted = null;
        }

        private static void DropRequest(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint iser = reader.ReadUInt32BE();
            int x = (short)reader.ReadUInt16BE();
            int y = (short)reader.ReadUInt16BE();
            int z = reader.ReadInt8();

            UOItem i = UOSObjects.FindItem(iser);
            if (i == null)
                return;
            if (Engine.UsePostKRPackets)
                i.GridNum = reader.ReadUInt8();
            uint dser = reader.ReadUInt32BE();

            UOItem dest = UOSObjects.FindItem(dser);
            if (dest != null && dest.IsContainer && UOSObjects.Player != null && (dest.IsChildOf(UOSObjects.Player.Backpack) || dest.IsChildOf(UOSObjects.Player.Quiver)))
                i.IsNew = true;
            if (UOSObjects.Gump.UseObjectsQueue)
                args.Block = DragDropManager.Drop(i, dser, new Point3D(x, y, z));

            if (ScriptManager.Recording)
            {
                if (UOSObjects.Gump.RecordTypeUse)
                {
                    //movetype (graphic) (source) (destination) [(x y z)] [color] [amount] [range]
                    string source, destination;
                    if (_ParentLifted is uint ui)
                        source = $"0x{ui:X}";
                    else if (_ParentLifted is UOEntity ent)
                        source = $"0x{ent.Serial:X}";
                    else
                        source = "\"world\"";
                    if (dser == uint.MaxValue || dser == 0)
                        destination = "\"ground\"";
                    else if (UOSObjects.Player.Backpack != null && dser == UOSObjects.Player.Backpack.Serial)
                        destination = "\"backpack\"";
                    else
                        destination = $"0x{dser:X}";
                    if (destination[0] == '0' || destination == "\"backpack\"")
                    {
                        ScriptManager.AddToScript($"movetype 0x{i.Graphic:X} {source} {destination} {x} {y} {z} -1 {i.Amount}");
                    }
                    else
                    {
                        x -= UOSObjects.Player.Position.X;
                        y -= UOSObjects.Player.Position.Y;
                        z -= UOSObjects.Player.Position.Z;
                        ScriptManager.AddToScript($"movetypeoffset 0x{i.Graphic:X} {source} {destination} {x} {y} {z} -1 {i.Amount}");
                    }
                }
                else ////moveitem (serial) (destination) [(x y z)] [amount]
                {
                    string destination;
                    if (dser == uint.MaxValue || dser == 0)
                        destination = "\"ground\"";
                    else if (UOSObjects.Player.Backpack != null && dser == UOSObjects.Player.Backpack.Serial)
                        destination = "\"backpack\"";
                    else
                        destination = $"0x{dser:X}";
                    ScriptManager.AddToScript($"moveitem 0x{iser:X} {destination} {x} {y} {z} {i.Amount}");
                }
            }
            _ParentLifted = null;
        }

        private static void MovementRequest(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player != null)
            {
                AssistDirection dir = (AssistDirection)reader.ReadUInt8();
                //byte seq = reader.ReadUInt8();

                bool run = (dir & AssistDirection.Running) == AssistDirection.Running;
                UOSObjects.Player.Direction = dir &= AssistDirection.Up;
                if (ScriptManager.Recording)
                {
                    if(run)
                        ScriptManager.AddToScript($"run \"{dir}\"");
                    else
                        ScriptManager.AddToScript($"walk \"{dir}\"");
                }
            }
        }

        private static void ContainerContentUpdate(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
            {
                return;
            }

            // This function will ignore the item if the container item has not been sent to the client yet.
            // We can do this because we can't really count on getting all of the container info anyway.
            // (So we'd need to request the container be updated, so why bother with the extra stuff required to find the container once its been sent?)
            uint serial = rw.ReadUInt32BE();
            ushort itemid = rw.ReadUInt16BE();
            sbyte itemid_inc = rw.ReadInt8();
            ushort amount = Math.Max((ushort)1, rw.ReadUInt16BE());
            Point3D pos = new Point3D(rw.ReadUInt16BE(), rw.ReadUInt16BE(), 0);
            byte gridPos = 0;
            if (Engine.UsePostKRPackets)
                gridPos = rw.ReadUInt8();
            uint cser = rw.ReadUInt32BE();
            ushort hue = rw.ReadUInt16BE();

            UOItem i = UOSObjects.FindItem(serial);
            if (i == null)
            {
                if (!SerialHelper.IsItem(serial))
                    return;

                UOSObjects.AddItem(i = new UOItem(serial));
                i.IsNew = i.AutoStack = true;
            }
            else
            {
                i.CancelRemove();
            }
            
            if (serial != DragDropManager.Pending)
            {
                if (!DragDropManager.EndHolding(serial))
                    return;
            }

            i.ItemID = (ushort)(itemid + itemid_inc);
            i.Amount = amount;
            i.Position = pos;
            i.GridNum = gridPos;
            i.Hue = hue;
            //TODO: SearchException
            /*if (SearchExemptionAgent.Contains(i))
            {
                p.Seek(p.Position - 2);
                p.Write((short)Config.GetInt("ExemptColor"));
            }*/

            i.Container = cser;
            if (i.IsNew)
                UOItem.UpdateContainers();
            if (i.RootContainer == UOSObjects.Player)
            {
                if (i.RootContainer == UOSObjects.Player)
                {
                    if (Scripts.Commands.NextUsedOnce == serial)
                    {
                        rw.Seek(rw.Position - 2);
                        rw.WriteUInt16BE(STB.RED_HUE & 0x3FFF);
                    }
                    else if (Scripts.Commands.UsedOnce.Contains(serial))
                    {
                        rw.Seek(rw.Position - 2);
                        rw.WriteUInt16BE(STB.GRAY_HUE & 0x3FFF);
                    }
                }
            }
        }

        private static void BeginContainerContent(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint ser = reader.ReadUInt32BE();
            if (!SerialHelper.IsItem(ser))
                return;
            UOItem item = UOSObjects.FindItem(ser);
            if (item != null)
            {
                if (IgnoreGumps.Contains(ser))
                {
                    IgnoreGumps.Remove(ser);
                    args.Block = true;
                }
            }
            else
            {
                UOSObjects.AddItem(new UOItem(ser));
                UOItem.UpdateContainers();
            }
        }
        private static void ContainerContent(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            int count = rw.ReadUInt16BE();
            for (int i = 0; i < count; i++)
            {
                uint serial = rw.ReadUInt32BE();
                // serial is purposely not checked to be valid, sometimes buy lists dont have "valid" item serials (and we are okay with that).
                UOItem item = UOSObjects.FindItem(serial);
                if (item == null)
                {
                    UOSObjects.AddItem(item = new UOItem(serial));
                    item.IsNew = true;
                    item.AutoStack = false;
                }
                else
                {
                    item.CancelRemove();
                }
                if (!DragDropManager.EndHolding(serial))
                    continue;

                item.ItemID = rw.ReadUInt16BE();
                item.ItemID = (ushort)(item.ItemID + rw.ReadInt8());// signed, itemID offset
                item.Amount = rw.ReadUInt16BE();
                if (item.Amount == 0)
                    item.Amount = 1;
                item.Position = new Point3D(rw.ReadUInt16BE(), rw.ReadUInt16BE(), 0);
                if (Engine.UsePostKRPackets)
                    item.GridNum = rw.ReadUInt8();
                uint cont = rw.ReadUInt32BE();
                item.Hue = rw.ReadUInt16BE();
                if(item.RootContainer == UOSObjects.Player)
                {
                    if (Scripts.Commands.NextUsedOnce == serial)
                    {
                        rw.Seek(rw.Position - 2);
                        rw.WriteUInt16BE(STB.RED_HUE & 0x3FFF);
                    }
                    else if (Scripts.Commands.UsedOnce.Contains(serial))
                    {
                        rw.Seek(rw.Position - 2);
                        rw.WriteUInt16BE(STB.GRAY_HUE & 0x3FFF);
                    }
                }
                //TODO: SearchException + Counters
                /*if (SearchExemption.IsExempt(item))
                {
                    p.Seek(p.Position - 2);
                    p.WriteUShort(Config.GetInt("ExemptColor"));
                }*/

                item.Container = cont; // must be done after hue is set (for counters)
            }
            UOItem.UpdateContainers();
        }

        private static void EquipmentUpdate(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            uint serial = rw.ReadUInt32BE();

            UOItem i = UOSObjects.FindItem(serial);
            bool isNew = false;
            if (i == null)
            {
                UOSObjects.AddItem(i = new UOItem(serial));
                isNew = true;
                UOItem.UpdateContainers();
            }
            else
            {
                i.CancelRemove();
            }
            if (!DragDropManager.EndHolding(serial))
                return;

            ushort iid = rw.ReadUInt16BE();
            i.ItemID = (ushort)(iid + rw.ReadInt8()); // signed, itemID offset
            i.Layer = (Layer)rw.ReadUInt8();
            uint ser = rw.ReadUInt32BE();// cont must be set after hue (for counters)
            i.Hue = rw.ReadUInt16BE();

            i.Container = ser;

            int ltHue = UOSObjects.Gump.HLTargetHue;
            if (ltHue != 0 && Targeting.IsLastTarget(i.Container as UOMobile))
            {
                rw.Seek(rw.Position - 2);
                rw.WriteUInt16BE((ushort)(ltHue & 0x3FFF));
            }

            if (i.Layer == Layer.Backpack && isNew && UOSObjects.Gump.AutoSearchContainers && ser == UOSObjects.Player.Serial)
            {
                IgnoreGumps.Add(serial);
                PlayerData.DoubleClick(i.Serial);
            }
        }

        private static void SetSkillLock(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            int i = reader.ReadUInt16BE();

            if (i >= 0 && i < Skill.Count)
            {
                Skill skill = UOSObjects.Player.Skills[i];

                skill.Lock = (LockType)reader.ReadUInt8();
            }
        }

        private static void Skills(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null || UOSObjects.Player.Skills == null)
                return;
            byte type = reader.ReadUInt8();

            switch (type)
            {
                case 0x02://list (with caps, 3.0.8 and up)
                {
                    int i;
                    while ((i = reader.ReadUInt16BE()) > 0)
                    {
                        if (i > 0 && i <= Skill.Count)
                        {
                            Skill skill = UOSObjects.Player.Skills[i - 1];

                            if (skill == null)
                                continue;

                            skill.FixedValue = reader.ReadUInt16BE();
                            skill.FixedBase = reader.ReadUInt16BE();
                            skill.Lock = (LockType)reader.ReadUInt8();
                            skill.FixedCap = reader.ReadUInt16BE();
                            if (!UOSObjects.Player.SkillsSent)
                                skill.Delta = 0;
                        }
                        else
                        {
                            reader.Seek(reader.Position + 7);
                        }
                    }

                    UOSObjects.Player.SkillsSent = true;
                    break;
                }

                case 0x00: // list (without caps, older clients)
                {
                    int i;
                    while ((i = reader.ReadUInt16BE()) > 0)
                    {
                        if (i > 0 && i <= Skill.Count)
                        {
                            Skill skill = UOSObjects.Player.Skills[i - 1];

                            if (skill == null)
                                continue;

                            skill.FixedValue = reader.ReadUInt16BE();
                            skill.FixedBase = reader.ReadUInt16BE();
                            skill.Lock = (LockType)reader.ReadUInt8();
                            skill.FixedCap = 100;//p.ReadUShort();
                            if (!UOSObjects.Player.SkillsSent)
                                skill.Delta = 0;
                        }
                        else
                        {
                            reader.Seek(reader.Position + 5);
                        }
                    }

                    UOSObjects.Player.SkillsSent = true;
                    break;
                }

                case 0xDF: //change (with cap, new clients)
                {
                    int i = reader.ReadUInt16BE();

                    if (i >= 0 && i < Skill.Count)
                    {
                        Skill skill = UOSObjects.Player.Skills[i];

                        if (skill == null)
                            break;

                        ushort old = skill.FixedBase;
                        skill.FixedValue = reader.ReadUInt16BE();
                        skill.FixedBase = reader.ReadUInt16BE();
                        skill.Lock = (LockType)reader.ReadUInt8();
                        skill.FixedCap = reader.ReadUInt16BE();
                    }
                    break;
                }

                case 0xFF: //change (without cap, older clients)
                {
                    int i = reader.ReadUInt16BE();

                    if (i >= 0 && i < Skill.Count)
                    {
                        Skill skill = UOSObjects.Player.Skills[i];

                        if (skill == null)
                            break;

                        ushort old = skill.FixedBase;
                        skill.FixedValue = reader.ReadUInt16BE();
                        skill.FixedBase = reader.ReadUInt16BE();
                        skill.Lock = (LockType)reader.ReadUInt8();
                        skill.FixedCap = 100;
                    }
                    break;
                }
            }
        }

        private static void LoginConfirm(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOSObjects.Items.Clear();
            UOSObjects.Mobiles.Clear();

            UseNewStatus = false;

            uint serial = reader.ReadUInt32BE();

            PlayerData m = new PlayerData(serial)
            {
                Name = UOSObjects.OrigPlayerName
            };

            UOMobile test = UOSObjects.FindMobile(serial);
            if (test != null)
                test.Remove();

            UOSObjects.AddMobile(UOSObjects.Player = m);

            reader.Skip(4);//reader.ReadUInt32BE(); // always 0?
            m.Body = reader.ReadUInt16BE();
            m.Position = new Point3D(reader.ReadUInt16BE(), reader.ReadUInt16BE(), (short)reader.ReadUInt16BE());
            m.Direction = (AssistDirection)reader.ReadUInt8();

            if (UOSObjects.Player != null)
                UOSObjects.Player.SetSeason();
        }

        private static void MobileMoving(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            uint serial = rw.ReadUInt32BE();
            UOMobile m = UOSObjects.FindMobile(serial);

            if(m == null)
            {
                UOSObjects.AddMobile(m = new UOMobile(serial));
                UOSObjects.RequestMobileStatus(m);
            }

            if (m != null)
            {
                m.Body = rw.ReadUInt16BE();
                m.Position = new Point3D(rw.ReadUInt16BE(), rw.ReadUInt16BE(), rw.ReadInt8());

                if (UOSObjects.Player != null && !Utility.InRange(UOSObjects.Player.Position, m.Position, UOSObjects.Player.VisRange))
                {
                    m.Remove();
                    return;
                }

                Targeting.CheckLastTargetRange(m);

                m.Direction = (AssistDirection)rw.ReadUInt8();
                m.Hue = rw.ReadUInt16BE();
                int ltHue = UOSObjects.Gump.HLTargetHue;
                if (ltHue != 0 && Targeting.IsLastTarget(m))
                {
                    rw.Seek(rw.Position - 2);
                    rw.WriteUInt16BE((ushort)(ltHue | 0x8000));
                }

                bool wasPoisoned = m.Poisoned;
                m.ProcessPacketFlags(rw.ReadUInt8());
                byte oldNoto = m.Notoriety;
                m.Notoriety = rw.ReadUInt8();
            }
        }

        private static readonly int[] HealthHues = new int[] { 428, 333, 37, 44, 49, 53, 158, 263, 368, 473, 578 };

        private static void HitsUpdate(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
            if (m != null)
            {
                m.HitsMax = reader.ReadUInt16BE();
                m.Hits = reader.ReadUInt16BE();
            }
        }

        private static void StamUpdate(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
            if (m != null)
            {
                m.StamMax = reader.ReadUInt16BE();
                m.Stam = reader.ReadUInt16BE();
            }
        }

        private static void ManaUpdate(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
            if (m != null)
            {
                m.ManaMax = reader.ReadUInt16BE();
                m.Mana = reader.ReadUInt16BE();
            }
        }

        private static void MobileStatInfo(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
            if (m == null)
                return;

            m.HitsMax = reader.ReadUInt16BE();
            m.Hits = reader.ReadUInt16BE();

            m.ManaMax = reader.ReadUInt16BE();
            m.Mana = reader.ReadUInt16BE();

            m.StamMax = reader.ReadUInt16BE();
            m.Stam = reader.ReadUInt16BE();
        }

        internal static bool UseNewStatus = false;

        private static void NewMobileStatus(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());

            if (m == null)
                return;

            UseNewStatus = true;

            // 00 01
            reader.ReadUInt16BE();

            // 00 01 Poison
            // 00 02 Yellow Health Bar

            ushort id = reader.ReadUInt16BE();

            // 00 Off
            // 01 On
            // For Poison: Poison Level + 1

            byte flag = reader.ReadUInt8();

            if (id == 1)
            {
                bool wasPoisoned = m.Poisoned;
                m.Poisoned = (flag != 0);
            }
        }

        private static void Damage(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            ushort damage = reader.ReadUInt16BE();
        }

        private static void MobileStatus(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            UOMobile m = UOSObjects.FindMobile(serial);
            if (m == null)
                UOSObjects.AddMobile(m = new UOMobile(serial));

            m.Name = reader.ReadASCII(30);

            m.Hits = reader.ReadUInt16BE();
            m.HitsMax = reader.ReadUInt16BE();

            if (reader.ReadBool())
                m.CanRename = true;

            byte type = reader.ReadUInt8();

            if (m == UOSObjects.Player && type != 0x00)
            {
                PlayerData player = (PlayerData)m;

                player.Female = reader.ReadBool();

                int oStr = player.Str, oDex = player.Dex, oInt = player.Int;

                player.Str = reader.ReadUInt16BE();
                player.Dex = reader.ReadUInt16BE();
                player.Int = reader.ReadUInt16BE();

                player.Stam = reader.ReadUInt16BE();
                player.StamMax = reader.ReadUInt16BE();
                player.Mana = reader.ReadUInt16BE();
                player.ManaMax = reader.ReadUInt16BE();

                player.Gold = reader.ReadUInt32BE();
                player.AR = reader.ReadUInt16BE(); // ar / physical resist
                player.Weight = reader.ReadUInt16BE();

                if (type >= 0x03)
                {
                    if (type > 0x04)
                    {
                        player.MaxWeight = reader.ReadUInt16BE();

                        reader.ReadUInt8(); // race?
                    }

                    player.StatCap = reader.ReadUInt16BE();

                    player.Followers = reader.ReadUInt8();
                    player.FollowersMax = reader.ReadUInt8();

                    if (type > 0x03)
                    {
                        player.FireResistance = (short)reader.ReadUInt16BE();
                        player.ColdResistance = (short)reader.ReadUInt16BE();
                        player.PoisonResistance = (short)reader.ReadUInt16BE();
                        player.EnergyResistance = (short)reader.ReadUInt16BE();

                        player.Luck = (short)reader.ReadUInt16BE();

                        player.DamageMin = reader.ReadUInt16BE();
                        player.DamageMin = reader.ReadUInt16BE();
                        player.DamageMax = reader.ReadUInt16BE();

                        player.Tithe = (int)reader.ReadUInt32BE();
                    }
                }
            }
        }

        private static void MobileUpdate(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            uint serial = rw.ReadUInt32BE();
            UOMobile m = UOSObjects.FindMobile(serial);
            if (m == null)
                UOSObjects.AddMobile(m = new UOMobile(serial));

            bool wasHidden = !m.Visible;
            ushort body = rw.ReadUInt16BE();
            sbyte body_inc = rw.ReadInt8();
            m.Body = (ushort)(body + body_inc);
            m.Hue = rw.ReadUInt16BE();
            int ltHue = UOSObjects.Gump.HLTargetHue;
            if (ltHue != 0 && Targeting.IsLastTarget(m))
            {
                rw.Seek(rw.Position - 2);
                rw.WriteUInt16BE((ushort)(ltHue | 0x8000));
            }

            bool wasPoisoned = m.Poisoned;
            byte flags = rw.ReadUInt8();
            m.ProcessPacketFlags(flags);

            ushort x = rw.ReadUInt16BE();
            ushort y = rw.ReadUInt16BE();
            ushort serverid = rw.ReadUInt16BE(); //always 0?
            m.Direction = (AssistDirection)rw.ReadUInt8();
            m.Position = new Point3D(x, y, rw.ReadInt8());

            if (m == UOSObjects.Player)
            {
                if (!wasHidden && !m.Visible)
                {
                    if (UOSObjects.Gump.CountStealthSteps)
                        StealthSteps.Hide();
                }
                else if (wasHidden && m.Visible)
                {
                    StealthSteps.Unhide();
                }
            }

            UOItem.UpdateContainers();
        }

        private static void MobileIncoming(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            uint serial = rw.ReadUInt32BE();
            ushort body = rw.ReadUInt16BE();

            Point3D position = new Point3D(rw.ReadUInt16BE(), rw.ReadUInt16BE(), rw.ReadInt8());

            UOMobile m = UOSObjects.FindMobile(serial);
            if (m == null)
                UOSObjects.AddMobile(m = new UOMobile(serial));

            bool wasHidden = !m.Visible;
            
            if (UOSObjects.Gump.ShowMobileFlags)
                Targeting.CheckTextFlags(m);

            int ltHue = UOSObjects.Gump.HLTargetHue;
            bool isLT;
            if (ltHue != 0)
                isLT = Targeting.IsLastTarget(m);
            else
                isLT = false;

            m.Body = body;
            if (m != UOSObjects.Player)
                m.Position = position;
            m.Direction = (AssistDirection)rw.ReadUInt8();
            m.Hue = rw.ReadUInt16BE();
            if (isLT)
            {
                rw.Seek(rw.Position - 2);
                rw.WriteUInt16BE((ushort)(ltHue | 0x8000));
            }

            bool wasPoisoned = m.Poisoned;
            m.ProcessPacketFlags(rw.ReadUInt8());
            byte oldNoto = m.Notoriety;
            m.Notoriety = rw.ReadUInt8();

            if (m == UOSObjects.Player)
            {
                if (!wasHidden && !m.Visible)
                {
                    if (UOSObjects.Gump.CountStealthSteps)
                        StealthSteps.Hide();
                }
                else if (wasHidden && m.Visible)
                {
                    StealthSteps.Unhide();
                }
            }

            while (true)
            {
                serial = rw.ReadUInt32BE();
                if (!SerialHelper.IsItem(serial))
                    break;

                UOItem item = UOSObjects.FindItem(serial);
                bool isNew = false;
                if (item == null)
                {
                    isNew = true;
                    UOSObjects.AddItem(item = new UOItem(serial));
                }
                if (!DragDropManager.EndHolding(serial))
                    continue;

                item.Container = m;

                ushort id = rw.ReadUInt16BE();

                if (Engine.UseNewMobileIncoming)
                    item.ItemID = (ushort)(id & 0xFFFF);
                else if (Engine.UsePostSAChanges)
                    item.ItemID = (ushort)(id & 0x7FFF);
                else
                    item.ItemID = (ushort)(id & 0x3FFF);

                item.Layer = (Layer)rw.ReadUInt8();

                if (Engine.UseNewMobileIncoming)
                {
                    item.Hue = rw.ReadUInt16BE();
                    if (isLT)
                    {
                        rw.Seek(rw.Position - 2);
                        rw.WriteUInt16BE((ushort)(ltHue & 0x3FFF));
                    }
                }
                else
                {
                    if ((id & 0x8000) != 0)
                    {
                        item.Hue = rw.ReadUInt16BE();
                        if (isLT)
                        {
                            rw.Seek(rw.Position - 2);
                            rw.WriteUInt16BE((ushort)(ltHue & 0x3FFF));
                        }
                    }
                    else
                    {
                        item.Hue = 0;
                        if (isLT)
                            ClientPackets.PRecv_EquipmentItem(item, (ushort)(ltHue & 0x3FFF), m.Serial);
                    }
                }

                if (item.Layer == Layer.Backpack && isNew && UOSObjects.Gump.AutoSearchContainers && m == UOSObjects.Player && m != null)
                {
                    IgnoreGumps.Add(serial);
                    PlayerData.DoubleClick(serial);
                }
            }

            UOItem.UpdateContainers();
        }

        private static void RemoveObject(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();

            if (SerialHelper.IsMobile(serial))
            {
                UOMobile m = UOSObjects.FindMobile(serial);
                if (m != null && m != UOSObjects.Player)
                    m.Remove();
            }
            else if (SerialHelper.IsItem(serial))
            {
                UOItem i = UOSObjects.FindItem(serial);
                if (i != null)
                {
                    if (DragDropManager.Holding == i)
                    {
                        i.Container = null;
                    }
                    else  
                        i.RemoveRequest();
                }
            }
        }

        private static void ServerChange(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player != null)
                UOSObjects.Player.Position = new Point3D(reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt16BE());
        }

        private static void WorldItem(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOItem item;
            uint serial = reader.ReadUInt32BE();
            item = UOSObjects.FindItem(serial & 0x7FFFFFFF);
            bool isNew = false;
            if (item == null)
            {
                UOSObjects.AddItem(item = new UOItem(serial & 0x7FFFFFFF));
                isNew = true;
            }
            else
            {
                item.CancelRemove();
            }
            if (!DragDropManager.EndHolding(serial))
                return;

            item.Container = null;

            ushort itemID = reader.ReadUInt16BE();
            item.ItemID = (ushort)(itemID & 0x7FFF);

            if ((serial & 0x80000000) != 0)
                item.Amount = reader.ReadUInt16BE();
            else
                item.Amount = 1;

            if ((itemID & 0x8000) != 0)
                item.ItemID = (ushort)(item.ItemID + reader.ReadInt8());

            ushort x = reader.ReadUInt16BE();
            ushort y = reader.ReadUInt16BE();

            if ((x & 0x8000) != 0)
                item.Direction = reader.ReadUInt8();
            else
                item.Direction = 0;

            short z = reader.ReadInt8();

            item.Position = new Point3D(x & 0x7FFF, y & 0x3FFF, z);

            if ((y & 0x8000) != 0)
            {
                item.Hue = reader.ReadUInt16BE();
            }
            else
                item.Hue = 0;

            byte flags = 0;
            if ((y & 0x4000) != 0)
                flags = reader.ReadUInt8();

            item.ProcessPacketFlags(flags);

            if (isNew && UOSObjects.Player != null)
            {
                if (item.ItemID == 0x2006)// corpse itemid = 0x2006
                {
                    if (UOSObjects.Gump.ShowCorpseNames)
                    {
                        NetClient.Socket.PSend_SingleClick(item.Serial);//Engine.Instance.SendToServer(new SingleClick(item));
                    }
                    //not necessary, already present in MobileUO/ClassicUO GUI
                    /*if (UOSObjects.Gump.OpenCorpses && Utility.InRange(item.Position, UOSObjects.Player.Position, UOSObjects.Gump.OpenCorpsesRange) && UOSObjects.Player != null && UOSObjects.Player.Visible)
                    {
                        PlayerData.DoubleClick(item.Serial);
                    }*/
                }
                else if (!item.IsMulti)
                {
                    int dist = Utility.Distance(item.GetWorldPosition(), UOSObjects.Player.Position);
                    if (!UOSObjects.Player.IsGhost && UOSObjects.Player.Visible && dist <= 2 && Scavenger.Enabled && item.Movable)
                        Scavenger.Scavenge(item);
                }
            }

            UOItem.UpdateContainers();
        }

        private static void SAWorldItem(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            /*
            New World UOItem Packet
            PacketID: 0xF3
            PacketLen: 24
            Format:

                 BYTE - 0xF3 packetId
                 WORD - 0x01
                 BYTE - ArtDataID: 0x00 if the item uses art from TileData table, 0x02 if the item uses art from MultiData table)
                 DWORD - item Serial
                 WORD - item ID
                 BYTE - item direction (same as old)
                 WORD - amount
                 WORD - amount
                 WORD - X
                 WORD - Y
                 SBYTE - Z
                 BYTE - item light
                 WORD - item Hue
                 BYTE - item flags (same as old packet)
            */

            // Post-7.0.9.0
            /*
            New World UOItem Packet
            PacketID: 0xF3
            PacketLen: 26
            Format:

                 BYTE - 0xF3 packetId
                 WORD - 0x01
                 BYTE - ArtDataID: 0x00 if the item uses art from TileData table, 0x02 if the item uses art from MultiData table)
                 DWORD - item Serial
                 WORD - item ID
                 BYTE - item direction (same as old)
                 WORD - amount
                 WORD - amount
                 WORD - X
                 WORD - Y
                 SBYTE - Z
                 BYTE - item light
                 WORD - item Hue
                 BYTE - item flags (same as old packet)
                 WORD ???
            */

            ushort _unk1 = reader.ReadUInt16BE();

            byte _artDataID = reader.ReadUInt8();

            UOItem item;
            uint serial = reader.ReadUInt32BE();
            item = UOSObjects.FindItem(serial);
            bool isNew = false;
            if (item == null)
            {
                UOSObjects.AddItem(item = new UOItem(serial));
                isNew = true;
            }
            else
            {
                item.CancelRemove();
            }
            if (!DragDropManager.EndHolding(serial))
                return;

            item.Container = null;

            ushort itemID = reader.ReadUInt16BE();
            item.ItemID = (ushort)(_artDataID == 0x02 ? itemID | 0x4000 : itemID);

            item.Direction = reader.ReadUInt8();

            ushort _amount = reader.ReadUInt16BE();
            item.Amount = _amount = reader.ReadUInt16BE();

            ushort x = reader.ReadUInt16BE();
            ushort y = reader.ReadUInt16BE();
            short z = reader.ReadInt8();

            item.Position = new Point3D(x, y, z);

            byte _light = reader.ReadUInt8();

            item.Hue = reader.ReadUInt16BE();

            byte flags = reader.ReadUInt8();

            item.ProcessPacketFlags(flags);

            if (Engine.UsePostHSChanges)
            {
                reader.ReadUInt16BE();
            }

            if (isNew && UOSObjects.Player != null)
            {
                if (item.ItemID == 0x2006)// corpse itemid = 0x2006
                {
                    if (UOSObjects.Gump.ShowCorpseNames)
                    {
                        NetClient.Socket.PSend_SingleClick(item.Serial);
                    }
                    //Already present in CUO/MUO, we don't really need it
                    /*if (UOSObjects.Gump.OpenCorpses && Utility.InRange(item.Position, UOSObjects.Player.Position, UOSObjects.Gump.OpenCorpsesRange) && UOSObjects.Player != null && UOSObjects.Player.Visible)
                        PlayerData.DoubleClick(item.Serial);*/
                }
                else if (!item.IsMulti)
                {
                    int dist = Utility.Distance(item.GetWorldPosition(), UOSObjects.Player.Position);
                    if (!UOSObjects.Player.IsGhost && UOSObjects.Player.Visible && dist <= 2 && Scavenger.Enabled && item.Movable)
                        Scavenger.Scavenge(item);
                }
            }

            UOItem.UpdateContainers();
        }

        internal static void HandleSpeech(uint ser, ushort body, MessageType type, ushort hue, ushort font, string lang, string name, string text, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            if (SerialHelper.IsMobile(ser) && type == MessageType.Label)
            {
                UOMobile m = UOSObjects.FindMobile(ser);
                if (m != null && m.Name.IndexOf(text) != 5 && m != UOSObjects.Player && !(text.StartsWith("(") && text.EndsWith(")")))
                    m.Name = text;
            }
            else
            {
                if (ser == uint.MaxValue && name == "System")
                {
                    if (text.StartsWith("You've committed a criminal act") || text.StartsWith("You are now a criminal"))
                    {
                        UOSObjects.Player.ResetCriminalTimer();
                    }
                }

                if (SerialHelper.IsValid(ser))
                {
                    if(type == MessageType.Emote || type == MessageType.Regular || type == MessageType.Whisper || type == MessageType.Yell)
                    {
                        Journal.AddLine($"{name}: {text}", type);
                    }
                    else if (type == MessageType.Guild || type == MessageType.Alliance)
                    {
                        Targeting.CheckSharedTarget(ser, text, args);
                    }
                }
                else if (!SerialHelper.IsValid(ser))
                {
                    Journal.AddLine(text, MessageType.System);
                }
            }
        }

        internal static void AsciiSpeech(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            // 0, 1, 2
            uint serial = reader.ReadUInt32BE(); // 3, 4, 5, 6
            ushort body = reader.ReadUInt16BE(); // 7, 8
            MessageType type = (MessageType)reader.ReadUInt8(); // 9
            ushort hue = reader.ReadUInt16BE(); // 10, 11
            ushort font = reader.ReadUInt16BE();
            string name = reader.ReadASCII(30);
            string text = reader.ReadASCII();

            if (UOSObjects.Player != null && serial == 0 && body == 0 && type == MessageType.Regular && hue == 0xFFFF && font == 0xFFFF && name == "SYSTEM")
            {
                return;
            }
            HandleSpeech(serial, body, type, hue, font, "A", name, text, args);

            if (!SerialHelper.IsValid(serial))
            {
                BandageTimer.OnAsciiMessage(text);
            }
            GateTimer.OnAsciiMessage(text);
        }

        internal static void UnicodeSpeech(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            // 0, 1, 2
            uint serial = reader.ReadUInt32BE(); // 3, 4, 5, 6
            ushort body = reader.ReadUInt16BE(); // 7, 8
            MessageType type = (MessageType)reader.ReadUInt8(); // 9
            ushort hue = reader.ReadUInt16BE(); // 10, 11
            ushort font = reader.ReadUInt16BE();
            string lang = reader.ReadASCII(4);
            string name = reader.ReadASCII(30);
            string text = reader.ReadUnicodeBE();

            HandleSpeech(serial, body, type, hue, font, lang, name, text, args);
            if (!SerialHelper.IsValid(serial))
            {
                BandageTimer.OnAsciiMessage(text);
            }
        }

        private static void OnLocalizedMessage(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            // 0, 1, 2
            uint serial = reader.ReadUInt32BE(); // 3, 4, 5, 6
            ushort body = reader.ReadUInt16BE(); // 7, 8
            MessageType type = (MessageType)reader.ReadUInt8(); // 9
            ushort hue = reader.ReadUInt16BE(); // 10, 11
            ushort font = reader.ReadUInt16BE();
            int num = (int)reader.ReadUInt32BE();
            string name = reader.ReadASCII(30);
            string ext_str = reader.ReadUnicodeLE();

            if ((num >= 3002011 && num < 3002011 + 64) || // reg spells
                 (num >= 1060509 && num < 1060509 + 16) || // necro
                 (num >= 1060585 && num < 1060585 + 10) || // chiv
                 (num >= 1060493 && num < 1060493 + 10) || // chiv
                 (num >= 1060595 && num < 1060595 + 6) || // bush
                 (num >= 1060610 && num < 1060610 + 8)) // ninj
            {
                type = MessageType.Spell;
            }
            BandageTimer.OnLocalizedMessage(num);
 
            string text = ClassicUO.Client.Game.UO.FileManager.Clilocs.Translate(num, ext_str);
            if (text == null)
                return;
            HandleSpeech(serial, body, type, hue, font, Settings.GlobalSettings.Language, name, text, args);
        }

        private static void OnLocalizedMessageAffix(ref StackDataReader reader, PacketHandlerEventArgs phea)
        {
            // 0, 1, 2
            uint serial = reader.ReadUInt32BE(); // 3, 4, 5, 6
            ushort body = reader.ReadUInt16BE(); // 7, 8
            MessageType type = (MessageType)reader.ReadUInt8(); // 9
            ushort hue = reader.ReadUInt16BE(); // 10, 11
            ushort font = reader.ReadUInt16BE();
            int num = (int)reader.ReadUInt32BE();
            byte affixType = reader.ReadUInt8();
            string name = reader.ReadASCII(30);
            string affix = reader.ReadASCII();
            string args = reader.ReadUnicodeBE();

            if ((num >= 3002011 && num < 3002011 + 64) || // reg spells
                 (num >= 1060509 && num < 1060509 + 16) || // necro
                 (num >= 1060585 && num < 1060585 + 10) || // chiv
                 (num >= 1060493 && num < 1060493 + 10) || // chiv
                 (num >= 1060595 && num < 1060595 + 6) || // bush
                 (num >= 1060610 && num < 1060610 + 8)     // ninj
                 )
            {
                type = MessageType.Spell;
            }

            string text;
            if ((affixType & 1) != 0) // prepend
                text = string.Format("{0}{1}", affix, ClassicUO.Client.Game.UO.FileManager.Clilocs.Translate(num, args));
            else // 0 == append, 2 = system
                text = string.Format("{0}{1}", ClassicUO.Client.Game.UO.FileManager.Clilocs.Translate(num, args), affix);
            HandleSpeech(serial, body, type, hue, font, Settings.GlobalSettings.Language, name, text, phea);
        }

        private static void ClientGumpResponse(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            uint ser = reader.ReadUInt32BE();
            uint typeid = reader.ReadUInt32BE();

            if(UOSObjects.Player.OpenedGumps.TryGetValue(typeid, out var list))
                list.Remove(list.First(g => g.ServerID == ser));

            if (!ScriptManager.Recording)
                return;

            int bid = reader.ReadInt32BE();
            int sc = reader.ReadInt32BE();
            if (sc < 0 || sc > 2000)
                return;
            //int[] switches = new int[sc];
            for (int i = 0; i < sc; i++, AssistantGump._InstanceSB.Append(' '))
                AssistantGump._InstanceSB.Append(reader.ReadUInt32BE());

            int ec = reader.ReadInt32BE();
            if (ec < 0 || ec > 2000)
                return;
            if (ec > 0)
                AssistantGump._InstanceSB.Append(' ');
            for (int x = 0; x < ec; ++x, AssistantGump._InstanceSB.Append(' '))
            {
                ushort id = reader.ReadUInt16BE();
                ushort len = Math.Min((ushort)239, reader.ReadUInt16BE());
                string text = reader.ReadUnicodeBE(len);
                AssistantGump._InstanceSB.Append($"\"{id} {text}\"");
            }
            ScriptManager.AddToScript($"waitforgump 0x{typeid:X} 15000");
            ScriptManager.AddToScript($"replygump 0x{typeid:X} {bid} {AssistantGump._InstanceSB}");
            AssistantGump._InstanceSB.Clear();
        }

        private static void ChangeSeason(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player != null)
            {
                byte season = reader.ReadUInt8();
                UOSObjects.Player.SetSeason(season);
            }

        }

        private static void ExtendedPacket(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            ushort type = reader.ReadUInt16BE();

            switch (type)
            {
                case 0x04: // close gump
                {
                    uint ser = reader.ReadUInt32BE();
                    // int serial, int tid
                    UOSObjects.Player.OpenedGumps.Remove(ser);
                    break;
                }
                case 0x06: // party messages
                {
                    OnPartyMessage(ref reader, args);
                    break;
                }
                case 0x08: // map change
                {
                    if (UOSObjects.Player != null)
                    {
                        UOSObjects.Player.Map = reader.ReadUInt8();
                        UOSObjects.Player.MapIndex = reader.ReadUInt8();
                    }
                    break;
                }
                case 0x14: // context menu
                {
                    reader.ReadUInt32BE(); // 0x01
                    UOEntity ent = null;
                    uint ser = reader.ReadUInt32BE();
                    if (SerialHelper.IsMobile(ser))
                        ent = UOSObjects.FindMobile(ser);
                    else if (SerialHelper.IsItem(ser))
                        ent = UOSObjects.FindItem(ser);

                    if (ent != null)
                    {
                        byte count = reader.ReadUInt8();

                        try
                        {
                            ent.ContextMenu.Clear();

                            for (int i = 0; i < count; i++)
                            {
                                ushort idx = reader.ReadUInt16BE();
                                ushort num = reader.ReadUInt16BE();
                                ushort flags = reader.ReadUInt16BE();
                                ushort color = 0;

                                if ((flags & 0x02) != 0)
                                    color = reader.ReadUInt16BE();

                                ent.ContextMenu.Add(idx, num);
                            }
                        }
                        catch
                        {
                        }
                    }
                    break;
                }
                case 0x19: //  stat locks
                {
                    if (reader.ReadUInt8() == 0x02)
                    {
                        UOMobile m = UOSObjects.FindMobile(reader.ReadUInt32BE());
                        if (UOSObjects.Player == m && m != null)
                        {
                            reader.ReadUInt8();// 0?

                            byte locks = reader.ReadUInt8();

                            UOSObjects.Player.StrLock = (LockType)((locks >> 4) & 3);
                            UOSObjects.Player.DexLock = (LockType)((locks >> 2) & 3);
                            UOSObjects.Player.IntLock = (LockType)(locks & 3);
                        }
                    }
                    break;
                }
            }
        }

        internal static int SpecialPartySent = 0;
        internal static int SpecialPartyReceived = 0;
        internal static int SpecialGuildSent = 0;
        internal static int SpecialGuildReceived = 0;
 
        private static void RunUOProtocolExtention(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            switch (reader.ReadUInt8())
            {
                case 1: // Custom Party information
                {
                    uint serial;

                    SpecialPartyReceived++;

                    while ((serial = reader.ReadUInt32BE()) > 0)
                    {
                        if (!Party.Contains(serial))
                            break;
                        UOMobile mobile = UOSObjects.FindMobile(serial);

                        short x = reader.ReadInt16BE();
                        short y = reader.ReadInt16BE();
                        byte map = reader.ReadUInt8();

                        if (mobile == null)
                        {
                            UOSObjects.AddMobile(mobile = new UOMobile(serial));
                            mobile.Visible = false;
                        }

                        if (mobile.Name == null || mobile.Name.Length <= 0)
                            mobile.Name = "(Not Seen)";

                        if (map == UOSObjects.Player.Map)
                            mobile.Position = new Point3D(x, y, mobile.Position.Z);
                        else
                            mobile.Position = Point3D.Zero;
                    }
                    break;
                }
                case 2: // Guild track information...
                {
                    bool locations = reader.ReadUInt8() != 0;
                    uint serial;
                    SpecialGuildReceived++;
                    if (!locations)
                    {
                        Faction.Clear();
                    }

                    while ((serial = reader.ReadUInt32BE()) > 0)
                    {
                        UOMobile mobile = UOSObjects.FindMobile(serial);
                        if (mobile == null)
                        {
                            UOSObjects.AddMobile(mobile = new UOMobile(serial));
                            mobile.Visible = false;
                        }
                        if (!locations || !Faction.Contains(serial) && (!UOSObjects.Gump.FriendsParty || !PacketHandlers.Party.Contains(serial)))
                        {
                            Faction.Add(serial);
                        }

                        if (locations)
                        {
                            short x = reader.ReadInt16BE();
                            short y = reader.ReadInt16BE();
                            byte map = reader.ReadUInt8();
                            byte hits = reader.ReadUInt8();
                            if (map == UOSObjects.Player.Map)
                            {
                                mobile.Position = new Point3D(x, y, mobile.Position.Z);
                            }
                            else
                            {
                                mobile.Position = Point3D.Zero;
                            }
                        }
                        if (string.IsNullOrEmpty(mobile.Name))
                        {
                            mobile.Name = "(Not Seen)";
                        }
                    }
                    break;
                }
                case 0xFE: // Begin Handshake/Features Negotiation
                {
                    ulong features = reader.ReadUInt64BE();
                    Engine.Instance.SetFeatures(features);
                    break;
                }
            }
        }

        internal static List<uint> Party { get; } = new List<uint>();
        internal static HashSet<uint> Faction { get; } = new HashSet<uint>();
        private static Timer _PartyDeclineTimer = null;
        internal static uint PartyLeader = 0;

        private static void OnPartyMessage(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            switch (reader.ReadUInt8())
            {
                case 0x01: // List
                {
                    Party.Clear();

                    int count = reader.ReadUInt8();
                    for (int i = 0; i < count; i++)
                    {
                        uint s = reader.ReadUInt32BE();
                        if (UOSObjects.Player == null || s != UOSObjects.Player.Serial)
                            Party.Add(s);
                    }

                    break;
                }
                case 0x02: // Remove Member/Re-list
                {
                    Party.Clear();
                    int count = reader.ReadUInt8();
                    uint remSerial = reader.ReadUInt32BE(); // the serial of who was removed

                    if (UOSObjects.Player != null)
                    {
                        UOMobile rem = UOSObjects.FindMobile(remSerial);
                        if (rem != null && !Utility.InRange(UOSObjects.Player.Position, rem.Position, UOSObjects.Player.VisRange))
                            rem.Remove();
                    }

                    for (int i = 0; i < count; i++)
                    {
                        uint s = reader.ReadUInt32BE();
                        if (UOSObjects.Player == null || s != UOSObjects.Player.Serial)
                            Party.Add(s);
                    }

                    break;
                }
                case 0x03: // text message

                case 0x04: // 3 = private, 4 = public
                {
                    uint from = reader.ReadUInt32BE();
                    string text = reader.ReadUnicodeBE();
                    Targeting.CheckSharedTarget(from, text, args);
                    break;
                }
                case 0x07: // party invite
                {
                    PartyLeader = reader.ReadUInt32BE();

                    //in original UOS we can't auto-accept party
                    if (UOSObjects.Gump.AutoAcceptParty)
                    {
                        UOMobile leaderMobile = UOSObjects.FindMobile(PartyLeader);
                        if (leaderMobile != null && FriendsManager.IsFriend(leaderMobile.Serial))
                        {
                            if (PartyLeader != 0)
                            {
                                UOSObjects.Player.SendMessage($"Auto accepted party invite from: {leaderMobile.Name}");

                                NetClient.Socket.PSend_PartyAccept(PacketHandlers.PartyLeader);
                                PartyLeader = 0;
                            }
                        }
                    }
                    else
                    {
                        if (_PartyDeclineTimer == null)
                            _PartyDeclineTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(10.0), new TimerCallback(PartyAutoDecline));
                        _PartyDeclineTimer.Start();
                    }

                    break;
                }
            }
        }

        private static void PartyAutoDecline()
        {
            PartyLeader = 0;
        }

        private static void PingResponse(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (Ping.Response(reader.ReadUInt8()))
                args.Block = true;
        }

        private static void ClientEncodedPacket(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            reader.Skip(4);
            ushort packetID = reader.ReadUInt16BE();
            switch (packetID)
            {
                case 0x19: // set ability
                {
                    uint ability = 0;
                    if (reader.ReadUInt8() == 0)
                        ability = reader.ReadUInt32BE();

                    if (ability >= 0 && ability < (int)Ability.Invalid)
                        ScriptManager.AddToScript($"setability '{ability}'");
                    break;
                }
            }
        }

        private static void MenuResponse(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            reader.Skip(6);
            //uint serial = reader.ReadUInt32BE();
            //ushort menuID = reader.ReadUInt16BE();
            ushort index = reader.ReadUInt16BE();
            ushort itemID = reader.ReadUInt16BE();
            ushort hue = reader.ReadUInt16BE();

            UOSObjects.Player.HasMenu = false;
            if (ScriptManager.Recording)
                ScriptManager.AddToScript($"menuresponse {index} {itemID} {hue}");
        }

        private static void SendMenu(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            UOSObjects.Player.CurrentMenuS = reader.ReadUInt32BE();
            UOSObjects.Player.CurrentMenuI = reader.ReadUInt16BE();
            UOSObjects.Player.HasMenu = true;
            if (ScriptManager.Recording)
            {
                ScriptManager.AddToScript($"replymenu {UOSObjects.Player.CurrentMenuI}");
                args.Block = true;
            }
        }

        private static void HueResponse(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            ushort iid = reader.ReadUInt16BE();
            ushort hue = reader.ReadUInt16BE();

            if (serial == uint.MaxValue)
            {
                //TODO: HueEntry - Callback <- REALLY NEEDED?!
            }
        }

        private static void HuePicker(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (Scripts.Commands.ColorPick >= 0)
            {
                uint serial = reader.ReadUInt32BE();
                reader.Skip(2);//the hue actually setted up in the internal color chooser
                ushort iid = reader.ReadUInt16BE();
                NetClient.Socket.PSend_ColorPickResponse(serial, iid, (ushort)Scripts.Commands.ColorPick);
                Scripts.Commands.ColorPick = -1;
                args.Block = true;
            }
        }

        private static void Features(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player != null)
                UOSObjects.Player.Features = reader.ReadUInt16BE();
        }

        private static void PersonalLight(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
        }

        private static void GlobalLight(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
        }

        private static bool EnforceLightLevels(int lightLevel)
        {
            return false;
        }

        private static void ServerSetWarMode(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            UOSObjects.Player.Warmode = reader.ReadBool();
        }

        private static void CustomHouseInfo(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
        }

        /*
        Packet Build
        1.  BYTE[1] Cmd
        2.  BYTE[2] len
        3.  BYTE[4] Player Serial
        4.  BYTE[4] Gump ID
        5.  BYTE[4] x
        6.  BYTE[4] y
        7.  BYTE[4] Compressed Gump Layout Length (CLen)
        8.  BYTE[4] Decompressed Gump Layout Length (DLen)
        9.  BYTE[CLen-4] Gump Data, zlib compressed
        10. BYTE[4] Number of text lines
        11. BYTE[4] Compressed Text Line Length (CTxtLen)
        12. BYTE[4] Decompressed Text Line Length (DTxtLen)
        13. BYTE[CTxtLen-4] Gump's Compressed Text data, zlib compressed
         */
        private static void CompressedGump(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            List<string> gumpStrings = new List<string>();
            uint progserial = reader.ReadUInt32BE(), typeidserial = reader.ReadUInt32BE();
            uint x = reader.ReadUInt32BE();
            uint y = reader.ReadUInt32BE();
            int clen = reader.ReadInt32BE() - 4;
            int dlen = (int)reader.ReadUInt32BE();

            byte[] decData = new byte[dlen];
            var buffer = reader.Buffer.ToArray();
            string layout;
            unsafe
            {
                fixed (byte* srcPtr = &buffer[reader.Position], destPtr = decData)
                {
                    ZLib.Decompress((IntPtr)srcPtr, clen, 0, (IntPtr)destPtr, dlen);
                    layout = Encoding.UTF8.GetString(destPtr, dlen);
                }
            }
            reader.Skip((int)clen);
            // Split on one or more non-digit characters.
            string[] numbers = Regex.Split(layout, @"\D+");

            foreach (string value in numbers)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (int.TryParse(value, out int i) && ((i >= 500000 && i <= 600000) || (i >= 1000000 && i <= 1200000) || (i >= 3000000 && i <= 3100000)))
                        gumpStrings.Add(ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(i));
                }
            }
            int linesNum = reader.ReadInt32BE();
            if (linesNum < 0 || linesNum > 256)
                linesNum = 0;
            if (linesNum > 0)
            {
                clen = reader.ReadInt32BE() - 4;
                dlen = reader.ReadInt32BE();
                decData = new byte[dlen];

                unsafe
                {
                    fixed (byte* srcPtr = &buffer[reader.Position], destPtr = decData)
                        ZLib.Decompress((IntPtr)srcPtr, clen, 0, (IntPtr)destPtr, dlen);
                }

                reader.Skip(clen);

                for (int i = 0, index = 0; i < linesNum; i++)
                {
                    int length = ((decData[index++] << 8) | decData[index++]) << 1;

                    int true_length = 0;

                    while (true_length < length)
                    {
                        if (((decData[index + true_length++] << 8) | decData[index + true_length++]) << 1 == '\0')
                            break;
                    }

                    gumpStrings.Add(Encoding.BigEndianUnicode.GetString(decData, index, true_length));
                    index += length;
                }
            }
            TryParseGump(layout, gumpStrings);
            AddObservedGump(new PlayerData.GumpData(typeidserial, progserial, gumpStrings));
        }

        private static void UncompressedGump(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            List<string> gumpStrings = new List<string>();
            uint progserial = reader.ReadUInt32BE(), typeidserial = reader.ReadUInt32BE();
            int x = (int)reader.ReadUInt32BE();
            int y = (int)reader.ReadUInt32BE();
            ushort cmdLen = reader.ReadUInt16BE();
            StringBuilder sb = new StringBuilder(cmdLen);
            for (int i = 0; i < cmdLen; ++i)
            {
                sb.Append((char)reader.ReadUInt8());
            }
            string cmd = sb.ToString();
            ushort textLinesCount = reader.ReadUInt16BE();
            if (textLinesCount < 0 || textLinesCount > 256)
                textLinesCount = 0;
            // Split on one or more non-digit characters.
            string[] numbers = Regex.Split(cmd, @"\D+");
            foreach (string value in numbers)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (int.TryParse(value, out int i) && ((i >= 500000 && i <= 600000) || (i >= 1000000 && i <= 1200000) || (i >= 3000000 && i <= 3100000)))
                        gumpStrings.Add(ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(i));
                }
            }

            var buffer = reader.Buffer.Slice(reader.Position, reader.Remaining).ToArray();
            for (int i = 0, index = 0; i < textLinesCount; i++)
            {
                int length = ((buffer[index++] << 8) | buffer[index++]) << 1;
                int true_length = 0;

                while (true_length < length)
                {
                    if (((buffer[index + true_length++] << 8) | buffer[index + true_length++]) << 1 == '\0')
                        break;
                }

                gumpStrings.Add(Encoding.BigEndianUnicode.GetString(buffer, index, true_length));
                index += length;
            }
            TryParseGump(cmd, gumpStrings);
            AddObservedGump(new PlayerData.GumpData(typeidserial, progserial, gumpStrings));
        }

        private static void AddObservedGump(PlayerData.GumpData data)
        {
            if (!UOSObjects.Player.OpenedGumps.TryGetValue(data.GumpID, out List<PlayerData.GumpData> glist))
                glist = UOSObjects.Player.OpenedGumps[data.GumpID] = new List<PlayerData.GumpData>();
            glist.Add(data);
        }

        private static void TryParseGump(string gumpData, List<string> pieces)
        {
            int dataIndex = 0;
            while (dataIndex < gumpData.Length)
            {
                if (gumpData.Substring(dataIndex) == "\0")
                {
                    break;
                }
                else
                {
                    int begin = gumpData.IndexOf("{", dataIndex);
                    int end = gumpData.IndexOf("}", dataIndex + 1);
                    if ((begin != -1) && (end != -1))
                    {
                        string sub = gumpData.Substring(begin + 1, end - begin - 1).Trim();
                        pieces.Add(sub);
                        dataIndex = end;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static List<string> ParseGumpString(string[] gumpPieces, string[] gumpLines)
        {
            List<string> gumpText = new List<string>();
            for (int i = 0; i < gumpPieces.Length; i++)
            {
                string[] gumpParams = Regex.Split(gumpPieces[i], @"\s+");
                switch (gumpParams[0].ToLower())
                {

                    case "croppedtext":
                        gumpText.Add(gumpLines[int.Parse(gumpParams[6])]);
                        // CroppedText [x] [y] [width] [height] [color] [text-id]
                        // Adds a text field to the gump. gump is similar to the text command, but the text is cropped to the defined area.
                        break;

                    case "htmlgump":
                        gumpText.Add(gumpLines[int.Parse(gumpParams[5])]);
                        // HtmlGump [x] [y] [width] [height] [text-id] [background] [scrollbar]
                        // Defines a text-area where Html-commands are allowed.
                        // [background] and [scrollbar] can be 0 or 1 and define whether the background is transparent and a scrollbar is displayed.
                        break;

                    case "text":
                        gumpText.Add(gumpLines[int.Parse(gumpParams[4])]);
                        // Text [x] [y] [color] [text-id]
                        // Defines the position and color of a text (data) entry.
                        break;
                }
            }

            return gumpText;
        }

        private static void ResurrectionGump(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
        }

        private static void BuffDebuff(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint ser = reader.ReadUInt32BE();
            ushort icon = reader.ReadUInt16BE();
            ushort action = reader.ReadUInt16BE();

            if (Enum.IsDefined(typeof(BuffIcon), icon))
            {
                BuffIcon buff = (BuffIcon)icon;
                switch (action)
                {
                    case 0x01: // show

                        reader.ReadUInt32BE(); //0x000
                        reader.ReadUInt16BE(); //icon # again..?
                        reader.ReadUInt16BE(); //0x1 = show
                        reader.ReadUInt32BE(); //0x000
                        ushort duration = reader.ReadUInt16BE();
                        reader.ReadUInt16BE(); //0x0000
                        reader.ReadUInt8(); //0x0

                        BuffsDebuffs buffInfo = new BuffsDebuffs
                        {
                            IconNumber = icon,
                            BuffIcon = (BuffIcon)icon,
                            ClilocMessage1 = ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString((int)reader.ReadUInt32BE()),
                            ClilocMessage2 = ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString((int)reader.ReadUInt32BE()),
                            Duration = duration,
                            Timestamp = DateTime.UtcNow
                        };

                        if (UOSObjects.Player != null && UOSObjects.Player.BuffsDebuffs.All(b => b.BuffIcon != buff))
                        {
                            UOSObjects.Player.BuffsDebuffs.Add(buffInfo);
                        }

                        break;

                    case 0x0: // remove
                        if (UOSObjects.Player != null)
                        {
                            UOSObjects.Player.BuffsDebuffs.RemoveAll(b => b.BuffIcon == buff);
                        }

                        break;
                }
            }
        }

        private static void AttackRequest(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            uint serial = reader.ReadUInt32BE();
            if (UOSObjects.Gump.PreventAttackFriends)
            {
                if(FriendsManager.IsFriend(serial))
                {
                    args.Block = true;
                    return;
                }
            }
            if (UOSObjects.Gump.ShowMobileFlags)
            {
                UOMobile m = UOSObjects.FindMobile(serial);

                if (m == null) return;

                UOSObjects.Player.OverheadMessage(FriendsManager.IsFriend(serial) ? 63 : m.GetNotorietyColorInt(), $"Attack: {m.Name}");
            }
        }

        private static void UnicodePromptSend(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            uint serial = reader.ReadUInt32BE();
            uint id = reader.ReadUInt32BE();
            uint type = reader.ReadUInt32BE();

            string lang = reader.ReadASCII(4);
            string text = reader.ReadUnicodeLE();

            UOSObjects.Player.HasPrompt = false;
            UOSObjects.Player.PromptSenderSerial = serial;
            UOSObjects.Player.PromptID = id;
            UOSObjects.Player.PromptType = type;
            UOSObjects.Player.PromptInputText = text;

            if (ScriptManager.Recording && !string.IsNullOrEmpty(UOSObjects.Player.PromptInputText))
                ScriptManager.AddToScript($"promptresponse \"{UOSObjects.Player.PromptInputText}\"");
        }

        private static void UnicodePromptReceived(ref StackDataReader reader, PacketHandlerEventArgs args)
        {
            if (UOSObjects.Player == null)
                return;

            uint serial = reader.ReadUInt32BE();
            uint id = reader.ReadUInt32BE();
            uint type = reader.ReadUInt32BE();

            UOSObjects.Player.HasPrompt = true;
            UOSObjects.Player.PromptSenderSerial = serial;
            UOSObjects.Player.PromptID = id;
            UOSObjects.Player.PromptType = type;
            if (ScriptManager.Recording)
                ScriptManager.AddToScript($"waitforprompt {id}");
        }
    }
}
