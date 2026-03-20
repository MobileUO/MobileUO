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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Assistant.Core;
using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Configuration;

namespace Assistant
{
    internal enum LockType : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    internal enum MsgLevel
    {
        None = 0,
        Info = 1,
        Friend = 2,
        Advise = 3,
        Force = 4,
        Debug = 5,
        Error = 6,
        Warning = 7
    }

    internal class Skill
    {
        internal static int Count = 55;

        private LockType _Lock;
        private ushort _Value;
        private ushort _Base;
        private ushort _Cap;
        private short _Delta;
        private int _Idx;

        internal Skill(int idx)
        {
            _Idx = idx;
        }

        internal int Index
        {
            get { return _Idx; }
        }

        internal LockType Lock
        {
            get { return _Lock; }
            set { _Lock = value; }
        }

        internal ushort FixedValue
        {
            get { return _Value; }
            set { _Value = value; }
        }

        internal ushort FixedBase
        {
            get { return _Base; }
            set
            {
                _Delta += (short)(value - _Base);
                _Base = value;
            }
        }

        internal ushort FixedCap
        {
            get { return _Cap; }
            set { _Cap = value; }
        }

        internal double Value
        {
            get { return _Value / 10.0; }
            set { _Value = (ushort)(value * 10.0); }
        }

        internal double Base
        {
            get { return _Base / 10.0; }
            set { _Base = (ushort)(value * 10.0); }
        }

        internal double Cap
        {
            get { return _Cap / 10.0; }
            set { _Cap = (ushort)(value * 10.0); }
        }

        internal double Delta
        {
            get { return _Delta / 10.0; }
            set { _Delta = (short)(value * 10); }
        }
    }

    internal enum SkillName
    {
        Alchemy = 0,
        Anatomy = 1,
        AnimalLore = 2,
        ItemID = 3,
        ArmsLore = 4,
        Parry = 5,
        Begging = 6,
        Blacksmith = 7,
        Fletching = 8,
        Peacemaking = 9,
        Camping = 10,
        Carpentry = 11,
        Cartography = 12,
        Cooking = 13,
        DetectHidden = 14,
        Discordance = 15,
        EvalInt = 16,
        Healing = 17,
        Fishing = 18,
        Forensics = 19,
        Herding = 20,
        Hiding = 21,
        Provocation = 22,
        Inscribe = 23,
        Lockpicking = 24,
        Magery = 25,
        MagicResist = 26,
        Tactics = 27,
        Snooping = 28,
        Musicianship = 29,
        Poisoning = 30,
        Archery = 31,
        SpiritSpeak = 32,
        Stealing = 33,
        Tailoring = 34,
        AnimalTaming = 35,
        TasteID = 36,
        Tinkering = 37,
        Tracking = 38,
        Veterinary = 39,
        Swords = 40,
        Macing = 41,
        Fencing = 42,
        Wrestling = 43,
        Lumberjacking = 44,
        Mining = 45,
        Meditation = 46,
        Stealth = 47,
        RemoveTrap = 48,
        Necromancy = 49,
        Focus = 50,
        Chivalry = 51,
        Bushido = 52,
        Ninjitsu = 53,
        SpellWeaving = 54
    }

    internal enum MaleSounds
    {
        Ah = 0x41A,
        Ahha = 0x41B,
        Applaud = 0x41C,
        BlowNose = 0x41D,
        Burp = 0x41E,
        Cheer = 0x41F,
        ClearThroat = 0x420,
        Cough = 0x421,
        CoughBS = 0x422,
        Cry = 0x423,
        Fart = 0x429,
        Gasp = 0x42A,
        Giggle = 0x42B,
        Groan = 0x42C,
        Growl = 0x42D,
        Hey = 0x42E,
        Hiccup = 0x42F,
        Huh = 0x430,
        Kiss = 0x431,
        Laugh = 0x432,
        No = 0x433,
        Oh = 0x434,
        Oomph1 = 0x435,
        Oomph2 = 0x436,
        Oomph3 = 0x437,
        Oomph4 = 0x438,
        Oomph5 = 0x439,
        Oomph6 = 0x43A,
        Oomph7 = 0x43B,
        Oomph8 = 0x43C,
        Oomph9 = 0x43D,
        Oooh = 0x43E,
        Oops = 0x43F,
        Puke = 0x440,
        Scream = 0x441,
        Shush = 0x442,
        Sigh = 0x443,
        Sneeze = 0x444,
        Sniff = 0x445,
        Snore = 0x446,
        Spit = 0x447,
        Whistle = 0x448,
        Yawn = 0x449,
        Yea = 0x44A,
        Yell = 0x44B,
    }

