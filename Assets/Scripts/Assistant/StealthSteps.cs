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
namespace Assistant
{
    public class StealthSteps
    {
        private static int m_Count;
        private static bool m_Hidden = false;

        public static int Count
        {
            get { return m_Count; }
        }

        public static bool Counting
        {
            get { return m_Hidden; }
        }

        public static bool Hidden
        {
            get { return m_Hidden; }
        }

        public static void OnMove()
        {
            if (m_Hidden && m_Count < 30 && UOSObjects.Player != null && UOSObjects.Gump.CountStealthSteps)
            {
                m_Count++;
                UOSObjects.Player.SendMessage(MsgLevel.Error, $"Stealth steps: {m_Count}");
            }
        }

        public static void Hide()
        {
            m_Hidden = true;
            m_Count = 0;
        }

        public static void Unhide()
        {
            m_Hidden = false;
            m_Count = 0;
        }
    }
}
