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

using ClassicUO;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistant
{
    internal delegate void DropDoneCallback(uint iser, uint dser, Point3D newPos);

    internal class DragDropManager
    {
        public enum ProcStatus
        {
            Nothing,
            Success,
            KeepWaiting,
            ReQueue
        }

        public enum ActionType : byte
        {
            None     = 0x00,
            Dressing = 0x01,
            Organize = 0x02,
            Forced   = 0x04
        }

        private class LiftReq
        {
            private static int NextID = 1;

            public LiftReq(uint s, int a, bool cli, bool last, ActionType lifttype)
            {
                Serial = s;
                Amount = a;
                FromClient = cli;
                DoLast = last;
                Id = NextID++;
                LiftType = lifttype;
            }

            public readonly uint Serial;
            public readonly int Amount;
            public readonly int Id;
            public readonly bool FromClient;
            public readonly bool DoLast;
            public readonly ActionType LiftType;

            public override string ToString()
            {
                return $"{Id}({Serial},{Amount},{FromClient},{DoLast})";
            }
        }

        private class DropReq
        {
            public DropReq(uint s, Point3D pt, ActionType droptype)
            {
                Serial = s;
                Point = pt;
                DropType = droptype;
            }

            public DropReq(uint s, Layer layer, ActionType droptype)
            {
                Serial = s;
                Layer = layer;
                DropType = droptype;
            }

            public uint Serial;
            public readonly Point3D Point;
            public readonly Layer Layer;
            public readonly ActionType DropType;
        }

        internal static void DropCurrent()
        {
            if (SerialHelper.IsItem(_Holding))
            {
                if (UOSObjects.Player.Backpack != null)
                    NetClient.Socket.PSend_DropRequest(_Holding, Point3D.MinusOne, UOSObjects.Player.Backpack.Serial);
                else
                    NetClient.Socket.PSend_DropRequest(_Holding, UOSObjects.Player.Position, 0);
            }
            else
            {
                UOSObjects.Player.SendMessage(MsgLevel.Force, "You are not holding anything");
            }

            Clear();
        }

        private static int _LastID;

        private static uint _Pending, _Holding;
        private static bool _ClientLiftReq = false;
        private static DateTime _Lifted = DateTime.MinValue;

        private static readonly Dictionary<uint, Queue<DropReq>>
            _DropReqs = new Dictionary<uint, Queue<DropReq>>();

        private static readonly LiftReq[] _LiftReqs = new LiftReq[256];
        private static byte _Front, _Back;

        public static UOItem Holding 
        {
            get;
            private set;
        }

        public static uint Pending
        {
            get { return _Pending; }
        }

        public static int LastIDLifted
        {
            get { return _LastID; }
        }

        public static void Clear()
        {
            _DropReqs.Clear();
            for (int i = 0; i < 256; i++)
                _LiftReqs[i] = null;
            _Front = _Back = 0;
            _Holding = _Pending = 0;
            Holding = null;
            _Lifted = DateTime.MinValue;
        }

        public static void DragDrop(UOItem i, uint to)
        {
            DragDrop(i, to, Point3D.MinusOne, i.Amount);
        }

        public static void DragDrop(UOItem i, uint to, Point3D p, int amount)
        {
            Drag(i, amount);
            Drop(i, to, p);
        }

        public static void DragDrop(UOItem i, UOItem to, ActionType actionType = ActionType.None)
        {
            DragDrop(i, to, Point3D.MinusOne, i.Amount, actionType);
        }

        public static void DragDrop(UOItem i, UOItem to, Point3D p, int amount, ActionType actionType = ActionType.None)
        {
            Drag(i, amount, actionType: actionType);
            Drop(i, to, p, actionType);
        }

        public static void DragDrop(UOItem i, Point3D dest)
        {
            DragDrop(i, dest, i.Amount);
        }

        public static void DragDrop(UOItem i, Point3D dest, int amount)
        {
            Drag(i, amount);
            Drop(i, uint.MaxValue, dest);
        }

        public static void DragDrop(UOItem i, UOMobile to, Layer layer, bool doLast = false, ActionType actionType = ActionType.None)
        {
            Drag(i, i.Amount, false, doLast, actionType);
            Drop(i, to, layer, actionType);
        }

        public static bool Empty
        {
            get { return _Back == _Front; }
        }

        public static bool Full
        {
            get { return ((byte)(_Back + 1)) == _Front; }
        }

        public static bool IsDressing()
        {
            return _LiftReqs.Any(lr => lr != null && lr.LiftType == ActionType.Dressing) || _DropReqs.Any(drd => drd.Value.Any(dr => dr.DropType == ActionType.Dressing));
        }

        public static bool IsOrganizing()
        {
            return _LiftReqs.Any(lr => lr != null && lr.LiftType == ActionType.Organize) || _DropReqs.Values.Any(drq => drq.Any(dr => dr.DropType == ActionType.Organize));
        }

        public static int Drag(UOItem i, int amount, bool fromClient = false, bool doLast = false, ActionType actionType = ActionType.None)
        {
            LiftReq lr = new LiftReq(i.Serial, amount, fromClient, doLast, actionType);
            LiftReq prev = null;

            if (Full)
            {
                if (fromClient)
                    ClientPackets.PRecv_LiftRej();
                else
                    UOSObjects.Player.SendMessage(MsgLevel.Error, "Drag drop queue is FULL! Please wait");
                return 0;
            }

            if (_Back >= _LiftReqs.Length)
                _Back = 0;

            if (_Back <= 0)
                prev = _LiftReqs[_LiftReqs.Length - 1];
            else if (_Back <= _LiftReqs.Length)
                prev = _LiftReqs[_Back - 1];

            // if the current last req must stay last, then insert this one in its place
            if (prev != null && prev.DoLast)
            {
                //Log("Back-Queuing {0}", prev);
                if (_Back <= 0)
                    _LiftReqs[_LiftReqs.Length - 1] = lr;
                else if (_Back <= _LiftReqs.Length)
                    _LiftReqs[_Back - 1] = lr;

                // and then re-insert it at the end
                lr = prev;
            }

            _LiftReqs[_Back++] = lr;

            ActionQueue.SignalLift(!fromClient);
            return lr.Id;
        }

        public static bool Drop(UOItem i, UOMobile to, Layer layer, ActionType actionType = ActionType.None)
        {
            if (_Pending == i.Serial)
            {
                NetClient.Socket.PSend_EquipRequest(i.Serial, to.Serial, layer);
                _Pending = 0;
                EndHolding(i.Serial);
                _Lifted = DateTime.MinValue;
                return true;
            }
            else
            {
                bool add = false;

                for (byte j = _Front; j != _Back && !add; j++)
                {
                    if (_LiftReqs[j] != null && _LiftReqs[j].Serial == i.Serial)
                    {
                        add = true;
                        break;
                    }
                }

                if (add)
                {
                    if (!_DropReqs.TryGetValue(i.Serial, out var q) || q == null)
                        _DropReqs[i.Serial] = q = new Queue<DropReq>();

                    q.Enqueue(new DropReq(to == null ? 0 : to.Serial, layer, actionType));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool Drop(UOItem i, uint dest, Point3D pt, ActionType actionType = ActionType.None)
        {
            if (_Pending == i.Serial)
            {
                NetClient.Socket.PSend_DropRequest(i.Serial, pt, dest);
                _Pending = 0;
                EndHolding(i.Serial);
                _Lifted = DateTime.MinValue;
                return true;
            }
            else
            {
                bool add = false;

                for (byte j = _Front; j != _Back && !add; j++)
                {
                    if (_LiftReqs[j] != null && _LiftReqs[j].Serial == i.Serial)
                    {
                        add = true;
                        break;
                    }
                }

                if (add)
                {
                    if (!_DropReqs.TryGetValue(i.Serial, out var q) || q == null)
                        _DropReqs[i.Serial] = q = new Queue<DropReq>();

                    q.Enqueue(new DropReq(dest, pt, actionType));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool Drop(UOItem i, UOItem to, Point3D pt, ActionType actionType = ActionType.None)
        {
            return Drop(i, to == null ? uint.MaxValue : to.Serial, pt, actionType);
        }

        public static bool Drop(UOItem i, UOItem to)
        {
            return Drop(i, to.Serial, Point3D.MinusOne);
        }

        public static bool LiftReject()
        {
            if (_Holding == 0)
                return true;

            _Holding = _Pending = 0;
            Holding = null;
            _Lifted = DateTime.MinValue;

            return _ClientLiftReq;
        }

        public static bool HasDragFor(uint s)
        {
            for (byte j = _Front; j != _Back; j++)
            {
                if (_LiftReqs[j] != null && _LiftReqs[j].Serial == s)
                    return true;
            }

            return false;
        }

        public static bool CancelDragFor(uint s)
        {
            if (Empty)
                return false;

            int skip = 0;
            for (byte j = _Front; j != _Back; j++)
            {
                if (skip == 0 && _LiftReqs[j] != null && _LiftReqs[j].Serial == s)
                {
                    _LiftReqs[j] = null;
                    skip++;
                    if (j == _Front)
                    {
                        _Front++;
                        break;
                    }
                    else
                    {
                        _Back--;
                    }
                }

                if (skip > 0)
                    _LiftReqs[j] = _LiftReqs[(byte)(j + skip)];
            }

            if (skip > 0)
            {
                _LiftReqs[_Back] = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool EndHolding(uint s)
        {
            if (_Holding == s)
            {
                _Holding = 0;
                Holding = null;
            }

            return true;
        }

        private static DropReq DequeueDropFor(uint s)
        {
            DropReq dr = null;
            if (_DropReqs.TryGetValue(s, out var q) && q != null)
            {
                if (q.Count > 0)
                    dr = q.Dequeue();
                if (q.Count <= 0)
                    _DropReqs.Remove(s);
            }

            return dr;
        }

        public static void GracefulStop()
        {
            _Front = _Back = 0;

            if (SerialHelper.IsValid(_Pending))
            {
                _DropReqs.TryGetValue(_Pending, out var q);
                _DropReqs.Clear();
                _DropReqs[_Pending] = q;
            }
        }

        public static ProcStatus ProcessNext(int numPending)
        {
            if (_Pending != 0)
            {
                if (_Lifted + TimeSpan.FromMinutes(2) < DateTime.UtcNow)
                {
                    if (Client.Game.UO.World.Player != null)
                    {
                        UOSObjects.Player.SendMessage(MsgLevel.Force, "WARNING: Drag/Drop timeout! Dropping item in hand to backpack");

                        if (UOSObjects.Player.Backpack != null)
                        {
                            NetClient.Socket.PSend_DropRequest(_Pending, Point3D.MinusOne, UOSObjects.Player.Backpack.Serial);
                        }
                        else
                        {
                            NetClient.Socket.PSend_DropRequest(_Pending, UOSObjects.Player.Position, 0);
                        }
                    }

                    _Holding = _Pending = 0;
                    Holding = null;
                    _Lifted = DateTime.MinValue;
                }
                else
                {
                    return ProcStatus.KeepWaiting;
                }
            }

            if (_Front == _Back)
            {
                _Front = _Back = 0;
                return ProcStatus.Nothing;
            }

            LiftReq lr = _LiftReqs[_Front];

            if (numPending > 0 && lr != null && lr.DoLast)
                return ProcStatus.ReQueue;

            _LiftReqs[_Front] = null;
            _Front++;
            if (lr != null)
            {
                UOItem item = UOSObjects.FindItem(lr.Serial);
                if (item != null && item.Container == null)
                {
                    // if the item is on the ground and out of range then dont grab it
                    if (Utility.Distance(item.GetWorldPosition(), UOSObjects.Player.Position) > 3)
                    {
                        Scavenger.Uncache(item.Serial);
                        return ProcStatus.Nothing;
                    }
                }

                NetClient.Socket.PSend_LiftRequest(lr.Serial, lr.Amount);

                _LastID = lr.Id;
                _Holding = lr.Serial;
                Holding = UOSObjects.FindItem(lr.Serial);
                _ClientLiftReq = lr.FromClient;

                DropReq dr = DequeueDropFor(lr.Serial);
                if (dr != null)
                {
                    _Pending = 0;
                    EndHolding(lr.Serial);
                    _Lifted = DateTime.MinValue;

                    if (SerialHelper.IsMobile(dr.Serial) && dr.Layer > Layer.Invalid && dr.Layer < Layer.Mount)
                    {
                        NetClient.Socket.PSend_EquipRequest(lr.Serial, dr.Serial, dr.Layer);
                    }
                    else
                    {
                        NetClient.Socket.PSend_DropRequest(lr.Serial, dr.Point, dr.Serial);
                    }
                }
                else
                {
                    _Pending = lr.Serial;
                    _Lifted = DateTime.UtcNow;
                }

                return ProcStatus.Success;
            }
            else
            {
                return ProcStatus.Nothing;
            }
        }
    }

    public class ActionQueue
    {
        private static uint _Last = 0;
        private static readonly Queue<uint> _Queue = new Queue<uint>();
        private static readonly ProcTimer _Timer = new ProcTimer();
        private static int _Total = 0;

        public static void DoubleClick(uint s, bool silent = true)
        {
            if (s != 0)
            {
                if (_Last != s)
                {
                    _Queue.Enqueue(s);
                    _Last = s;
                    _Total++;
                    if (!silent && _Total > 1)
                        UOSObjects.Player.SendMessage($"Queuing action request {_Queue.Count}... {TimeLeft} left.");
                }
                else if (!silent)
                {
                    UOSObjects.Player.SendMessage("Ignoring action request (already queued)");
                }
            }
        }

        public static void SignalLift(bool silent)
        {
            _Queue.Enqueue(0);
            _Total++;

            if (!silent && _Total > 1)
                UOSObjects.Player.SendMessage($"Queuing dragdrop request {_Queue.Count}... {TimeLeft} left.");
        }

        public static void Stop()
        {
            if (_Timer != null && _Timer.Running)
                _Timer.Stop();
            _Queue.Clear();
            DragDropManager.Clear();
        }

        public static void ClearActions()
        {
            _Queue?.Clear();
        }

        public static int QueuedActions => _Queue.Count;

        public static bool Empty
        {
            get { return _Queue.Count <= 0 && !_Timer.Running; }
        }

        public static string TimeLeft
        {
            get
            {
                if (_Timer.Running)
                {
                    double time = UOSObjects.Gump.ActionDelay / 1000.0;

                    if (!UOSObjects.Gump.UseObjectsQueue)
                    {
                        time = 0;
                    }

                    double init = 0;
                    if (_Timer.LastTick != DateTime.MinValue)
                        init = time - (DateTime.UtcNow - _Timer.LastTick).TotalSeconds;
                    time = init + time * _Queue.Count;
                    if (time < 0)
                        time = 0;
                    return string.Format("{0:F1} seconds", time);
                }
                else
                {
                    return "0.0 seconds";
                }
            }
        }

        internal static void StartTimer()
        {
            if(_Timer.Running)
            {
                Stop();
            }
            _Timer.StartMe();
        }

        private class ProcTimer : Timer
        {
            private DateTime _StartTime;
            private DateTime _LastTick;
            private bool _SendMSG = false;
            private uint _TimeDragDrop;

            public DateTime LastTick
            {
                get { return _LastTick; }
            }

            public ProcTimer() : base(TimeSpan.Zero, TimeSpan.FromMilliseconds(UOSObjects.Gump.ActionDelay))
            {
            }

            internal void StartMe()
            {
                _LastTick = DateTime.UtcNow;
                _StartTime = DateTime.UtcNow;

                Start();
            }

            protected override void OnTick()
            {
                _LastTick = DateTime.UtcNow;

                if (_Queue != null && _Queue.Count > 0)
                {
                    if (!_SendMSG)
                    {
                        _StartTime = DateTime.UtcNow;
                        _SendMSG = true;
                    }

                    Interval = TimeSpan.FromMilliseconds(UOSObjects.Gump.ActionDelay);//action such as dclick must be done as commanded and as fast as possible

                    while (_Queue.Count > 0)
                    {
                        uint s = _Queue.Peek();
                        if (s == 0) // dragdrop action
                        {
                            if(_TimeDragDrop + ScriptManager.ActionDelayDragDrop > Time.Ticks)
                            {
                                break;
                            }
                            DragDropManager.ProcStatus status = DragDropManager.ProcessNext(_Queue.Count - 1);
                            if (status != DragDropManager.ProcStatus.KeepWaiting)
                            {
                                _Queue.Dequeue(); // if not waiting then dequeue it

                                if (status == DragDropManager.ProcStatus.ReQueue)
                                    _Queue.Enqueue(s);
                            }

                            if (status == DragDropManager.ProcStatus.KeepWaiting || status == DragDropManager.ProcStatus.Success)
                            {
                                if(status == DragDropManager.ProcStatus.Success)
                                {
                                    _TimeDragDrop = Time.Ticks;//write doing the successful action to postpone the next dragdrop
                                }
                                break; // don't process more if we're waiting or we just processed something
                            }
                        }
                        else
                        {
                            _Queue.Dequeue();
                            NetClient.Socket.PSend_DoubleClick(s);
                            break;
                        }
                    }
                }
                else
                {
                    if (_SendMSG && _Total > 1 && UOSObjects.Player != null)
                    {
                        UOSObjects.Player.SendMessage($"Finished {_Total} queued actions in {(((DateTime.UtcNow - _StartTime) - this.Interval).TotalSeconds)} seconds.");
                        _SendMSG = false;
                    }

                    _Last = 0;
                    _Total = 0;
                }
            }
        }
    }
}
