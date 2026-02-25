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

using ClassicUO.Game.Data;

namespace Assistant.Core
{
    internal static class Dress
    {
        private static UOItem _Right, _Left;

        public static void ToggleRight(bool quiet = false)
        {
            if (UOSObjects.Player == null)
                return;

            UOItem item = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);
            if (item == null)
            {
                if (_Right != null)
                    _Right = UOSObjects.FindItem(_Right.Serial);

                if (_Right != null && _Right.IsChildOf(UOSObjects.Player.Backpack))
                {
                    // try to also undress conflicting hand(s)
                    UOItem conflict = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);
                    if (conflict != null && (conflict.IsTwoHanded || _Right.IsTwoHanded))
                    {
                        Unequip(DressList.GetLayerFor(conflict));
                    }

                    Equip(_Right, DressList.GetLayerFor(_Right));
                }
                else if(!quiet)
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Force, "You must disarm something before you can arm it");
                }
            }
            else
            {
                Unequip(DressList.GetLayerFor(item));
                _Right = item;
            }
        }

        public static void ToggleLeft(bool quiet = false)
        {
            if (UOSObjects.Player == null || UOSObjects.Player.Backpack == null)
                return;

            UOItem item = UOSObjects.Player.GetItemOnLayer(Layer.TwoHanded);
            if (item == null)
            {
                if (_Left != null)
                    _Left = UOSObjects.FindItem(_Left.Serial);

                if (_Left != null && _Left.IsChildOf(UOSObjects.Player.Backpack))
                {
                    UOItem conflict = UOSObjects.Player.GetItemOnLayer(Layer.OneHanded);
                    if (conflict != null && (conflict.IsTwoHanded || _Left.IsTwoHanded))
                    {
                        Unequip(DressList.GetLayerFor(conflict));
                    }

                    Equip(_Left, DressList.GetLayerFor(_Left));
                }
                else if (!quiet)
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Force, "You must disarm something before you can arm it");
                }
            }
            else
            {
                Unequip(DressList.GetLayerFor(item));
                _Left = item;
            }
        }

        public static bool Equip(UOItem item, Layer layer, bool force = false)
        {
            if (layer == Layer.Invalid || layer >= Layer.Mount || item == null || item.Layer == Layer.Invalid ||
                item.Layer >= Layer.Mount)
                return false;

            if (item != null && UOSObjects.Player != null && item.IsChildOf(UOSObjects.Player.Backpack))
            {
                DragDropManager.DragDrop(item, UOSObjects.Player, layer, force);
                return true;
            }

            return false;
        }

        public static bool Unequip(Layer layer)
        {
            if (layer == Layer.Invalid || layer >= Layer.Mount)
                return false;

            UOItem item = UOSObjects.Player.GetItemOnLayer(layer);
            if (item != null)
            {
                UOItem pack = DressList.FindUndressBag(item);
                if (pack != null)
                {
                    DragDropManager.DragDrop(item, pack);
                    return true;
                }
            }

            return false;
        }
    }
}