    internal enum FemaleSounds
    {
        Ah = 0x30B,
        Ahha = 0x30C,
        Applaud = 0x30D,
        BlowNose = 0x30E,
        Burp = 0x30F,
        Cheer = 0x310,
        ClearThroat = 0x311,
        Cough = 0x312,
        CoughBS = 0x313,
        Cry = 0x314,
        Fart = 0x319,
        Gasp = 0x31A,
        Giggle = 0x31B,
        Groan = 0x31C,
        Growl = 0x31D,
        Hey = 0x31E,
        Hiccup = 0x31F,
        Huh = 0x320,
        Kiss = 0x321,
        Laugh = 0x322,
        No = 0x323,
        Oh = 0x324,
        Oomph1 = 0x325,
        Oomph2 = 0x326,
        Oomph3 = 0x327,
        Oomph4 = 0x328,
        Oomph5 = 0x329,
        Oomph6 = 0x32A,
        Oomph7 = 0x32B,
        Oooh = 0x32C,
        Oops = 0x32D,
        Puke = 0x32E,
        Scream = 0x32F,
        Shush = 0x330,
        Sigh = 0x331,
        Sneeze = 0x332,
        Sniff = 0x333,
        Snore = 0x334,
        Spit = 0x335,
        Whistle = 0x336,
        Yawn = 0x337,
        Yea = 0x338,
        Yell = 0x339,
    }

    internal class PlayerData : UOMobile
    {
        internal int VisRange = 18;

        internal int MultiVisRange
        {
            get { return VisRange + 5; }
        }

        private int _MaxWeight = -1;

        private short _FireResist, _ColdResist, _PoisonResist, _EnergyResist, _Luck;
        private ushort _DamageMin, _DamageMax;

        private ushort _Str, _Dex, _Int;
        private LockType _StrLock, _DexLock, _IntLock;
        private uint _Gold;
        private ushort _Weight;
        private Skill[] _Skills;
        private ushort _AR;
        private ushort _StatCap;
        private byte _Followers;
        private byte _FollowersMax;
        private int _Tithe;
        private sbyte _LocalLight;
        private byte _GlobalLight;
        private ushort _Features;
        private byte _Season;
        private byte _DefaultSeason;

        private bool _SkillsSent;
        private DateTime _CriminalStart = DateTime.MinValue;
        internal static Dictionary<string, int> BuffNames { get; } = new Dictionary<string, int>();

        internal List<BuffsDebuffs> BuffsDebuffs { get; } = new List<BuffsDebuffs>();

        internal HashSet<uint> OpenedCorpses { get; } = new HashSet<uint>();

        internal PlayerData(uint serial) : base(serial)
        {
            Targeting.Instance = new Targeting.InternalSorter(this);
            Targeting.ContainedInstance = new Targeting.ContainedInternalSorter(this);
            _Skills = new Skill[Skill.Count];
            for (int i = 0; i < _Skills.Length; i++)
                _Skills[i] = new Skill(i);
        }

        internal ushort Str
        {
            get { return _Str; }
            set { _Str = value; }
        }

        internal ushort Dex
        {
            get { return _Dex; }
            set { _Dex = value; }
        }

        internal ushort Int
        {
            get { return _Int; }
            set { _Int = value; }
        }

        internal uint Gold
        {
            get { return _Gold; }
            set { _Gold = value; }
        }

        internal ushort Weight
        {
            get { return _Weight; }
            set { _Weight = value; }
        }

        internal ushort MaxWeight
        {
            get
            {
                if (_MaxWeight == -1)
                    return (ushort)((_Str * 3.5) + 40);
                else
                    return (ushort)_MaxWeight;
            }
            set { _MaxWeight = value; }
        }

        internal short FireResistance
        {
            get { return _FireResist; }
            set { _FireResist = value; }
        }

        internal short ColdResistance
        {
            get { return _ColdResist; }
            set { _ColdResist = value; }
        }

        internal short PoisonResistance
        {
            get { return _PoisonResist; }
            set { _PoisonResist = value; }
        }

        internal short EnergyResistance
        {
            get { return _EnergyResist; }
            set { _EnergyResist = value; }
        }

        internal short Luck
        {
            get { return _Luck; }
            set { _Luck = value; }
        }

        internal ushort DamageMin
        {
            get { return _DamageMin; }
            set { _DamageMin = value; }
        }

        internal ushort DamageMax
        {
            get { return _DamageMax; }
            set { _DamageMax = value; }
        }

        internal LockType StrLock
        {
            get { return _StrLock; }
            set { _StrLock = value; }
        }

