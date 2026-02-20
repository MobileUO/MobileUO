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
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using CUO_API;
using ClassicUO.Network;
using ClassicUO;
using ClassicUO.Utility;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using Assistant.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using UOScript;

namespace Assistant
{
    public class FeatureBit
    {
        public static readonly int WeatherFilter = 0;
        public static readonly int LightFilter = 1;
        public static readonly int SmartLT = 2;
        public static readonly int RangeCheckLT = 3;
        public static readonly int AutoOpenDoors = 4;
        public static readonly int UnequipBeforeCast = 5;
        public static readonly int AutoPotionEquip = 6;
        public static readonly int BlockHealPoisoned = 7;
        public static readonly int LoopingMacros = 8; // includes fors and macros running macros
        public static readonly int UseOnceAgent = 9;
        public static readonly int RestockAgent = 10;
        public static readonly int SellAgent = 11;
        public static readonly int BuyAgent = 12;
        public static readonly int PotionHotkeys = 13;
        public static readonly int RandomTargets = 14;
        public static readonly int ClosestTargets = 15;
        public static readonly int OverheadHealth = 16;
        public static readonly int AutolootAgent = 17;
        public static readonly int BoneCutterAgent = 18;
        public static readonly int AdvancedMacros = 19;//we can't use any scripting language here
        public static readonly int AutoRemount = 20;
        public static readonly int AutoBandage = 21;
        public static readonly int EnemyTargetShare = 22;
        public static readonly int FilterSeason = 23;
        public static readonly int SpellTargetShare = 24;
        public static readonly int HumanoidHealthChecks = 25;
        public static readonly int SpeechJournalChecks = 26;

        public static readonly int MaxBit = 26;
    }

    internal enum ContainerType
    {
        None,
        Ground,
        Serial,
        Any
    }

    internal static class UOSObjects
    {
        public static bool GumpLoaded { get; private set; } = false;
        public static bool GumpIsLoading { get; set; }

        private static ClassicUO.Game.UI.Gumps.AssistantGump _Gump;

        internal static ClassicUO.Game.UI.Gumps.AssistantGump Gump
        {
            get
            {
                if (_Gump == null || _Gump.IsDisposed)
                {
                    GumpIsLoading = true;
                    _Gump = new ClassicUO.Game.UI.Gumps.AssistantGump();
                    GumpIsLoading = false;
                    AfterBuild();
                }
                return _Gump;
            }
            set
            {
                if (_Gump != value)
                {
                    ClassicUO.Game.UI.Gumps.AssistantGump old = _Gump;
                    _Gump = value;
                    GumpLoaded = false;
                    old?.Dispose();
                    if(_Gump != null)
                        AfterBuild();
                }
            }
        }

        internal static void AfterBuild(byte itr = 0)
        {
            if (_Gump != null && !_Gump.IsDisposed)
            {
                UOSObjects.GumpIsLoading = true;
                _Gump.LoadConfig();
                XmlFileParser.LoadPrivate(_Gump);
                XmlFileParser.LoadProfile(_Gump, _Gump.ProfileSelected);
                UOSObjects.GumpIsLoading = false;
                GumpLoaded = true;
            }
            else if(itr < 10)
                Timer.DelayedCallbackState(TimeSpan.FromMilliseconds(200), AfterBuild, ++itr).Start();
        }

        internal static Dictionary<uint, UOItem> Items { get; } = new Dictionary<uint, UOItem>();
        internal static Dictionary<uint, UOMobile> Mobiles { get; } = new Dictionary<uint, UOMobile>();

        internal static void ClearAll()
        {
            Items.Clear();
            Mobiles.Clear();
            GumpLoaded = false;
        }

        internal static PlayerData Player;

        internal static string OrigPlayerName
        {
            get;
            set;
        }

        internal static UOItem FindItem(uint serial)
        {
            Items.TryGetValue(serial, out UOItem it);
            return it;
        }

