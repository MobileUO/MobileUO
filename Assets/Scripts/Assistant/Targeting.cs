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
using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Game.UI.Controls;

using Assistant.Core;
using UOScript;
using AssistGump = ClassicUO.Game.UI.Gumps.AssistantGump;

namespace Assistant
{
    internal class TargetInfo
    {
        public byte Type;
        public uint TargID;
        public byte Flags;
        public uint Serial;
        public int X, Y;
        public int Z;
        public ushort Gfx;

        public bool Equals(TargetInfo ti)
        {
            if (ti != null && Flags == ti.Flags && Type == ti.Type && Serial == ti.Serial && Gfx == ti.Gfx && X == ti.X && Y == ti.Y && Z == ti.Z)
                return true;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is TargetInfo ti)
                return Equals(ti);
            return false;
        }
    }

    public enum MobType
    {
        Any,
        Humanoid,
        Monster
    }

    internal class Targeting
    {
        internal static void ClearAll()
        {
            _OldLT = _OldBeneficialLT = _OldHarmfulLT = 0;
        }

        public const uint LOCAL_TARG_ID = 0x7FFFFFFF; // uid for target sent from CUO

        public delegate void TargetResponseCallback(bool location, uint serial, Point3D p, ushort gfxid);

        public delegate void CancelTargetCallback();

        private static CancelTargetCallback _OnCancel;
        private static TargetResponseCallback _OnTarget;

        private static bool _Intercept;
        private static bool _HasTarget;
        private static bool _ClientTarget;
        private static TargetInfo _LastTarget;
        private static TargetInfo _LastGroundTarg;
        private static TargetInfo _LastBeneTarg;
        private static TargetInfo _LastHarmTarg;

        private static bool _FromGrabHotKey;

        private static bool _AllowGround;
        private static uint _CurrentID;
        private static byte _CurFlags;

        private static uint _PreviousID;
        private static bool _PreviousGround;
        private static byte _PrevFlags;

        private static uint _LastCombatant;
        internal static uint LastCombatant => _LastCombatant;

        internal delegate bool QueueTarget();

        private static QueueTarget TargetSelfAction = new QueueTarget(DoTargetSelf);
        private static QueueTarget LastTargetAction = new QueueTarget(DoLastTarget);
        private static QueueTarget _QueueTarget;


        private static uint _SpellTargID = 0;

        public static uint SpellTargetID
        {
            get { return _SpellTargID; }
            set { _SpellTargID = value; }
        }

        private static List<uint> _FilterCancel = new List<uint>();

        public static bool HasTarget
        {
            get 
            { 
                return _HasTarget; 
            }
        }

        public static bool ServerTarget => !_Intercept && _HasTarget;

        public static TargetInfo LastTargetInfo
        {
            get { return _LastTarget; }
        }

        public static bool FromGrabHotKey
        {
            get { return _FromGrabHotKey; }
        }

        private static List<ushort> _MonsterIds = new List<ushort>()
        {
            0x1, 0x2, 0x3, 0x4, 0x7, 0x8, 0x9, 0xC, 0xD, 0xE, 0xF,
            0x10, 0x11, 0x12, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C,
            0x1E, 0x1F, 0x21, 0x23, 0x24, 0x25, 0x27, 0x29, 0x2A, 0x2C,
            0x2D, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
            0x39, 0x3B, 0x3C, 0x3D, 0x42, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
            0x4B, 0x4F, 0x50, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x59, 0x5A,
            0x5B, 0x5C, 0x5D, 0x5E, 0x60, 0x61, 0x62, 0x69, 0x6A, 0x6B, 0x6C,
            0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x87, 0x88, 0x89,
            0x8A, 0x8B, 0x8C, 0x8E, 0x8F, 0x91, 0x93, 0x96, 0x99, 0x9B, 0x9E,
            0x9F, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xB4, 0x4C, 0x4D, 0x3D
        };

        internal enum TargetType : byte
        {
            None        = 0x00, 
            Invalid     = 0x01, // invalid/across server line
            Innocent    = 0x02, //Blue
            Friend      = 0x04, //Green,
            Gray        = 0x08, //Attackable but not criminal (gray)
            Criminal    = 0x10, //gray
            Enemy       = 0x20, //orange
            Murderer    = 0x40, //red
            Invulnerable= 0x80, //invulnerable
            Any         = 0x7E  //any without invalid and invulnerable
        }

        internal static bool ValidTarget(TargetType target, byte noto)
        {
            return (target & (TargetType)(1 << noto)) != 0;
        }

        public static void Initialize()
        {
            PacketHandler.RegisterClientToServerViewer(0x6C, TargetResponse);
            PacketHandler.RegisterServerToClientViewer(0x6C, NewTarget);
            PacketHandler.RegisterServerToClientViewer(0xAA, CombatantChange);
        }


        private static void CombatantChange(ref StackDataReader p, PacketHandlerEventArgs e)
        {
            uint ser = p.ReadUInt32BE();
            if (ser != 0 && ser != uint.MaxValue && SerialHelper.IsValid(ser) && ser != UOSObjects.Player.Serial)
                _LastCombatant = ser;
        }

        internal static void AttackTarget(uint serial)
        {
            if (SerialHelper.IsValid(serial))
            {
                NetClient.Socket.PSend_AttackRequest(serial);
                if (Engine.Instance.AllowBit(FeatureBit.EnemyTargetShare))
                {
                    UOMobile m = UOSObjects.FindMobile(serial);
                    if (m != null)
                        SendSharedTarget(m);
                }
            }
        }

        internal static void AttackLastComb()
        {
            if (SerialHelper.IsValid(_LastCombatant))
            {
                AttackTarget(_LastCombatant);
            }
        }

        internal static void OnFriendTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if (SerialHelper.IsMobile(serial) && serial != UOSObjects.Player.Serial && !FriendsManager.FriendDictionary.ContainsKey(serial))
            {
                UOMobile m = UOSObjects.FindMobile(serial);
                if (m != null)
                {
                    if (string.IsNullOrEmpty(m.Name))
                        m.Name = "(Not Seen)";
                    FriendsManager.FriendDictionary[serial] = m.Name;
                    UOSObjects.Player.SendMessage(MsgLevel.Info, $"Friend List: Adding {m}");
                    UOSObjects.Gump.UpdateFriendListGump();
                    XmlFileParser.SaveData();
                }
            }
        }

        internal static void OnRemoveFriendSelected(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if (SerialHelper.IsValid(serial))
            {
                if(FriendsManager.FriendDictionary.Remove(serial))
                    UOSObjects.Gump.UpdateFriendListGump();
            }
        }

        internal static void OnSetEnemyTarget(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if(SerialHelper.IsValid(serial))
                Interpreter.SetAlias("enemy", serial);
            else
                UOSObjects.Player.SendMessage(MsgLevel.Error, "Invalid target selected!");
        }

        internal static void OnSetFriendTarget(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if (SerialHelper.IsValid(serial))
                Interpreter.SetAlias("friend", serial);
            else
                UOSObjects.Player.SendMessage(MsgLevel.Error, "Invalid target selected!");
        }

        internal static void OnSetLastTarget(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if (SerialHelper.IsValid(serial))
            {
                SetLastTargetTo(serial);
            }
            else
                UOSObjects.Player.SendMessage(MsgLevel.Error, "Invalid target selected!");
        }

        internal static void OnSetMountTarget(bool loc, uint serial, Point3D p, ushort itemid)
        {
            if (SerialHelper.IsMobile(serial))
            {
                Interpreter.SetAlias("mount", serial);
            }
            else
                UOSObjects.Player.SendMessage(MsgLevel.Error, "Invalid target selected!");
        }

        internal static void AttackLastTarg()
        {
            TargetInfo targ;
            if (IsSmartTargetingEnabled(2))
            {
                // If Smart Targetting is being used we'll assume that the user would like to attack the harmful target.
                targ = _LastHarmTarg;

                // If there is no last harmful target, then we'll attack the last target.
                if (targ == null)
                    targ = _LastTarget;
            }
            else
            {
                targ = _LastTarget;
            }

            if (targ != null && SerialHelper.IsValid(targ.Serial))
            {
                AttackTarget(targ.Serial);
            }
        }

        public static uint RandomTarget(byte range, bool friends, bool isdead, MobType type, TargetType noto = TargetType.Any, bool noset = false)
        {
            if (!Engine.Instance.AllowBit(FeatureBit.RandomTargets))
                return 0;

            List<UOMobile> list = new List<UOMobile>();
            foreach (UOMobile m in UOSObjects.MobilesInRange(range))
            {
                if (type == MobType.Humanoid)
                {
                    if (!m.IsHuman)
                        continue;
                }
                else if (type == MobType.Monster)
                {
                    if (!m.IsMonster)
                        continue;
                }

                if (!m.Blessed && m.IsGhost == isdead && m.Serial != ClassicUO.Client.Game.UO.World.Player.Serial &&
                    Utility.InRange(UOSObjects.Player.Position, m.Position, UOSObjects.Gump.SmartTargetRangeValue))
                {
                    if (noto == TargetType.Any && !friends)
                    {
                        list.Add(m);
                    }
                    else if (friends && FriendsManager.IsFriend(m.Serial))
                    {
                        list.Add(m);
                    }
                    else if(ValidTarget(noto, m.Notoriety))
                    {
                        list.Add(m);
                    }
                }
            }

            if (list.Count > 0)
            {
                UOMobile m = list[Utility.Random(list.Count)];
                if (!noset)
                    SetLastTargetTo(m);
                return m.Serial;

            }
            else if(!noset)
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "No one matching that was found on your screen.");
            return 0;
        }

        internal static HashSet<ushort> Humanoid = new HashSet<ushort>();
        internal static HashSet<ushort> Transformation = new HashSet<ushort>();
        internal enum FilterType : byte
        {
            Next            = 0x00,//funzionamento simile a nextprev di razor
            Closest         = 0x01,//solo il più vicino
            Nearest         = 0x02,//ultimi due target
            AnyTarget       = 0x0F,
            Humanoid        = 0x10,
            Transformation  = 0x20,
            AnyForm         = 0xF0
        }
        internal static FilterType GetFilterType(UOMobile m)
        {
            FilterType f = FilterType.Next;
            if (Humanoid.Contains(m.Body))
                f |= FilterType.Humanoid;
            if (Transformation.Contains(m.Body))
                f |= FilterType.Transformation;
            return f;
        }

        internal static InternalSorter Instance;
        internal class InternalSorter : IComparer<UOEntity>
        {
            private PlayerData _From;

            public InternalSorter(PlayerData from)
            {
                _From = from;
            }

            public int Compare(UOEntity x, UOEntity y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                return _From.GetDistanceToSqrt(x).CompareTo(_From.GetDistanceToSqrt(y));
            }
        }

        internal static ContainedInternalSorter ContainedInstance;
        internal class ContainedInternalSorter : IComparer<UOEntity>
        {
            private PlayerData _From;

            public ContainedInternalSorter(PlayerData from)
            {
                _From = from;
            }

            public int Compare(UOEntity x, UOEntity y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }
                int ret = _From.GetDistanceToSqrt(x).CompareTo(_From.GetDistanceToSqrt(y));
                if(ret == 0 && _From.Backpack != null)
                {
                    //same location as player, check if parent is a container and if that container is backpack, other or self
                    //order is: backpack - other sublayers && self after
                    int xdepth = -1, ydepth = -1;
                    if(x is UOItem xitem)
                    {
                        xitem = xitem.GetRootContainerItem(out xdepth);
                        if (xitem == _From.Backpack)
                            --ret;
                    }
                    if(y is UOItem yitem)
                    {
                        yitem = yitem.GetRootContainerItem(out ydepth);
                        if (yitem == _From.Backpack)
                            ++ret;
                    }
                    if(ret == 0 && xdepth >= 0 && ydepth >= 0)
                    {
                        if(xdepth == 0 && ydepth == 0)
                        {
                            return 0;
                        }
                        else if(xdepth < ydepth)
                        {
                            return -1;
                        }
                        else if(ydepth < xdepth)
                        {
                            return 1;
                        }
                    }
                }

                return ret;
            }


        }

        internal static void GetTarget(TargetType targets, FilterType filter, bool isenemy, bool quiet, bool next = true, bool self = false)
        {
            List<UOEntity> list = new List<UOEntity>();
            if (UOSObjects.Player == null)
                return;
            byte map = UOSObjects.Player.Map;
            foreach (UOMobile m in UOSObjects.Mobiles.Values)
            {
                if (m.Map == map && Utility.InRange(UOSObjects.Player.Position, m.Position, ClassicUO.Client.Game.UO.World.ClientViewRange))
                {
                    if (ValidTarget(targets, m.Notoriety) && ((filter & FilterType.AnyForm) == 0 || (GetFilterType(m) & filter) != 0))
                    {
                        list.Add(m);
                    }
                }
            }
            if (!self)
                list.Remove(UOSObjects.Player);

            if ((filter &= FilterType.AnyTarget) != FilterType.Next && list.Count > 1)
            {
                list.Sort(Instance);
                if ((filter & FilterType.Closest) != FilterType.Next)
                    list.RemoveRange(1, list.Count - 1);
                else if (list.Count > 2)
                    list.RemoveRange(2, list.Count - 2);
            }
            UOEntity e = GetFromTargets(list, next, out _);
            if (e != null)
            {
                MsgLevel level = e is UOMobile m ? (MsgLevel)m.Notoriety : (isenemy ? MsgLevel.Debug : MsgLevel.Friend);
                if(!quiet)
                    UOSObjects.Player.OverheadMessage(PlayerData.GetColorCode(level), $"[{(isenemy ? "Enemy" : "Friend")}]: {e.GetName()}");
                Interpreter.SetAlias(isenemy ? "enemy" : "friend", e.Serial);
                if (e is UOMobile uom)
                    SetLastTargetTo(uom, (byte)(isenemy ? 1 : 2));
            }
        }

        /// <summary>
        /// Index used to keep track of the current Next/Prev target
        /// </summary>
        private static int _nextPrevTargetIndex;
        private static UOEntity GetFromTargets(List<UOEntity> targets, bool nextTarget, out TargetInfo target)
        {
            UOEntity entity = null, old = UOSObjects.FindMobile(_LastTarget?.Serial ?? 0);
            target = new TargetInfo();
            if (targets.Count <= 0)
            {
                //UOSObjects.Player.SendMessage(MsgLevel.Warning, "No one matching that was found on your screen.");
                return null;
            }

            // Loop through 3 times and break out if you can't get a target for some reason
            for (int i = 0; i < 3; i++)
            {
                if (nextTarget)
                {
                    _nextPrevTargetIndex++;

                    if (_nextPrevTargetIndex >= targets.Count)
                        _nextPrevTargetIndex = 0;
                }
                else
                {
                    _nextPrevTargetIndex--;

                    if (_nextPrevTargetIndex < 0)
                        _nextPrevTargetIndex = targets.Count - 1;
                }

                entity = targets[_nextPrevTargetIndex];


                if (entity != null && entity != UOSObjects.Player && entity != old)
                    break;

                entity = null;
            }

            if (entity == null)
                entity = old;

            if (entity == null)
            {
                //UOSObjects.Player.SendMessage(MsgLevel.Warning, "No one matching that was found on your screen.");
                return null;
            }

            if (_HasTarget)
                target.Flags = _CurFlags;
            else
                target.Type = 0;

            target.Gfx = entity.Graphic;
            target.Serial = entity.Serial;
            target.X = entity.Position.X;
            target.Y = entity.Position.Y;
            target.Z = entity.Position.Z;

            return entity;
        }

        /// <summary>
        /// Handles the common Next/Prev logic based on a list of targets passed in already filtered by the calling
        /// functions conditions.
        /// </summary>
        /// <param name="targets">The list of targets (already filtered)</param>
        /// <param name="nextTarget">next target true, previous target false</param>
        /// <param name="isenemy">only valid if is enemy</param>
        public static void NextPrevTarget(List<UOEntity> targets, bool nextTarget, bool isenemy)
        {
            GetFromTargets(targets, nextTarget, out TargetInfo target);

            if(target.Serial != UOSObjects.Player.Serial)
                _LastGroundTarg = _LastTarget = target;
            else
                _LastGroundTarg = target;

            if (isenemy)
                _LastHarmTarg = target;
            else
                _LastBeneTarg = target;

            if (isenemy && target.Serial > 0)
            {
                ClientPackets.PRecv_ChangeCombatant(target.Serial);
                _LastCombatant = target.Serial;
            }

            UOSObjects.Player.SendMessage(MsgLevel.Force, "New target set.");

            OverheadTargetMessage(target);
        }

        private static void OnClearQueue()
        {
            ClearQueue();

            UOSObjects.Player.SendMessage(MsgLevel.Force, "Target Queue Cleared");
        }

        internal static void OneTimeTarget(TargetResponseCallback onTarget)
        {
            OneTimeTarget(false, onTarget, null);
        }

        internal static void OneTimeTarget(TargetResponseCallback onTarget, bool fromGrab)
        {
            _FromGrabHotKey = fromGrab;

            OneTimeTarget(false, onTarget, null);
        }

        internal static void OneTimeTarget(bool ground, TargetResponseCallback onTarget)
        {
            OneTimeTarget(ground, onTarget, null);
        }

        internal static void OneTimeTarget(TargetResponseCallback onTarget, CancelTargetCallback onCancel)
        {
            OneTimeTarget(false, onTarget, onCancel);
        }

        internal static void OneTimeTarget(bool ground, TargetResponseCallback onTarget, CancelTargetCallback onCancel)
        {
            if (_Intercept && _OnCancel != null)
            {
                _OnCancel();
                CancelOneTimeTarget();
            }

            if (_HasTarget && _CurrentID != 0 && _CurrentID != LOCAL_TARG_ID)
            {
                _PreviousID = _CurrentID;
                _PreviousGround = _AllowGround;
                _PrevFlags = _CurFlags;

                _FilterCancel.Add(_PreviousID);
            }

            _Intercept = true;
            _CurrentID = LOCAL_TARG_ID;
            _OnTarget = onTarget;
            _OnCancel = onCancel;

            _ClientTarget = _HasTarget = true;
            ClientPackets.PRecv_Target(LOCAL_TARG_ID, ground);
            ClearQueue();
        }

        internal static void CancelOneTimeTarget()
        {
            _ClientTarget = _HasTarget = _FromGrabHotKey = false;
            ClientPackets.PRecv_CancelTarget(LOCAL_TARG_ID);
            EndIntercept();
        }

        internal static bool HasTargetType(string type)
        {
            //['any'/'beneficial'/'harmful'/'neutral'/'server'/'system']
            if (_HasTarget)
            {
                switch(type)
                {
                    case "any":
                        return true;
                    case "server":
                        return !_ClientTarget;
                    case "system":
                        return _ClientTarget;
                    case "neutral":
                        return _CurFlags == 0x00;
                    case "harmful":
                        return _CurFlags == 0x01;
                    case "beneficial":
                        return _CurFlags == 0x02;
                }
            }
            return false;
        }

        internal static void SetAutoTargetAction(params int[] ints)
        {
            CancelAutoTargetAction();
            _AutoTargetTimer = new AutoTargetTimer(ints);
            _AutoTargetTimer.Start();
        }

        internal static void CancelAutoTargetAction()
        {
            _AutoTargetTimer?.Stop();
        }

        private static AutoTargetTimer _AutoTargetTimer;
        private class AutoTargetTimer : Timer
        {
            private int _Count = 0, _MaxCount = 39;
            private int[] _Ints;

            internal AutoTargetTimer(ushort count, uint serial) : this((int)serial)
            {
                _MaxCount = count;
            }

            internal AutoTargetTimer(params int[] ints) : base(TimeSpan.Zero, TimeSpan.FromMilliseconds(50))
            {
                _Ints = ints;
            }

            protected override void OnTick()
            {
                _Count++;
                if (_Count <= _MaxCount)
                {
                    if (_HasTarget)
                    {
                        if (_Ints.Length == 0)//last target
                        {
                            Target(_LastTarget, false);
                        }
                        else if (_Ints.Length == 1)//serial
                        {
                            uint ser = (uint)_Ints[0];
                            if (ser == UOSObjects.Player.Serial)
                                DoTargetSelf(true);
                            else
                                Target(ser);
                        }
                        else if(_Ints.Length == 3)//point3d
                        {
                            Target(new Point3D(_Ints[0], _Ints[1], _Ints[2]));
                        }
                    }
                }
                else
                    Stop();
            }
        }

        private static bool _LTWasSet;

        private static uint _OldLT = 0;
        private static uint _OldBeneficialLT = 0;
        private static uint _OldHarmfulLT = 0;

        private static void RemoveTextFlags(UOEntity ue)
        {
            if (ue != null && ue.ObjPropList != null)
            {
                bool oplchanged = false;

                oplchanged |= ue.ObjPropList.Remove("Last Target");
                oplchanged |= ue.ObjPropList.Remove("Harmful Target");
                oplchanged |= ue.ObjPropList.Remove("Beneficial Target");

                if (oplchanged)
                    ue.OPLChanged();
            }
        }

        private static void AddTextFlags(UOEntity m)
        {
            if (m != null && m.ObjPropList != null)
            {
                bool oplchanged = false;

                if (_LastHarmTarg != null && _LastHarmTarg.Serial == m.Serial && IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Enemy))
                {
                    oplchanged = true;
                    m.ObjPropList.Add("Harmful Target");
                }

                if (_LastBeneTarg != null && _LastBeneTarg.Serial == m.Serial && IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Friend))
                {
                    oplchanged = true;
                    m.ObjPropList.Add("Beneficial Target");
                }

                if (!oplchanged && _LastTarget != null && _LastTarget.Serial == m.Serial)
                {
                    oplchanged = true;
                    m.ObjPropList.Add("Last Target");
                }

                if (oplchanged)
                    m.OPLChanged();
            }
        }

        private static void LastTargetChanged()
        {
            if (_LastTarget != null)
            {
                _LTWasSet = true;
                bool lth = UOSObjects.Gump.HLTargetHue > 0;

                if (SerialHelper.IsItem(_OldLT))
                {
                    RemoveTextFlags(UOSObjects.FindItem(_OldLT));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_OldLT);
                    if (m != null)
                    {
                        if (lth)
                            ClientPackets.PRecv_MobileIncoming(m);

                        RemoveTextFlags(m);
                    }
                }

                if (SerialHelper.IsItem(_LastTarget.Serial))
                {
                    AddTextFlags(UOSObjects.FindItem(_LastTarget.Serial));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_LastTarget.Serial);
                    if (m != null)
                    {
                        if (IsLastTarget(m) && lth)
                            ClientPackets.PRecv_MobileIncoming(m);

                        CheckLastTargetRange(m);

                        AddTextFlags(m);
                    }
                }

                _OldLT = _LastTarget.Serial;
            }
        }

        private static void LastBeneficialTargetChanged()
        {
            if (_LastBeneTarg != null)
            {
                if (SerialHelper.IsItem(_OldBeneficialLT))
                {
                    RemoveTextFlags(UOSObjects.FindItem(_OldBeneficialLT));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_OldBeneficialLT);
                    if (m != null)
                    {
                        RemoveTextFlags(m);
                    }
                }

                if (SerialHelper.IsItem(_LastBeneTarg.Serial))
                {
                    AddTextFlags(UOSObjects.FindItem(_LastBeneTarg.Serial));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_LastBeneTarg.Serial);
                    if (m != null)
                    {
                        CheckLastTargetRange(m);

                        AddTextFlags(m);
                    }
                }

                _OldBeneficialLT = _LastBeneTarg.Serial;
            }
        }

        private static void LastHarmfulTargetChanged()
        {
            if (_LastHarmTarg != null)
            {
                if (SerialHelper.IsItem(_OldHarmfulLT))
                {
                    RemoveTextFlags(UOSObjects.FindItem(_OldHarmfulLT));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_OldHarmfulLT);
                    if (m != null)
                    {
                        RemoveTextFlags(m);
                    }
                }

                if (SerialHelper.IsItem(_LastHarmTarg.Serial))
                {
                    AddTextFlags(UOSObjects.FindItem(_LastHarmTarg.Serial));
                }
                else
                {
                    UOMobile m = UOSObjects.FindMobile(_LastHarmTarg.Serial);
                    if (m != null)
                    {
                        CheckLastTargetRange(m);

                        AddTextFlags(m);
                        
                        if ((Engine.Instance.AllowBit(FeatureBit.SpellTargetShare) && _LastTarget.TargID == _SpellTargID) || Engine.Instance.AllowBit(FeatureBit.EnemyTargetShare))
                            SendSharedTarget(m);
                    }
                }

                _OldHarmfulLT = _LastHarmTarg.Serial;
            }
        }

        private static void SendSharedTarget(UOMobile m, string s = null)
        {
            var player = UOSObjects.Player;
            if (player == null)
                return;

            byte setting;
            if (s == null)
            {
                s = $"enemy_target:{m.Serial}";
                setting = UOSObjects.Gump.EnemyTargetShare;
            }
            else
            {
                setting = UOSObjects.Gump.SpellsTargetShare;
            }

            if (setting != 0)
            {
                if ((setting & (byte)AssistGump.ShareTargetTo.Alliance) != 0)
                {
                    UOSObjects.Player.Say(ScriptTextBox.RED_HUE, s, MessageType.Alliance);
                }
                if ((setting & (byte)AssistGump.ShareTargetTo.Guild) != 0)
                {
                    UOSObjects.Player.Say(ScriptTextBox.RED_HUE, s, MessageType.Guild);
                }
                if ((setting & (byte)AssistGump.ShareTargetTo.Party) != 0 && player.InParty)
                {
                    NetClient.Socket.PSend_PartyMessage(s);
                }
            }
        }

        internal static void CheckSharedTarget(uint from, string text, PacketHandlerEventArgs args)
        {
            if (text == null || text.Length < 14 || !text.StartsWith("enemy_target:"))
            {
                return;
            }

            if(from == UOSObjects.Player.Serial)
            {
                args.Block = true;
                return;
            }

            if(uint.TryParse(text.Substring(13), out uint serial) && SerialHelper.IsMobile(serial))
            {
                UOMobile m = UOSObjects.FindMobile(serial);
                if(m != null)
                {
                    if(!UOSObjects.Gump.SmartTargetRange || (byte)UOSObjects.Player.GetDistanceToSqrt(m) <= UOSObjects.Gump.SmartTargetRangeValue)
                    {
                        if(UOSObjects.Gump.SharedTargetInAliasEnemy)
                        {
                            Interpreter.SetAlias("enemy", serial);
                        }
                        Interpreter.SetAlias("shared", serial);
                    }
                }
            }
            args.Block = true;
        }

        public static bool LTWasSet
        {
            get { return _LTWasSet; }
        }

        public static void SetLastTargetTo(uint serial)
        {
            UOMobile m = UOSObjects.FindMobile(serial);
            if (m != null)
                SetLastTargetTo(m);
        }

        public static void SetLastTargetTo(UOMobile m)
        {
            SetLastTargetTo(m, 0);
        }

        public static void SetLastTargetTo(UOMobile m, byte flagType)
        {
            TargetInfo targ = new TargetInfo();
            _LastGroundTarg = _LastTarget = targ;

            if ((_HasTarget && _CurFlags == 1) || flagType == 1)
                _LastHarmTarg = targ;
            else if ((_HasTarget && _CurFlags == 2) || flagType == 2)
                _LastBeneTarg = targ;

            targ.Type = 0;
            if (_HasTarget)
                targ.Flags = _CurFlags;
            else
                targ.Flags = flagType;

            targ.Gfx = m.Body;
            targ.Serial = m.Serial;
            targ.X = m.Position.X;
            targ.Y = m.Position.Y;
            targ.Z = m.Position.Z;

            ClientPackets.PRecv_ChangeCombatant(m.Serial);
            _LastCombatant = m.Serial;
            UOSObjects.Player.SendMessage(MsgLevel.Force, "New target set");

            OverheadTargetMessage(targ);

            byte wasSmart = UOSObjects.Gump.SmartTarget;
            UOSObjects.Gump.SmartTarget = 0;
            LastTarget();
            UOSObjects.Gump.SmartTarget = wasSmart;
            LastTargetChanged();
        }

        private static void EndIntercept()
        {
            _Intercept = false;
            _OnTarget = null;
            _OnCancel = null;
            _FromGrabHotKey = false;
        }

        public static void TargetSelf(bool forceQ = false)
        {
            if (UOSObjects.Player == null)
                return;

            if (_HasTarget)
            {
                if (!DoTargetSelf())
                    ResendTarget();
            }
            else if (forceQ)
            {
                _QueueTarget = TargetSelfAction;
            }
        }

        private static bool DoTargetSelf()
        {
            return DoTargetSelf(false);
        }

        public static bool DoTargetSelf(bool nointercept)
        {
            if (UOSObjects.Player == null)
                return false;

            if (CheckHealPoisonTarg(_CurrentID, UOSObjects.Player.Serial))
                return false;

            CancelClientTarget();
            _HasTarget = false;
            _FromGrabHotKey = false;

            if (!nointercept && _Intercept)
            {
                TargetInfo targ = new TargetInfo();
                targ.Serial = UOSObjects.Player.Serial;
                targ.Gfx = UOSObjects.Player.Body;
                targ.Type = 0;
                targ.X = UOSObjects.Player.Position.X;
                targ.Y = UOSObjects.Player.Position.Y;
                targ.Z = UOSObjects.Player.Position.Z;
                targ.TargID = LOCAL_TARG_ID;
                targ.Flags = 0;

                OneTimeResponse(targ);
            }
            else
            {
                NetClient.Socket.PSend_TargetResponse(_CurrentID, UOSObjects.Player);
            }

            return true;
        }

        public static void LastTarget()
        {
            LastTarget(false);
        }

        public static void LastTarget(bool forceQ)
        {
            if (FromGrabHotKey)
                return;

            if (_HasTarget)
            {
                if (!DoLastTarget())
                    ResendTarget();
            }
            else if (forceQ)
            {
                _QueueTarget = LastTargetAction;
            }
        }

        public static bool DoLastTarget()
        {
            if (FromGrabHotKey)
                return true;

            TargetInfo targ;
            if (_CurFlags > 0 && IsSmartTargetingEnabled((byte)(_CurFlags == 1 ? 2 : 1)))
            {
                if (_AllowGround && _LastGroundTarg != null)
                    targ = _LastGroundTarg;
                else if (_CurFlags == 1)
                    targ = _LastHarmTarg;
                else if (_CurFlags == 2)
                    targ = _LastBeneTarg;
                else
                    targ = _LastTarget;

                if (targ == null)
                    targ = _LastTarget;
            }
            else
            {
                if (_AllowGround && _LastGroundTarg != null)
                    targ = _LastGroundTarg;
                else
                    targ = _LastTarget;
            }

            if (targ == null)
                return false;

            Point3D pos = Point3D.Zero;
            if (SerialHelper.IsMobile(targ.Serial))
            {
                UOMobile m = UOSObjects.FindMobile(targ.Serial);
                if (m != null)
                {
                    pos = m.Position;

                    targ.X = pos.X;
                    targ.Y = pos.Y;
                    targ.Z = pos.Z;
                }
                else
                {
                    pos = Point3D.Zero;
                }
            }
            else if (SerialHelper.IsItem(targ.Serial))
            {
                UOItem i = UOSObjects.FindItem(targ.Serial);
                if (i != null)
                {
                    pos = i.GetWorldPosition();

                    targ.X = i.Position.X;
                    targ.Y = i.Position.Y;
                    targ.Z = i.Position.Z;
                }
                else
                {
                    pos = Point3D.Zero;
                    targ.X = targ.Y = targ.Z = 0;
                }
            }
            else
            {
                if (!_AllowGround && !SerialHelper.IsValid(targ.Serial))
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "Warning: Current target does not allow to target Ground. Last Target NOT performed");
                    return false;
                }
                else
                {
                    pos = new Point3D(targ.X, targ.Y, targ.Z);
                }
            }

            if (UOSObjects.Gump.SmartTargetRange && Engine.Instance.AllowBit(FeatureBit.RangeCheckLT) &&
                (pos == Point3D.Zero || !Utility.InRange(UOSObjects.Player.Position, pos, UOSObjects.Gump.SmartTargetRangeValue)))
            {
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "Requested Target is out of range, Last Target NOT executed!");
                return false;
            }

            if (CheckHealPoisonTarg(_CurrentID, targ.Serial))
                return false;

            CancelClientTarget();
            _HasTarget = false;

            targ.TargID = _CurrentID;

            if (_Intercept)
                OneTimeResponse(targ);
            else
                NetClient.Socket.PSend_TargetResponse(targ);
            return true;
        }

        public static bool DoQueueTarget()
        {
            if (FromGrabHotKey)
                return true;

            TargetInfo targ;
            if (_CurFlags > 0 && IsSmartTargetingEnabled((byte)(_CurFlags == 1 ? 2 : 1)))
            {
                if (_AllowGround && _LastGroundTarg != null)
                    targ = _LastGroundTarg;
                else if (_CurFlags == 1)
                    targ = _LastHarmTarg;
                else if (_CurFlags == 2)
                    targ = _LastBeneTarg;
                else
                    targ = _LastTarget;

                if (targ == null)
                    targ = _LastTarget;
            }
            else
            {
                if (_AllowGround && _LastGroundTarg != null)
                    targ = _LastGroundTarg;
                else
                    targ = _LastTarget;
            }

            if (targ == null)
                return false;

            Point3D pos = Point3D.Zero;
            if (SerialHelper.IsMobile(targ.Serial))
            {
                UOMobile m = UOSObjects.FindMobile(targ.Serial);
                if (m != null)
                {
                    pos = m.Position;

                    targ.X = pos.X;
                    targ.Y = pos.Y;
                    targ.Z = pos.Z;
                }
                else
                {
                    pos = Point3D.Zero;
                }
            }
            else if (SerialHelper.IsItem(targ.Serial))
            {
                UOItem i = UOSObjects.FindItem(targ.Serial);
                if (i != null)
                {
                    pos = i.GetWorldPosition();

                    targ.X = i.Position.X;
                    targ.Y = i.Position.Y;
                    targ.Z = i.Position.Z;
                }
                else
                {
                    pos = Point3D.Zero;
                    targ.X = targ.Y = targ.Z = 0;
                }
            }
            else
            {
                if (!_AllowGround && !SerialHelper.IsValid(targ.Serial))
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "Warning: Current target does not allow to target Ground. Last Target NOT performed");
                    return false;
                }
                else
                {
                    pos = new Point3D(targ.X, targ.Y, targ.Z);
                }
            }

            if (UOSObjects.Gump.SmartTargetRange && Engine.Instance.AllowBit(FeatureBit.RangeCheckLT) &&
                (pos == Point3D.Zero || !Utility.InRange(UOSObjects.Player.Position, pos, UOSObjects.Gump.SmartTargetRangeValue)))
            {
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "Requested Target is out of range, Last Target NOT executed!");
                return false;
            }

            if (CheckHealPoisonTarg(_CurrentID, targ.Serial))
                return false;

            CancelClientTarget();
            _HasTarget = false;

            targ.TargID = _CurrentID;

            if (_Intercept)
                OneTimeResponse(targ);
            else
                NetClient.Socket.PSend_TargetResponse(targ);
            return true;
        }

        public static void ClearQueue()
        {
            _QueueTarget = null;
        }

        private static TimerCallbackState<TargetInfo> _OneTimeRespCallback = new TimerCallbackState<TargetInfo>(OneTimeResponse);

        private static void OneTimeResponse(TargetInfo info)
        {
            if ((info.X == 0xFFFF && info.Y == 0xFFFF) && (info.Serial == 0 || info.Serial >= 0x80000000))
            {
                _OnCancel?.Invoke();
            }
            else
            {
                if (ScriptManager.Recording)
                    ScriptManager.AddToScript($"target {info.Serial}");
                _OnTarget?.Invoke(info.Type == 1 ? true : false, info.Serial, new Point3D(info.X, info.Y, info.Z), info.Gfx);
            }
            EndIntercept();
        }

        internal static void CancelTarget()
        {
            OnClearQueue();
            if (_HasTarget)
            {
                if(!_ClientTarget)
                    NetClient.Socket.PSend_TargetCancelResponse(_CurrentID);
                _HasTarget = false;
            }
            CancelClientTarget();

            _FromGrabHotKey = false;
        }

        private static void CancelClientTarget()
        {
            if (_ClientTarget)
            {
                _FilterCancel.Add((uint)_CurrentID);
                ClientPackets.PRecv_CancelTarget(_CurrentID);
                _ClientTarget = false;
            }
        }

        private static TargetInfo _QueuedTarget = null;
        internal static TargetInfo QueuedTarget => _QueuedTarget;
        private static bool OnSimpleTarget()
        {
            if(_QueuedTarget != null)
            {
                _QueuedTarget.TargID = _CurrentID;
                if(_QueuedTarget.Serial != UOSObjects.Player.Serial)
                    _LastGroundTarg = _LastTarget = _QueuedTarget;
                else
                    _LastGroundTarg = _QueuedTarget;
                NetClient.Socket.PSend_TargetResponse(_QueuedTarget);
            }
            return true;
        }

        public static void Target(TargetInfo info, bool forceQ)
        {
            void cancel()
            {
                CancelClientTarget();
                _HasTarget = false;
                _FromGrabHotKey = false;
            }

            if (_Intercept)
            {
                cancel();
                OneTimeResponse(info);
            }
            else if (_HasTarget)
            {
                info.TargID = _CurrentID;
                if(info.Serial != UOSObjects.Player.Serial)
                    _LastGroundTarg = _LastTarget = info;
                else
                    _LastGroundTarg = info;
                cancel();
                NetClient.Socket.PSend_TargetResponse(info);
            }
            else if (forceQ)
            {
                _QueuedTarget = info;
                _QueueTarget = OnSimpleTarget;
            }
        }

        public static void Target(Point3D pt, bool forceQ = false)
        {
            TargetInfo info = new TargetInfo
            {
                Type = 1,
                Flags = 0,
                Serial = 0,
                X = pt.X,
                Y = pt.Y,
                Z = pt.Z,
                Gfx = UOSObjects.GetTileNear(pt.X, pt.Y, pt.Z)
            };

            Target(info, forceQ);
        }

        public static bool WaitingForTarget => _QueueTarget != null && !_Intercept;

        public static void Target(Point3D pt, int gfx, bool forceQ = false)
        {
            TargetInfo info = new TargetInfo
            {
                Type = 1,
                Flags = 0,
                Serial = 0,
                X = pt.X,
                Y = pt.Y,
                Z = pt.Z,
                Gfx = (ushort)(gfx & 0x3FFF)
            };

            Target(info, forceQ);
        }

        public static TargetInfo Target(uint s, bool forceQ = false)
        {
            TargetInfo info = new TargetInfo
            {
                Type = 0,
                Flags = 0,
                Serial = s
            };

            if (SerialHelper.IsItem(s))
            {
                UOItem item = UOSObjects.FindItem(s);
                if (item != null)
                {
                    info.X = item.Position.X;
                    info.Y = item.Position.Y;
                    info.Z = item.Position.Z;
                    info.Gfx = item.ItemID;
                }
            }
            else if (SerialHelper.IsMobile(s))
            {
                UOMobile m = UOSObjects.FindMobile(s);
                if (m != null)
                {
                    info.X = m.Position.X;
                    info.Y = m.Position.Y;
                    info.Z = m.Position.Z;
                    info.Gfx = m.Body;
                }
            }

            Target(info, forceQ);
            return info;
        }

        public static TargetInfo Target(object o, bool forceQ = false)
        {
            TargetInfo info;
            if (o is UOItem item)
            {
                info = new TargetInfo
                {
                    Type = 0,
                    Flags = 0,
                    Serial = item.Serial,
                    X = item.Position.X,
                    Y = item.Position.Y,
                    Z = item.Position.Z,
                    Gfx = item.ItemID
                };
                Target(info, forceQ);
            }
            else if (o is UOMobile m)
            {
                info = new TargetInfo
                {
                    Type = 0,
                    Flags = 0,
                    Serial = m.Serial,
                    X = m.Position.X,
                    Y = m.Position.Y,
                    Z = m.Position.Z,
                    Gfx = m.Body
                };
                Target(info, forceQ);
            }
            else if (o is uint u)
            {
                info = Target(u, forceQ);
            }
            else if (o is TargetInfo ti)
            {
                info = ti;
                Target(ti, forceQ);
            }
            else
            {
                info = new TargetInfo
                {
                    Type = 0,
                    Flags = 0,
                    Serial = 0
                };
            }
            return info;
        }

        private static DateTime _lastFlagCheck = DateTime.UtcNow;
        private static uint _lastFlagCheckSerial;

        public static void CheckTextFlags(UOMobile m)
        {
            if (DateTime.UtcNow - _lastFlagCheck < TimeSpan.FromMilliseconds(250) && m.Serial == _lastFlagCheckSerial)
                return;

            bool harm = _LastHarmTarg != null && _LastHarmTarg.Serial == m.Serial;
            bool bene = _LastBeneTarg != null && _LastBeneTarg.Serial == m.Serial;

            if (harm && IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Enemy))
                m.OverheadMessage(0x90, "[Harmful Target]");
            if (bene && IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Friend))
                m.OverheadMessage(0x3F, "[Beneficial Target]");

            if (_LastTarget != null && _LastTarget.Serial == m.Serial)
                m.OverheadMessage(0x3B2, "[Last Target]");

            _lastFlagCheck = DateTime.UtcNow;
            _lastFlagCheckSerial = m.Serial;
        }

        public static bool IsLastTarget(UOMobile m)
        {
            if (m != null)
            {
                if (IsSmartTargetingEnabled(3))
                {
                    if (_LastHarmTarg != null && _LastHarmTarg.Serial == m.Serial)
                        return true;
                }
                else
                {
                    if (_LastTarget != null && _LastTarget.Serial == m.Serial)
                        return true;
                }
            }

            return false;
        }

        public static bool IsBeneficialTarget(UOMobile m)
        {
            if (m != null)
            {
                if (IsSmartTargetingEnabled(1))
                {
                    if (_LastBeneTarg != null && _LastBeneTarg.Serial == m.Serial)
                        return true;
                }
                else
                {
                    if (_LastTarget != null && _LastTarget.Serial == m.Serial)
                        return true;
                }
            }

            return false;
        }

        public static bool IsHarmfulTarget(UOMobile m)
        {
            if (m != null)
            {
                if (IsSmartTargetingEnabled(2))
                {
                    if (_LastHarmTarg != null && _LastHarmTarg.Serial == m.Serial)
                        return true;
                }
                else
                {
                    if (_LastTarget != null && _LastTarget.Serial == m.Serial)
                        return true;
                }
            }

            return false;
        }

        public static void CheckLastTargetRange(UOMobile m)
        {
            if (UOSObjects.Player == null)
                return;

            if (_HasTarget && m != null && _LastTarget != null && m.Serial == _LastTarget.Serial &&
                _QueueTarget == LastTargetAction)
            {
                if (UOSObjects.Gump.SmartTargetRange && Engine.Instance.AllowBit(FeatureBit.RangeCheckLT))
                {
                    if (Utility.InRange(UOSObjects.Player.Position, m.Position, UOSObjects.Gump.SmartTargetRangeValue))
                    {
                        if (_QueueTarget())
                            ClearQueue();
                    }
                }
            }
        }

        private static bool CheckHealPoisonTarg(uint targID, uint ser)
        {
            if (UOSObjects.Player == null)
                return false;

            if (targID == _SpellTargID && SerialHelper.IsMobile(ser) &&
                (UOSObjects.Player.LastSpell == Spell.ToID(1, 4) || UOSObjects.Player.LastSpell == Spell.ToID(4, 5)) &&
                UOSObjects.Gump.BlockInvalidHeal && Engine.Instance.AllowBit(FeatureBit.BlockHealPoisoned))
            {
                UOMobile m = UOSObjects.FindMobile(ser);

                if (m != null && m.Poisoned)
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "Heal Blocked (the target is poisoned)");
                    return true;
                }
            }

            return false;
        }

        private static void TargetResponse(ref StackDataReader p, PacketHandlerEventArgs args)
        {
            TargetInfo info = new TargetInfo
            {
                Type = p.ReadUInt8(),
                TargID = p.ReadUInt32BE(),
                Flags = p.ReadUInt8(),
                Serial = p.ReadUInt32BE(),
                X = p.ReadUInt16BE(),
                Y = p.ReadUInt16BE(),
                Z = (short)p.ReadUInt16BE(),
                Gfx = p.ReadUInt16BE()
            };

            _ClientTarget = false;

            OverheadTargetMessage(info);

            // check for cancel
            if (info.X == 0xFFFF && info.Y == 0xFFFF && (info.Serial <= 0 || info.Serial >= 0x80000000))
            {
                bool prevhas = _HasTarget, prevgrab = _FromGrabHotKey;
                _HasTarget = false;
                _FromGrabHotKey = false;

                if (_Intercept)
                {
                    args.Block = true;
                    Timer.DelayedCallbackState(TimeSpan.Zero, _OneTimeRespCallback, info).Start();

                    if (_PreviousID != 0)
                    {
                        _CurrentID = _PreviousID;
                        _AllowGround = _PreviousGround;
                        _CurFlags = _PrevFlags;

                        _PreviousID = 0;

                        ResendTarget();
                    }
                }
                else if (_FilterCancel.Contains((uint)info.TargID) || info.TargID == LOCAL_TARG_ID)
                {
                    args.Block = true;
                    if (info.TargID != LOCAL_TARG_ID)
                    {
                        _HasTarget = prevhas;
                        _FromGrabHotKey = prevgrab;
                    }
                }

                _FilterCancel.Clear();
                return;
            }

            ClearQueue();

            if (_Intercept)
            {
                if (info.TargID == LOCAL_TARG_ID)
                {
                    Timer.DelayedCallbackState(TimeSpan.Zero, _OneTimeRespCallback, info).Start();

                    _HasTarget = false;
                    _FromGrabHotKey = false;
                    args.Block = true;

                    if (_PreviousID != 0)
                    {
                        _CurrentID = _PreviousID;
                        _AllowGround = _PreviousGround;
                        _CurFlags = _PrevFlags;

                        _PreviousID = 0;

                        ResendTarget();
                    }

                    _FilterCancel.Clear();

                    return;
                }
                else
                {
                    EndIntercept();
                }
            }

            _HasTarget = false;

            if (CheckHealPoisonTarg(_CurrentID, info.Serial))
            {
                ResendTarget();
                args.Block = true;
            }

            if (info.Serial != UOSObjects.Player.Serial)
            {
                if (SerialHelper.IsValid(info.Serial))
                {
                    // only let lasttarget be a non-ground target

                    _LastTarget = info;
                    if (info.Flags == 1)
                    {
                        _LastHarmTarg = info;
                        if (IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Enemy))
                        {
                            Interpreter.SetAlias("enemy", info.Serial);
                        }
                        
                        if (info.Serial != _OldHarmfulLT)
                        {
                            LastHarmfulTargetChanged();
                        }
                    }
                    else if (info.Flags == 2)
                    {
                        _LastBeneTarg = info;
                        if (IsSmartTargetingEnabled((byte)AssistGump.SmartTargetFor.Friend))
                        {
                            Interpreter.SetAlias("friend", info.Serial);
                        }
                        
                        if (info.Serial != _OldBeneficialLT)
                        {
                            LastBeneficialTargetChanged();
                        }
                    }

                    if(SerialHelper.IsMobile(info.Serial) && _SpellTargID == info.TargID && Engine.Instance.AllowBit(FeatureBit.SpellTargetShare))
                    {
                        UOMobile m = UOSObjects.FindMobile(info.Serial);
                        if (m != null)
                        {
                            string name = Spell.GetName(UOSObjects.Player.LastSpell);
                            if (!string.IsNullOrEmpty(name))
                            {
                                SendSharedTarget(m, $"targeting \"{m.Name}\" with {name}.");
                            }
                        }
                    }

                    LastTargetChanged();
                }

                _LastGroundTarg = info; // ground target is the true last target
                if (ScriptManager.Recording)
                    ScriptManager.AddToScript(info.Serial == 0 ? $"targettile {info.X} {info.Y} {info.Z}" : (UOSObjects.Gump.RecordTypeUse ? $"targettype 0x{info.Gfx:X4}" : $"target 0x{info.Serial:X}"));
            }
            else
            {
                if (ScriptManager.Recording)
                {
                    if (UOSObjects.Gump.RecordTypeUse)
                        ScriptManager.AddToScript($"targettype 0x{info.Gfx:X4}");
                    else
                        ScriptManager.AddToScript($"target 0x{info.Serial:X}");
                }
            }

            if (UOSObjects.Player.LastSpell == 52 && !GateTimer.Running)
            {
                GateTimer.Start();
            }

            _FilterCancel.Clear();
        }

        private static void NewTarget(ref StackDataReader p, PacketHandlerEventArgs args)
        {
            bool prevAllowGround = _AllowGround;
            uint prevID = _CurrentID;
            byte prevFlags = _CurFlags;
            bool prevClientTarget = _ClientTarget;

            _AllowGround = p.ReadBool(); // allow ground
            _CurrentID = p.ReadUInt32BE(); // target uid
            _CurFlags = p.ReadUInt8(); // flags
            // the rest of the packet is 0s

            // check for a server cancel command
            if (!_AllowGround && _CurrentID == 0 && _CurFlags == 3)
            {
                _HasTarget = false;
                _FromGrabHotKey = false;

                _ClientTarget = false;
                if (_Intercept)
                {
                    EndIntercept();
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "Server sent new target, canceling internal target.");
                }

                return;
            }

            if (Spell.LastCastTime + TimeSpan.FromMilliseconds(Utility.ISTANT_CASTING ? 1000 : 3000) > DateTime.UtcNow && Spell.LastCastTime + TimeSpan.FromMilliseconds(Utility.ISTANT_CASTING ? 0 : 500) <= DateTime.UtcNow && _SpellTargID == 0)
                _SpellTargID = _CurrentID;

            _HasTarget = true;
            _ClientTarget = false;
            if (ScriptManager.Recording)
                ScriptManager.AddToScript("waitfortarget 30000");

            if (_QueueTarget != null && _QueueTarget())
            {
                ClearQueue();
                _HasTarget = false;
                _FromGrabHotKey = false;
                args.Block = true;
            }

            if (args.Block)
            {
                if (prevClientTarget)
                {
                    _AllowGround = prevAllowGround;
                    _CurrentID = prevID;
                    _CurFlags = prevFlags;

                    _ClientTarget = true;

                    if (!_Intercept)
                        CancelClientTarget();
                }
            }
            else
            {
                _ClientTarget = true;

                if (_Intercept)
                {
                    _OnCancel?.Invoke();
                    EndIntercept();
                    UOSObjects.Player.SendMessage(MsgLevel.Error, "Server sent new target, canceling internal target.");

                    _FilterCancel.Add((uint)prevID);
                }
            }
        }

        public static void ResendTarget()
        {
            if (!_ClientTarget || !_HasTarget)
            {
                CancelClientTarget();
                _ClientTarget = _HasTarget = true;
                ClientPackets.PRecv_Target(_CurrentID, _AllowGround, _CurFlags);
            }
        }

        private static TargetInfo _lastOverheadMessageTarget = new TargetInfo();

        public static void OverheadTargetMessage(TargetInfo info)
        {
            if (info == null)
                return;

            if (UOSObjects.Gump.HLTargetHue > 0 && SerialHelper.IsMobile(info.Serial))
            {
                UOMobile m = UOSObjects.FindMobile(info.Serial);

                if (m == null)
                    return;

                UOSObjects.Player.OverheadMessage(FriendsManager.IsFriend(m.Serial) ? PlayerData.GetColorCode(MsgLevel.Friend) : m.GetNotorietyColorInt(), $"Target: {m.Name}");

                m.OverheadMessage(UOSObjects.Gump.HLTargetHue, $"*{m.Name}*");
            }

            _lastOverheadMessageTarget = info;
        }

        private static bool IsSmartTargetingEnabled(byte type)
        {
            byte st = UOSObjects.Gump.SmartTarget;
            if(st > 0 && Engine.Instance.AllowBit(FeatureBit.SmartLT))
            {
                if((type & 2) != 0)
                {
                    return (st & 2) != 0;
                }
                else if((type & 1) != 0)
                {
                    return (st & 1) != 0;
                }
            }
            return false;
        }

        public static void ClosestTarget(MobType type, params int[] noto)
        {
            ClosestTarget(12, false, false, type, noto);
        }

        public static void ClosestTarget(bool friends, MobType type, params int[] noto)
        {
            ClosestTarget(12, friends, false, type, noto);
        }

        public static void ClosestTarget(bool friends, bool isdead, MobType type, params int[] noto)
        {
            ClosestTarget(12, friends, isdead, type, noto);
        }

        public static void ClosestTarget(byte range, bool friends, bool isdead, MobType type, params int[] noto)
        {
            if (!Engine.Instance.AllowBit(FeatureBit.ClosestTargets))
                return;

            List<UOMobile> list = new List<UOMobile>();
            foreach (UOMobile m in UOSObjects.MobilesInRange(range))
            {
                if (type == MobType.Humanoid)
                {
                    if (!m.IsHuman)
                        continue;
                }
                else if (type == MobType.Monster)
                {
                    if (!m.IsMonster)
                        continue;
                }
                if (!m.Blessed && m.IsGhost == isdead && m.Serial != ClassicUO.Client.Game.UO.World.Player.Serial &&
                    Utility.InRange(UOSObjects.Player.Position, m.Position, UOSObjects.Gump.SmartTargetRangeValue))
                {
                    if (noto.Length == 0 && !friends)
                    {
                        list.Add(m);
                    }
                    else if (friends && FriendsManager.IsFriend(m.Serial))
                    {
                        list.Add(m);
                    }
                    else
                    {
                        for (int i = 0; i < noto.Length; i++)
                        {
                            if (noto[i] == m.Notoriety)
                            {
                                list.Add(m);
                                break;
                            }
                        }
                    }
                }
            }

            UOMobile closest = null;
            double closestDist = double.MaxValue;

            foreach (UOMobile m in list)
            {
                double dist = Utility.DistanceSqrt(m.Position, UOSObjects.Player.Position);

                if (dist < closestDist || closest == null)
                {
                    closestDist = dist;
                    closest = m;
                }
            }

            if (closest != null)
                SetLastTargetTo(closest);
            else
                UOSObjects.Player.SendMessage(MsgLevel.Warning, "No one matching that was found on your screen.");
        }
    }
}
