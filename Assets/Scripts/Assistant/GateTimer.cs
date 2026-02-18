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
using System.Linq;

using ClassicUO.Assets;

namespace Assistant
{
    public class GateTimer
    {
        private static int _Count;
        private static Timer _Timer;

        private static readonly int[] _ClilocsStop = { 502632 };

        private static readonly int[] _ClilocsRestart = { 501024 };

        static GateTimer()
        {
            _Timer = new InternalTimer();
        }

        public static int Count
        {
            get { return _Count; }
        }

        public static void OnAsciiMessage(string msg)
        {
            if (Running)
            {
                if (_ClilocsStop.Any(t => ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(t) == msg))
                {
                    Stop();
                }

                if (_ClilocsRestart.Any(t => ClassicUO.Client.Game.UO.FileManager.Clilocs.GetString(t) == msg))
                {
                    Start();
                }
            }
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
                if (_Count > 30)
                {
                    Stop();
                }
            }
        }
    }
}