        internal static UOItem FindItemByType(int itemId, int color = -1, int range = -1, ContainerType cnttype = ContainerType.Any)
        {
            List<UOItem> list = new List<UOItem>();
            foreach (UOItem item in Items.Values)
            {
                if (item.ItemID == itemId && (color == -1 || item.Hue == color) && (range == -1 || Utility.InRange(Player.Position, item.WorldPosition, range)) && (cnttype == ContainerType.Any || (cnttype == ContainerType.Ground && item.OnGround) || (cnttype == ContainerType.Serial && item.Container != null)))
                    list.Add(item);
            }

            list.RemoveAll(i => Scripts.Expressions.IgnoredObjects.Contains(i.Serial));

            return GetObjectInList(list);
        }

        private static int _Pos = 0;
        internal static void ResetPos()
        {
            _Pos = 0;
        }

        internal static T GetObjectInList<T>(List<T> list)
        {
            if (list.Count < 1)
            {
                return default;
            }

            _Pos = Math.Min(_Pos, list.Count - 1);
            T found = list[_Pos];
            
            ++_Pos;
            if(_Pos >= list.Count)
            {
                _Pos = 0;
            }

            return found;
        }

        internal static List<UOItem> FindItemsByTypes(HashSet<ushort> itemIds, int color = -1, int range = -1, ContainerType cnttype = ContainerType.Any)
        {
            List<UOItem> list = new List<UOItem>();

            Parallel.ForEach(Items.Values, item =>
            {
                if (itemIds.Contains(item.ItemID) && (color == -1 || item.Hue == color) && (range == -1 || Utility.InRange(Player.Position, item.WorldPosition, range)) && (cnttype == ContainerType.Any || (cnttype == ContainerType.Ground && item.OnGround) || (cnttype == ContainerType.Serial && item.Container != null)))
                    list.Add(item);
            });

            list.RemoveAll(i => Scripts.Expressions.IgnoredObjects.Contains(i.Serial));

            return list;
        }

        internal static List<UOItem> FindItemsByName(string name)
        {
            List<UOItem> items = new List<UOItem>();

            Parallel.ForEach(Items.Values, item =>
            {
                if (item.DisplayName.ToLower().StartsWith(name.ToLower()))
                    items.Add(item);
            });

            return items;
        }

        internal static List<UOMobile> FindMobilesByName(string name)
        {
            List<UOMobile> mobiles = new List<UOMobile>();

            Parallel.ForEach(Mobiles.Values, mobile =>
            {
                if (mobile.Name != null && mobile.Name.ToLower().Equals(name.ToLower()))
                    mobiles.Add(mobile);
            });

            return mobiles;
        }

        internal static UOMobile FindMobile(uint serial)
        {
            Mobiles.TryGetValue(serial, out UOMobile m);
            return m;
        }

        internal static UOEntity FindEntity(uint serial)
        {
            if (Mobiles.TryGetValue(serial, out UOMobile m))
                return m;
            if (Items.TryGetValue(serial, out UOItem i))
                return i;
            return null;
        }

        internal static UOEntity FindEntityByType(int graphic, int hue = -1, int range = 24)
        {
            List<UOEntity> list = new List<UOEntity>();
            foreach (UOEntity ie in EntitiesInRange(range, false))
            {
                if(ie.Graphic == graphic && (hue == -1 || hue  == ie.Hue))
                    list.Add(ie);
            }

            return GetObjectInList(list);
        }

        internal static List<UOEntity> FindEntitiesByType(int graphic, int hue = -1, int range = 24, bool contained = true)
        {
            List<UOEntity> list = new List<UOEntity>();
            foreach (UOEntity ie in EntitiesInRange(range, false, contained))
            {
                if (ie.Graphic == graphic && (hue == -1 || hue == ie.Hue))
                    list.Add(ie);
            }

            return list;
        }

