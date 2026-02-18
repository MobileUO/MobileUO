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
using ClassicUO.Utility.Logging;
using ClassicUO.Network;
using ClassicUO.IO;
using Assistant.IO;
using System.Net.Sockets;
using ClassicUO;

namespace Assistant
{
    internal delegate void PacketViewerCallback(ref StackDataReader reader, PacketHandlerEventArgs args);
    internal delegate void PacketFilterCallback(ref StackDataFixedReadWrite rw, PacketHandlerEventArgs args);


    internal class PacketHandlerEventArgs
	{
		internal bool Block { get; set; }

		internal PacketHandlerEventArgs()
		{
			Reinit();
		}

		internal void Reinit()
		{
			Block = false;
		}
	}

	internal class PacketHandler
	{

        private static Dictionary<int, List<PacketViewerCallback>> _ClientViewers;
        private static Dictionary<int, List<PacketViewerCallback>> _ServerViewers;

        private static Dictionary<int, List<PacketFilterCallback>> _ClientFilters;
        private static Dictionary<int, List<PacketFilterCallback>> _ServerFilters;

        static PacketHandler()
		{
            _ClientViewers = new Dictionary<int, List<PacketViewerCallback>>();
            _ServerViewers = new Dictionary<int, List<PacketViewerCallback>>();

            _ClientFilters = new Dictionary<int, List<PacketFilterCallback>>();
            _ServerFilters = new Dictionary<int, List<PacketFilterCallback>>();
        }

		internal static void RegisterClientToServerViewer(int packetID, PacketViewerCallback callback)
		{
			if (!_ClientViewers.TryGetValue(packetID, out List<PacketViewerCallback> list) || list == null)
				_ClientViewers[packetID] = list = new List<PacketViewerCallback>();
			list.Add(callback);
		}

		internal static void RegisterServerToClientViewer(int packetID, PacketViewerCallback callback)
		{
			if (!_ServerViewers.TryGetValue(packetID, out List<PacketViewerCallback> list) || list == null)
				_ServerViewers[packetID] = list = new List<PacketViewerCallback>();
			list.Add(callback);
		}

		internal static void RemoveClientToServerViewer(int packetID, PacketViewerCallback callback)
		{
			if (_ClientViewers.TryGetValue(packetID, out List<PacketViewerCallback> list) && list != null)
				list.Remove(callback);
		}

		internal static void RemoveServerToClientViewer(int packetID, PacketViewerCallback callback)
		{
			if (_ServerViewers.TryGetValue(packetID, out List<PacketViewerCallback> list) && list != null)
				list.Remove(callback);
		}

		internal static void RegisterClientToServerFilter(int packetID, PacketFilterCallback callback)
		{
			if (!_ClientFilters.TryGetValue(packetID, out List<PacketFilterCallback> list) || list == null)
				_ClientFilters[packetID] = list = new List<PacketFilterCallback>();
			list.Add(callback);
		}

		internal static void RegisterServerToClientFilter(int packetID, PacketFilterCallback callback)
		{
			if (!_ServerFilters.TryGetValue(packetID, out List<PacketFilterCallback> list) || list == null)
				_ServerFilters[packetID] = list = new List<PacketFilterCallback>();
			list.Add(callback);
		}

		internal static void RemoveClientToServerFilter(int packetID, PacketFilterCallback callback)
		{
			if (_ClientFilters.TryGetValue(packetID, out List<PacketFilterCallback> list) && list != null)
				list.Remove(callback);
		}

		internal static void RemoveServerToClientFilter(int packetID, PacketFilterCallback callback)
		{
			if (_ServerFilters.TryGetValue(packetID, out List<PacketFilterCallback> list) && list != null)
				list.Remove(callback);
		}

		internal static bool OnServerPacket(int id, ref Span<byte> p, ref int length, PacketAction pkta)
		{
			bool result = false;
			if ((pkta & PacketAction.Viewer) == PacketAction.Viewer)
			{
                var reader = new StackDataReader(p);
				if (_ServerViewers.TryGetValue(id, out List<PacketViewerCallback> list) && list != null && list.Count > 0)
					result = ProcessViewers(list, ref reader);
			}
			if((pkta & PacketAction.Filter) == PacketAction.Filter)
			{
                var readwriter = new StackDataFixedReadWrite(ref p);
                if (_ServerFilters.TryGetValue(id, out List<PacketFilterCallback> list) && list != null && list.Count > 0)
					result |= ProcessFilters(list, ref readwriter);
			}

			return result;
		}

        internal static bool OnServerPacketViewer(int id, ref StackDataReader reader)
        {
            if (_ServerViewers.TryGetValue(id, out List<PacketViewerCallback> list) && list != null)
            {
                return ProcessViewers(list, ref reader);
            }

            return false;
        }

        internal static bool OnClientPacket(int id, ref Span<byte> data, PacketAction pkta)
		{
			bool result = false;
			if ((pkta & PacketAction.Viewer) == PacketAction.Viewer)
			{
                StackDataReader reader = new StackDataReader(data);
				if (_ClientViewers.TryGetValue(id, out List<PacketViewerCallback> list) && list != null && list.Count > 0)
					result = ProcessViewers(list, ref reader);
			}
			if ((pkta & PacketAction.Filter) == PacketAction.Filter)
			{
                var readwriter = new StackDataFixedReadWrite(ref data);
                if (_ClientFilters.TryGetValue(id, out List<PacketFilterCallback> list) && list != null && list.Count > 0)
					result |= ProcessFilters(list, ref readwriter);
			}

			return result;
		}

		internal static PacketAction HasClientViewerFilter(int packetID)
		{
			PacketAction pkt = PacketAction.None;
			if (_ClientViewers.TryGetValue(packetID, out List<PacketViewerCallback> flist) && flist != null && flist.Count > 0)
				pkt |= PacketAction.Viewer;
			if (_ClientFilters.TryGetValue(packetID, out List<PacketFilterCallback> list) && list != null && list.Count > 0)
				pkt |= PacketAction.Filter;
			return pkt;
		}

		internal static PacketAction HasServerViewerFilter(int packetID)
		{
			PacketAction pkt = PacketAction.None;
			if (_ServerViewers.TryGetValue(packetID, out List<PacketViewerCallback> list) && list != null && list.Count > 0)
				pkt |= PacketAction.Viewer;
			if (_ServerFilters.TryGetValue(packetID, out List<PacketFilterCallback> flist) && flist != null && flist.Count > 0)
				pkt |= PacketAction.Filter;
			
			return pkt;
		}

		private static PacketHandlerEventArgs _Args = new PacketHandlerEventArgs();

        private static bool ProcessViewers(List<PacketViewerCallback> list, ref StackDataReader reader)
        {
            _Args.Reinit();

            int count = list.Count;
            int datastart = NetClient.Socket.PacketsTable.GetPacketLength(reader.ReadUInt8()) == -1 ? 3 : 1;
            for (int i = 0; i < count; ++i)
            {
                reader.Seek(datastart);
                list[i](ref reader, _Args);
            }

            return _Args.Block;
        }

        private static bool ProcessFilters(List<PacketFilterCallback> list, ref StackDataFixedReadWrite rw)
		{
			_Args.Reinit();

            int count = list.Count;
            int datastart = NetClient.Socket.PacketsTable.GetPacketLength(rw.ReadUInt8()) == -1 ? 3 : 1;
            if (list != null)
			{
				for (int i = 0; i < count; ++i)
				{
                    rw.Seek(datastart);
					list[i](ref rw, _Args);
				}
			}

			return _Args.Block;
		}
	}
}
