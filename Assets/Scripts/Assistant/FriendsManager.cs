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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

using ClassicUO.Game;
using ClassicUO.Network;

namespace Assistant
{
    internal static class FriendsManager
    {
        internal static Dictionary<uint, string> FriendDictionary { get; } = new Dictionary<uint, string>();
        internal static bool IsFriend(uint serial)
        {
            if (!SerialHelper.IsMobile(serial))
                return false;

            if (FriendDictionary.ContainsKey(serial))
                return true;

            // Check if they have treat party as friends enabled and check the party if so
            if (!UOSObjects.Gump.FriendsListOnly && ((UOSObjects.Gump.FriendsParty && PacketHandlers.Party.Contains(serial)) || PacketHandlers.Faction.Contains(serial) || UOScript.Interpreter.GetAlias("friend") == serial))
                return true;

            return false;
        }

        public static void ClearAll()
        {
            FriendDictionary.Clear();
        }
    }
}