        internal static List<UOEntity> EntitiesInRange(int range, bool restrictrange = true, bool contained = true)
        {
            List<UOEntity> list = new List<UOEntity>();

            if (UOSObjects.Player == null || range < 0)
                return list;

            foreach (UOItem i in Items.Values)
            {
                if (Utility.InRange(Player.Position, i.GetWorldPosition(), restrictrange ? Math.Min(range, Client.Game.UO.World.ClientViewRange) : range) && (contained || i.OnGround))
                    list.Add(i);
            }
            foreach (UOMobile m in Mobiles.Values)
            {
                if (Utility.InRange(UOSObjects.Player.Position, m.Position, restrictrange ? Math.Min(range, Client.Game.UO.World.ClientViewRange) : range))
                    list.Add(m);
            }

            list.RemoveAll(e => Scripts.Expressions.IgnoredObjects.Contains(e.Serial));

            return list;
        }

        internal static List<UOItem> ItemsInRange()
        {
            return ItemsInRange(Client.Game.UO.World.ClientViewRange);
        }

        internal static List<UOItem> ItemsInRange(int range, bool restrictrange = true, bool sort = false)
        {
            List<UOItem> list = new List<UOItem>();

            if (Player == null)
                return list;

            foreach(UOItem i in Items.Values)
            {
                if (Utility.InRange(Player.Position, i.GetWorldPosition(), restrictrange ? Math.Min(range, Client.Game.UO.World.ClientViewRange) : range))
                    list.Add(i);
            }

            if(sort)
                list.Sort(Targeting.Instance);

            return list;
        }

        internal static List<UOMobile> MobilesInRange(int range, bool restrictrange = true, bool sort = false)
        {
            List<UOMobile> list = new List<UOMobile>();

            if (Player == null)
                return list;

            foreach (UOMobile m in Mobiles.Values)
            {
                if (Utility.InRange(Player.Position, m.Position, restrictrange ? Math.Min(range, Client.Game.UO.World.ClientViewRange) : range))
                    list.Add(m);
            }
            list.Remove(Player);

            if (sort)
                list.Sort(Targeting.Instance);

            return list;
        }

        internal static ushort GetTileNear(int x, int y, int z)
        {
            const int DIFF = 10;
            Chunk chunk = Client.Game.UO.World.Map.GetChunk(x, y);
            if (chunk == null) return 0;

            ClassicUO.Game.GameObjects.GameObject gameObject = chunk.GetHeadObject(x % 8, y % 8);

            ushort closestGraphic = 0;
            int minDiff = int.MaxValue;

            while (gameObject != null)
            {
                int currentDiff = Math.Abs(gameObject.Z - z);

                if (gameObject is Static st)
                {
                    if (currentDiff <= DIFF && currentDiff <= minDiff)
                    {
                        minDiff = currentDiff;
                        closestGraphic = st.Graphic;
                    }
                }
                else if (gameObject is Land)
                {
                    if (currentDiff <= DIFF && currentDiff < minDiff)
                    {
                        minDiff = currentDiff;
                        closestGraphic = 0;
                    }
                }

                gameObject = gameObject.TNext;
            }

            return closestGraphic;
        }

        internal static void SnapShot(bool quiet = true)
        {
            /*try
            {
                UnityEngine.Texture2D tex = new UnityEngine.Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, UnityEngine.Screen.height), 0, 0);
                byte[] pngdata = tex.EncodeToPNG();
                string path = Engine.DataPath;
                DateTime date = Engine.MistedDateTime;
                path = Path.Combine(path, $"AssistUO_{date.Year}-{date.Month}-{date.Day}_{date.Hour}-{date.Minute}-{date.Second}_{date.Millisecond}.png");
                Task.Run(() =>
                {
                    using (Stream stream = File.Create(path))
                    {
                        File.WriteAllBytes(path, pngdata);

                        if (!quiet)
                            UOSObjects.Player.SendMessage(MsgLevel.Info, $"Screenshot stored in: {path}");
                    }
                });
            }
            catch
            {
            }*/
        }

