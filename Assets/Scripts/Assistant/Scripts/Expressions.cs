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
using System.Linq;
using System.Collections.Generic;
using UOScript;

using Assistant.Core;
using ClassicUO;
using ClassicUO.Utility;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Assets;

namespace Assistant.Scripts
{
    public static class Expressions
    {
        public static void Register()
        {
            Interpreter.RegisterExpressionHandler("contents", Contents, "contents (serial) (operator) (value)");
            Interpreter.RegisterExpressionHandler("inregion", InRegion, "inregion (\"guards\"/\"town\"/\"dungeon\"/\"forest\"/\"any\") [serial] [range]");
            Interpreter.RegisterExpressionHandler("skill", SkillExpression, "skill (\"skill name\") (operator) (value)");
            Interpreter.RegisterExpressionHandler("x", X, "x [serial]");
            Interpreter.RegisterExpressionHandler("y", Y, "y [serial]");
            Interpreter.RegisterExpressionHandler("z", Z, "z [serial]");
            Interpreter.RegisterExpressionHandler("physical", Physical, "physical (operator) (value)");
            Interpreter.RegisterExpressionHandler("fire", Fire, "fire (operator) (value)");
            Interpreter.RegisterExpressionHandler("cold", Cold, "cold (operator) (value)");
            Interpreter.RegisterExpressionHandler("poison", Poison, "poison (operator) (value)");
            Interpreter.RegisterExpressionHandler("energy", Energy, "energy (operator) (value)");
            Interpreter.RegisterExpressionHandler("str", Str, "str (operator) (value)");
            Interpreter.RegisterExpressionHandler("dex", Dex, "dex (operator) (value)");
            Interpreter.RegisterExpressionHandler("int", Int, "int (operator) (value)");
            Interpreter.RegisterExpressionHandler("hits", Hits, "hits [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("findalias", FindAlias, "findalias (\"alias\")");
            Interpreter.RegisterExpressionHandler("maxhits", MaxHits, "maxhits [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("diffhits", DiffHits, "diffhits [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("stam", Stam, "stam [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("maxstam", MaxStam, "maxstam [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("mana", Mana, "mana [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("maxmana", MaxMana, "maxmana [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("usequeue", UseQueue, "usequeue (operator) (value)");
            Interpreter.RegisterExpressionHandler("dressing", Dressing, "dressing");
            Interpreter.RegisterExpressionHandler("organizing", Organizing, "organizing");
            Interpreter.RegisterExpressionHandler("followers", Followers, "followers (operator) (value)");
            Interpreter.RegisterExpressionHandler("maxfollowers", MaxFollowers, "maxfollowers (operator) (value)");
            Interpreter.RegisterExpressionHandler("gold", Gold, "gold (operator) (value)");
            Interpreter.RegisterExpressionHandler("hidden", Hidden, "hidden [serial]");
            Interpreter.RegisterExpressionHandler("abilitypoints", Luck, "abilitypoints (operator) (value)");
            Interpreter.RegisterExpressionHandler("faithpoints", TithingPoints, "faithpoints (operator) (value)");
            Interpreter.RegisterExpressionHandler("weight", Weight, "weight (operator) (value)");
            Interpreter.RegisterExpressionHandler("maxweight", MaxWeight, "maxweight (operator) (value)");
            Interpreter.RegisterExpressionHandler("diffweight", DiffWeight, "diffweight (operator) (value)");
            Interpreter.RegisterExpressionHandler("serial", Serial, "serial (\"alias\") (operator) (value)");
            Interpreter.RegisterExpressionHandler("graphic", Graphic, "graphic (serial) (operator) (value)");
            Interpreter.RegisterExpressionHandler("color", Color, "color (serial) (operator) (value)");
            Interpreter.RegisterExpressionHandler("amount", Amount, "amount (serial) (operator) (value)");
            Interpreter.RegisterExpressionHandler("name", Name, "name [serial] (== or !=) (value)");
            Interpreter.RegisterExpressionHandler("dead", Dead, "dead [serial]");
            Interpreter.RegisterExpressionHandler("direction", Direction, "direction [serial] (operator) (value)");
            Interpreter.RegisterExpressionHandler("flying", Flying, "flying [serial]");
            Interpreter.RegisterExpressionHandler("paralyzed", Paralyzed, "paralyzed [serial]");
            Interpreter.RegisterExpressionHandler("poisoned", Poisoned, "poisoned [serial]");
            Interpreter.RegisterExpressionHandler("mounted", Mounted, "mounted [serial]");
            Interpreter.RegisterExpressionHandler("yellowhits", YellowHits, "yellowhits [serial]");
            Interpreter.RegisterExpressionHandler("criminal", Criminal, "criminal [serial]");
            Interpreter.RegisterExpressionHandler("enemy", Enemy, "enemy [serial]");
            Interpreter.RegisterExpressionHandler("friend", Friend, "friend [serial]");
            Interpreter.RegisterExpressionHandler("gray", Gray, "gray [serial]");
            Interpreter.RegisterExpressionHandler("innocent", Innocent, "innocent [serial]");
            Interpreter.RegisterExpressionHandler("invulnerable", Invulnerable, "invulnerable [serial]");
            Interpreter.RegisterExpressionHandler("murderer", Murderer, "murderer [serial]");
            Interpreter.RegisterExpressionHandler("findobject", FindObject, "findobject (serial) [color] [source] [amount] [range]");
            Interpreter.RegisterExpressionHandler("distance", Distance, "distance (serial) (operator) (value)");
            Interpreter.RegisterExpressionHandler("inrange", InRange, "inrange (serial) (range)");
            Interpreter.RegisterExpressionHandler("buffexists", BuffExists, "buffexists (\"buff name\")");
            Interpreter.RegisterExpressionHandler("findtype", FindType, "findtype (graphic) [color] [source] [amount] [range]");
            Interpreter.RegisterExpressionHandler("findlayer", FindLayer, "findlayer (serial) (layer)");
            Interpreter.RegisterExpressionHandler("skillstate", SkillState, "skillstate (\"skill name\") (== or !=) (\"up/down/locked\")");
            Interpreter.RegisterExpressionHandler("counttype", CountType, "counttype (graphic) (color) (source) (operator) (value)");
            Interpreter.RegisterExpressionHandler("counttypeground", CountTypeGround, "counttypeground (graphic) (color) (range) (operator) (value)");
            Interpreter.RegisterExpressionHandler("findwand", FindWand, "findwand (\"spell name\"/\"any\"/\"undefined\") [source] [minimum charges]");
            Interpreter.RegisterExpressionHandler("infriendslist", InFriendsList, "infriendlist (serial)");
            Interpreter.RegisterExpressionHandler("war", War, "war [serial]");
            Interpreter.RegisterExpressionHandler("ingump", InGump, "ingump (gump id/\"any\") (\"text\")");
            Interpreter.RegisterExpressionHandler("gumpexists", GumpExists, "gumpexists (gump id/\"any\")");
            Interpreter.RegisterExpressionHandler("injournal", InJournal, "injournal (\"text\") [\"author\"/\"system\"]");
            Interpreter.RegisterExpressionHandler("listexists", ListExists, "listexists (\"list name\")");
            Interpreter.RegisterExpressionHandler("list", ListLength, "list (\"list name\") (operator) (value)");
            Interpreter.RegisterExpressionHandler("inlist", InList, "inlist (\"list name\") (\"element value\")");
            Interpreter.RegisterExpressionHandler("targetexists", TargetExists, "targetexists [\"any\"/\"beneficial\"/\"harmful\"/\"neutral\"/\"server\"/\"system\"]");
            Interpreter.RegisterExpressionHandler("waitingfortarget", WaitingForTarget, "waitingfortarget");
            Interpreter.RegisterExpressionHandler("timer", TimerValue, "timer (\"timer name\") (operator) (value)");
            Interpreter.RegisterExpressionHandler("timerexists", TimerExists, "timerexists (\"timer name\")");
            Interpreter.RegisterExpressionHandler("clearhands", ClearHands, "clearhands (\"left\"/\"right\"/\"both\")");
            Interpreter.RegisterExpressionHandler("random", RandomNumber, "random [from] to");

            //as stated in uos documents, move* provide expression handling, but there is no way to check for the correct action without pausing the whole execution of the script, this is unacceptable, 
            //we will only check correct execution of the normal iteration, so we can check if there is a valid item to move and a valid destination
            Interpreter.RegisterExpressionHandler("moveitem", MoveItem, null);
            Interpreter.RegisterExpressionHandler("moveitemoffset", MoveItemOffset, null);
            Interpreter.RegisterExpressionHandler("movetype", MoveType, null);
            Interpreter.RegisterExpressionHandler("movetypeoffset", MoveTypeOffset, null);

            if (Client.Game.UO.FileManager.Version > ClientVersion.CV_200)
            {
                Interpreter.RegisterExpressionHandler("property", Property, "property (\"name\") (serial) [operator] [value]");
                Interpreter.RegisterExpressionHandler("inparty", InParty, "inparty (serial)");
            }
        }

        private static bool FindAlias(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = Interpreter.GetAlias(args[0].AsString());

            return serial != uint.MaxValue;
        }

        private static int Contents(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = args[0].AsSerial();

            UOItem container = UOSObjects.FindItem(serial);
            if (container == null || !container.IsContainer)
            {
                ScriptManager.Message(quiet, "Serial not found or is not a container.");
                return 0;
            }

            return container.ItemCount;
        }

        private static bool InRegion(string expression, Argument[] args, bool quiet, bool force)
        {
            if(args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            string type = args[0].AsString();
            Point3D p;
            if (args.Length > 1)
            {
                uint serial = args[1].AsSerial();
                if (SerialHelper.IsValid(serial))
                {
                    UOEntity ent = UOSObjects.FindEntity(serial);
                    if(ent != null)
                    {
                        p = ent.Position;
                    }
                    else
                    {
                        ScriptManager.Message(quiet, expression, $"object not found 0x{serial:X}");
                        return false;
                    }
                }
                else
                {
                    ScriptManager.Message(quiet, expression, $"invalid serial 0x{serial:X}");
                    return false;
                }
            }
            else
            {
                p = UOSObjects.Player.Position;
            }
            int range = 24;
            if(args.Length > 2)
            {
                range = args[2].AsInt();
            }
            string name = Region.Contains(p, type, range);

            if(!string.IsNullOrEmpty(name))
            {
                ScriptManager.Message(quiet, expression, $"object in {p} is inside {type} region: {name}");
                return true;
            }

            return false;
        }

        private static double SkillExpression(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            if (UOSObjects.Player == null)
                return 0;

            var skill = ScriptManager.GetSkill(args[0].AsString());
            if(skill == null)
            {
                ScriptManager.Message(quiet, expression, $"Unknown skill name: {args[0].AsString()}");
                return 0;
            }

            return skill.Value;
        }

        private static int X(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.Position.X;
            else if (args.Length != 1)
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Position.X;
        }

        private static int Y(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.Position.Y;
            else if (args.Length != 1)
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Position.Y;
        }

        private static int Z(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.Position.Z;
            else if (args.Length != 1)
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Position.Z;
        }

        private static int Physical(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.AR;
        }

        private static int Fire(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.FireResistance;
        }

        private static int Cold(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.ColdResistance;
        }

        private static int Poison(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.PoisonResistance;
        }

        private static int Energy(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.EnergyResistance;
        }

        private static int Str(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            return UOSObjects.Player.Str;
        }

        private static int Dex(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            return UOSObjects.Player.Dex;
        }

        private static int Int(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            return UOSObjects.Player.Int;
        }

        private static int Hits(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.Hits;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Hits;
        }

        private static int MaxHits(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.HitsMax;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.HitsMax;
        }

        private static int DiffHits(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.HitsMax - UOSObjects.Player.Hits;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.HitsMax - mobile.Hits;
        }


        private static int Stam(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.Stam;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Stam;
        }

        private static int MaxStam(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return 0;

            if (args.Length == 0)
                return UOSObjects.Player.StamMax;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.StamMax;
        }

        private static int Mana(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Mana;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.Mana;
        }

        private static int MaxMana(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.ManaMax;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return 0;
            }

            return mobile.ManaMax;
        }

        private static int UseQueue(string expression, Argument[] args, bool quiet, bool force)
        { 
            return ActionQueue.QueuedActions;
        }

        private static bool Dressing(string expression, Argument[] args, bool quiet, bool force)
        {
            return DragDropManager.IsDressing();
        }

        private static bool Organizing(string expression, Argument[] args, bool quiet, bool force)
        {
            return DragDropManager.IsOrganizing();
        }

        private static int Followers(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.Followers;
        }

        private static int MaxFollowers(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.FollowersMax;
        }

        private static uint Gold(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.Gold;
        }

        private static bool Hidden(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return !UOSObjects.Player.Visible;
            }
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return true;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return true;
            }
            else if (SerialHelper.IsItem(serial))
            {
                UOItem item = UOSObjects.FindItem(serial);

                if (item == null)
                {
                    ScriptManager.Message(quiet, expression, "item not found");
                    return true;
                }

                return !item.Visible;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return true;
                }

                return !mobile.Visible;
            }
        }

        private static int Luck(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.Luck;
        }

        private static int TithingPoints(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.Tithe;
        }

        private static double Weight(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.Weight;
        }

        private static int MaxWeight(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.MaxWeight;
        }

        private static int DiffWeight(string expression, Argument[] args, bool quiet, bool force)
        {
            return UOSObjects.Player.MaxWeight - UOSObjects.Player.Weight;
        }

        private static uint Serial(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = Interpreter.GetAlias(args[0].AsString());

            return serial;
        }

        private static int Graphic(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Body;
            }
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return 0;
            }
            else if (SerialHelper.IsItem(serial))
            {
                UOItem item = UOSObjects.FindItem(serial);

                if (item == null)
                {
                    ScriptManager.Message(quiet, expression, "item not found");
                    return 0;
                }

                return item.ItemID;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return 0;
                }

                return mobile.Body;
            }
        }

        private static int Color(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return 0;
            }
            else if (SerialHelper.IsItem(serial))
            {
                UOItem item = UOSObjects.FindItem(serial);

                if (item == null)
                {
                    ScriptManager.Message(quiet, expression, "item not found");
                    return 0;
                }

                return item.Hue;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return 0;
                }

                return mobile.Hue;
            }
        }