        internal LockType DexLock
        {
            get { return _DexLock; }
            set { _DexLock = value; }
        }

        internal LockType IntLock
        {
            get { return _IntLock; }
            set { _IntLock = value; }
        }

        internal ushort StatCap
        {
            get { return _StatCap; }
            set { _StatCap = value; }
        }

        internal ushort AR
        {
            get { return _AR; }
            set { _AR = value; }
        }

        internal byte Followers
        {
            get { return _Followers; }
            set { _Followers = value; }
        }

        internal byte FollowersMax
        {
            get { return _FollowersMax; }
            set { _FollowersMax = value; }
        }

        internal int Tithe
        {
            get { return _Tithe; }
            set { _Tithe = value; }
        }

        internal Skill[] Skills
        {
            get { return _Skills; }
        }

        internal bool SkillsSent
        {
            get { return _SkillsSent; }
            set { _SkillsSent = value; }
        }

        internal int CriminalTime
        {
            get
            {
                if (_CriminalStart != DateTime.MinValue)
                {
                    int sec = (int)(DateTime.UtcNow - _CriminalStart).TotalSeconds;
                    if (sec > 300)
                    {
                        _CriminalStart = DateTime.MinValue;
                        return 0;
                    }
                    else
                    {
                        return sec;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        //Feature already present in CUO, so we don't really need it ATM
        /*public void TryOpenCorpses()
        {
            if (UOSObjects.Gump.OpenCorpses)
            {
                if ((ProfileManager.Current.CorpseOpenOptions == 1 || ProfileManager.Current.CorpseOpenOptions == 3) && TargetManager.IsTargeting)
                    return;

                if ((ProfileManager.Current.CorpseOpenOptions == 2 || ProfileManager.Current.CorpseOpenOptions == 3) && IsHidden)
                    return;

                foreach (Item item in World.Items)
                {
                    if (!item.IsDestroyed && item.IsCorpse && item.Distance <= ProfileManager.Current.AutoOpenCorpseRange && !AutoOpenedCorpses.Contains(item.Serial))
                    {
                        AutoOpenedCorpses.Add(item.Serial);
                        GameActions.DoubleClickQueued(item.Serial);
                    }
                }
            }
        }*/

        private void AutoOpenDoors(bool onDirChange)
        {
            if (!Engine.Instance.AllowBit(FeatureBit.AutoOpenDoors) || !UOSObjects.Gump.OpenDoors || (!Visible && (UOSObjects.Gump.OpenDoorsMode & 2) != 0) || (Targeting.ServerTarget && (UOSObjects.Gump.OpenDoorsMode & 1) != 0))
                return;

            if (Body != 0x03DB && !IsGhost && !Blessed && ((int)(Direction & AssistDirection.Up)) % 2 == 0)
            {
                int x = Position.X, y = Position.Y, z = Position.Z;

                /* Check if one more tile in the direction we just moved is a door */
                Utility.Offset(Direction, ref x, ref y);

                List<UOItem> doors = UOSObjects.ItemsInRange(1);
                foreach (UOItem s in doors)
                {
                    if (s.IsDoor && s.Position.X == x && s.Position.Y == y && s.Position.Z - 15 <= z && s.Position.Z + 15 >= z)
                    {
                        // ClassicUO requires a slight pause before attempting to
                        // open a door after a direction change
                        if (onDirChange)
                        {
                            Timer.DelayedCallbackState(TimeSpan.FromMilliseconds(5), RequestOpen, s).Start();
                        }
                        else
                        {
                            RequestOpen(s);
                        }
                        break;
                    }
                }
            }
        }

        private void RequestOpen(UOItem item)
        {
            if (UOSObjects.Gump.UseDoors)
            {
                if (item != null)
                    NetClient.Socket.PSend_DoubleClick(item.Serial);
            }
            else
            {
                NetClient.Socket.PSend_OpenDoorMacro();
            }
        }

        internal override void OnPositionChanging(Point3D oldPos)
        {
            if (!IsGhost)
                StealthSteps.OnMove();

            AutoOpenDoors(false);

            List<UOMobile> mlist = new List<UOMobile>(UOSObjects.Mobiles.Values);
            for (int i = 0; i < mlist.Count; i++)
            {
                UOMobile m = mlist[i];
                if (m != this)
                {
                    if (!Utility.InRange(m.Position, Position, VisRange))
                        m.Remove();
                }
            }

            List<UOItem> ilist = new List<UOItem>(UOSObjects.ItemsInRange(Math.Max(VisRange, MultiVisRange), false, true));
            for (int i = 0; i < ilist.Count; i++)
            {
                UOItem item = ilist[i];
                if (item.Deleted || item.Container != null)
                    continue;

                int dist = Utility.Distance(item.GetWorldPosition(), Position);
                if (item != DragDropManager.Holding && (dist > MultiVisRange || (!item.IsMulti && dist > VisRange)))
                    item.Remove();
                else if (!IsGhost && Visible && dist <= 2 && Scavenger.Enabled && item.Movable)
                    Scavenger.Scavenge(item);
            }

            base.OnPositionChanging(oldPos);
        }

        internal override void OnDirectionChanging(AssistDirection oldDir)
        {
            AutoOpenDoors(true);
        }

        internal override void OnMapChange(byte old, byte cur)
        {
            List<UOMobile> list = new List<UOMobile>(UOSObjects.Mobiles.Values);
            for (int i = 0; i < list.Count; i++)
            {
                UOMobile m = list[i];
                if (m != this && m.Map != cur)
                    m.Remove();
            }

            list = null;

            UOSObjects.Items.Clear();
            for (int i = 0; i < Contains.Count; i++)
            {
                UOItem item = (UOItem)Contains[i];
                UOSObjects.AddItem(item);
                item.Contains.Clear();
            }

            if (UOSObjects.Gump.AutoSearchContainers && Backpack != null)
                PlayerData.DoubleClick(Backpack.Serial);
        }

        protected override void OnNotoChange(byte old, byte cur)
        {
            if ((old == 3 || old == 4) && (cur != 3 && cur != 4))
            {
                _CriminalStart = DateTime.MinValue;
            }
            else if ((cur == 3 || cur == 4) && (old != 3 && old != 4 && old != 0))
            {
                // grey is turning on
                ResetCriminalTimer();
            }
        }

        internal void ResetCriminalTimer()
        {
            if (_CriminalStart == DateTime.MinValue || DateTime.UtcNow - _CriminalStart >= TimeSpan.FromSeconds(1))
            {
                _CriminalStart = DateTime.UtcNow;
            }
        }

        internal void SendMessage(int hue, string text)
        {
            ClientPackets.PRecv_UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, hue, 3, "ENU", "System", text);
        }

        internal void SendMessage(MsgLevel lvl, string format, params object[] args)
        {
            SendMessage(lvl, string.Format(format, args));
        }

        internal void SendMessage(string format, params object[] args)
        {
            SendMessage(MsgLevel.Info, string.Format(format, args));
        }

        internal void SendMessage(string text)
        {
            SendMessage(MsgLevel.Info, text);
        }

        internal static int GetColorCode(MsgLevel lvl)
        {
            switch (lvl)
            {
                case MsgLevel.Info:
                    return 0x59;
                case MsgLevel.Friend:
                    return 0x3F;
                case MsgLevel.Advise:
                    return 0x384;
                case MsgLevel.Force:
                    return 0x7E8;
                case MsgLevel.Debug:
                    return 0x90;
                case MsgLevel.Error:
                    return 0x20;
                case MsgLevel.Warning:
                    return 0x35;
                default:
                    return 945;
            }
        }

        internal void SendMessage(MsgLevel lvl, string text)
        {
            if (text.Length > 0)
            {
                int hue = GetColorCode(lvl);

                ClientPackets.PRecv_UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, hue, 3, "ENU", "System", text);
            }
        }

        internal void Say(int hue, string msg, MessageType msgtype = MessageType.Regular)
        {
            NetClient.Socket.PSend_UniEncodedCommandMessage(msgtype, (ushort)hue, 3, msgtype == MessageType.Emote ? $"*{msg}*" : msg);
        }

        internal void Say(string msg)
        {
            Say(ProfileManager.CurrentProfile.SpeechHue, msg);
        }

        internal class GumpData
        {
            //gump ID is univocal, on runuo and servuo it depends on gethashcode, even if gethashcode is not guaranteed to be always the same, it will remain the same as long as the machine doesn't changes or the underlying system won't change
            internal uint GumpID { get; }
            internal uint ServerID { get; }
            internal List<string> GumpStrings { get; }
            internal GumpData(uint gumpid, uint serverid, List<string> strings = null)
            {
                GumpID = gumpid;
                ServerID = serverid;
                GumpStrings = strings;
            }
        }

        internal Dictionary<uint, List<GumpData>> OpenedGumps = new Dictionary<uint, List<GumpData>>();//not saved, on logout all gumps are gone
        internal delegate void ContextQueuedResponse(uint serial, int option);
        internal uint CurrentMenuS;
        internal ushort CurrentMenuI;
        internal bool HasMenu;

        internal bool HasPrompt;
        internal uint PromptSenderSerial;
        internal uint PromptID;
        internal uint PromptType;
        internal string PromptInputText;

        internal void CancelPrompt()
        {
            NetClient.Socket.PSend_PromptResponse(UOSObjects.Player.PromptSenderSerial, UOSObjects.Player.PromptID, 0, string.Empty);
            UOSObjects.Player.HasPrompt = false;
        }

        internal void ResponsePrompt(string text)
        {
            NetClient.Socket.PSend_PromptResponse(UOSObjects.Player.PromptSenderSerial, UOSObjects.Player.PromptID, 1, text);

            PromptInputText = text;
            UOSObjects.Player.HasPrompt = false;
        }

        private ushort _SpeechHue;

        internal ushort SpeechHue
        {
            get { return _SpeechHue; }
            set { _SpeechHue = value; }
        }

        internal sbyte LocalLightLevel
        {
            get { return _LocalLight; }
            set { _LocalLight = value; }
        }

        internal byte GlobalLightLevel
        {
            get { return _GlobalLight; }
            set { _GlobalLight = value; }
        }

        internal enum SeasonFlag
        {
            Spring,
            Summer,
            Fall,
            Winter,
            Desolation
        }

        internal byte Season
        {
            get { return _Season; }
            set { _Season = value; }
        }

        internal byte DefaultSeason
        {
            get { return _DefaultSeason; }
            set { _DefaultSeason = value; }
        }

        /// <summary>
        /// Sets the player's season, set a default to revert back if required
        /// </summary>
        /// <param name="defaultSeason"></param>
        internal void SetSeason(byte defaultSeason = 0)
        {
            UOSObjects.Player.Season = defaultSeason;
            UOSObjects.Player.DefaultSeason = defaultSeason;
        }

        internal static Timer _SeasonTimer = new SeasonTimer();

        private class SeasonTimer : Timer
        {
            internal SeasonTimer() : base(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))
            {
            }

            protected override void OnTick()
            {
                ClientPackets.PRecv_SeasonChange(UOSObjects.Player.Season, true);
                _SeasonTimer.Stop();
            }
        }

        internal ushort Features
        {
            get { return _Features; }
            set { _Features = value; }
        }

        private int _LastSkill = -1;

        internal int LastSkill
        {
            get { return _LastSkill; }
            set { _LastSkill = value; }
        }

        internal uint LastObject { get; set; } = 0;

        private int _LastSpell = -1;

        internal int LastSpell
        {
            get { return _LastSpell; }
            set { _LastSpell = value; }
        }

        internal bool UseItem(UOItem cont, ushort find)
        {
            if (!Engine.Instance.AllowBit(FeatureBit.PotionHotkeys))
                return false;

            for (int i = 0; i < cont.Contains.Count; i++)
            {
                UOItem item = (UOItem)cont.Contains[i];

                if (item.ItemID == find)
                {
                    PlayerData.DoubleClick(item.Serial);
                    return true;
                }
                else if (item.Contains != null && item.Contains.Count > 0)
                {
                    if (UseItem(item, find))
                        return true;
                }
            }

            return false;
        }

        internal static bool DoubleClick(uint s, bool silent = true, bool force = false)
        {
            if (s != 0)
            {
                UOItem free = null, pack = UOSObjects.Player.Backpack;
                if (SerialHelper.IsItem(s) && pack != null && UOSObjects.Gump.HandsBeforePotions && Engine.Instance.AllowBit(FeatureBit.AutoPotionEquip))
                {
                    UOItem i = UOSObjects.FindItem(s);
                    if (i != null && i.IsPotion && i.ItemID != 3853) // dont unequip for exploison potions
                    {
                        // dont worry about uneqipping RuneBooks or SpellBooks
                        UOItem left = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);
                        UOItem right = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);

                        if (left != null && (right != null || left.IsTwoHanded))
                            free = left;
                        else if (right != null && right.IsTwoHanded)
                            free = right;

                        if (free != null)
                        {
                            if (DragDropManager.HasDragFor(free.Serial))
                                free = null;
                            else
                                DragDropManager.DragDrop(free, pack);
                        }
                    }
                }

                if (free != null)
                    DragDropManager.DragDrop(free, UOSObjects.Player, free.Layer, true);
                if(!force)
                    ActionQueue.DoubleClick(s, silent);
                else
                    NetClient.Socket.PSend_DoubleClick(s);

                if (SerialHelper.IsItem(s))
                    UOSObjects.Player.LastObject = s;
            }

            return false;
        }
    }
}
