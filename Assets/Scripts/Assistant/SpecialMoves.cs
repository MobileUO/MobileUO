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
using ClassicUO.Game.Data;

namespace Assistant
{
    public class SpecialMoves
    {
        private class AbilityInfo
        {
            private int[][] _Items;

            public AbilityInfo(Ability ab, params int[][] items)
            {
                Ability = ab;
                _Items = items;
            }

            public Ability Ability { get; }

            public bool HasItem(int item)
            {
                for (int a = 0; a < _Items.Length; a++)
                {
                    for (int b = 0; b < _Items[a].Length; b++)
                    {
                        if (_Items[a][b] == item)
                            return true;
                    }
                }

                return false;
            }
        }

        private static DateTime _LastToggle = DateTime.MinValue;

        private static int[] HatchetID = new int[] { 0xF43, 0xF44 };
        private static int[] LongSwordID = new int[] { 0xF60, 0xF61 };
        private static int[] BroadswordID = new int[] { 0xF5E, 0xF5F };
        private static int[] KatanaID = new int[] { 0x13FE, 0x13FF };
        private static int[] BladedStaffID = new int[] { 0x26BD, 0x26C7 };
        private static int[] HammerPickID = new int[] { 0x143C, 0x143D };
        private static int[] WarAxeID = new int[] { 0x13AF, 0x13B0 };
        private static int[] KryssID = new int[] { 0x1400, 0x1401 };
        private static int[] SpearID = new int[] { 0xF62, 0xF63 };
        private static int[] CompositeBowID = new int[] { 0x26C2, 0x26CC };
        private static int[] CleaverID = new int[] { 0xEC2, 0xEC3 };
        private static int[] LargeBattleAxeID = new int[] { 0x13FA, 0x13FB };
        private static int[] BattleAxeID = new int[] { 0xF47, 0xF48 };
        private static int[] ExecAxeID = new int[] { 0xF45, 0xF46 };
        private static int[] CutlassID = new int[] { 0x1440, 0x1441 };
        private static int[] ScytheID = new int[] { 0x26BA, 0x26C4 };
        private static int[] WarMaceID = new int[] { 0x1406, 0x1407 };
        private static int[] PitchforkID = new int[] { 0xE87, 0xE88 };
        private static int[] WarForkID = new int[] { 0x1404, 0x1405 };
        private static int[] HalberdID = new int[] { 0x143E, 0x143F };
        private static int[] MaulID = new int[] { 0x143A, 0x143B };
        private static int[] MaceID = new int[] { 0xF5C, 0x45D };
        private static int[] GnarledStaffID = new int[] { 0x13F8, 0x13F9 };
        private static int[] QuarterStaffID = new int[] { 0xE89, 0xE8A };
        private static int[] LanceID = new int[] { 0x26C0, 0x26CA };
        private static int[] CrossbowID = new int[] { 0xF4F, 0xF50 };
        private static int[] VikingSwordID = new int[] { 0x13B9, 0x13BA };
        private static int[] AxeID = new int[] { 0xF49, 0xF4A };
        private static int[] ShepherdsCrookID = new int[] { 0xE81, 0xE82 };
        private static int[] SmithsHammerID = new int[] { 0x13EC, 0x13E4 };
        private static int[] WarHammerID = new int[] { 0x1438, 0x1439 };
        private static int[] ScepterID = new int[] { 0x26BC, 0x26C6 };
        private static int[] SledgeHammerID = new int[] { 0xFB4, 0xFB5 };
        private static int[] ButcherKnifeID = new int[] { 0x13F6, 0x13F7 };
        private static int[] PickaxeID = new int[] { 0xE85, 0xE86 };
        private static int[] SkinningKnifeID = new int[] { 0xEC4, 0xEC5 };
        private static int[] WandID = new int[] { 0xDF2, 0xDF3, 0xDF4, 0xDF5 };
        private static int[] BardicheID = new int[] { 0xF4D, 0xF4E };
        private static int[] ClubID = new int[] { 0x13B3, 0x13B4 };
        private static int[] ScimitarID = new int[] { 0x13B5, 0x13B6 };
        private static int[] HeavyCrossbowID = new int[] { 0x13FC, 0x13FD };
        private static int[] TwoHandedAxeID = new int[] { 0x1442, 0x1443 };
        private static int[] DoubleAxeID = new int[] { 0xF4B, 0xF4C };
        private static int[] CrescentBladeID = new int[] { 0x26C1, 0x26C2 };
        private static int[] DoubleBladedStaffID = new int[] { 0x26BF, 0x26C9 };
        private static int[] RepeatingCrossbowID = new int[] { 0x26C3, 0x26CD };
        private static int[] DaggerID = new int[] { 0xF51, 0xF52 };
        private static int[] PikeID = new int[] { 0x26BE, 0x26C8 };
        private static int[] BoneHarvesterID = new int[] { 0x26BB, 0x26C5 };
        private static int[] ShortSpearID = new int[] { 0x1402, 0x1403 };
        private static int[] BowID = new int[] { 0x13B1, 0x13B2 };
        private static int[] BlackStaffID = new int[] { 0xDF0, 0xDF1 };
        private static int[] FistsID = new int[] { 0 };

