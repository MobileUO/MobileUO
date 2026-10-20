#region license
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

namespace Assistant.Scripts
{
    public static class Aliases
    {
        public static void Register()
        {
            Interpreter.RegisterAliasHandler("backpack", Backpack);
            Interpreter.RegisterAliasHandler("last", Last);
            Interpreter.RegisterAliasHandler("lasttarget", Last);
            Interpreter.RegisterAliasHandler("lastobject", LastObject);
            Interpreter.RegisterAliasHandler("self", Self);
            Interpreter.RegisterAliasHandler("mount", Mounted);
            Interpreter.RegisterAliasHandler("righthand", RHandEmpty);
            Interpreter.RegisterAliasHandler("lefthand", LHandEmpty);
        }

        private static uint Mounted(string alias)
        {
            return UOSObjects.Player != null && UOSObjects.Player.GetItemOnLayer(Layer.Mount) != null
                ? (uint) 1
                : (uint) 0;
        }

        private static uint RHandEmpty(string alias)
        {
            return UOSObjects.Player != null && UOSObjects.Player.GetItemOnLayer(Layer.RightHand) != null
                ? (uint) 1
                : (uint) 0;
        }

        private static uint LHandEmpty(string alias)
        {
            return UOSObjects.Player != null && UOSObjects.Player.GetItemOnLayer(Layer.LeftHand) != null
                ? (uint) 1
                : (uint) 0;
        }

        private static uint Backpack(string alias)
        {
            if (UOSObjects.Player == null || UOSObjects.Player.Backpack == null)
                return 0;

            return UOSObjects.Player.Backpack.Serial;
        }

        private static uint Last(string alias)
        {
            if (!Targeting.DoLastTarget())
                Targeting.ResendTarget();

            return 0;
        }

        private static uint LastObject(string alias)
        {
            if (SerialHelper.IsValid(UOSObjects.Player.LastObject))
                return UOSObjects.Player.LastObject;

            return 0;
        }

        private static uint Self(string alias)
        {
            if (UOSObjects.Player == null)
                return 0;

            return UOSObjects.Player.Serial;
        }
    }
}