        internal static string GetDefaultItemName(ushort graphic)
        {
            return (graphic < Client.Game.UO.FileManager.TileData.StaticData.Length ? Client.Game.UO.FileManager.TileData.StaticData[graphic].Name : Client.Game.UO.FileManager.TileData.StaticData[0].Name).Replace("%", "");
        }

        internal class PlayerDistanceComparer : IComparer<UOEntity>
        {
            public static IComparer<UOEntity> Instance { get; } = new PlayerDistanceComparer();

            public int Compare(UOEntity x, UOEntity y)
            {
                return Utility.Distance(Player.Position, x.WorldPosition).CompareTo(Utility.Distance(Player.Position, y.WorldPosition));
            }
        }

        internal static List<UOMobile> MobilesInRange()
        {
            return MobilesInRange(Client.Game.UO.World.ClientViewRange);
        }

        internal static void AddItem(UOItem item)
        {
            Items[item.Serial] = item;
        }

        internal static void AddMobile(UOMobile mob)
        {
            Mobiles[mob.Serial] = mob;
        }

        internal static void RequestMobileStatus(UOMobile m)
        {
            if (Client.Game.UO.FileManager.Version <= ClientVersion.CV_200)
                NetClient.Socket.PSend_SingleClick(m.Serial);
            else
                NetClient.Socket.PSend_StatusQuery(m);
        }

        internal static void RemoveMobile(UOMobile mob)
        {
            Mobiles.Remove(mob.Serial);
        }

        internal static void RemoveItem(UOItem item)
        {
            Items.Remove(item.Serial);
        }

        internal static byte ClientViewRange { get; set; } = 18;
        internal static bool Recording { get; set; } = false;
    }

    internal enum PacketAction : byte
    {
        None   = 0x0,
        Viewer = 0x1,
        Filter = 0x2,
        Both   = 0x3
    }

    internal class Engine
    {
        internal static readonly AssistClient Instance = new AssistClient();

        internal static bool PreSAPackets => Client.Game.UO.FileManager.Version < ClientVersion.CV_60142;
        internal static bool UsePostKRPackets => Client.Game.UO.FileManager.Version >= ClientVersion.CV_6017;
        internal static bool UseNewMobileIncoming => Client.Game.UO.FileManager.Version >= ClientVersion.CV_70331;
        internal static bool UsePostSAChanges => Client.Game.UO.FileManager.Version >= ClientVersion.CV_7000;
        internal static bool UsePostHSChanges => Client.Game.UO.FileManager.Version >= ClientVersion.CV_7090;

        private static int _PreviousHour = -1;
        private static int _Differential;
        public static int Differential//to use in all cases where you rectify normal clocks obtained with utctimer!
        {
            get
            {
                if (_PreviousHour != DateTime.UtcNow.Hour)
                {
                    _PreviousHour = DateTime.UtcNow.Hour;
                    _Differential = DateTimeOffset.Now.Offset.Hours;
                }
                return _Differential;
            }
        }
        public static DateTime MistedDateTime => DateTime.UtcNow.AddHours(Differential);

        internal static string ProfilePath { get; } = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
        internal static string DataPath { get; } = Path.Combine(CUOEnviroment.ExecutablePath, "Data");

        internal class AssistClient
        {
            internal bool Initialized { get; set; } = false;
            public void Init()
            {
                if (Initialized) return;

                PacketHandlers.Initialize();
                Filter.Initialize();
                Targeting.Initialize();
                HotKeys.Initialize();
                Scavenger.Initialize();
                Vendors.Buy.Initialize();
                Vendors.Sell.Initialize();
                Initialized = true;
            }

