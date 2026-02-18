﻿#region License
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
using System.Linq;

using Assistant.Core;
using ClassicUO;
using ClassicUO.Utility;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Assets;
using UOScript;
using ClassicUO.Input;
using ClassicUO.Network;

namespace Assistant.Scripts
{
    internal static class Commands
    {
        public static void Register()
        {
            // Commands. From UOSteam Documentation
            Interpreter.RegisterCommandHandler("attack", Attack, "attack (serial)", 1, 1);
            Interpreter.RegisterCommandHandler("warmode", WarMode, "warmode [\"on\"/\"off\"]", 0, 1);

            // Menu
            Interpreter.RegisterCommandHandler("clearjournal", ClearJournal, "clearjournal", 0, 0);
            Interpreter.RegisterCommandHandler("waitforjournal", WaitForJournal, "waitforjournal (\"text\") (timeout) [\"author\"/\"system\"]", 2, 3);


            Interpreter.RegisterCommandHandler("msg", Msg, "msg (\"text\") [color]", 1, 2);
            Interpreter.RegisterCommandHandler("whispermsg", WhisperMsg, "whispermsg (\"text\") [color]", 1, 2);
            Interpreter.RegisterCommandHandler("yellmsg", YellMsg, "yellmsg (\"text\") [color]", 1, 2);
            Interpreter.RegisterCommandHandler("emotemsg", EmoteMsg, "emotemsg (\"text\") [color]", 1, 2);

            Interpreter.RegisterCommandHandler("headmsg", HeadMsg, "headmsg (\"text\") [color] [serial]", 1, 3);
            Interpreter.RegisterCommandHandler("sysmsg", SysMsg, "sysmsg (\"text\") [color]", 1, 2);
            Interpreter.RegisterCommandHandler("timermsg", TimerMsg, "timermsg (\"timer name\") [color]", 1, 2);

            Interpreter.RegisterCommandHandler("poplist", PopList, "poplist (\"list name\") (\"element value\"/\"front\"/\"back\")", 2, 2);
            Interpreter.RegisterCommandHandler("pushlist", PushList, "pushlist (\"list name\") (\"element value\") [\"front\"/\"back\"]", 2, 3);
            Interpreter.RegisterCommandHandler("removelist", RemoveList, "removelist (\"list name\")", 1, 1);
            Interpreter.RegisterCommandHandler("createlist", CreateList, "createlist (\"list name\")", 1, 1);
            Interpreter.RegisterCommandHandler("clearlist", ClearList, "clearlist (\"list name\")", 1, 1);

            Interpreter.RegisterCommandHandler("setalias", SetAlias, "setalias (\"name\") [serial]", 1, 2);
            Interpreter.RegisterCommandHandler("unsetalias", UnsetAlias, "unsetalias (\"name\")", 1, 1);

            Interpreter.RegisterCommandHandler("shownames", ShowNames, "shownames [\"all\"/mobiles\"/\"corpses\"] [range]", 0, 2);

            Interpreter.RegisterCommandHandler("contextmenu", ContextMenu, "contextmenu (serial) (option)", 2, 2); //ContextMenuAction
            Interpreter.RegisterCommandHandler("waitforcontext", WaitForContext, "waitforcontext (serial) (option) (timeout)", 3, 3); //WaitForMenuAction

            // Targets
            Interpreter.RegisterCommandHandler("target", Target, "target (serial) [timeout]", 1, 2);
            Interpreter.RegisterCommandHandler("targettype", TargetType, "targettype (graphic) [color] [range]", 1, 3);
            Interpreter.RegisterCommandHandler("targetground", TargetGround, "targetground (graphic) [color] [range]", 1, 3);
            Interpreter.RegisterCommandHandler("targettile", TargetTile, "targettile (\"last\"/\"current\"/(x y z))", 1, 3);
            Interpreter.RegisterCommandHandler("targettileoffset", TargetTileOffset, "targettileoffset (x y z)", 3, 3);
            Interpreter.RegisterCommandHandler("targettilerelative", TargetTileRelative, "targettilerelative (serial) (range)", 2, 2);
            Interpreter.RegisterCommandHandler("waitfortarget", WaitForTarget, "waitfortarget (timeout)", 1, 1);
            Interpreter.RegisterCommandHandler("canceltarget", CancelTarget, "canceltarget", 0, 0);
            Interpreter.RegisterCommandHandler("cleartargetqueue", ClearTargetQueue, "cleartargetqueue", 0, 0);
            Interpreter.RegisterCommandHandler("autotargetlast", AutoTargetLast, "autotargetlast", 0, 0);
            Interpreter.RegisterCommandHandler("autotargetself", AutoTargetSelf, "autotargetself", 0, 0);
            Interpreter.RegisterCommandHandler("autotargetobject", AutoTargetObject, "autotargetobject (serial)", 1, 1);
            Interpreter.RegisterCommandHandler("autotargettype", TargetType, "autotargettype (graphic) [color] [range]", 1, 3);
            Interpreter.RegisterCommandHandler("autotargettile", TargetTile, "autotargettile (\"last\"/\"current\"/(x y z))", 1, 3);
            Interpreter.RegisterCommandHandler("autotargettileoffset", TargetTileOffset, "autotargettileoffset (x y z)", 3, 3);
            Interpreter.RegisterCommandHandler("autotargettilerelative", TargetTileRelative, "autotargettilerelative (serial) (range)", 2, 2);
            Interpreter.RegisterCommandHandler("autotargetghost", AutoTargetGhost, "autotargetghost (range) [z-range]", 1, 2);
            Interpreter.RegisterCommandHandler("autotargetground", TargetGround, "autotargetground (graphic) [color] [range]", 1, 3);
            Interpreter.RegisterCommandHandler("cancelautotarget", CancelAutoTarget, "cancelautotarget", 0, 0);
            Interpreter.RegisterCommandHandler("getenemy", GetEnemy, "getenemy (\"innocent\"/\"criminal\"/\"enemy\"/\"murderer\"/\"friend\"/\"gray\"/\"invulnerable\"/\"any\") [\"humanoid\"/\"transformation\"/\"nearest\"/\"closest\"]", 1, 2);
            Interpreter.RegisterCommandHandler("getfriend", GetFriend, "getfriend (\"innocent\"/\"criminal\"/\"enemy\"/\"murderer\"/\"friend\"/\"gray\"/\"invulnerable\"/\"any\") [\"humanoid\"/\"transformation\"/\"nearest\"/\"closest\"]", 1, 2);

            Interpreter.RegisterCommandHandler("settimer", SetTimer, "settimer (\"timer name\") (value)", 2, 2);
            Interpreter.RegisterCommandHandler("removetimer", RemoveTimer, "removetimer (\"timer name\")", 1, 1);
            Interpreter.RegisterCommandHandler("createtimer", CreateTimer, "createtimer (\"timer name\")", 1, 1);

            Interpreter.RegisterCommandHandler("clickobject", ClickObject, "clickobject (serial)", 1, 1);

            // Using stuff
            Interpreter.RegisterCommandHandler("usetype", UseType, "usetype (graphic) [color] [source] [range]", 1, 4);
            Interpreter.RegisterCommandHandler("useobject", UseObject, "useobject (serial)", 1, 1);
            Interpreter.RegisterCommandHandler("useonce", UseOnce, "useonce (graphic) [color]", 1, 2);

            /*Interpreter.RegisterCommandHandler("fly", Fly, null);
            Interpreter.RegisterCommandHandler("land", Land, null);*/

            Interpreter.RegisterCommandHandler("bandage", Bandage, "bandage [serial]", 1, 1);
            Interpreter.RegisterCommandHandler("bandageself", BandageSelf, "bandageself", 0, 0);

            Interpreter.RegisterCommandHandler("clearuseonce", ClearUseOnce, "clearuseonce", 0, 0);
            Interpreter.RegisterCommandHandler("clearusequeue", ClearUseQueue, "clearusequeue", 0, 0);
            Interpreter.RegisterCommandHandler("moveitem", MoveItem, "moveitem (serial) (destination) [(x y z)] [amount]", 2, 6);
            Interpreter.RegisterCommandHandler("moveitemoffset", MoveItemOffset, "moveitemoffset (serial) (destination) [(x y z)] [amount]", 2, 6);
            Interpreter.RegisterCommandHandler("movetype", MoveType, "movetype (graphic) (source) (destination) [(x y z)] [color] [amount] [range]", 2, 9);
            Interpreter.RegisterCommandHandler("movetypeoffset", MoveTypeOffset, "movetypeoffset (graphic) (source) (destination) [(x y z)] [color] [amount] [range]", 3, 9);

            Interpreter.RegisterCommandHandler("feed", Feed, "feed (serial) (\"food name\"/\"food group\"/\"any\"/graphic) [color] [amount]", 2, 4);
            Interpreter.RegisterCommandHandler("rename", Rename, "rename (serial) (\"new name\")", 2, 2);
            Interpreter.RegisterCommandHandler("togglehands", ToggleHands, "togglehands (\"left\"/\"right\")", 1, 1);
            Interpreter.RegisterCommandHandler("equipitem", EquipItem, "equipitem (serial/item id) (layer)", 2, 2);
            Interpreter.RegisterCommandHandler("equipwand", EquipWand, "equipwand (\"spell name\"/\"any\"/\"undefined\") [minimum charges]", 1, 2);

            Interpreter.RegisterCommandHandler("buy", Buy, "buy (\"list name\")", 1 ,1);
            Interpreter.RegisterCommandHandler("sell", Sell, "sell (\"list name\")", 1, 1);
            Interpreter.RegisterCommandHandler("clearbuy", ClearBuy, "clearbuy", 0, 0);
            Interpreter.RegisterCommandHandler("clearsell", ClearSell, "clearsell", 0, 0);
            Interpreter.RegisterCommandHandler("organizer", Organize, "organizer (\"profile name\") [source] [destination]", 1, 3);

            Interpreter.RegisterCommandHandler("autoloot", AutoLoot, "autoloot", 0, 0);
            Interpreter.RegisterCommandHandler("toggleautoloot", ToggleAutoLoot, "toggleautoloot", 0, 0);
            Interpreter.RegisterCommandHandler("togglescavenger", ToggleScavenger, "togglescavenger", 0, 0);

            Interpreter.RegisterCommandHandler("togglemounted", ToggleMounted, "togglemounted", 0, 0);

            // Gump
            Interpreter.RegisterCommandHandler("waitforgump", WaitForGump, "waitforgump (gump id/\"any\") (timeout)", 2, 2);
            Interpreter.RegisterCommandHandler("replygump", ReplyGump, "replygump (gump id/\"any\") (button) [checkboxid/\"textid textresponse\"]", 2, 3); // GumpResponseAction
            Interpreter.RegisterCommandHandler("closegump", CloseGump, "closegump (\"paperdoll\"/\"status\"/\"profile\"/\"container\") (\"serial\")", 2, 2); // GumpResponseAction

            // Dress
            Interpreter.RegisterCommandHandler("dress", DressCommand, "dress [\"profile name\"]", 0, 1); //DressAction
            Interpreter.RegisterCommandHandler("undress", UnDressCommand, "undress [\"profile name\"]", 0, 1); //UndressAction
            Interpreter.RegisterCommandHandler("dressconfig", DressConfig, "dressconfig", 0, 0); //DressConfig

            // Prompt
            Interpreter.RegisterCommandHandler("promptalias", PromptAlias, "promptalias (\"alias name\")", 1, 1);
            Interpreter.RegisterCommandHandler("promptmsg", PromptMsg, "promptmsg (\"text\")", 1, 1);
            Interpreter.RegisterCommandHandler("cancelprompt", CancelPrompt, "cancelprompt", 0, 0);
            Interpreter.RegisterCommandHandler("waitforprompt", WaitForPrompt, "waitforprompt (timeout)", 1, 1); //WaitForPromptAction

            // General Waits/Pauses
            Interpreter.RegisterCommandHandler("pause", Pause, "pause (timeout)", 1, 1); //PauseAction

            // Misc
            Interpreter.RegisterCommandHandler("clearability", ClearAbilities, "clearability", 0, 0);
            Interpreter.RegisterCommandHandler("setability", SetAbility, "setability (\"primary\"/\"secondary\"/\"stun\"/\"disarm\") [\"on\"/\"off\"]", 1, 2);
            Interpreter.RegisterCommandHandler("useskill", UseSkill, "useskill (\"skill name\"/\"last\")", 1, 1);
            Interpreter.RegisterCommandHandler("walk", Walk, "walk (\"direction\")", 1, 1);//blu
            Interpreter.RegisterCommandHandler("turn", Turn, "turn (\"direction\")", 1, 1);//blu
            Interpreter.RegisterCommandHandler("run", Run, "run (\"direction\")", 1, 1);//blu
            Interpreter.RegisterCommandHandler("setskill", SetSkill, "setskill (\"skill name\") (\"up/down/locked\")", 2, 2);

            Interpreter.RegisterCommandHandler("info", Info, "info", 0, 0);
            Interpreter.RegisterCommandHandler("ping", Ping, "ping", 0, 0);
            Interpreter.RegisterCommandHandler("playmacro", PlayMacro, "playmacro (\"macro name\")", 1, 1);
            Interpreter.RegisterCommandHandler("playsound", PlaySound, "playsound (sound id)", 1, 1);
            Interpreter.RegisterCommandHandler("resync", Resync, "resync", 0, 0);
            Interpreter.RegisterCommandHandler("snapshot", SnapShot, "snapshot [timer]", 0, 1);
            Interpreter.RegisterCommandHandler("hotkeys", HotKeys, "hotkeys", 0, 0);
            Interpreter.RegisterCommandHandler("where", Where, "where", 0, 0);
            Interpreter.RegisterCommandHandler("messagebox", MessageBox, "messagebox (\"title\") (\"body\")", 2, 2);
            Interpreter.RegisterCommandHandler("clickscreen", ClickScreen, "clickscreen (x) (y) [\"single\"/\"double\"] [\"left\"/\"right\"]", 2, 4);
            Interpreter.RegisterCommandHandler("paperdoll", Paperdoll, "paperdoll [serial]", 0, 1);
            Interpreter.RegisterCommandHandler("cast", Cast, "cast (\"spell name\") [serial]", 1, 2);
            Interpreter.RegisterCommandHandler("helpbutton", HelpButton, "helpbutton", 0, 0);
            Interpreter.RegisterCommandHandler("guildbutton", GuildButton, "guildbutton", 0, 0);
            Interpreter.RegisterCommandHandler("questsbutton", QuestsButton, "questsbutton", 0, 0);
            Interpreter.RegisterCommandHandler("logoutbutton", LogoutButton, "logoutbutton", 0, 0);
            Interpreter.RegisterCommandHandler("virtue", Virtue, "virtue (\"honor\"/\"sacrifice\"/\"valor\")", 1, 1);

            Interpreter.RegisterCommandHandler("addfriend", AddFriend, "addfriend", 0, 0);
            Interpreter.RegisterCommandHandler("removefriend", RemoveFriend, "removefriend", 0, 0);
            Interpreter.RegisterCommandHandler("ignoreobject", IgnoreObject, "ignoreobject (serial)", 1, 1);
            Interpreter.RegisterCommandHandler("clearignorelist", ClearIgnoreList, "clearignorelist", 0, 0);
            Interpreter.RegisterCommandHandler("autocolorpick", AutoColorPick, "autocolorpick (color)", 1, 1);
            Interpreter.RegisterCommandHandler("waitforcontents", WaitForContents, "waitforcontents (serial) (timeout)", 2, 2);

            Interpreter.RegisterCommandHandler("random", RandomNumber, "random [from] to", 1, 2);

            if (ClassicUO.Client.Game.UO.Version > ClientVersion.CV_200)
            {
                Interpreter.RegisterCommandHandler("waitforproperties", WaitForProperties, "waitforproperties (serial) (timeout)", 2, 2);
                Interpreter.RegisterCommandHandler("partymsg", PartyMsg, "partymsg (\"text\") [serial]", 1, 2);
                Interpreter.RegisterCommandHandler("guildmsg", GuildMsg, "guildmsg (\"text\")", 1, 1);
                Interpreter.RegisterCommandHandler("allymsg", AllyMsg, "allymsg (\"text\")", 1, 1);
                Interpreter.RegisterCommandHandler("chatmsg", ChatMsg, "chatmsg (\"text\")", 1, 1);
            }
            else
            {
                Interpreter.RegisterCommandHandler("guildmsg", GuildMsg, "guildmsg (\"text\")", 1, 1);
                Interpreter.RegisterCommandHandler("allymsg", AllyMsg, "allymsg (\"text\")", 1, 1);
            }
            //those are too osi specific and I've no interest in emulate
            /*Interpreter.RegisterCommandHandler("miniheal", Command, "helper", 0, 0);
            Interpreter.RegisterCommandHandler("bigheal", Command, "helper", 0, 0);
            Interpreter.RegisterCommandHandler("chivalryheal", Command, "helper", 0, 0);*/
        }

