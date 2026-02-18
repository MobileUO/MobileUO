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

using System;

namespace Assistant
{
    public class SkillTimer
    {
        private static int _Count;
        private static Timer _Timer;

        static SkillTimer()
        {
            _Timer = new InternalTimer();
        }

        public static int Count
        {
            get { return _Count; }
        }

        public static bool Running
        {
            get { return _Timer.Running; }
        }

        public static void Start()
        {
            _Count = 0;

            if (_Timer.Running)
            {
                _Timer.Stop();
            }

            _Timer.Start();
        }

        public static void Stop()
        {
            _Timer.Stop();
        }

        private class InternalTimer : Timer
        {
            public InternalTimer() : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
            }

            protected override void OnTick()
            {
                _Count++;
                if (_Count > 10)
                {
                    Stop();
                }
            }
        }
    }
}