            internal void ReInit()
            {
                PlayerMobile mobile;
                if ((mobile = Client.Game.UO?.World?.Player) == null)
                    return;

                PlayerData m = new PlayerData(mobile.Serial)
                {
                    Name = UOSObjects.OrigPlayerName
                };

                UOMobile test = UOSObjects.FindMobile(mobile.Serial);
                if (test != null)
                    test.Remove();

                UOSObjects.AddMobile(UOSObjects.Player = m);
                Init();
                OnConnected();
                //needed for full resync of surrounding things plus ourselves
                NetClient.Socket.PSend_Resync();
            }

            internal void OnClientClosing()
            {
                OnDisconnected();
                Initialized = false;
            }

            internal void OnConnected()
            {
                if (!Initialized) return;

                ScriptManager.OnLogin();
                UIManager.Add(UOSObjects.Gump);
                ScriptManager.SetMacroButton();
                ActionQueue.StartTimer();
                Client.Game.UO.World.CommandManager.Register("ping", (str) =>
                {
                    if (str != null && str.Length > 1 && int.TryParse(str[1], out int num) && num > 0)
                        Ping.StartPing(Math.Min(num, 10));
                    else
                        Ping.StartPing(5);
                });
            }

            internal void OnDisconnected()
            {
                if (!Initialized) return;

                if (UOSObjects.GumpLoaded)
                {
                    XmlFileParser.SaveData();
                }
                ActionQueue.Stop();
                UOSObjects.Gump?.Dispose();
                ScriptManager.OnLogout();
                UOSObjects.ClearAll();
                DressList.ClearAll();
                FriendsManager.ClearAll();
                Organizer.ClearAll();
                Scavenger.ClearAll();
                Vendors.Buy.ClearAll();
                SearchExemption.ClearAll();
                Targeting.ClearAll();
                Client.Game.UO.World.CommandManager.UnRegister("ping");
            }

            internal void OnLogout()
            {
                if (!Initialized) return;

                UOSObjects.ResetPos();
                UOScript.Interpreter.OnLogout();
            }

            internal void OnFocusGained()
            {

            }

            internal void OnFocusLost()
            {

            }

            internal void Tick()
            {
                if (!Initialized) return;

                Timer.Slice();
            }

            internal void OnPlayerPositionChanged(int x, int y, int z)
            {
                if (!Initialized) return;

                UOSObjects.Player.Position = new Point3D(x, y, z);
            }

            internal bool OnRecv(byte[] message, ref int length)
            {
                if (!Initialized) return false;

                byte id = message[0];
                PacketAction pkta = PacketHandler.HasServerViewerFilter(id);
                bool result = true;

                if (pkta != PacketAction.None)
                {
                    Span<byte> data = new ArraySegment<byte>(message, 0, length);

                    switch (pkta)
                    {
                        case PacketAction.Both://if we have both filter & viewer we must treat it as filter, so we must use stackdatawriter on it
                        case PacketAction.Filter:
                            result = !PacketHandler.OnServerPacket(id, ref data, ref length, pkta);

                            break;
                        case PacketAction.Viewer:
                            result = !PacketHandler.OnServerPacket(id, ref data, ref length, pkta);
                            break;
                    }
                }

                return result;
            }

            internal bool OnSend(ref Span<byte> data)
            {
                if (!Initialized) return false;

                bool result = true;
                byte id = data[0];
                PacketAction pkta = PacketHandler.HasClientViewerFilter(id);
                switch (pkta)
                {
                    case PacketAction.Both:
                    case PacketAction.Filter:
                        result = !PacketHandler.OnClientPacket(id, ref data, pkta);
                        break;
                    case PacketAction.Viewer:
                        result = !PacketHandler.OnClientPacket(id, ref data, pkta);
                        break;
                }

                return result;
            }

            internal void OnMouseHandler(int button, int wheel)
            {
                if (!Initialized) return;

                if (Client.Game.UO.World.Player == null)
                    return;
                if (wheel > 0)
                    button = 0x101;
                else if (wheel < 0)
                    button = 0x102;
                else
                    button -= 1;
                if (HotKeys.GetVKfromSDL(button, _KeyMod, out uint vkey))
                    HotKeys.NonBlockHotKeyAction(vkey);
            }

