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
// See <https://www.gnu.org> for details.
#endregion

namespace Assistant
{
    public class StealthSteps
    {
        private static int _Count;
        private static bool _Hidden = false;

        public static int Count
        {
            get { return _Count; }
        }

        public static bool Counting
        {
            get { return _Hidden; }
        }

        public static bool Hidden
        {
            get { return _Hidden; }
        }

        public static void OnMove()
        {
            if (_Hidden && _Count < 30 && UOSObjects.Player != null && UOSObjects.Gump.CountStealthSteps)
            {
                _Count++;
                UOSObjects.Player.SendMessage(MsgLevel.Error, $"Stealth steps: {_Count}");
            }
        }

        public static void Hide()
        {
            _Hidden = true;
            _Count = 0;
        }

        public static void Unhide()
        {
            _Hidden = false;
            _Count = 0;
        }
    }
}