        private static AbilityInfo[] _Primary = new AbilityInfo[]
        {
            new AbilityInfo(Ability.ArmorIgnore, HatchetID, LongSwordID, BladedStaffID, HammerPickID, WarAxeID,
                KryssID, SpearID, CompositeBowID),
            new AbilityInfo(Ability.BleedAttack, CleaverID, BattleAxeID, ExecAxeID, CutlassID, ScytheID, PitchforkID,
                WarForkID),
            new AbilityInfo(Ability.ConcussionBlow, MaceID, GnarledStaffID, CrossbowID),
            new AbilityInfo(Ability.CrushingBlow, VikingSwordID, AxeID, BroadswordID, ShepherdsCrookID,
                SmithsHammerID, MaulID, WarMaceID, ScepterID, SledgeHammerID),
            new AbilityInfo(Ability.Disarm, FistsID),
            new AbilityInfo(Ability.Dismount, WandID, LanceID),
            new AbilityInfo(Ability.DoubleStrike, PickaxeID, TwoHandedAxeID, DoubleAxeID, ScimitarID, KatanaID,
                CrescentBladeID, QuarterStaffID, DoubleBladedStaffID, RepeatingCrossbowID),
            new AbilityInfo(Ability.InfectiousStrike, ButcherKnifeID, DaggerID),
            //new AbilityInfo( Ability.MortalStrike ), // not primary for anything
            new AbilityInfo(Ability.MovingShot, HeavyCrossbowID),
            new AbilityInfo(Ability.ParalyzingBlow, BardicheID, BoneHarvesterID, PikeID, BowID),
            new AbilityInfo(Ability.ShadowStrike, SkinningKnifeID, ClubID, ShortSpearID),
            new AbilityInfo(Ability.WhirlwindAttack, LargeBattleAxeID, HalberdID, WarHammerID, BlackStaffID)
        };

        private static AbilityInfo[] _Secondary = new AbilityInfo[]
        {
            new AbilityInfo(Ability.ArmorIgnore, LargeBattleAxeID, BroadswordID, KatanaID),
            new AbilityInfo(Ability.BleedAttack, WarMaceID, WarAxeID),
            new AbilityInfo(Ability.ConcussionBlow, LongSwordID, BattleAxeID, HalberdID, MaulID, QuarterStaffID,
                LanceID),
            new AbilityInfo(Ability.CrushingBlow, WarHammerID),
            new AbilityInfo(Ability.Disarm, ButcherKnifeID, PickaxeID, SkinningKnifeID, HatchetID, WandID,
                ShepherdsCrookID, MaceID, WarForkID),
            new AbilityInfo(Ability.Dismount, BardicheID, AxeID, BladedStaffID, ClubID, PitchforkID,
                HeavyCrossbowID),
            //new AbilityInfo( Ability.DoubleStrike ), // secondary on none
            new AbilityInfo(Ability.InfectiousStrike, CleaverID, PikeID, KryssID, DoubleBladedStaffID),
            new AbilityInfo(Ability.MortalStrike, ExecAxeID, BoneHarvesterID, CrescentBladeID, HammerPickID,
                ScepterID, ShortSpearID, CrossbowID, BowID),
            new AbilityInfo(Ability.MovingShot, CompositeBowID, RepeatingCrossbowID),
            new AbilityInfo(Ability.ParalyzingBlow, VikingSwordID, ScimitarID, ScytheID, GnarledStaffID,
                BlackStaffID, SpearID, FistsID),
            new AbilityInfo(Ability.ShadowStrike, TwoHandedAxeID, CutlassID, SmithsHammerID, DaggerID,
                SledgeHammerID),
            new AbilityInfo(Ability.WhirlwindAttack, DoubleAxeID)
        };

        internal static void OnStun()
        {
            if (_LastToggle + TimeSpan.FromSeconds(0.5) < DateTime.UtcNow)
            {
                _LastToggle = DateTime.UtcNow;
                NetClient.Socket.PSend_StunDisarm(true);
            }
        }

        internal static void OnDisarm()
        {
            if (_LastToggle + TimeSpan.FromSeconds(0.5) < DateTime.UtcNow)
            {
                _LastToggle = DateTime.UtcNow;
                NetClient.Socket.PSend_StunDisarm(false);
            }
        }

        private static Ability GetAbility(int item, AbilityInfo[] list)
        {
            for (int a = 0; a < list.Length; a++)
            {
                if (list[a].HasItem(item))
                    return list[a].Ability;
            }

            return Ability.Invalid;
        }

        public static void SetPrimaryAbility()
        {
            UOItem right = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);
            UOItem left = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);

            Ability a = Ability.Invalid;
            if (right != null)
                a = GetAbility(right.ItemID, _Primary);

            if (a == Ability.Invalid && left != null)
                a = GetAbility(left.ItemID, _Primary);

            if (a == Ability.Invalid)
                a = GetAbility(FistsID[0], _Primary);

            if (a != Ability.Invalid)
            {
                ClientPackets.PRecv_ClearAbility();
                NetClient.Socket.PSend_UseAbility(a);
                UOSObjects.Player.SendMessage($"Setting ability: {a}");
            }
        }

        public static void SetSecondaryAbility()
        {
            UOItem right = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);
            UOItem left = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);

            Ability a = Ability.Invalid;
            if (right != null)
                a = GetAbility(right.ItemID, _Secondary);

            if (a == Ability.Invalid && left != null)
                a = GetAbility(left.ItemID, _Secondary);

            if (a == Ability.Invalid)
                a = GetAbility(FistsID[0], _Secondary);

            if (a != Ability.Invalid)
            {
                ClientPackets.PRecv_ClearAbility();
                NetClient.Socket.PSend_UseAbility(a);
                UOSObjects.Player.SendMessage($"Setting ability: {a}");
            }
        }

        public static void ClearAbilities()
        {
            ClientPackets.PRecv_ClearAbility();
            NetClient.Socket.PSend_UseAbility(Ability.None);
            UOSObjects.Player.SendMessage("Abilities cleared");
        }
    }
}