            private int _KeyMod;
            internal bool OnHotKeyHandler(int key, int mod, bool ispressed)
            {
                if (!Initialized) return true;

                if (ispressed)
                {
                    if (HotKeys.GetVKfromSDL(key, mod, out uint vkey))
                        return HotKeys.NonBlockHotKeyAction(vkey);
                }
                _KeyMod = mod;
                return true;
            }

            internal void RequestMove(AssistDirection _Dir, bool run = true)
            {
                Client.Game.UO.World.Player?.Walk((ClassicUO.Game.Data.Direction)_Dir, run);
            }

            private ulong _Features = 0;
            public bool AllowBit(int bit)
            {
                return (_Features & (1U << bit)) == 0;
            }

            public void SetFeatures(ulong features)
            {
                _Features = features;
                if(!Engine.Instance.AllowBit(FeatureBit.AutolootAgent))
                {
                    UOSObjects.Gump.DisableAutoLoot();
                }
                if(!Engine.Instance.AllowBit(FeatureBit.LoopingMacros))
                {
                    UOSObjects.Gump.DisableLoop();
                }
                UOSObjects.Gump.UpdateVendorsListGump();
            }
        }

        private static readonly char[] _Exceptions = new char[] { ' ', '-', '_' };
        internal static bool Validate(string name, int minLength = 3, int maxLength = 24, bool allowLetters = true, bool allowDigits = true, int maxExceptions = 0, bool noExceptionsAtStart = true, char[] exceptions = null, string[] disallowed = null, bool allowSpaces = true)
        {
            if (name == null || name.Length < minLength || name.Length > maxLength)
            {
                return false;
            }
            if (exceptions == null)
                exceptions = _Exceptions;
            int exceptCount = 0;
            name = name.ToLower();

            if (!allowSpaces || !allowLetters || !allowDigits || (exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue)))
            {
                int length = name.Length;
                for (int i = 0; i < length; ++i)
                {
                    char c = name[i];
                    if (c >= 'a' && c <= 'z')
                    {
                        if (!allowLetters)
                        {
                            return false;
                        }

                        exceptCount = 0;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        if (!allowDigits)
                        {
                            return false;
                        }

                        exceptCount = 0;
                    }
                    else
                    {
                        if(!allowSpaces && c == ' ')
                        {
                            return false;
                        }

                        bool except = false;

                        for (int j = 0; !except && j < exceptions.Length; ++j)
                        {
                            if (c == exceptions[j])
                            {
                                except = true;
                            }
                        }

                        if (!except || (i == 0 && noExceptionsAtStart))
                        {
                            return false;
                        }

                        if (exceptCount++ == maxExceptions)
                        {
                            return false;
                        }
                    }
                }
            }

            if (disallowed != null && disallowed.Length > 0)
            {
                for (int i = 0; i < disallowed.Length; ++i)
                {
                    int indexOf = name.IndexOf(disallowed[i]);

                    if (indexOf == -1)
                    {
                        continue;
                    }

                    bool badPrefix = (indexOf == 0);

                    for (int j = 0; !badPrefix && j < exceptions.Length; ++j)
                    {
                        badPrefix = (name[indexOf - 1] == exceptions[j]);
                    }

                    if (badPrefix)//we don't want those word in the start or in the end of a phrase
                    {
                        return false;
                    }

                    bool badSuffix = ((indexOf + disallowed[i].Length) >= name.Length);

                    for (int j = 0; !badSuffix && j < exceptions.Length; ++j)
                    {
                        badSuffix = (name[indexOf + disallowed[i].Length] == exceptions[j]);
                    }

                    if (badSuffix)
                    {
                        return false;
                    }

                    if (indexOf > 0)
                    {
                        if (name[indexOf - 1] == ' ')
                            return false;
                        else if (indexOf + disallowed[i].Length < name.Length && name[indexOf + disallowed[i].Length] == ' ')
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
