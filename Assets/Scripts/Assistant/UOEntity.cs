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
using System.Collections.Generic;

using ClassicUO.Network;
using ClassicUO.Game;
using ClassicUO.IO;

namespace Assistant
{
    internal class UOEntity
    {
        internal class ContextMenuList : List<KeyValuePair<ushort, ushort>>
        {
            internal void Add(ushort key, ushort value)
            {
                var element = new KeyValuePair<ushort, ushort>(key, value);
                Add(element);
            }
        }

        private uint _Serial;
        private Point3D _Pos;
        private ushort _Hue;
        private bool _Deleted;
        private ContextMenuList _ContextMenu = new ContextMenuList();
        protected ObjectPropertyList _ObjPropList = null;

        internal ObjectPropertyList ObjPropList
        {
            get { return _ObjPropList; }
        }

        internal UOEntity(uint ser)
        {
            _ObjPropList = new ObjectPropertyList(this);

            _Serial = ser;
            _Deleted = false;
        }

        internal uint Serial
        {
            get { return _Serial; }
        }

        internal virtual Point3D Position
        {
            get { return _Pos; }
            set
            {
                if (value != _Pos)
                {
                    var oldPos = _Pos;
                    _Pos = value;
                    OnPositionChanging(oldPos);
                }
            }
        }

        internal virtual Point3D WorldPosition => Position;

        internal bool Deleted
        {
            get { return _Deleted; }
        }

        internal ContextMenuList ContextMenu
        {
            get { return _ContextMenu; }
        }

        internal virtual ushort Hue
        {
            get { return _Hue; }
            set { _Hue = value; }
        }

        internal virtual void Remove()
        {
            _Deleted = true;
        }

        internal virtual void OnPositionChanging(Point3D oldPos)
        {
        }

        public double GetDistanceToSqrt(UOEntity e)
        {
            int xDelta = WorldPosition._X - e.WorldPosition._X;
            int yDelta = WorldPosition._Y - e.WorldPosition._Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public override int GetHashCode()
        {
            return (int)_Serial;
        }

        internal uint OPLHash
        {
            get
            {
                if (_ObjPropList != null)
                    return _ObjPropList.Hash;
                else
                    return 0;
            }
            set
            {
                if (_ObjPropList != null)
                    _ObjPropList.Hash = value;
            }
        }

        internal virtual ushort Graphic => 0;

        internal bool ModifiedOPL
        {
            get { return _ObjPropList.Customized; }
        }

        internal void ReadPropertyList(ref StackDataReader p, out string name)
        {
            _ObjPropList.Read(p, out name);
        }

        internal void OPLChanged()
        {
            ClientPackets.PRecv_OPLInfo(Serial, OPLHash);
        }

        internal virtual string GetName()
        {
            return null;
        }

        internal bool Equals(UOEntity other)
        {
            return other != null && other._Serial == _Serial;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as UOEntity);
        }
    }
}