        private static bool _hasAction = false;
        private static uint _hasObject = 0;

        private static void GetFilterTargetTypes(Argument[] args, out Targeting.TargetType targetType, out Targeting.FilterType filterType)
        {
            targetType = Targeting.TargetType.None;
            filterType = Targeting.FilterType.Next;
            for(int i = 0; i < args.Length; i++)
            {
                string val = args[i].AsString().ToLower(Interpreter.Culture);
                switch(val)
                {
                    case "innocent":
                        targetType |= Targeting.TargetType.Innocent;
                        break;
                    case "criminal":
                        targetType |= Targeting.TargetType.Criminal;
                        break;
                    case "enemy":
                        targetType |= Targeting.TargetType.Enemy;
                        break;
                    case "murderer":
                        targetType |= Targeting.TargetType.Murderer;
                        break;
                    case "friend":
                        targetType |= Targeting.TargetType.Friend;
                        break;
                    case "gray":
                        targetType |= Targeting.TargetType.Gray;
                        break;
                    case "invulnerable":
                        targetType |= Targeting.TargetType.Invulnerable;
                        break;
                    case "any":
                        targetType |= Targeting.TargetType.Any;
                        break;
                    case "humanoid":
                        filterType |= Targeting.FilterType.Humanoid;
                        break;
                    case "transformation":
                        filterType |= Targeting.FilterType.Transformation;
                        break;
                    case "nearest":
                        filterType |= Targeting.FilterType.Nearest;
                        break;
                    case "closest":
                        filterType |= Targeting.FilterType.Closest;
                        break;
                }
            }
        }

        private static bool GetEnemy(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            GetFilterTargetTypes(args, out Targeting.TargetType target, out Targeting.FilterType filter);
            Targeting.GetTarget(target, filter, true, quiet);
            return true;
        }

        private static bool GetFriend(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            GetFilterTargetTypes(args, out Targeting.TargetType target, out Targeting.FilterType filter);
            Targeting.GetTarget(target, filter, false, quiet);
            return true;
        }

        private static bool PlayMacro(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            ScriptManager.PlayScript(args[0].AsString(), true);
            return true;
        }

        private static bool PlaySound(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            int soundid = Utility.ToInt32(args[0].AsString(), -1);
            if(soundid >= 0)
                Client.Game.Audio.PlaySound(soundid);
            else
                ScriptManager.Message(quiet, "Invalid sound id, only numbers are supported at the moment, and the number must be greater or equal than zero");
            return true;
        }

        private static bool SnapShot(string command, Argument[] args, bool quiet, bool force)
        {
            if(args.Length > 0)
            {
                uint timeout = args[0].AsUInt();
                if(timeout > 0)
                {
                    Interpreter.Timeout(timeout, () =>
                    {
                        UOSObjects.SnapShot(quiet);
                        return true;
                    });
                    return false;
                }
            }

            UOSObjects.SnapShot(quiet);
            return true;
        }

        internal static bool HotKeys(string command, Argument[] args, bool quiet, bool force)
        {
            UOSObjects.Gump.ToggleHotKeys = !UOSObjects.Gump.ToggleHotKeys;
            if (!quiet)
            {
                if (UOSObjects.Gump.ToggleHotKeys)
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "Hotkeys Disabled!");
                else
                    UOSObjects.Player.SendMessage(MsgLevel.Friend, "Hotkeys Enabled!");
            }
            return true;
        }

        private readonly static string[] _cityNames = { "Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno", "Ter Mur" };
        internal static string CityName(byte mapid) 
        { 
            if (mapid < _cityNames.Length)
                return _cityNames[mapid];
            return $"Unknown ({mapid})";
        }

        internal static bool Where(string command, Argument[] args, bool quiet, bool force)
        {
            var p = UOSObjects.Player;
            if (p != null)
            {
                p.SendMessage(MsgLevel.Info, $"Location: {p.Position.X} {p.Position.Y} {p.Position.Z} in {CityName(p.Map)}");
            }
            return true;
        }

        private static bool ChatMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            NetClient.Socket.Send_ChatMessageCommand(args[0].AsString());

