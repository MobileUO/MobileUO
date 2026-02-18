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
using ClassicUO.Network;

namespace Assistant
{
    public class Ping
    {
        private static DateTime _Start;
        private static byte _Seq;
        private static double _Time, _Min, _Max;
        private static int _Total;
        private static int _Count;

        public static bool Response(byte seq)
        {
            if (seq == _Seq && _Start != DateTime.MinValue)
            {
                double ms = (DateTime.UtcNow - _Start).TotalMilliseconds;

                if (ms < _Min)
                    _Min = ms;
                if (ms > _Max)
                    _Max = ms;

                if (_Count-- > 0)
                {
                    _Time += ms;
                    UOSObjects.Player.SendMessage(MsgLevel.Force, $"Response: {ms:F1}ms");
                    DoPing();
                }
                else
                {
                    _Start = DateTime.MinValue;
                    UOSObjects.Player.SendMessage(MsgLevel.Force, "Ping Result:");
                    UOSObjects.Player.SendMessage(MsgLevel.Force, "Min: {0:F1}ms  Max: {1:F1}ms  Avg: {2:F1}ms", _Min, _Max, _Time / ((double)_Total));
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void StartPing(int count)
        {
            if (count <= 0 || count > 20)
                _Count = 5;
            else
                _Count = count;

            _Total = _Count;
            _Time = 0;
            _Min = double.MaxValue;
            _Max = 0;

            UOSObjects.Player.SendMessage(MsgLevel.Force, "Pinging server with {0} packets ({1} bytes)...", _Count, _Count * 2);
            DoPing();
        }

        private static void DoPing()
        {
            _Seq = (byte)Utility.Random(256);
            _Start = DateTime.UtcNow;
            NetClient.Socket.PSend_PingPacket(_Seq);
        }
    }
}
