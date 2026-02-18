﻿#region License
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

using UOScript;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using System.Linq;
using System.Collections.Generic;

namespace Assistant.Scripts
{
    public static class Aliases
    {
        public static void Register()
        {
            Interpreter.RegisterAliasHandler("backpack", Backpack);
            Interpreter.RegisterAliasHandler("bank", Bank);
            
            Interpreter.RegisterAliasHandler("last", LastTarget);
            Interpreter.RegisterAliasHandler("lasttarget", LastTarget);
            Interpreter.RegisterAliasHandler("lastobject", LastObject);
            Interpreter.RegisterAliasHandler("self", Self);
            Interpreter.RegisterAliasHandler("righthand", RightHand);
            Interpreter.RegisterAliasHandler("lefthand", LeftHand);
            Interpreter.RegisterAliasHandler("lastcombatant", LastCombatant);

            Interpreter.RegisterAliasHandler("friend", Friend);
        }

        private static uint RightHand(string alias)
        {
            return UOSObjects.Player?.GetItemOnLayer(Layer.OneHanded)?.Serial ?? 0;
        }

        private static uint LeftHand(string alias)
        {
            return UOSObjects.Player?.GetItemOnLayer(Layer.TwoHanded)?.Serial ?? 0;
        }

        private static uint Bank(string alias)
        {
            return UOSObjects.Player?.GetItemOnLayer(Layer.Bank)?.Serial ?? 0;
        }

        private static uint Backpack(string alias)
        {
            return UOSObjects.Player?.Backpack?.Serial ?? 0;
        }

        private static uint LastTarget(string alias)
        {
            if (Targeting.LastTargetInfo == null || !SerialHelper.IsValid(Targeting.LastTargetInfo.Serial))
                return 0;

            return Targeting.LastTargetInfo.Serial;
        }

        private static uint LastCombatant(string alias)
        {
            if (!SerialHelper.IsValid(Targeting.LastCombatant))
                return 0;

            return Targeting.LastCombatant;
        }

        private static uint LastObject(string alias)
        {
            if (SerialHelper.IsValid(UOSObjects.Player.LastObject))
                return UOSObjects.Player.LastObject;

            return 0;
        }

        private static uint Self(string alias)
        {
            return UOSObjects.Player?.Serial ?? 0;
        }

        //if there isn't a friend explicitly set or if the friend is outside our visible range multiplied 2, it returns the friend alias as the nearest friend present in the friendsmanager list.
        private static uint Friend(string alias)
        {
            uint val = Interpreter.GetMainAlias(alias);
            if (SerialHelper.IsValid(val) && UOSObjects.FindEntity(val) is UOEntity ent && Utility.InRange(ent.WorldPosition, UOSObjects.Player.Position, UOSObjects.ClientViewRange * 2))
            {
                return val;
            }
            else if(FriendsManager.FriendDictionary.Count > 0)
            {
                double prev = double.MaxValue;
                void getval(IEnumerable<uint> list)
                {
                    foreach (uint key in list)
                    {
                        if (UOSObjects.FindEntity(key) is UOEntity entity && Utility.InRange(entity.WorldPosition, UOSObjects.Player.Position, UOSObjects.ClientViewRange))
                        {
                            if (Utility.DistanceSqrt(entity.WorldPosition, UOSObjects.Player.Position) is double d && d < prev)
                            {
                                prev = d;
                                val = key;
                            }
                        }
                    }
                }

                if(FriendsManager.FriendDictionary.Count > 0)
                {
                    getval(FriendsManager.FriendDictionary.Keys);
                }
                
                if(!UOSObjects.Gump.FriendsListOnly)
                {
                    if(UOSObjects.Gump.FriendsParty && PacketHandlers.Party.Count > 0)
                    {
                        getval(PacketHandlers.Party);
                    }
                    if(PacketHandlers.Faction.Count > 0)
                    {
                        getval(PacketHandlers.Faction);
                    }
                }
            }

            return val;
        }
    }
}