            return true;
        }

        private static bool AutoLoot(string command, Argument[] args, bool quiet, bool force)
        {
            Targeting.OneTimeTarget(false, Assistant.HotKeys.AutoLootOnTarget);
            return true;
        }

        private static bool ToggleAutoLoot(string command, Argument[] args, bool quiet, bool force)
        {
            UOSObjects.Gump.AutoLoot = !UOSObjects.Gump.AutoLoot;
            UOSObjects.Player?.SendMessage(UOSObjects.Gump.AutoLoot ? MsgLevel.Friend : MsgLevel.Info, $"AutoLoot {(UOSObjects.Gump.AutoLoot ? "Enabled" : "Disabled" )}");
            return true;
        }

        private static bool Fly(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static bool Land(string command, Argument[] args, bool quiet, bool force)
        {
            return true;
        }

        private static bool CancelPrompt(string command, Argument[] args, bool quiet, bool force)
        {
            if(UOSObjects.Player.HasPrompt)
                UOSObjects.Player.CancelPrompt();

            return true;
        }

        private static bool WaitForPrompt(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.Timeout(args[0].AsUInt(), () =>
            {
                return true;
            });

            return UOSObjects.Player.HasPrompt;
        }

        private static DressList _Temporary;
        private static bool DressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (_Temporary != null)
                    _Temporary.Dress();
                else
                    ScriptManager.Message(quiet, $"No dresslist specified and no temporary 'dressconfig' present - Usage: {Interpreter.GetCmdHelper(command)}");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Dress();
                else
                    ScriptManager.Message(quiet, command, $"{args[0].AsString()} not found");
            }

            return true;
        }

        private static bool UnDressCommand(string command, Argument[] args, bool quiet, bool force)
        {
            //we're using a named dresslist or a temporary dresslist?
            if (args.Length == 0)
            {
                if (_Temporary != null)
                    _Temporary.Undress();
                else
                    ScriptManager.Message(quiet, $"No dresslist specified and no temporary 'dressconfig' present - Usage: {Interpreter.GetCmdHelper(command)}");
            }
            else
            {
                var d = DressList.Find(args[0].AsString());
                if (d != null)
                    d.Undress();
                else
                    ScriptManager.Message(quiet, command, $"{args[0].AsString()} not found");
            }

            return true;
        }

        private static bool DressConfig(string command, Argument[] args, bool quiet, bool force)
        {
            if (_Temporary == null)
                _Temporary = new DressList("dressconfig");

            _Temporary.LayerItems.Clear();
            for (int i = 0; i < UOSObjects.Player.Contains.Count; i++)
            {
                UOItem item = UOSObjects.Player.Contains[i];
                if (item.Layer < Layer.Mount && item.Layer != Layer.Backpack && item.Layer != Layer.Hair &&
                    item.Layer != Layer.Beard)
                    _Temporary.LayerItems[item.Layer] = new DressItem(item.Serial, item.Graphic, UOSObjects.Gump.TypeDress);
            }

            return true;
        }

        private static bool Buy(string command, Argument[] args, bool quiet, bool force)
        {
            if (Engine.Instance.AllowBit(FeatureBit.BuyAgent))
            {
                if (args.Length < 1)
                {
                    ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                    return true;
                }
                string name = args[0].AsString();
                if (!string.IsNullOrEmpty(name))
                {
                    var bs = Vendors.Buy.BuyList.Keys.FirstOrDefault(k => k.Name == name);
                    if (bs != null)
                    {
                        if (UOSObjects.Gump.SelectedBuyOrSell == 0)
                            UOSObjects.Gump.UpdateVendorsListGump(bs);
                        else
                            Vendors.Buy.BuySelected = bs;
                    }
                }
            }
            return true;
        }

        private static bool Sell(string command, Argument[] args, bool quiet, bool force)
        {
            if (Engine.Instance.AllowBit(FeatureBit.SellAgent))
            {
                if (args.Length < 1)
                {
                    ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                    return true;
                }
                string name = args[0].AsString();
                if (!string.IsNullOrEmpty(name))
                {
                    var bs = Vendors.Sell.SellList.Keys.FirstOrDefault(k => k.Name == name);
                    if (bs != null)
                    {
                        if (UOSObjects.Gump.SelectedBuyOrSell == 1)
                            UOSObjects.Gump.UpdateVendorsListGump(bs);
                        else
                            Vendors.Sell.SellSelected = bs;
                    }
                }
            }
            return true;
        }

        private static bool ClearBuy(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Gump.SelectedBuyOrSell == 0)
                UOSObjects.Gump.EnableBuySell.IsChecked = false;
            else
                Vendors.Buy.BuyEnabled = false;
            return true;
        }

        private static bool ClearSell(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Gump.SelectedBuyOrSell == 1)
                UOSObjects.Gump.EnableBuySell.IsChecked = false;
            else
                Vendors.Sell.SellEnabled = false;
            return true;
        }

        private static bool Organize(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (!_hasAction)
            {
                string name = args[0].AsString();
                if (!string.IsNullOrEmpty(name))
                {
                    var organizer = Organizer.Organizers.FirstOrDefault(org => org.Name == name);
                    if (organizer != null)
                    {
                        if (args.Length > 1)//"organizer ('profile name') [source] [destination]"
                        {
                            uint source = args[1].AsSerial(false);
                            if (SerialHelper.IsItem(source))
                                organizer.SourceCont = source;
                            else
                                ScriptManager.Message(quiet, $"Organizer source is not a valid item serial: {args[1].AsString()}");
                            if (args.Length > 2)
                            {
                                source = args[2].AsSerial(false);
                                if (SerialHelper.IsItem(source))
                                    organizer.TargetCont = source;
                                else
                                    ScriptManager.Message(quiet, $"Organizer target is not a valid item serial: {args[2].AsString()}");
                            }
                        }

                        _hasAction = !organizer.Organize();
                    }
                    else
                        ScriptManager.Message(quiet, command, $"{args[0].AsString()} not found");
                }
            }
            else if (Organizer.HasContainerSelection || Organizer.IsTimerActive)
                return false;
            _hasAction = false;
            return true;
        }

        private static bool ClearAbilities(string command, Argument[] args, bool quiet, bool force)
        {
            ClientPackets.PRecv_ClearAbility();
            NetClient.Socket.PSend_UseAbility(Ability.None);
            return true;
        }

        private static string[] abilities = new string[4] { "primary", "secondary", "stun", "disarm" };
        private static bool SetAbility(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || !abilities.Contains(args[0].AsString()))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if ((args.Length == 2 && args[1].AsString() == "on") || args.Length == 1)
            {
                switch (args[0].AsString())
                {
                    case "primary":
                        SpecialMoves.SetPrimaryAbility();
                        break;
                    case "secondary":
                        SpecialMoves.SetSecondaryAbility();
                        break;
                    case "stun":
                        NetClient.Socket.PSend_StunDisarm(true);
                        break;
                    case "disarm":
                        NetClient.Socket.PSend_StunDisarm(false);
                        break;
                    default:
                        break;
                }
            }
            else if (args.Length == 2 && args[1].AsString() == "off")
            {
                ClientPackets.PRecv_ClearAbility();
                NetClient.Socket.PSend_UseAbility(Ability.None);
            }

            return true;
        }

        private static bool ClickObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint serial = args[0].AsSerial();
            NetClient.Socket.PSend_SingleClick(serial);

            return true;
        }

        private static bool Bandage(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return true;

            UOItem pack = UOSObjects.Player.Backpack;
            if (pack != null)
            {
                UOItem obj = pack.FindItemByID(3617);
                if (obj == null)
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "No bandages found");
                }
                else
                {
                    if (args.Length > 0)
                        NetClient.Socket.PSend_BandageReq(obj.Serial, args[0].AsSerial());
                    else
                        NetClient.Socket.PSend_DoubleClick(obj.Serial);
                }
            }
            else
                ScriptManager.Message(quiet, command, "No backpack could be found");
            return true;
        }

        private static bool BandageSelf(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return true;

            UOItem pack = UOSObjects.Player.Backpack;
            if (pack != null)
            {
                UOItem obj = pack.FindItemByID(3617);
                if (obj == null)
                {
                    UOSObjects.Player.SendMessage(MsgLevel.Warning, "No bandages found");
                }
                else
                {
                    //we use internal Bandage for Extended packet!
                    NetClient.Socket.PSend_BandageReq(obj.Serial, UOSObjects.Player.Serial);
                }
            }

            return true;
        }

        private static bool UseType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            //usetype (graphic) [color] [source] [range]
            string graphicString = args[0].AsString();
            ushort graphicId = args[0].AsUShort();

            ushort? color = null;
            if (args.Length >= 2 && args[1].AsString().ToLower(Interpreter.Culture) != "any")
            {
                color = args[1].AsUShort();
                if (color == ushort.MaxValue)
                    color = null;
            }

            string sourceStr = null;
            uint source = 0;

            if (args.Length >= 3)
            {
                sourceStr = args[2].AsString().ToLower(Interpreter.Culture);
                if (sourceStr != "world" && sourceStr != "any" && sourceStr != "ground")
                {
                    source = args[2].AsSerial();
                }
            }

            uint? range = null;
            if (args.Length >= 4 && args[3].AsString().ToLower(Interpreter.Culture) != "any")
            {
                range = args[3].AsUInt();
            }

            List<uint> list = new List<uint>();

            bool ground = sourceStr == "world" || sourceStr == "ground";
            bool any = sourceStr == "any" || (source == 0 && !ground);
            UOItem container = null;
            if (ground || any || ((container = UOSObjects.FindItem(source)) != null && container.IsContainer))
            {
                List<UOEntity> entities = new List<UOEntity>();
                if (container != null)
                {
                    entities.AddRange(container.FindItemsByID(graphicId));
                }
                else if (ground || any)
                {
                    entities.AddRange(UOSObjects.FindEntitiesByType(graphicId, contained: any));
                    if (ground)
                        entities.Sort(Targeting.Instance);
                    else
                        entities.Sort(Targeting.ContainedInstance);
                }

                foreach (UOEntity entity in entities)
                {
                    if (entity != null &&
                        (!color.HasValue || color.Value == ushort.MaxValue || entity.Hue == color.Value) &&
                        (!range.HasValue || Utility.InRange(UOSObjects.Player.Position, entity.WorldPosition, (int)range.Value)))
                    {
                        list.Add(entity.Serial);
                    }
                }
            }
            else if (container != null && !container.IsContainer)
            {
                ScriptManager.Message(quiet, $"Script Error: Source '{sourceStr}' is not a container!");
            }

            if (list.Count > 0)
            {
                uint serial = list[0];//always take the first in list, nearest object

                if (serial != 0)
                {
                    if (force || !UOSObjects.Gump.UseObjectsQueue)
                        NetClient.Socket.PSend_DoubleClick(serial);
                    else
                        ActionQueue.DoubleClick(serial);
                    Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
                    return true;
                }
            }

            ScriptManager.Message(quiet, $"Script Error: Couldn't find '{graphicString}'");
            return true;
        }

        private static bool UseObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, command, "invalid serial");
                return true;
            }

            if (force || !UOSObjects.Gump.UseObjectsQueue)
                NetClient.Socket.PSend_DoubleClick(serial);
            else
                ActionQueue.DoubleClick(serial, quiet);
            Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            return true;
        }

        private static bool UseOnce(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
            }

            ushort graphicId = args[0].AsUShort();
            int color = -1;
            if (args.Length >= 2)
            {
                color = args[1].AsInt();
            }
            UOItem pack = UOSObjects.Player.Backpack;
            if (pack != null)
            {
                List<UOItem> items = pack.FindItemsByID(graphicId, true, color);
                if (items.Count > 0)
                {
                    bool found = false;
                    for(int i = 0; i < items.Count && !found; ++i)
                    {
                        UOItem item = items[i];
                        if (UsedOnce.Add(item.Serial))
                        {
                            found = true;
                            if (i + 1 < items.Count)
                            {
                                NextUsedOnce = items[i + 1].Serial;
                                UseOnceForceUpdate(items[i + 1], true);
                            }
                            else
                                NextUsedOnce = 0;
                            UseOnceForceUpdate(item, true);
                            PlayerData.DoubleClick(item.Serial, quiet, force);
                            Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
                        }
                    }
                    if(!found)
                        ScriptManager.Message(quiet, command, $"Items with graphic '0x{graphicId:X}' where already used, consider using 'clearuseonce'", level: MsgLevel.Warning);
                }
                else
                    ScriptManager.Message(quiet, command, $"Couldn't find item with graphic '0x{graphicId:X}'");
            }
            else
                ScriptManager.Message(quiet, command, "No backpack could be found");
            return true;
        }

        private static void UseOnceForceUpdate(UOItem item, bool nocheck = false)
        {
            if(nocheck || item != null)
            {
                ClientPackets.PRecv_ContainerItem(item);
            }
        }

        internal static HashSet<uint> UsedOnce = new HashSet<uint>();
        internal static uint NextUsedOnce { get; set; } = 0;

        private static bool ClearUseOnce(string command, Argument[] args, bool quiet, bool force)
        {
            List<uint> list = new List<uint>(UsedOnce);
            UsedOnce.Clear();
            foreach (uint u in list)
            {
                UseOnceForceUpdate(UOSObjects.FindItem(u));
            }
            uint ui = NextUsedOnce;
            NextUsedOnce = 0;
            UseOnceForceUpdate(UOSObjects.FindItem(ui));
            ScriptManager.Message(quiet, "use once list cleared", null, MsgLevel.Info);
            return true;
        }

        private static bool ClearUseQueue(string command, Argument[] args, bool quiet, bool force)
        {
            ActionQueue.ClearActions();
            ScriptManager.Message(quiet, "use queue list cleared", null, MsgLevel.Info);
            return true;
        }

        private static bool MoveItem(string command, Argument[] args, bool quiet, bool force)
        {
            return MoveItemUniversal(command, args, quiet, force, true);
        }

        internal static bool MoveItemUniversal(string command, Argument[] args, bool quiet, bool force = false, bool iscommand = false)
        {
            string nameStr = null;
            if (args.Length < 2 || ((nameStr = args[1].AsString()) == "ground" && args.Length < 5))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return iscommand;
            }

            uint serial = args[0].AsSerial();
            Point3D p = Point3D.MinusOne;
            uint destination = 0;
            if (nameStr == "ground" || args.Length >= 5)
            {
                int x = args[2].AsInt(), y = args[3].AsInt(), z = args[4].AsInt();
                if (x >= 0 && y >= 0)
                {
                    p = new Point3D(x, y, z);
                    if (p == Point3D.Zero)
                        p = Point3D.MinusOne;
                }
            }
            destination = args[1].AsSerial(false);
            int amount = -1;
            if (args.Length > 5)
                amount = Math.Max(1, args[5].AsInt());
            UOItem item;
            if (!SerialHelper.IsValid(serial) || (item = UOSObjects.FindItem(serial)) == null)
            {
                ScriptManager.Message(quiet, command, $"invalid item '0x{serial:X8}'");
                return iscommand;
            }
            else if(p == Point3D.MinusOne && !SerialHelper.IsValid(destination) && nameStr != "any")
            {
                ScriptManager.Message(quiet, command, $"invalid destination '0x{destination:X8}'");
                return iscommand;
            }
            else
            {
                if(nameStr == "any")
                {
                    if (item.Container is uint)
                        destination = (uint)item.Container;
                    else if (item.Container is UOItem cnt)
                        destination = cnt.Serial;
                }
                if (force)//avoid stacking of the item at all costs
                {
                    if (p == Point3D.MinusOne)
                    {
                        p = new Point3D(Utility.Random(20, 50), Utility.Random(20, 50), p._Z);
                    }
                    else if(destination > 0)
                    {
                        UOItem dest = UOSObjects.FindItem(destination);
                        if(dest.Container == null)//on ground, drop at the coordinates
                        {
                            p = dest.Position;
                            destination = 0;
                        }
                        else if(dest.Graphic == item.Graphic && dest.Container is UOItem uit && uit.IsContainer)
                        {
                            destination = uit.Serial;
                        }
                    }
                }
                if (p == Point3D.MinusOne || destination > 0)
                {
                    DragDropManager.DragDrop(item, destination, p, amount < 1 ? item.Amount : amount);
                }
                else
                    DragDropManager.DragDrop(item, p, amount < 1 ? item.Amount : amount);
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            return true;
        }

        private static bool MoveItemOffset(string command, Argument[] args, bool quiet, bool force)
        {
            return MoveItemOffsetUniversal(command, args, quiet, force, true);
        }

        internal static bool MoveItemOffsetUniversal(string command, Argument[] args, bool quiet, bool force = false, bool iscommand = false)
        {
            if (args.Length < 5)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return iscommand;
            }

            uint serial = args[0].AsSerial();
            uint destination = args[1].AsSerial(false);
            int amount = -1;
            if (args.Length > 5)
                amount = Math.Max(1, args[5].AsInt());
            UOItem item;
            string nameStr = args[1].AsString();
            if (!SerialHelper.IsValid(serial) || (item = UOSObjects.FindItem(serial)) == null)
            {
                ScriptManager.Message(quiet, command, $"invalid item '0x{serial:X8}'");
                return iscommand;
            }
            else if (nameStr != "ground" && nameStr != "any" && !SerialHelper.IsValid(destination))
            {
                ScriptManager.Message(quiet, command, $"invalid destination '0x{destination:X8}'");
                return iscommand;
            }
            else
            {
                if(nameStr == "any")
                {
                    if (item.Container is uint)
                    {
                        destination = (uint)item.Container;
                    }
                    else if (item.Container is UOItem cnt)
                    {
                        destination = cnt.Serial;
                    }
                }
                Point3D p = nameStr == "ground" ? item.WorldPosition : (destination == 0 && nameStr == "any" ? item.WorldPosition : item.Position);
                p.X += args[2].AsInt();
                p.Y += args[3].AsInt();
                p.Z += args[4].AsInt();
                if (destination > 0)
                    DragDropManager.DragDrop(item, destination, p, amount < 1 ? item.Amount : amount);
                else
                    DragDropManager.DragDrop(item, p, amount < 1 ? item.Amount : amount);
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            return true;
        }

        private static bool MoveType(string command, Argument[] args, bool quiet, bool force)
        {
            return MoveTypeUniversal(command, args, quiet, force, true);
        }

        internal static bool MoveTypeUniversal(string command, Argument[] args, bool quiet, bool force = false, bool iscommand = false)
        {
            if (args.Length < 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return iscommand;
            }

            int graphicId = args[0].AsInt();
            if(graphicId <= 0 || graphicId >= ushort.MaxValue)
            {
                ScriptManager.Message(quiet, command, $"invalid graphic '0x{graphicId:X}'");
                return iscommand;
            }
            ContainerType sourceType;
            switch(args[1].AsString())
            {
                case "world":
                case "ground":
                    sourceType = ContainerType.Ground;
                    break;
                case "any":
                    sourceType = ContainerType.Any;
                    break;
                default:
                    sourceType = ContainerType.None;
                    break;
            }
            uint source = 0;
            if(sourceType == ContainerType.None)
            {
                source = args[1].AsSerial();
                sourceType = ContainerType.Serial;
            }
            ContainerType destType;
            switch (args[2].AsString())
            {
                case "world":
                case "ground":
                    destType = ContainerType.Ground;
                    break;
                default:
                    destType = ContainerType.None;
                    break;
            }
            uint destination = 0;
            if (destType == ContainerType.None)
            {
                destination = args[2].AsSerial();
                destType = ContainerType.Serial;
            }
            int color = -1;
            int amount = -1;
            int range = -1;
            Point3D p = Point3D.MinusOne;
            if (args.Length > 8)
                range = Math.Max(0, args[8].AsInt());
            if (args.Length > 7)
                amount = Math.Max(1, args[7].AsInt());
            if (args.Length > 6)
                color = args[6].AsInt();
            if (args.Length > 5)
            {
                int x = args[3].AsInt(), y = args[4].AsInt(), z = args[5].AsInt();
                if (x >= 0 && y >= 0)
                {
                    p = new Point3D(x, y, z);
                    if (p == Point3D.Zero)
                        p = Point3D.MinusOne;
                }
            }
            UOEntity sent, dend;
            UOItem item = UOSObjects.FindItemByType(graphicId, color, range, sourceType);
            if(item == null)
            {
                ScriptManager.Message(quiet, command, $"item not found");
                return iscommand;
            }
            else if ((sourceType == ContainerType.Ground && !item.OnGround) || (sourceType == ContainerType.Serial && (!SerialHelper.IsValid(source) || (sent = UOSObjects.FindEntity(source)) == null)) || ((destType & ContainerType.Ground) != ContainerType.Ground && (!SerialHelper.IsValid(destination) || (dend = UOSObjects.FindEntity(destination)) == null)))
            {
                ScriptManager.Message(quiet, command, $"invalid source '0x{source:X}' or destination '0x{destination:X}' for item '{item}'");
                return iscommand;
            }
            else
            {
                if (force)//avoid stacking of the item at all costs
                {
                    if (p == Point3D.MinusOne)
                    {
                        p = new Point3D(Utility.Random(20, 50), Utility.Random(20, 50), p._Z);
                    }
                    else if (destination > 0)
                    {
                        UOItem dest = UOSObjects.FindItem(destination);
                        if (dest.Container == null)//on ground, drop at the coordinates
                        {
                            p = dest.Position;
                            destination = 0;
                        }
                        else if (dest.Graphic == item.Graphic && dest.Container is UOItem uit && uit.IsContainer)
                        {
                            destination = uit.Serial;
                        }
                    }
                }
                if (!item.OnGround && ((destType & ContainerType.Ground) == ContainerType.Ground || destination == 0))
                    DragDropManager.DragDrop(item, p, amount < 1 ? item.Amount : amount);
                else
                    DragDropManager.DragDrop(item, destination, p, amount < 1 ? item.Amount : amount);
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            return true;
        }

        private static bool MoveTypeOffset(string command, Argument[] args, bool quiet, bool force)
        {
            return MoveTypeOffsetUniversal(command, args, quiet, force, true);
        }

        internal static bool MoveTypeOffsetUniversal(string command, Argument[] args, bool quiet, bool force = false, bool iscommand = false)
        {
            if (args.Length < 6)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return iscommand;
            }

            int graphicId = args[0].AsInt();
            if (graphicId <= 0 || graphicId >= ushort.MaxValue)
            {
                ScriptManager.Message(quiet, command, $"invalid graphic '0x{graphicId:X}'");
                return iscommand;
            }
            ContainerType sourceType;
            switch (args[1].AsString())
            {
                case "world":
                case "ground":
                    sourceType = ContainerType.Ground;
                    break;
                case "any":
                    sourceType = ContainerType.Any;
                    break;
                default:
                    sourceType = ContainerType.None;
                    break;
            }
            uint source = 0;
            if (sourceType == ContainerType.None)
            {
                source = args[1].AsSerial();
                sourceType = ContainerType.Serial;
            }
            ContainerType destType;
            switch (args[2].AsString())
            {
                case "world":
                case "ground":
                    destType = ContainerType.Ground;
                    break;
                default:
                    destType = ContainerType.None;
                    break;
            }
            uint destination = 0;
            if (destType == ContainerType.None)
            {
                destination = args[2].AsSerial();
                destType = ContainerType.Serial;
            }
            int color = -1;
            int amount = -1;
            int range = -1;
            if (args.Length > 8)
                range = Math.Max(0, args[8].AsInt());
            if (args.Length > 7)
                amount = Math.Max(1, args[7].AsInt());
            if (args.Length > 6)
                color = args[6].AsInt();
            UOEntity sent, dend;
            UOItem item = UOSObjects.FindItemByType(graphicId, color, range, sourceType);
            if (item == null)
            {
                ScriptManager.Message(quiet, command, $"item not found");
                return iscommand;
            }
            else if ((sourceType == ContainerType.Ground && !item.OnGround) || (sourceType == ContainerType.Serial && (!SerialHelper.IsValid(source) || (sent = UOSObjects.FindEntity(source)) == null)) || ((destType & ContainerType.Ground) != ContainerType.Ground && (!SerialHelper.IsValid(destination) || (dend = UOSObjects.FindEntity(destination)) == null)))
            {
                ScriptManager.Message(quiet, command, $"invalid source '0x{source:X}' or destination '0x{destination:X}' for item '{item}'");
                return iscommand;
            }
            else
            {
                Point3D p;
                if (!item.OnGround && ((destType & ContainerType.Ground) == ContainerType.Ground || destination == 0))
                {
                    p = new Point3D(item.WorldPosition.X + args[3].AsInt(), item.WorldPosition.Y + args[4].AsInt(), item.WorldPosition.Z + args[5].AsInt());
                    DragDropManager.DragDrop(item, p, amount < 1 ? item.Amount : amount);
                }
                else
                {
                    p = new Point3D(item.Position.X + args[3].AsInt(), item.Position.Y + args[4].AsInt(), item.Position.Z + args[5].AsInt());
                    DragDropManager.DragDrop(item, destination, p, amount < 1 ? item.Amount : amount);
                }
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            return true;
        }

        private static Dictionary<string, AssistDirection> _Directions = new Dictionary<string, AssistDirection>()
        {
            { "north", AssistDirection.North },
            { "nord", AssistDirection.North },
            { "northeast", AssistDirection.Right },
            { "right", AssistDirection.Right },
            { "nordest", AssistDirection.Right },
            { "destra", AssistDirection.Right },
            { "east", AssistDirection.East },
            { "est", AssistDirection.East },
            { "southeast", AssistDirection.Down },
            { "down", AssistDirection.Down },
            { "sudest", AssistDirection.Down },
            { "basso", AssistDirection.Down },
            { "south", AssistDirection.South },
            { "sud", AssistDirection.South },
            { "southwest", AssistDirection.Left },
            { "sudovest", AssistDirection.Left },
            { "left", AssistDirection.Left },
            { "sinistra", AssistDirection.Left },
            { "west", AssistDirection.West },
            { "ovest", AssistDirection.West },
            { "northwest", AssistDirection.Up },
            { "nordovest", AssistDirection.Up },
            { "up", AssistDirection.Up },
            { "sopra", AssistDirection.Up }
        };

        private static Queue<AssistDirection> _MoveDirection = new Queue<AssistDirection>();
        private static bool Walk(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            else if(args[0] != null)
            {
                _MoveDirection.Clear();
                string[] sp = args[0].AsString().ToLower().Split(',');
                for(int i = 0; i < sp.Length; i++)
                {
                    if(_Directions.TryGetValue(sp[i].Trim(), out AssistDirection d))
                    {
                        _MoveDirection.Enqueue(d);
                    }
                }
                args[0] = null;
            }
            if (_MoveDirection.Count == 0)
            {
                return true;
            }
            if (ScriptManager.LastWalk > DateTime.UtcNow)
            {
                return false;
            }

            ScriptManager.LastWalk = DateTime.UtcNow + TimeSpan.FromMilliseconds(MovementSpeed.TimeToCompleteMovement(false,
                                                                             ClassicUO.Client.Game.UO.World.Player.IsMounted ||
                                                                             ClassicUO.Client.Game.UO.World.Player.SpeedMode == CharacterSpeedType.FastUnmount ||
                                                                             ClassicUO.Client.Game.UO.World.Player.SpeedMode == CharacterSpeedType.FastUnmountAndCantRun ||
                                                                             ClassicUO.Client.Game.UO.World.Player.IsFlying
                                                                             ));

            AssistDirection dir = _MoveDirection.Peek();
            if ((UOSObjects.Player.Direction & AssistDirection.Up) == dir)
            {
                _MoveDirection.Dequeue();
            }
            else
            {
                ScriptManager.LastWalk = DateTime.UtcNow + TimeSpan.FromMilliseconds(Constants.TURN_DELAY);
            }

            Engine.Instance.RequestMove(dir, false);
            return _MoveDirection.Count < 1;
        }

        private static bool Turn(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (_Directions.TryGetValue(args[0].AsString().ToLower(), out AssistDirection dir))
            {
                if ((UOSObjects.Player.Direction & AssistDirection.Up) != dir)
                {
                    if (ScriptManager.LastWalk > DateTime.UtcNow)
                    {
                        return false;
                    }

                    ScriptManager.LastWalk = DateTime.UtcNow + TimeSpan.FromMilliseconds(Constants.TURN_DELAY);
                    Engine.Instance.RequestMove(dir, false);
                }
            }

            return true;
        }

        private static bool Run(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            else if (args[0] != null)
            {
                _MoveDirection.Clear();
                string[] sp = args[0].AsString().ToLower().Split(',');
                for (int i = 0; i < sp.Length; i++)
                {
                    if (_Directions.TryGetValue(sp[i].Trim(), out AssistDirection d))
                    {
                        _MoveDirection.Enqueue(d);
                    }
                }
                args[0] = null;
            }
            if (_MoveDirection.Count == 0)
            {
                return true;
            }
            if (ScriptManager.LastWalk > DateTime.UtcNow)
            {
                return false;
            }

            AssistDirection dir = _MoveDirection.Peek();
            if ((UOSObjects.Player.Direction & AssistDirection.Up) == dir)
            {
                ScriptManager.LastWalk = DateTime.UtcNow + TimeSpan.FromMilliseconds(MovementSpeed.TimeToCompleteMovement(true,
                                                                             ClassicUO.Client.Game.UO.World.Player.IsMounted ||
                                                                             ClassicUO.Client.Game.UO.World.Player.SpeedMode == CharacterSpeedType.FastUnmount ||
                                                                             ClassicUO.Client.Game.UO.World.Player.SpeedMode == CharacterSpeedType.FastUnmountAndCantRun ||
                                                                             ClassicUO.Client.Game.UO.World.Player.IsFlying
                                                                             ));
                _MoveDirection.Dequeue();
            }
            else
            {
                ScriptManager.LastWalk = DateTime.UtcNow + TimeSpan.FromMilliseconds(Constants.TURN_DELAY);
            }

            Engine.Instance.RequestMove(dir, true);
            return _MoveDirection.Count < 1;
        }

        private static bool SetSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Skill sk;
            if (args[0].AsString() == "last" && UOSObjects.Player.LastSkill >= 0 && UOSObjects.Player.LastSkill < ClassicUO.Client.Game.UO.FileManager.Skills.Skills.Count)
            {
                sk = UOSObjects.Player.Skills[UOSObjects.Player.LastSkill];
            }
            else
            {
                sk = ScriptManager.GetSkill(args[0].AsString());
            }
            if (sk != null && sk.Index < ClassicUO.Client.Game.UO.FileManager.Skills.Skills.Count)
            {
                //"setskill ('skill name') ('up/down/locked')"
                switch(args[1].AsString().ToLower(XmlFileParser.Culture))
                {
                    case "up":
                        sk.Lock = LockType.Up;
                        break;
                    case "down":
                        sk.Lock = LockType.Down;
                        break;
                    case "locked":
                        sk.Lock = LockType.Locked;
                        break;
                    default:
                        ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                        return true;
                }
                NetClient.Socket.PSend_SkillsStatusChange((ushort)sk.Index, (byte)sk.Lock);
            }

            return true;
        }

        private static bool UseSkill(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args[0].AsString() == "last")
            {
                NetClient.Socket.PSend_UseSkill(UOSObjects.Player.LastSkill);
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            else
            {
                Skill sk = ScriptManager.GetSkill(args[0].AsString());
                if (sk != null && sk.Index < ClassicUO.Client.Game.UO.FileManager.Skills.Skills.Count)
                {
                    if (ClassicUO.Client.Game.UO.FileManager.Skills.Skills[sk.Index].HasAction)
                    {
                        UOSObjects.Player.LastSkill = sk.Index;
                        NetClient.Socket.PSend_UseSkill(sk.Index);
                        Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
                    }
                    else
                        ScriptManager.Message(quiet, $"Non usable skill: {args[0].AsString()}");
                }
                else
                    ScriptManager.Message(quiet, $"Unknown skill name: {args[0].AsString()}");
            }

            return true;
        }

        private static bool Feed(string command, Argument[] args, bool quiet, bool force)
        {
            if(args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            UOItem backpack = UOSObjects.Player.GetItemOnLayer(Layer.Backpack);
            if(backpack == null)
            {
                ScriptManager.Message(quiet, "Backpack not found");
                return true;
            }
            uint targetSerial = args[0].AsSerial();
            if (SerialHelper.IsMobile(targetSerial))
            {
                ushort graph = args[1].AsUShort(false);
                int hue = -1;
                int amount = -1;
                if(args.Length > 2)
                {
                    hue = args[2].AsInt();
                    if (args.Length > 3)
                        amount = args[3].AsInt();
                }
                UOItem item = null;
                if (graph > 0)
                {
                    item = backpack.FindItemByID(graph, true, hue);
                }
                else//not a graphic id, maybe a name?
                {
                    item = backpack.FindItemByID(Foods.GetFoodGraphics(args[1].AsString()), true, hue);
                }
                if(item != null)
                {
                    DragDropManager.DragDrop(item, targetSerial, Point3D.MinusOne, amount < 1 ? item.Amount : amount);
                }
                else
                    ScriptManager.Message(quiet, $"No valid food found");
            }
            return true;
        }

        private static bool Rename(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint targetSerial = args[0].AsSerial();
            if (SerialHelper.IsValid(targetSerial))
                NetClient.Socket.PSend_RenameReq(targetSerial, args[1].AsString());
            return true;
        }

        
        private static string _nextPromptAliasName = "";
        private static void OnPromptAliasTarget(bool location, uint serial, Point3D p, ushort gfxid)
        {
            if (SerialHelper.IsValid(serial))
            {
                if(!string.IsNullOrEmpty(_nextPromptAliasName))
                    Interpreter.SetAlias(_nextPromptAliasName, serial);
            }
            else
                ScriptManager.Message(false, "Invalid object targeted");
        }

        private static bool PromptAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, "Usage: promptalias ('name')");
                return true;
            }

            if (!_hasAction)
            {
                _hasAction = true;
                _nextPromptAliasName = args[0].AsString();
                Targeting.OneTimeTarget(false, OnPromptAliasTarget);
                ScriptManager.Message(false, $"Select the object for {_nextPromptAliasName}", null, MsgLevel.Info);
                return false;
            }

            if (!Targeting.HasTarget)
            {
                _hasAction = false;
                _nextPromptAliasName = "";
                return true;
            }

            return false;
        }

        private static bool Pause(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.Timeout(args[0].AsUInt(), () => { return true; });
            return false;
        }

        private static bool WaitForGump(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
            bool any = args[0].AsString() == "any";
            if (any)
            {
                if (UOSObjects.Player.OpenedGumps.Count > 0)
                {
                    return true;
                }
            }
            else
            {
                uint gumpId = args[0].AsSerial();
                if(UOSObjects.Player.OpenedGumps.TryGetValue(gumpId, out var glist) && glist.Count > 0)
                    return true;
            }
            return false;
        }

        private static bool Attack(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, "attack - invalid serial");
                return true;
            }

            if (SerialHelper.IsMobile(serial))
            {
                NetClient.Socket.PSend_AttackRequest(serial);
            }

            return true;
        }

        private static bool WarMode(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length > 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
            {
                switch(args[0].AsString().ToLower(XmlFileParser.Culture))
                {
                    case "on":
                    case "true":
                        NetClient.Socket.PSend_SetWarMode(true);
                        break;
                    case "off":
                    case "false":
                        NetClient.Socket.PSend_SetWarMode(false);
                        break;
                    default:
                        ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                        break;
                }
            }
            else
                NetClient.Socket.PSend_SetWarMode(!UOSObjects.Player.Warmode);
            return true;
        }

        private static bool ClearJournal(string command, Argument[] args, bool quiet, bool force)
        {
            Journal.Clear();
            return true;
        }

        private static bool WaitForJournal(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            bool system = args.Length > 2 ? args[2].AsString() == "system" : true, found;
            if (system)
            {
                found = Journal.ContainsSafe(args[0].AsString());
            }
            else
                found = Journal.ContainsFrom(args[2].AsString(), args[0].AsString());

            if (!found)
            {
                Interpreter.Timeout(args[1].AsUInt(), () => { return true; });
                return false;
            }
            return true;
        }

        private static bool Msg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString());
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString());

            return true;
        }

        private static bool WhisperMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString(), MessageType.Whisper);
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString(), MessageType.Whisper);

            return true;
        }

        private static bool YellMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString(), MessageType.Yell);
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString(), MessageType.Yell);

            return true;
        }

        private static bool EmoteMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString(), MessageType.Emote);
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString(), MessageType.Emote);

            return true;
        }

        private static bool PartyMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            if (PacketHandlers.PartyLeader == 0)
            {
                ScriptManager.Message(quiet, command, "You must be in a party to use 'partymsg'");
                return true;
            }

            NetClient.Socket.PSend_PartyMessage(args[0].AsString(), args.Length > 1 ? args[1].AsSerial() : 0);
            return true;
        }

        private static bool GuildMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString(), MessageType.Guild);
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString(), MessageType.Guild);

            return true;
        }

        private static bool AllyMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.Say(0x03B1, args[0].AsString(), MessageType.Alliance);
            else
                UOSObjects.Player.Say(args[1].AsInt(), args[0].AsString(), MessageType.Alliance);

            return true;
        }

        private static bool HeadMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
            {
                UOSObjects.Player.OverheadMessage(0x03B1, args[0].AsString());
            }
            else
            {
                int hue = Utility.ToInt32(args[1].AsString(), 0);

                if (args.Length > 2)
                {
                    uint serial = args[2].AsSerial();
                    UOMobile m = UOSObjects.FindMobile((uint)serial);

                    if (m != null)
                        m.OverheadMessage(hue, args[0].AsString());
                }
                else
                {
                    UOSObjects.Player.OverheadMessage(hue, args[0].AsString());
                }
            }

            return true;
        }

        private static bool SysMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
                UOSObjects.Player.SendMessage(0x03B1, args[0].AsString());
            else if (args.Length == 2)
                UOSObjects.Player.SendMessage(args[1].AsInt(), args[0].AsString());

            return true;
        }

        private static bool TimerMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            TimeSpan ts = Interpreter.GetTimer(args[0].AsString());
            if (args.Length == 1)
                UOSObjects.Player.SendMessage(0x03B1, ts.TotalMilliseconds.ToString("F0"));
            else if (args.Length == 2)
                UOSObjects.Player.SendMessage(args[1].AsInt(), ts.TotalMilliseconds.ToString("F0"));

            return true;
        }

        private static bool PopList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args[1].AsString() == "front")
            {
                if (force)
                    while (Interpreter.PopList(args[0].AsString(), true)) { }
                else
                    Interpreter.PopList(args[0].AsString(), true);
            }
            else if (args[1].AsString() == "back")
            {
                if (force)
                    while (Interpreter.PopList(args[0].AsString(), false)) { }
                else
                    Interpreter.PopList(args[0].AsString(), false);
            }
            else
            {
                if (force)
                    while (Interpreter.PopList(args[0].AsString(), args[1])) { }
                else
                    Interpreter.PopList(args[0].AsString(), args[1]);
            }

            return true;
        }

        private static bool PushList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            bool front = false;
            if (args.Length >= 3)
            {
                if (args[2].AsString() == "front")
                    front = true;
            }

            Interpreter.PushList(args[0].AsString(), args[1], front, force);

            return true;
        }

        private static bool RemoveList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.DestroyList(args[0].AsString());

            return true;
        }

        private static bool CreateList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.CreateList(args[0].AsString());

            return true;
        }

        private static bool ClearList(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.ClearList(args[0].AsString());

            return true;
        }

        private static bool SetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args.Length == 1)
            {
                return PromptAlias(command, args, true, force);
            }
            else
                Interpreter.SetAlias(args[0].AsString(), args[1].AsSerial());

            return true;
        }

        private static bool UnsetAlias(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.SetAlias(args[0].AsString(), 0);

            return true;
        }

        private static bool ShowNames(string command, Argument[] args, bool quiet, bool force)
        {
            if (ClassicUO.Client.Game.UO.World.Player == null)
                return true;
            byte type = 1;//mobile as default
            int range = ClassicUO.Client.Game.UO.World.ClientViewRange;
            if (args.Length > 0)
            {
                switch (args[0].AsString())
                {
                    case "any":
                    case "all":
                        type = 3;
                        break;
                    case "mobiles":
                        type = 1;
                        break;
                    case "corpses":
                        type = 2;
                        break;
                }
            }
            if (args.Length > 1)
                range = args[1].AsInt();
            switch (type)
            {
                case 3:
                case 1:
                    foreach (UOMobile m in UOSObjects.MobilesInRange(range))
                    {
                        if (m != UOSObjects.Player)
                        {
                            NetClient.Socket.PSend_SingleClick(m.Serial);
                        }
                    }
                    if (type == 3)
                        goto case 2;
                    break;
                case 2:
                    foreach (UOItem i in UOSObjects.ItemsInRange(range))
                    {
                        if (i.IsCorpse)
                        {
                            NetClient.Socket.PSend_SingleClick(i.Serial);
                        }
                    }
                    break;
            }
            return true;
        }

        private static bool ContextMenu(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint s = args[0].AsSerial();
            ushort index = args[1].AsUShort();

            if (s == 0 && UOSObjects.Player != null)
                s = UOSObjects.Player.Serial;

            NetClient.Socket.PSend_ContextMenuRequest(s);
            NetClient.Socket.PSend_ContextMenuResponse(s, index);
            return true;
        }

        private static bool WaitForContext(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.Timeout(args[2].AsUInt(), () =>
            {
                return true;
            });

            if (UOSObjects.Player.HasMenu)
            {
                NetClient.Socket.PSend_ContextMenuResponse(args[0].AsSerial(), args[1].AsUShort());
                return true;
            }
            return false;
        }

        private static DelayQueueTimer _DelayQueueTimer;
        private static bool Target(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint target = args[0].AsSerial();
            if (SerialHelper.IsValid(target))
            {
                if (force)
                {
                    if (!Targeting.HasTarget)
                    {
                        ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                    }
                    else
                    {
                        _DelayQueueTimer?.Stop();
                        Targeting.Target(target);
                    }
                }
                else
                {
                    _DelayQueueTimer?.Stop();
                    if (!Targeting.HasTarget)
                    {
                        uint time = 5000;
                        if (args.Length > 1)
                        {
                            time = args[1].AsUInt();
                        }
                        (_DelayQueueTimer = new DelayQueueTimer(Targeting.Target(target, true), time)).Start();
                    }
                    else
                    {
                        Targeting.Target(target);
                    }
                }
            }
            else
                ScriptManager.Message(quiet, command, "Invalid target serial.");
            return true;
        }
        private class DelayQueueTimer : Timer
        {
            private TargetInfo _Info;
            private uint _Total;
            private const uint INTERVAL = 50;
            private uint _Passed = INTERVAL;
            internal DelayQueueTimer(TargetInfo t, uint delay) : base(TimeSpan.FromMilliseconds(INTERVAL), TimeSpan.FromMilliseconds(INTERVAL))
            {
                _Info = t;
                _Total = delay;
            }

            protected override void OnTick()
            {
                _Passed += INTERVAL;
                if (_Passed >= _Total || _Info != Targeting.QueuedTarget)
                {
                    if (_Info == Targeting.QueuedTarget)
                        Targeting.ClearQueue();
                    Stop();
                }
            }
        }

        private static bool TargetType(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (command == "targettype" && !Targeting.HasTarget)
            {
                ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                return true;
            }

            var graphic = args[0].AsInt();

            uint serial = uint.MaxValue;

            switch (args.Length)
            {
                case 1:
                {
                    // Only graphic
                    
                    UOEntity ent = UOSObjects.FindEntityByType(graphic);
                    if (ent != null)
                        serial = ent.Serial;
                    break;
                }
                case 2:
                {
                    // graphic and color
                    var color = args[1].AsUShort();
                    UOEntity ent = UOSObjects.FindEntityByType(graphic, color);
                    if (ent != null)
                        serial = ent.Serial;
                    break;
                }
                case 3:
                {
                    // graphic, color, range
                    var color = args[1].AsUShort();
                    var range = args[2].AsInt();
                    UOEntity ent = UOSObjects.FindEntityByType(graphic, color, range);
                    if (ent != null)
                        serial = ent.Serial;
                    break;
                }
            }

            if (serial == uint.MaxValue)
            {
                ScriptManager.Message(quiet, "Unable to find suitable target");
                return true;
            }
            if(command == "targettype")
                Targeting.Target(serial);
            else
                Targeting.SetAutoTargetAction((int)serial);
            return true;
        }

        private static bool PromptMsg(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            UOSObjects.Player.ResponsePrompt(args[0].AsString());
            return true;
        }

        private static bool ToggleHands(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (args[0].AsString() == "left")
                Dress.ToggleLeft(quiet);
            else
                Dress.ToggleRight(quiet);

            return true;
        }

        private static bool EquipItem(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return true;

            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint ser = args[0].AsSerial();
            Layer layer = args[1].AsLayer();
            if (layer != Layer.Invalid && layer < Layer.Mount)
            {
                UOItem equip = null;
                if (SerialHelper.IsItem(ser))
                {
                    equip = UOSObjects.FindItem(ser);
                }
                else if (UOSObjects.Player.Backpack != null && ser > 0 && ser < ClassicUO.Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    equip = UOSObjects.Player.Backpack.FindItemByID((ushort)ser, true, -1, layer, true);
                }

                if (equip != null)
                {
                    Dress.Unequip(layer);
                    Dress.Equip(equip, layer, force);
                }
            }
            else
                ScriptManager.Message(quiet, $"equipitem: invalid layer");

            return true;
        }

        internal static HashSet<ushort> WandTypes { get; } = new HashSet<ushort>() { 0xDEB, 0xDEC, 0xDED, 0xDEE, 0xDF2, 0xDF3, 0xDF4, 0xDF5 };
        private static bool EquipWand(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return true;

            string spellname;
            if (args.Length < 1 || string.IsNullOrEmpty(spellname = args[0].AsString()))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            int charges = 0;
            if(args.Length > 1)
            {
                charges = args[1].AsInt();
            }
            bool anyspell = spellname == "any" || spellname == "undefined";

            List<UOItem> list = new List<UOItem>(UOSObjects.FindItemsByTypes(WandTypes, range: 3).Where(w => w.Name != null && (anyspell || w.Name.IndexOf(spellname, StringComparison.InvariantCultureIgnoreCase) >= 0) && (charges <= 0 || (w.ObjPropList != null && w.ObjPropList.Content.Any(opl => opl.Number == 1076207 && int.TryParse(opl.Args, out int wcharges) && wcharges >= charges)))));
            if (list.Count > 0)
            {
                Dress.Unequip(Layer.OneHanded);
                Dress.Equip(list[Utility.Random(list.Count)], Layer.OneHanded);
            }
            else
                ScriptManager.Message(quiet, $"equipwand: No wands found");

            return true;
        }

        private static bool ToggleScavenger(string command, Argument[] args, bool quiet, bool force)
        {
            UOSObjects.Gump.EnabledScavenger.IsChecked = !UOSObjects.Gump.EnabledScavenger.IsChecked;
            return true;
        }

        private static bool Info(string command, Argument[] args, bool quiet, bool force)
        {
            if (!_hasAction)
            {
                _hasAction = true;
                Targeting.OneTimeTarget(false, InfoTargetSelected, TargetCancel);
            }
            return !_hasAction;
        }

        private static void InfoTargetSelected(bool loc, uint serial, Point3D p, ushort itemid)
        {
            _hasAction = false;
            UOEntity ent;
            if (SerialHelper.IsValid(serial) && (ent = UOSObjects.FindEntity(serial)) != null)
            {
                UIManager.Add(new AssistantGump.ObjectInspectorGump(ent));
            }
        }

        private static bool Ping(string command, Argument[] args, bool quiet, bool force)
        {
            Assistant.Ping.StartPing(5);

            return true;
        }

        private static DateTime lastResync = DateTime.MinValue;
        private static bool Resync(string command, Argument[] args, bool quiet, bool force)
        {
            if (lastResync.AddMilliseconds(800) < DateTime.UtcNow)
            {
                lastResync = DateTime.UtcNow;
                NetClient.Socket.PSend_Resync();
            }

            return true;
        }

        internal static bool HasMessageGump = false, MessageEnded = true;
        private static bool MessageBox(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            if(!HasMessageGump)
            {
                if (MessageEnded)
                {
                    string title = args[0].AsString(), body = args[1].AsString();
                    MessageEnded = false;
                    HasMessageGump = true;
                    UIManager.Add(new AssistantGump.MessageBoxGump(title, body));
                }
                else
                    MessageEnded = true;
            }
            return MessageEnded;
        }

        private static bool ClickScreen(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            int x = Utility.ToInt32(args[0].AsString(), -1);
            int y = Utility.ToInt32(args[1].AsString(), -1);
            if(x >= 0 && y >= 0 && x <= Client.Game.Window.ClientBounds.Width && y <= Client.Game.Window.ClientBounds.Height)
            {
                Mouse.Position.X = x;
                Mouse.Position.Y = y;
                MouseButtonType mbt = MouseButtonType.Left;
                bool dclick = false;
                for(int i = args.Length - 1; i >= 2; --i)
                {
                    string s = args[i].AsString();
                    if (s == "double")
                        dclick = true;
                    else if (s == "right")
                        mbt = MouseButtonType.Right;
                }

                if(dclick)
                {
                    UIManager.OnMouseDoubleClick(mbt);
                }
                else
                {
                    UIManager.OnMouseButtonDown(mbt);
                }

                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
                UIManager.OnMouseButtonUp(mbt);
            }
            else
                ScriptManager.Message(quiet, command, "x or y coordinates are out of bounds or negative value!");

            return true;
        }

        private static bool Paperdoll(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length > 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
            }
            else
            {
                uint serial = args.Length == 0 ? UOSObjects.Player.Serial : args[0].AsSerial();
                NetClient.Socket.PSend_DoubleClick(serial);
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }

            return true;
        }

        public static bool Cast(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Spell spell = int.TryParse(args[0].AsString(), out int spellnum)
                ? Spell.Get(spellnum)
                : Spell.GetByName(args[0].AsString());

            if (spell != null)
            {
                if (args.Length > 1)
                {
                    uint s = args[1].AsSerial();
                    if (force)
                        Targeting.ClearQueue();
                    if (SerialHelper.IsValid(s))
                    {
                        Targeting.Target(s, true);
                    }
                    else if (!quiet)
                        ScriptManager.Message(quiet, command, "invalid serial or alias");
                }
                Spell.FullCast(spell.Number);//.OnCast(new CastSpellFromMacro((ushort)spell.GetID()));
                Interpreter.Pause(ScriptManager.MACRO_ACTION_DELAY);
            }
            else if (!quiet)
            {
                ScriptManager.Message(quiet, command, "spell name or number not valid");
            }

            return true;
        }

        private static bool HelpButton(string command, Argument[] args, bool quiet, bool force)
        {
            GameActions.RequestHelp();
            return true;
        }

        private static bool GuildButton(string command, Argument[] args, bool quiet, bool force)
        {
            NetClient.Socket.Send_GuildMenuRequest(ClassicUO.Client.Game.UO.World);
            return true;
        }

        private static bool QuestsButton(string command, Argument[] args, bool quiet, bool force)
        {
            NetClient.Socket.Send_QuestMenuRequest(ClassicUO.Client.Game.UO.World);
            return true;
        }

        private static bool LogoutButton(string command, Argument[] args, bool quiet, bool force)
        {
            Client.Game.GetScene<ClassicUO.Game.Scenes.GameScene>()?.RequestQuitGame();
            return true;
        }

        private static bool Virtue(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            byte id = 0;
            switch(args[0].AsString().ToLower(XmlFileParser.Culture))
            {
                case "honor":
                    id = 1; break;
                case "sacrifice":
                    id = 2; break;
                case "valor":
                    id = 3; break;
                default:
                    ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}"); break;
            }
            if (id > 0)
                NetClient.Socket.PSend_InvokeVirtue(id);
            return true;
        }

        private static bool AddFriend(string command, Argument[] args, bool quiet, bool force)
        {
            if (!_hasAction)
            {
                _hasAction = true;
                Targeting.OneTimeTarget(false, FriendSelected, TargetCancel);
            }
            return !_hasAction;
        }

        private static bool RemoveFriend(string command, Argument[] args, bool quiet, bool force)
        {
            if (!_hasAction)
            {
                _hasAction = true;
                Targeting.OneTimeTarget(false, RemoveFriendSelected, TargetCancel);
            }
            return !_hasAction;
        }

        private static bool IgnoreObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            uint serial = args[0].AsSerial();
            if (SerialHelper.IsValid(serial))
                Expressions.IgnoredObjects.Add(serial);
            return true;
        }

        private static bool ClearIgnoreList(string command, Argument[] args, bool quiet, bool force)
        {
            Expressions.IgnoredObjects.Clear();
            return !_hasAction;
        }

        private static void FriendSelected(bool loc, uint serial, Point3D p, ushort itemid)
        {
            _hasAction = false;
            Targeting.OnFriendTargetSelected(loc, serial, p, itemid);
        }

        private static void RemoveFriendSelected(bool loc, uint serial, Point3D p, ushort itemid)
        {
            _hasAction = false;
            Targeting.OnRemoveFriendSelected(loc, serial, p, itemid);
        }

        private static void TargetCancel()
        {
            _hasAction = false;
        }

        internal static int ColorPick { get; set; }
        private static bool AutoColorPick(string command, Argument[] args, bool quiet, bool force)
        {
            if(args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            ColorPick = args[0].AsUShort();
            return true;
        }

        private static bool WaitForContents(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            if(!_hasAction)
            {
                uint ser = args[0].AsSerial();
                if (SerialHelper.IsItem(ser))
                {
                    _hasObject = ser;
                    _hasAction = true;
                    PacketHandler.RegisterServerToClientViewer(0x3C, OnContentPacket);
                    NetClient.Socket.PSend_DoubleClick(ser);
                }
                else
                    return true;
            }

            Interpreter.Timeout(args[1].AsUInt(), () =>
            {
                _hasAction = false;
                _hasObject = 0;
                PacketHandler.RemoveServerToClientViewer(0x3C, OnContentPacket);
                return true;
            });

            if(!_hasAction)
            {
                _hasObject = 0;
                PacketHandler.RemoveServerToClientViewer(0x3C, OnContentPacket);
                return true;//ended with positive result
            }

            return false;
        }

        private static void OnContentPacket(ref StackDataReader p, PacketHandlerEventArgs args)
        {
            if (!_hasAction)
                return;
            int count = p.ReadUInt16BE();
            for (int i = 0; i < count; i++)
            {
                if (!DragDropManager.EndHolding(p.ReadUInt32BE()))
                    continue;
                p.Skip(9 + (Engine.UsePostKRPackets ? 1 : 0));
                if(_hasObject == p.ReadUInt32BE())
                {
                    _hasAction = false;
                    break;
                }
                p.ReadUInt16BE();
            }
        }

        private static bool SetTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.SetTimer(args[0].AsString(), args[1].AsInt());
            return true;
        }

        private static bool RemoveTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.RemoveTimer(args[0].AsString());
            return true;
        }

        private static bool CreateTimer(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.CreateTimer(args[0].AsString());
            return true;
        }

        private static bool TargetTileRelative(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (!Targeting.HasTarget && command == "targettilerelative")
            {
                ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                return true;
            }

            var serial = args[0].AsSerial();
            var range = args[1].AsInt();

            var mobile = UOSObjects.FindMobile(serial);

            if (mobile == null)
            {
                /* TODO: Search items if mobile not found. Although this isn't very useful. */
                ScriptManager.Message(quiet, command, "item or mobile not found.");
                return true;
            }

            var position = new Point3D(mobile.Position);

            switch (mobile.Direction)
            {
                case AssistDirection.North:
                    position.Y -= range;
                    break;
                case AssistDirection.Right:
                    position.X += range;
                    position.Y -= range;
                    break;
                case AssistDirection.East:
                    position.X += range;
                    break;
                case AssistDirection.Down:
                    position.X += range;
                    position.Y += range;
                    break;
                case AssistDirection.South:
                    position.Y += range;
                    break;
                case AssistDirection.Left:
                    position.X -= range;
                    position.Y += range;
                    break;
                case AssistDirection.West:
                    position.X -= range;
                    break;
                case AssistDirection.Up:
                    position.X -= range;
                    position.Y -= range;
                    break;
            }

            if (command == "targettilerelative")
                Targeting.Target(position);
            else
                Targeting.SetAutoTargetAction(position.X, position.Y, position.Z);
            return true;
        }

        private static bool WaitForTarget(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Interpreter.Timeout(args[0].AsUInt(), () =>
            {
                return true;
            });

            return Targeting.HasTarget;
        }

        private static bool TargetGround(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (!Targeting.HasTarget && command == "targetground")
            {
                ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                return true;
            }

            ushort graphic = args[0].AsUShort();
            if (graphic == 0)
            {
                ScriptManager.Message(quiet, command, "invalid graphic in targetground");
                return true;
            }
            int color = args.Length >= 2 ? args[1].AsUShort() : -1;
            int range = args.Length >= 3 ? args[2].AsInt() : ClassicUO.Client.Game.UO.World.ClientViewRange;
            Point3D p = Point3D.MinusOne;
            foreach (UOEntity ie in UOSObjects.EntitiesInRange(range))
            {
                if (ie.Graphic == graphic && (color == -1 || ie.Hue == color))
                {
                    p = ie.Position;
                    break;
                }
            }
            if (p != Point3D.MinusOne)
            {
                if (command == "targetground")
                    Targeting.Target(p);
                else
                    Targeting.SetAutoTargetAction(p.X, p.Y, p.Z);
            }
            else
                ScriptManager.Message(quiet, command, "No valid target found");
            return true;
        }

        private static bool TargetTile(string command, Argument[] args, bool quiet, bool force)
        {
            if (!(args.Length == 1 || args.Length == 3))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (!Targeting.HasTarget && command == "targettile")
            {
                ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                return true;
            }

            Point3D position = Point3D.MinusOne;

            switch (args.Length)
            {
                case 1:
                {
                    var alias = args[0].AsString();
                    if (alias == "last")
                    {
                        if (Targeting.LastTargetInfo.Type != 1)
                        {
                            ScriptManager.Message(quiet, command, "Last target was not a ground target");
                            return true;
                        }

                        position = new Point3D(Targeting.LastTargetInfo.X, Targeting.LastTargetInfo.Y, Targeting.LastTargetInfo.Z);
                    }
                    else if (alias == "current")
                    {
                        position = UOSObjects.Player.Position;
                    }
                    break;
                }
                case 3:
                    position = new Point3D(args[0].AsInt(), args[1].AsInt(), args[2].AsInt());
                    break;
            }

            if (position == Point3D.MinusOne)
            {
                ScriptManager.Message(quiet, command, "No valid target found");
                return true;
            }
            if(command == "targettile")
                Targeting.Target(position);
            else
                Targeting.SetAutoTargetAction(position.X, position.Y, position.Z);
            return true;
        }

        private static bool TargetTileOffset(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (!Targeting.HasTarget && command == "targettileoffset")
            {
                ScriptManager.Message(quiet, command, "No target cursor available. Consider using waitfortarget.");
                return true;
            }

            var position = new Point3D(UOSObjects.Player.Position);

            position.X += args[0].AsInt();
            position.Y += args[1].AsInt();
            position.Z += args[2].AsInt();
            if (command == "targettileoffset")
                Targeting.Target(position);
            else
                Targeting.SetAutoTargetAction(position.X, position.Y, position.Z);
            return true;
        }

        private static bool CancelTarget(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            if (Targeting.HasTarget)
                Targeting.CancelTarget();

            return true;
        }

        private static bool ClearTargetQueue(string command, Argument[] args, bool quiet, bool force)
        {
            Targeting.ClearQueue();
            return true;
        }

        private static bool AutoTargetLast(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            Targeting.SetAutoTargetAction();
            return true;
        }

        private static bool AutoTargetSelf(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            Targeting.SetAutoTargetAction((int)UOSObjects.Player.Serial);
            return true;
        }

        private static bool AutoTargetObject(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            uint ser = args[0].AsSerial();
            if (!SerialHelper.IsValid(ser))
                ScriptManager.Message(quiet, command, "invalid target serial");
            else
                Targeting.SetAutoTargetAction((int)ser);
            return true;
        }

        private static bool AutoTargetGhost(string command, Argument[] args, bool quiet, bool force)
        {
            if(args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            int range = args[0].AsInt();
            int zrange = -1;
            if (args.Length > 1)
                zrange = args[1].AsInt();

            List<UOMobile> l = UOSObjects.MobilesInRange(range, false);
            if (l.Count > 0)
            {
                l.Sort(UOSObjects.PlayerDistanceComparer.Instance);
                Targeting.SetAutoTargetAction((int)l[0].Serial);
            }
            else
                ScriptManager.Message(quiet, command, "No valid target found");
            return true;
        }

        private static bool CancelAutoTarget(string command, Argument[] args, bool quiet, bool force)
        {
            Targeting.CancelAutoTargetAction();
            return true;
        }

        internal static bool ToggleMounted(string command, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Gump.MountSerial == uint.MaxValue)
            {
                UOSObjects.Gump.MountSerial = 0;
                Targeting.OneTimeTarget(false, TargetMountResponse, CancelMountResponse);
                return false;
            }
            if (UOSObjects.Gump.MountSerial == 0)
                return false;
            FinalizeMounting(quiet, command);
            return true;
        }

        internal static void TargetMountResponse(bool location, uint serial, Point3D p, ushort gfxid)
        {
            if (serial != 0)
                UOSObjects.Gump.MountSerial = serial;
            else
                UOSObjects.Gump.MountSerial = uint.MaxValue;
        }

        private static void CancelMountResponse()
        {
            UOSObjects.Gump.MountSerial = uint.MaxValue;
        }

        private static void FinalizeMounting(bool quiet, string command)
        {
            if (SerialHelper.IsValid(UOSObjects.Gump.MountSerial))
            {
                uint? ser = UOSObjects.FindMobile(UOSObjects.Gump.MountSerial)?.Serial;
                if (!ser.HasValue)
                    ser = UOSObjects.Player.GetItemOnLayer(Layer.Mount)?.Serial;
                if (ser.HasValue)
                {
                    NetClient.Socket.PSend_DoubleClick(ser.Value);
                    return;
                }
                ScriptManager.Message(quiet, command, "Mount not found");
            }
            else
            {
                ScriptManager.Message(quiet, command, "Invalid mount type selected");
            }
            UOSObjects.Gump.MountSerial = uint.MaxValue;
        }

        private static bool ReplyGump(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            bool any = false;
            if (args[0].AsString() == "any")
                any = true;
            uint gumpid = 0;
            if(!any)
                gumpid = args[0].AsUInt();
            List<PlayerData.GumpData> gumps;
            if (any)
            {
                gumps = new List<PlayerData.GumpData>();
                foreach(var list in UOSObjects.Player.OpenedGumps.Values)
                {
                    gumps.AddRange(list);
                }
            }
            else if(!UOSObjects.Player.OpenedGumps.TryGetValue(gumpid, out gumps) || gumps.Count == 0)
            {
                ScriptManager.Message(quiet, command, $"gump id 0x{gumpid:X} not found");
            }
            if (gumps != null && gumps.Count > 0)
            {
                var gump = gumps[gumps.Count - 1];
                int buttonId = args[1].AsInt();
                List<int> checkboxes = new List<int>();
                List<GumpTextEntry> textentries = new List<GumpTextEntry>();
                if (args.Length > 2)
                {
                    for (int i = 2; i < args.Length; i++)
                    {
                        string[] split = args[i].AsString().Split(' ');
                        if (split.Length > 1)
                        {
                            textentries.Add(new GumpTextEntry(Utility.ToUInt16(split[0], 0), args[i].AsString().Remove(0, split[0].Length + 1)));
                        }
                        else
                            checkboxes.Add(args[i].AsInt(false));
                    }
                }

                List<Gump> ie = new List<Gump>(UIManager.Gumps.Where(g => g.ServerSerial == gumpid));
                foreach (var gmp in ie)
                {
                    UIManager.Gumps.Remove(gmp);
                    gmp.Dispose();
                }
                NetClient.Socket.PSend_GumpResponse(gump.ServerID, gump.GumpID, buttonId, checkboxes, textentries);
            }
            return true;
        }

        private static bool CloseGump(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }
            uint serial = args[1].AsSerial();
            UOEntity entity = null;
            if(SerialHelper.IsMobile(serial))
                entity = UOSObjects.FindMobile(serial);
            else if(SerialHelper.IsItem(serial))
                entity = UOSObjects.FindItem(serial);
            if (entity != null)
            {
                switch(args[0].AsString())
                {
                    case "paperdoll":
                    {
                        UIManager.GetGump<PaperDollGump>(serial)?.Dispose();
                        break;
                    }
                    case "status":
                    {
                        UIManager.GetGump<StatusGumpBase>(serial)?.Dispose();
                        break;
                    }
                    case "profile":
                    {
                        UIManager.GetGump<ProfileGump>(serial)?.Dispose();
                        break;
                    }
                    case "container":
                    {
                        UIManager.GetGump<ContainerGump>(serial)?.Dispose();
                        break;
                    }
                }
            }
            else
                ScriptManager.Message(quiet, command, "No objects with that serial was found");
            return true;
        }

        private static bool RandomNumber(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, command, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            Expressions.RandomNumber(command, args, quiet, force);
            return true;
        }

        private static bool _SentRequest = false;
        private static bool WaitForProperties(string command, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetCmdHelper(command)}");
                return true;
            }

            uint serial = args[0].AsSerial();

            if(!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, command, $"Invalid serial 0x{serial:X8}");
                return true;
            }

            UOEntity entity = UOSObjects.FindEntity(serial);
            if(!_SentRequest && (entity == null || entity.ObjPropList == null))
            {
                _SentRequest = true;
                NetClient.Socket.PSend_SingleClick(serial);//look request
            }

            Interpreter.Timeout(args[1].AsUInt(), () =>
            {
                _SentRequest = false;
                return true;
            });

            if(entity != null && entity.ObjPropList != null)
            {
                _SentRequest = false;
                return true;
            }

            return false;
        }

        internal static void ClearAll()
        {
            _SentRequest = false;
            ColorPick = -1;
            _DelayQueueTimer?.Stop();
            _hasAction = false;
            _nextPromptAliasName = "";
            _MoveDirection.Clear();
        }

        internal static void OnDisconnected()
        {
            _hasAction = false;
            _hasObject = 0;
            ColorPick = -1;
            HasMessageGump = false;
            MessageEnded = true;
            _nextPromptAliasName = "";
            _MoveDirection.Clear();
            UsedOnce.Clear();
            NextUsedOnce = 0;
            _Temporary = null;
            _SentRequest = false;
            _DelayQueueTimer?.Stop();
        }
    }
}
