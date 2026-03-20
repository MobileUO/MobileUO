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

using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using SpellFlag = ClassicUO.Game.Managers.TargetType;

namespace Assistant
{
    internal class Spell
    {
        readonly public SpellFlag Flag;
        readonly public int Circle;
        readonly public int Number;
        readonly public string WordsOfPower;
        readonly public string[] Reagents;

        public Spell(int flag, int n, int c, string power, List<Reagents> reags)
        {
            Flag = (SpellFlag)flag;
            Number = n;
            Circle = c;
            WordsOfPower = power;
            Reagents = new string[reags.Count];
            for (int i = 0; i < reags.Count; i++)
                Reagents[i] = StringHelper.AddSpaceBeforeCapital(reags[i].ToString());
        }

        public int Name
        {
            get
            {
                if (Circle <= 8) // Mage
                    return 3002011 + ((Circle - 1) * 8) + (Number - 1);
                else if (Circle == 10) // Necr
                    return 1060509 + Number - 1;
                else if (Circle == 20) // Chiv
                    return 1060585 + Number - 1;
                else if (Circle == 40) // Bush
                    return 1060595 + Number - 1;
                else if (Circle == 50) // Ninj
                    return 1060610 + Number - 1;
                else if (Circle == 60) // Elfs
                    return 1071026 + Number - 1;
                else
                    return -1;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} (#{1})", SpellDefinition.FullIndexGetSpell(Number).Name, Number);
        }

        public int GetID()
        {
            return Number;
        }

        public static void FullCast(int spellid)
        {
            Cast(spellid);
            NetClient.Socket.PSend_CastSpell(spellid);
        }

        private static void Cast(int spellid)
        {
            if (UOSObjects.Gump.HandsBeforeCasting && Engine.Instance.AllowBit(FeatureBit.UnequipBeforeCast))
            {
                UOItem pack = UOSObjects.Player.Backpack;
                if (pack != null)
                {
                    // dont worry about unequipping RuneBooks or SpellBooks
                    UOItem item = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);

					if ( item != null && item.ItemID != 0x22C5 && item.ItemID != 0xE3B && item.ItemID != 0xEFA )
                    {
                        DragDropManager.Drag(item, item.Amount);
                        DragDropManager.Drop(item, pack);
                    }

                    item = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);

					if ( item != null && item.ItemID != 0x22C5 && item.ItemID != 0xE3B && item.ItemID != 0xEFA )
                    {
                        DragDropManager.Drag(item, item.Amount);
                        DragDropManager.Drop(item, pack);
                    }
                }
            }

            if (UOSObjects.Player != null)
            {
                UOSObjects.Player.LastSpell = spellid;
                LastCastTime = DateTime.UtcNow;
                Targeting.SpellTargetID = 0;
            }
        }

        public static DateTime LastCastTime = DateTime.MinValue;

        internal static Dictionary<int, Spell> SpellsByID { get; } = new Dictionary<int, Spell>();
        internal static Dictionary<string, Spell> SpellsByName { get; } = new Dictionary<string, Spell>();

        static Spell()
        {
            
        }

        public static void HealOrCureSelf()
        {
            Spell s = null;

            if (!Engine.Instance.AllowBit(FeatureBit.BlockHealPoisoned))
            {
                if (UOSObjects.Player.Hits + 30 < UOSObjects.Player.HitsMax && UOSObjects.Player.Mana >= 12)
                    s = Get(4, 5); // greater heal
                else
                    s = Get(1, 4); // mini heal
            }
            else
            {
                if (UOSObjects.Player.Poisoned && Engine.Instance.AllowBit(FeatureBit.BlockHealPoisoned))
                {
                    s = Get(2, 3); // cure 
                }
                else if (UOSObjects.Player.Hits + 2 < UOSObjects.Player.HitsMax)
                {
                    if (UOSObjects.Player.Hits + 30 < UOSObjects.Player.HitsMax && UOSObjects.Player.Mana >= 12)
                        s = Get(4, 5); // greater heal
                    else
                        s = Get(1, 4); // mini heal
                }
                else
                {
                    if (UOSObjects.Player.Mana >= 12)
                        s = Get(4, 5); // greater heal
                    else
                        s = Get(1, 4); // mini heal
                }
            }

            if (s != null)
            {
                if (UOSObjects.Player.Poisoned || UOSObjects.Player.Hits < UOSObjects.Player.HitsMax)
                    Targeting.TargetSelf(true);
                FullCast(s.Number);
            }
        }

        public static void MiniHealOrCureSelf()
        {
            Spell s;

            if (!Engine.Instance.AllowBit(FeatureBit.BlockHealPoisoned))
            {
                s = Get(1, 4); // mini heal
            }
            else
            {
                if (UOSObjects.Player.Poisoned)
                    s = Get(2, 3); // cure
                else
                    s = Get(1, 4); // mini heal
            }

            if (s != null)
            {
                if (UOSObjects.Player.Poisoned || UOSObjects.Player.Hits < UOSObjects.Player.HitsMax)
                    Targeting.TargetSelf(true);
                FullCast(s.Number);
            }
        }

        public static void GHealOrCureSelf()
        {
            Spell s = null;

            if (!Engine.Instance.AllowBit(FeatureBit.BlockHealPoisoned))
            {
                s = Get(4, 5); // gheal
            }
            else
            {
                if (UOSObjects.Player.Poisoned)
                    s = Get(2, 3); // cure
                else
                    s = Get(4, 5); // gheal
            }

            if (s != null)
            {
                if (UOSObjects.Player.Poisoned || UOSObjects.Player.Hits < UOSObjects.Player.HitsMax)
                    Targeting.TargetSelf(true);
                FullCast(s.Number);
            }
        }

        public static void Interrupt()
        {
            UOItem item = FindUsedLayer();

            if (item != null)
            {
                NetClient.Socket.PSend_LiftRequest(item.Serial, 1);
                NetClient.Socket.PSend_EquipRequest(item.Serial, UOSObjects.Player.Serial, item.Layer);
            }
        }

        private static UOItem FindUsedLayer()
        {
            UOItem layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Shirt);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Shoes);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Pants);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Helmet);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Gloves);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Ring);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Necklace);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Waist);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Torso);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Bracelet);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Tunic);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Earrings);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Arms);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Cloak);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Robe);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Skirt);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.Legs);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);
            if (layeredItem != null)
                return layeredItem;

            layeredItem = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);
            if (layeredItem != null)
                return layeredItem;

            return null;
        }

        public static void OnHotKey(ushort id)
        {
            Spell s = Spell.Get(id);
            if (s != null)
            {
                Spell.FullCast(id);//.OnCast(new CastSpellFromMacro(id));
                //if ( Macros.MacroManager.AcceptActions )
                //	Macros.MacroManager.Action( new Macros.MacroCastSpellAction( s ) );
            }
        }

        public static int ToID(int circle, int num)
        {
            if (circle < 10)
                return ((circle - 1) * 8) + num;
            else
                return (circle * 10) + num;
        }

        public static Spell Get(int num)
        {
            Spell s;
            SpellsByID.TryGetValue(num, out s);
            return s;
        }

        public static Spell GetByName(string name)
        {
            SpellsByName.TryGetValue(name.ToLower(), out Spell s);
            return s;
        }

        public static string GetName(int num)
        {
            var res = SpellsByName.FirstOrDefault(kvp => kvp.Value.Number == num);
            if (res.Key == null)
                return SpellsByName.First().Key;
            return res.Key;
        }

        public static Spell Get(int circle, int num)
        {
            return Get(Spell.ToID(circle, num));
        }
    }
}