        private static int Amount(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial) || SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return 0;
            }
            else
            {
                UOItem item = UOSObjects.FindItem(serial);

                if (item == null)
                {
                    ScriptManager.Message(quiet, expression, "item not found");
                    return 0;
                }

                return item.Amount;
            }
        }

        private static string Name(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Name;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return string.Empty;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsValid(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return string.Empty;
            }
            else if (SerialHelper.IsItem(serial))
            {
                UOItem item = UOSObjects.FindItem(serial);

                if (item == null)
                {
                    ScriptManager.Message(quiet, expression, "item not found");
                    return string.Empty;
                }

                return item.Name;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return string.Empty;
                }

                return mobile.Name;
            }
        }

        private static bool Dead(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.IsGhost;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.IsGhost;
            }
        }

        private static int Direction(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return (int)UOSObjects.Player.Direction;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return 0;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return 0;
                }

                return (int)mobile.Direction;
            }
        }

        private static bool Flying(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Flying;
            }

            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.Flying;
            }
        }

        private static bool Paralyzed(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Paralyzed;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.Paralyzed;
            }
        }

        private static string[] hands = new string[3] { "left", "right", "both" };
        private static bool ClearHands(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0 || !hands.Contains(args[0].AsString()))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            switch (args[0].AsString())
            {
                case "left":
                    return Dress.Unequip(Layer.TwoHanded);
                case "right":
                    return Dress.Unequip(Layer.OneHanded);
                default:
                    return Dress.Unequip(Layer.TwoHanded) || Dress.Unequip(Layer.OneHanded);
            }
        }

        internal static int RandomNumber(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            int min, max;

            if((max = args[0].AsInt(false)) < 0)
            {
                ScriptManager.Message(false, "The value 'from' and 'to' must ALWAYS BE numbers equal or greater than zero");
            }
            else
            {
                if (args.Length > 1)
                {
                    if ((min = args[1].AsInt(false)) < 0)
                    {
                        ScriptManager.Message(false, "The value 'from' and 'to' must ALWAYS BE numbers equal or greater than zero");
                    }
                    else
                    {
                        int rnd;
                        if (min == max)
                        {
                            ScriptManager.Message(quiet, "The numbers 'from' and 'to' should be different");
                        }
                        else if(min > max)
                        {
                            rnd = max;
                            max = min;
                            min = rnd;
                        }
                        
                        rnd = Utility.Random(min, max);
                        Interpreter.SetAlias("rnd", (uint)rnd);

                        return rnd;
                    }
                }
                else
                {
                    int rnd = Utility.Random(max);
                    Interpreter.SetAlias("rnd", (uint)rnd);
                    return rnd;
                }
            }
            return 0;
        }

        private static bool Poisoned(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Poisoned;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.Poisoned;
            }
        }

        private static bool Mounted(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                if (UOSObjects.Player.GetItemOnLayer(Layer.Mount) != null)
                {
                    return true;
                }
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }
            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                if(mobile.GetItemOnLayer(Layer.Mount) != null)
                    return true;
            }
            return false;
        }

        private static bool YellowHits(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Blessed;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.Blessed;
            }
        }
        private static bool Criminal(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x4;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x4;
        }

        private static bool Enemy(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x5;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x5;
        }

        private static bool Friend(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x2;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x2;
        }

        private static bool Gray(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x3;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x3;
        }

        private static bool Innocent(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x1;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x1;
        }

        private static bool Invulnerable(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x7;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x7;
        }
        private static bool Murderer(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.Notoriety == 0x6;
            else if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }
            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.Notoriety == 0x6;
        }

        private static int Distance(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return int.MaxValue;
            }

            uint objSerial = args[0].AsSerial();
            if (!SerialHelper.IsValid(objSerial))
            {
                ScriptManager.Message(quiet, "invalid serial!");
                return int.MaxValue;
            }

            UOEntity entity;
            if (SerialHelper.IsItem(objSerial))
                entity = UOSObjects.FindItem(objSerial);
            else
                entity = UOSObjects.FindMobile(objSerial);

            if (entity == null)
            {
                if (!quiet)
                    ScriptManager.Message(quiet, $"No valid object found in distance -> {args[0].AsString()}");
                return int.MaxValue;
            }
            else
            {
                return Utility.Distance(UOSObjects.Player.Position, entity.WorldPosition);
            }
        }

        private static bool InRange(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint objSerial = args[0].AsSerial();
            if (!SerialHelper.IsValid(objSerial))
            {
                ScriptManager.Message(quiet, "invalid serial!");
                return false;
            }
            int range = args[1].AsInt();
            if (range >= 0)
            {
                
                UOEntity e = UOSObjects.FindEntity(objSerial);
                if(e != null)
                {
                    return (int)UOSObjects.Player.GetDistanceToSqrt(e) <= range;
                }
            }
            else
                ScriptManager.Message(quiet, "range can't be a negative value!");
            return false;
        }

        private static bool BuffExists(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }
            string name = args[0].AsString();
            if (!string.IsNullOrEmpty(name) && PlayerData.BuffNames.TryGetValue(name.ToLower(XmlFileParser.Culture), out int icon))
            {
                return UOSObjects.Player.BuffsDebuffs.Exists(b => b.IconNumber == icon);
            }
            else
                ScriptManager.Message(quiet, "buffexists: not a valid buff name, check for buff names in bufficons.xml inside Data directory");
            return false; 
        }

        private static double Property(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0.0;
            }
            string name = args[0].AsString();
            if (!string.IsNullOrEmpty(name))
            {
                //"property (\"name\") (serial) [operator] [value]"
                UOEntity entity = UOSObjects.FindEntity(args[1].AsSerial());
                if(entity != null)
                {
                    foreach(var opl in entity.ObjPropList.Content)
                    {
                        string text = Client.Game.UO.FileManager.Clilocs.Translate(opl.Number, opl.Args);
                        if(!string.IsNullOrEmpty(text) && text.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            string[] ss = opl.Args.Split('\t');
                            foreach (string s in ss)
                            {
                                if (!string.IsNullOrEmpty(s) && s[0] != '#')//just avoid clilocs
                                {
                                    const System.Globalization.NumberStyles STYLE = System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowParentheses | System.Globalization.NumberStyles.AllowTrailingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowTrailingWhite;
                                    if (s.StartsWith("0x"))
                                    {
                                        return Utility.ToInt32(s, 0, STYLE);
                                    }
                                    else
                                    {
                                        return Utility.ToDouble(s, 0.0, STYLE);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                    ScriptManager.Message(quiet, "property: object not found or invalid serial");
            }
            else
                ScriptManager.Message(quiet, "property: you need to use a valid property name");
            
            return 0.0; 
        }

        private static bool FindObject(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint objSerial = args[0].AsSerial();
            if (!SerialHelper.IsValid(objSerial))
            {
                return false;
            }
            UOEntity entity = null;
            if (SerialHelper.IsItem(objSerial))
                entity = UOSObjects.FindItem(objSerial);
            else
                entity = UOSObjects.FindMobile(objSerial);
            if(entity == null)
            {
                if(!quiet)
                    ScriptManager.Message(quiet, $"No valid object found in findobject -> {args[0].AsString()}");
                return false;
            }

            uint? color = null;
            if (args.Length >= 2 && args[1].AsString().ToLower(Interpreter.Culture) != "any")
            {
                color = args[1].AsUInt();
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

            uint? amount = null;
            if (args.Length >= 4 && args[3].AsString().ToLower(Interpreter.Culture) != "any")
            {
                amount = args[3].AsUInt();
            }

            uint? range = null;
            if (args.Length >= 5 && args[4].AsString().ToLower(Interpreter.Culture) != "any")
            {
                range = args[4].AsUInt();
            }
            
            if (args.Length < 3 || source == 0)
            {
                // No source provided or invalid. Treat as UOSObjects.
                if (color.HasValue && entity.Hue != color.Value)
                {
                    entity = null;
                }
                else if (entity is UOItem i)
                {
                    if (sourceStr == "ground" && !i.OnGround)
                        entity = null;
                    else if (amount.HasValue && i.Amount < amount)
                        entity = null;
                    else if (range.HasValue && !Utility.InRange(UOSObjects.Player.Position, i.GetWorldPosition(), (int)range.Value))
                        entity = null;
                }
                else if (range.HasValue && !Utility.InRange(UOSObjects.Player.Position, entity.Position, (int)range.Value))
                {
                    entity = null;
                }
            }
            else
            {
                if (entity is UOItem item)
                {
                    UOItem container = UOSObjects.FindItem(source);
                    if (container != null && container.IsContainer && item.Container == container)
                    {
                        if ((color.HasValue && item.Hue != color.Value) ||
                            (sourceStr == "ground" && !item.OnGround) ||
                            (amount.HasValue && item.Amount < amount) ||
                            (range.HasValue && !Utility.InRange(UOSObjects.Player.Position, item.GetWorldPosition(), (int)range.Value)))
                        {
                            entity = null;
                        }
                    }
                    else if (container == null)
                    {
                        ScriptManager.Message(quiet, $"Script Error: Couldn't find source '{sourceStr}'");
                    }
                    else if (!container.IsContainer)
                    {
                        ScriptManager.Message(quiet, $"Script Error: Source '{sourceStr}' is not a container!");
                    }
                }
                else
                {

                }
            }

            if (entity != null)
            {
                Interpreter.SetAlias("found", entity.Serial);
                return true;
            }

            if (!quiet)
            {
                ScriptManager.Message(quiet, $"Script Error: Couldn't find '{args[0].AsString()}'");
            }

            return false;
        }

        private static bool FindType(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length < 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            string graphicString = args[0].AsString();
            ushort graphicId = args[0].AsUShort();
            if(graphicId == 0)
                return false;
            
            ushort? color = null;
            if (args.Length >= 2 && args[1].AsString().ToLower(Interpreter.Culture) != "any")
            {
                color = args[1].AsUShort();
            }

            string sourceStr = null;
            uint source = 0;
            bool islayer = false;

            if (args.Length >= 3)
            {
                sourceStr = args[2].AsString().ToLower(Interpreter.Culture);
                if (sourceStr != "world" && sourceStr != "any" && sourceStr != "ground")
                {
                    source = args[2].AsSerial();
                    if((sourceStr == "righthand" || sourceStr == "lefthand") && SerialHelper.IsValid(source))
                    {
                        islayer = true;
                    }
                }
            }

            uint? amount = null;
            if (args.Length >= 4 && args[3].AsString().ToLower(Interpreter.Culture) != "any")
            {
                amount = args[3].AsUInt();
            }

            uint? range = null;
            if (args.Length >= 5 && args[4].AsString().ToLower(Interpreter.Culture) != "any")
            {
                range = args[4].AsUInt();
            }

            List<uint> list = new List<uint>();

            bool ground = sourceStr == "world" || sourceStr == "ground";
            bool any = sourceStr == "any" || (source == 0 && !ground);
            UOItem container = null;
            if (ground || any || ((container = UOSObjects.FindItem(source)) != null && container.IsContainer))
            {
                List<UOEntity> entities = new List<UOEntity>();
                if (container != null)
                    entities.AddRange(container.FindItemsByID(graphicId));
                else if (ground || any)
                    entities.AddRange(UOSObjects.FindEntitiesByType(graphicId, contained: any));

                foreach (UOEntity entity in entities)
                {
                    if (entity != null &&
                        (!color.HasValue || color.Value == ushort.MaxValue || entity.Hue == color.Value) &&
                        (!amount.HasValue || !SerialHelper.IsItem(entity.Serial) || (entity is UOItem item && item.Amount >= amount)) &&
                        (!range.HasValue || Utility.InRange(UOSObjects.Player.Position, entity.WorldPosition, (int)range.Value)))
                    {
                        list.Add(entity.Serial);
                    }
                }
            }
            else if (container != null && !container.IsContainer)
            {
                if (islayer)
                {
                    list.Add(source);
                }
                else
                {
                    ScriptManager.Message(quiet, $"Script Error: Source '{sourceStr}' is not a container!");
                }
            }

            list.RemoveAll(s => IgnoredObjects.Contains(s));
            uint found = UOSObjects.GetObjectInList(list);
            if (found > 0)
            {
                Interpreter.SetAlias("found", found);
                return true;
            }

            ScriptManager.Message(quiet, expression, $"Script Error: Couldn't find '{graphicString}'");
            return false;
        }

        internal static HashSet<uint> IgnoredObjects = new HashSet<uint>();

        private static bool FindLayer(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            UOItem layeredItem = mobile.GetItemOnLayer((Layer)args[1].AsInt());

            if(layeredItem != null)
            {
                Interpreter.SetAlias("found", layeredItem.Serial);
                return true;
            }
            return false;
        }

        private static string SkillState(string expression, Argument[] args, bool quiet, bool force)
        {
            if(args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return "unknown";
            }
            var skill = ScriptManager.GetSkill(args[0].AsString());
            if(skill == null)
            {
                ScriptManager.Message(quiet, expression, $"Unknown skill name: {args[0].AsString()}");
                return "unknown";
            }

            switch (skill.Lock)
            {
                case LockType.Down:
                    return "down";
                case LockType.Up:
                    return "up";
                case LockType.Locked:
                    return "locked";
            }
            return "unknown";
        }

        private static int CountType(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var graphic = args[0].AsInt();

            int hue = int.MaxValue;
            if (args[1].AsString().ToLower(Interpreter.Culture) != "any")
                hue = args[1].AsInt();

            var container = UOSObjects.FindItem(args[2].AsSerial());

            if (container == null)
            {
                ScriptManager.Message(quiet, expression, "Unable to find source container");
                return 0;
            }

            int count = 0;
            foreach (var item in container.Contents(true))
            {
                if (item.ItemID != graphic)
                    continue;

                if (hue != int.MaxValue && item.Hue != hue)
                    continue;

                count++;
            }

            return count;
        }

        private static int CountTypeGround(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 3)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var graphic = args[0].AsInt();

            int hue = int.MaxValue;
            if (args[1].AsString().ToLower(Interpreter.Culture) != "any")
                hue = args[1].AsInt();

            int range = Math.Max(0, args[2].AsInt());
            int count = 0;

            foreach (var item in UOSObjects.ItemsInRange(range, false))
            {
                if (item.ItemID != graphic)
                    continue;

                if (hue != int.MaxValue && item.Hue != hue)
                    continue;

                count++;
            }

            return count;
        }

        private static bool FindWand(string expression, Argument[] args, bool quiet, bool force)
        {
            if (UOSObjects.Player == null)
                return true;

            string spellname;
            if (args.Length < 1 || string.IsNullOrEmpty(spellname = args[0].AsString()))
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return true;
            }

            //"findwand ('spell name'/'any'/'undefined') [source] [minimum charges]"
            uint? source = null;
            int mincharges = -1;
            if(args.Length > 1)
            {
                source = args[1].AsSerial();
                if (args.Length > 2)
                    mincharges = args[2].AsInt();
            }

            List<UOItem> list;
            if(source.HasValue)
            {
                if(SerialHelper.IsItem(source.Value) && UOSObjects.FindItem(source.Value) is UOItem cont)
                {
                    list = cont.FindItemsByID(Commands.WandTypes);
                }
                else
                {
                    ScriptManager.Message(quiet, $"findwand: invalid source or source not found {args[1].AsString()}");
                    return false;
                }
            }
            else
                list = UOSObjects.FindItemsByTypes(Commands.WandTypes, range: 3);
            if (list.Count > 0)
            {
                if (spellname != "any" && spellname != "undefined")
                {
                    foreach (UOItem item in list)
                    {
                        if (item.Name != null && item.Name.IndexOf(spellname, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            Interpreter.SetAlias("found", item.Serial);
                            return true;
                        }
                    }
                    ScriptManager.Message(quiet, $"findwand: No wands of {spellname} found");
                }
                else
                {
                    Interpreter.SetAlias("found", list[Utility.Random(list.Count)].Serial);
                    return true;
                }
            }
            else
                ScriptManager.Message(quiet, $"findwand: No wands found");
            return false; 
        }

        private static bool InParty(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
                return UOSObjects.Player.InParty;

            var mobile = UOSObjects.FindMobile(args[0].AsSerial());

            if (mobile == null)
            {
                ScriptManager.Message(quiet, expression, "mobile not found.");
                return false;
            }

            return mobile.InParty;
        }

        private static bool InFriendsList(string expression, Argument[] args, bool quiet, bool force)
        {
            ScriptManager.Message(false, $"Expression {expression} not yet supported."); 
            return false; 
        }

        private static bool War(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length == 0)
            {
                return UOSObjects.Player.Warmode;
            }
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            uint serial = args[0].AsSerial();

            if (!SerialHelper.IsMobile(serial))
            {
                ScriptManager.Message(quiet, expression, "serial invalid");
                return false;
            }
            else
            {
                UOMobile mobile = UOSObjects.FindMobile(serial);

                if (mobile == null)
                {
                    ScriptManager.Message(quiet, expression, "mobile not found");
                    return false;
                }

                return mobile.Warmode;
            }
        }

        private static bool InGump(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }
            static bool checkList(List<PlayerData.GumpData> list, string gumpstring)
            {
                foreach (var g in list)
                {
                    if (g.GumpStrings.Contains(gumpstring))
                        return true;
                }
                return false;
            }
            if (args[0].AsString() == "any")
            {
                string gumpstring = args[1].AsString();
                foreach (var list in UOSObjects.Player.OpenedGumps.Values)
                {
                    if (checkList(list, gumpstring))
                        return true;
                }
            }
            else
            {
                uint gumpid = args[0].AsUInt();
                if (UOSObjects.Player.OpenedGumps.TryGetValue(gumpid, out var list) && checkList(list, args[1].AsString()))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool GumpExists(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }
            if (args[0].AsString() == "any")
                return UOSObjects.Player.OpenedGumps.Count > 0;

            return UOSObjects.Player.OpenedGumps.TryGetValue(args[0].AsUInt(), out var glist) && glist.Count > 0;
        }

        private static bool InJournal(string expression, Argument[] args, bool quiet, bool force)
        {
            if (!Engine.Instance.AllowBit(FeatureBit.SpeechJournalChecks))
            {
                ScriptManager.Message(quiet, "injournal: this functionality is not allowed by your server");
                return false;
            }
            if (args.Length == 0)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            string text = args[0].AsString();
            string name = args.Length > 1 ? args[1].AsString() : "system";
            if (name == "system")
            {
                if(Journal.ContainsSafe(text))
                    return true;
            }
            else if (Journal.ContainsFrom(name, text))
                return true;
            ScriptManager.Message(quiet, "injournal: text not found");
            return false;
        }

        private static bool ListExists(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            if (Interpreter.ListExists(args[0].AsString()))
                return true;

            return false;
        }

        private static int ListLength(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            return Interpreter.ListLength(args[0].AsString());
        }

        private static bool InList(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 2)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            if (Interpreter.ListContains(args[0].AsString(), args[1], !force))
                return true;

            return false;
        }

        private static bool TargetExists(string expression, Argument[] args, bool quiet, bool force)
        {
            if(args.Length > 0)
            {
                bool hastargtype = false;
                for (int i = 0; !hastargtype && i < args.Length; i++)
                {
                    hastargtype = Targeting.HasTargetType(args[i].AsString());
                }
                return hastargtype;
            }
            return Targeting.HasTarget;
        }

        private static bool WaitingForTarget(string expression, Argument[] args, bool quiet, bool force)
        {
            return Targeting.WaitingForTarget;
        }

        private static int TimerValue(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return 0;
            }

            var ts = Interpreter.GetTimer(args[0].AsString());

            return (int)ts.TotalMilliseconds;
        }

        private static bool TimerExists(string expression, Argument[] args, bool quiet, bool force)
        {
            if (args.Length != 1)
            {
                ScriptManager.Message(quiet, $"Usage: {Interpreter.GetExprHelper(expression)}");
                return false;
            }

            return Interpreter.TimerExists(args[0].AsString());
        }

        private static bool MoveItem(string expression, Argument[] args, bool quiet, bool force)
        {
            return Commands.MoveItemUniversal(expression, args, quiet, force);
        }

        private static bool MoveItemOffset(string expression, Argument[] args, bool quiet, bool force)
        {
            return Commands.MoveItemOffsetUniversal(expression, args, quiet, force);
        }

        private static bool MoveType(string expression, Argument[] args, bool quiet, bool force)
        {
            return Commands.MoveTypeUniversal(expression, args, quiet, force);
        }

        private static bool MoveTypeOffset(string expression, Argument[] args, bool quiet, bool force)
        {
            return Commands.MoveTypeOffsetUniversal(expression, args, quiet, force);
        }

        internal static void OnDisconnected()
        {
            IgnoredObjects.Clear();
        }
    }
}