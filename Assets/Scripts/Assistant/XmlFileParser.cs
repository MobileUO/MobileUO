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
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

using ClassicUO;
using ClassicUO.Configuration;
using ClassicUO.Utility.Logging;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;

using SDL2;

using AssistantGump = ClassicUO.Game.UI.Gumps.AssistantGump;
using ClassicUO.Utility.Collections;
using ClassicUO.Game.UI.Controls;

namespace Assistant
{
    #region XmlFileLoaderSaver
    internal static class XmlFileParser
    {
        internal static readonly CultureInfo Culture;

        static XmlFileParser()
        {
            Culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        }

        private static string GetAttribute(XmlElement node, string attributeName, string defaultValue = null)
        {
            if (node == null)
            {
                return defaultValue;
            }

            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null)
            {
                return defaultValue;
            }

            return attr.Value;
        }

        private static bool GetAttributeBool(XmlElement node, string attributeName, bool defaultValue = false)
        {
            if (node == null)
            {
                return defaultValue;
            }

            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null || !bool.TryParse(attr.Value, out bool b))
            {
                return defaultValue;
            }

            return b;
        }

        private static uint GetAttributeUInt(XmlElement node, string attributeName, uint defaultvalue = 0x0)
        {
            if (node == null)
            {
                return defaultvalue;
            }

            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null)
            {
                return defaultvalue;
            }
            uint i;
            if (attr.Value.StartsWith("0x"))
            {
                if (!uint.TryParse(attr.Value.Substring(2), NumberStyles.HexNumber, Culture, out i))
                {
                    i = defaultvalue;
                }
            }
            else
            {
                if (!uint.TryParse(attr.Value.Split('_')[0], out i))
                {
                    i = defaultvalue;
                }
            }
            return i;
        }

        private static ushort GetAttributeUShort(XmlElement node, string attributeName, ushort defaultvalue = 0x0)
        {
            return (ushort)GetAttributeUInt(node, attributeName, defaultvalue);
        }

        private static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
            {
                return defaultValue;
            }

            return node.InnerText;
        }

        private static bool GetBool(XmlElement node, bool defaultValue)
        {
            if (node == null || !bool.TryParse(node.InnerText, out bool result))
            {
                return defaultValue;
            }

            return result;
        }

        private static byte GetByte(XmlElement node, byte defaultvalue = 0x0)
        {
            return (byte)GetUInt(node, defaultvalue);
        }

        private static ushort GetUShort(XmlElement node, ushort defaultvalue = 0x0)
        {
            return (ushort)GetUInt(node, defaultvalue);
        }

        private static uint GetUInt(XmlElement node, uint defaultvalue = 0x0)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText))
            {
                return defaultvalue;
            }
            uint i;
            if (node.InnerText.StartsWith("0x"))
            {
                if (!uint.TryParse(node.InnerText.Substring(2), NumberStyles.HexNumber, Culture, out i))
                {
                    i = defaultvalue;
                }
            }
            else
            {
                if (!uint.TryParse(node.InnerText, out i))
                {
                    i = defaultvalue;
                }
            }
            return i;
        }

        internal static void LoadConfig(FileInfo info, AssistantGump gump)
        {
            if (gump == null || gump.IsDisposed || UOSObjects.Player == null)
            {
                return;
            }

            XmlDocument doc = new XmlDocument();
            bool retried = false;
            retry:
            try
            {
                doc.Load(info.FullName);
            }
            catch
            {
                if(!retried)
                {
                    retried = true;
                    try
                    {
                        string str = File.ReadAllText(info.FullName);
                        StringBuilder sb = new StringBuilder();
                        using (StringReader sr = new StringReader(str))
                        {
                            string s;
                            while ((s = sr.ReadLine()) != null)
                            {
                                if (s.Contains("</config>"))
                                {
                                    sb.AppendLine("</config>");
                                    break;
                                }
                                else
                                    sb.AppendLine(s);
                            }
                        }
                        if (sb.Length > 10)
                        {
                            File.WriteAllText(info.FullName, sb.ToString());
                        }
                    }
                    catch
                    {
                        return;
                    }
                    goto retry;
                }
                return;
            }
            if (doc == null)
            {
                return;
            }

            XmlElement root = doc["config"];
            if (root == null)
            {
                return;
            }
            foreach (XmlElement data in root.GetElementsByTagName("data"))
            {
                switch (GetAttribute(data, "name"))
                {
                    case "LastProfile":
                    {
                        gump.LastProfile = GetText(data, "").Replace(".xml", "");
                        break;
                    }
                    case "SmartProfile":
                    {
                        gump.SmartProfile = GetBool(data, false);
                        break;
                    }
                    /*case "NegotiateFeatures":
                    {
                        gump.NegotiateFeatures = GetBool(data, false);
                        break;
                    }*/
                }
                /*for (int i = 0; i < Filter.List.Count; i++)
                {
                    Filter f = Filter.List[i];
                    if (f.XmlName == name)
                    {
                        bool.TryParse(GetText(data, "False"), out bool enabled);
                        AssistantGump.FiltersCB[i].IsChecked = enabled;
                    }
                }*/
            }
            XmlElement sub = root["snapshots"];
            if(sub != null)
            {
                UOSObjects.Gump.SnapOwnDeath = GetBool(sub["ownDeath"], false);
                UOSObjects.Gump.SnapOtherDeath = GetBool(sub["othersDeath"], false);
            }
            sub = root["autoloot"];
            gump.ItemsToLoot.Clear();
            if (sub != null)
            {
                foreach (XmlElement item in sub.ChildNodes)
                {
                    ushort type = GetAttributeUShort(item, "type");
                    ushort limit = Math.Min((ushort)60000, GetAttributeUShort(item, "limit"));
                    string name = GetAttribute(item, "name");
                    if (type > 0 && type < Client.Game.UO.FileManager.TileData.StaticData.Length && !string.IsNullOrEmpty(name))
                        gump.ItemsToLoot[type] = (limit, name);
                }
            }
            else
                gump.ItemsToLoot.Clear();
            gump.UpdateAutolootList();
            sub = root["gump"];
            if(sub != null)
            {
                int x = GetAttributeUShort(sub, "x", 300);
                int y = GetAttributeUShort(sub, "y", 300);
                gump.X = x;
                gump.Y = y;
            }
        }

        internal static void LoadPrivate(AssistantGump gump)
        {
            if (gump == null || gump.IsDisposed || UOSObjects.Player == null || !gump.SmartProfile)
            {
                return;
            }

            FileInfo info = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UOItalia", "private.xml"));
            XmlDocument doc = new XmlDocument();
            bool retried = false;
            retry:
            try
            {
                if (!info.Exists)
                    return;
                doc.Load(info.FullName);
            }
            catch
            {
                if (!retried)
                {
                    retried = true;
                    try
                    {
                        string str = File.ReadAllText(info.FullName);
                        StringBuilder sb = new StringBuilder();
                        using (StringReader sr = new StringReader(str))
                        {
                            string s;
                            while ((s = sr.ReadLine()) != null)
                            {
                                if (s.Contains("</links>"))
                                {
                                    sb.AppendLine("</links>");
                                    break;
                                }
                                else
                                    sb.AppendLine(s);
                            }
                        }
                        if (sb.Length > 10)
                        {
                            File.WriteAllText(info.FullName, sb.ToString());
                        }
                    }
                    catch
                    {
                        return;
                    }
                    goto retry;
                }
                return;
            }
            if (doc == null)
            {
                return;
            }
            XmlElement root = doc["links"];
            if (root != null)
            {
                //LoginScene scene = Client.Game.GetScene<LoginScene>();
                foreach (XmlElement link in root.GetElementsByTagName("link"))
                {
                    uint serial = GetAttributeUInt(link, "serial");
                    string profile = GetAttribute(link, "profile");
                    if (string.IsNullOrWhiteSpace(profile) || serial == 0)
                    {
                        continue;
                    }
                    if(SerialHelper.IsMobile(serial))
                    {
                        if (UOSObjects.Player?.Serial == serial)
                        {
                            FileInfo profileinfo = new FileInfo(Path.Combine(Engine.ProfilePath, $"{profile}.xml"));
                            if (profileinfo.Exists)
                            {
                                gump.LastProfile = profile;
                            }
                        }
                        gump.LinkedProfiles[serial] = profile;
                    }
                }
            }
        }

        internal static void SavePrivate(AssistantGump gump)
        {
            if (gump.LinkedProfiles.Count < 1)
                return;
            try
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UOItalia")))
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UOItalia"));

                FileInfo info = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UOItalia", "private.xml"));
                bool exists = info.Exists;
                using (FileStream op = new FileStream(info.FullName, FileMode.OpenOrCreate))
                {
                    using (XmlTextWriter xml = new XmlTextWriter(op, Encoding.UTF8) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 1 })
                    {
                        xml.WriteStartDocument(true);
                        xml.WriteStartElement("links");

                        foreach (KeyValuePair<uint, string> kvp in gump.LinkedProfiles)
                        {
                            xml.WriteStartElement("link");
                            xml.WriteAttributeString("serial", $"{kvp.Key}");
                            xml.WriteAttributeString("profile", $"{kvp.Value}");
                            xml.WriteEndElement();
                        }

                        xml.WriteEndElement();
                        //xml.Flush();
                    }
                }
                if (exists)
                {
                    try
                    {
                        //keep the backup if they are not older than 7/14 days
                        FileInfo binfo = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UOItalia", "private_backup"));
                        if (binfo.Exists)
                        {
                            if (binfo.LastWriteTimeUtc.AddDays(7) <= DateTime.UtcNow)
                            {
                                binfo.CopyTo(Path.Combine(Engine.DataPath, $"private_backup2"));
                                info.CopyTo(Path.Combine(Engine.DataPath, $"private_backup"));
                            }
                        }
                        else
                        {
                            info.CopyTo(binfo.FullName);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        
        internal static void LoadSpellDef(FileInfo info, AssistantGump gump)
        {
            void writeSpells()
            {
                #region default_spells
                const string DEFSPELLS = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<spells>
	<!-- MAGHI -->
	<spell id=""1"" circle=""1"" name=""Clumsy"" words=""Uus Jux"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""2"" circle=""1"" name=""Create Food"" words=""In Mani Ylem"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""3"" circle=""1"" name=""Feeblemind"" words=""Rel Wis"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""4"" circle=""1"" name=""Heal"" words=""In Mani"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""5"" circle=""1"" name=""Magic Arrow"" words=""In Por Ylem"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""6"" circle=""1"" name=""Night Sight"" words=""In Lor"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""7"" circle=""1"" name=""Reveal"" words=""Wis Lor Quas"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""8"" circle=""1"" name=""Weaken"" words=""Des Mani"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""9"" circle=""2"" name=""Agility"" words=""Ex Uus"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""10"" circle=""2"" name=""Cunning"" words=""Uus Wis"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""11"" circle=""2"" name=""Cure"" words=""An Nox"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""12"" circle=""2"" name=""Harm"" words=""An Mani"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""13"" circle=""2"" name=""Electric Wave"" words=""Ort Wek"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""14"" circle=""2"" name=""Acid Drop"" words=""Uus Nox"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""15"" circle=""2"" name=""Protection"" words=""Uus Sanct"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""16"" circle=""2"" name=""Strength"" words=""Uus Mani"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""17"" circle=""3"" name=""Bless"" words=""Rel Sanct"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""18"" circle=""3"" name=""Fireball"" words=""Vas Flam"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""19"" circle=""3"" name=""Energy Ball"" words=""Vas Wek"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""20"" circle=""3"" name=""Poison"" words=""In Nox"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""21"" circle=""3"" name=""Acid Ball"" words=""Vas Nox"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""22"" circle=""3"" name=""Teleport"" words=""Rel Por"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""23"" circle=""3"" name=""Ice Ball"" words=""Vas Ter"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""24"" circle=""3"" name=""Wall of Stone"" words=""In Sanct Ylem"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""25"" circle=""4"" name=""Arch Cure"" words=""Vas An Nox"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""26"" circle=""4"" name=""Arch Protection"" words=""Vas Uus Sanct"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""27"" circle=""4"" name=""Curse"" words=""Des Sanct"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""28"" circle=""4"" name=""Fire Field"" words=""In Flam Grav"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""29"" circle=""4"" name=""Greater Heal"" words=""In Vas Mani"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""30"" circle=""4"" name=""Lightning"" words=""Por Ort Grav"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""31"" circle=""4"" name=""Mana Drain"" words=""Ort Rel"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""32"" circle=""4"" name=""Recall"" words=""Kal Ort Por"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""33"" circle=""5"" name=""Blade Spirits"" words=""In Jux Hur Ylem"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""34"" circle=""5"" name=""Dispel Field"" words=""An Grav"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""35"" circle=""5"" name=""Incognito"" words=""Kal In Ex"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""36"" circle=""5"" name=""Magic Reflection"" words=""In Jux Sanct"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""37"" circle=""5"" name=""Icicle Strike"" words=""Kal Uus Ter"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""38"" circle=""5"" name=""Paralyze"" words=""An Ex Por"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""39"" circle=""5"" name=""Vitriol"" words=""Zu Corp Nox"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""40"" circle=""5"" name=""Summon Creature"" words=""Kal Xen"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""41"" circle=""6"" name=""Dispel"" words=""An Ort"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""42"" circle=""6"" name=""Cold Explosion"" words=""Vas Ort Ter"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""43"" circle=""6"" name=""Explosion"" words=""Vas Ort Flam"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""44"" circle=""6"" name=""Invisibility"" words=""An Lor Xen"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""45"" circle=""6"" name=""Mark"" words=""Kal Por Ylem"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""46"" circle=""6"" name=""Mass Curse"" words=""Vas Des Sanct"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""47"" circle=""6"" name=""Paralyze Field"" words=""In Ex Grav"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""48"" circle=""6"" name=""Acid Corrosion"" words=""In Jux Nox"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""49"" circle=""7"" name=""Chain Lightning"" words=""Vas Ort Grav"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""50"" circle=""7"" name=""Energy Field"" words=""In Sanct Grav"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""51"" circle=""7"" name=""Flame Strike"" words=""Kal Vas Flam"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""52"" circle=""7"" name=""Gate Travel"" words=""Vas Rel Por"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""53"" circle=""7"" name=""Mana Vampire"" words=""Ort Sanct"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""54"" circle=""7"" name=""Mass Dispel"" words=""Vas An Ort"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""55"" circle=""7"" name=""Meteor Swarm"" words=""Flam Kal Des Ylem"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""56"" circle=""7"" name=""Polymorph"" words=""Vas Ylem Rel"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""57"" circle=""8"" name=""Earthquake"" words=""In Vas Por"" flag=""Harmful"" classname=""Mage"" />
	<spell id=""58"" circle=""8"" name=""Energy Vortex"" words=""Vas Corp Por"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""59"" circle=""8"" name=""Resurrection"" words=""An Corp"" flag=""Beneficial"" classname=""Mage"" />
	<spell id=""60"" circle=""8"" name=""Summon Air Elemental"" words=""Kal Vas Xen Hur"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""61"" circle=""8"" name=""Summon Daemon"" words=""Kal Vas Xen Corp"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""62"" circle=""8"" name=""Summon Earth Elemental"" words=""Kal Vas Xen Ylem"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""63"" circle=""8"" name=""Summon Fire Elemental"" words=""Kal Vas Xen Flam"" flag=""Neutral"" classname=""Mage"" />
	<spell id=""64"" circle=""8"" name=""Summon Water Elemental"" words=""Kal Vas Xen An Flam"" flag=""Neutral"" classname=""Mage"" />

	<!-- NECROMANTI -->
	<spell id=""101"" circle=""10"" name=""Rianima Morti"" words=""Uus An Corp"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""102"" circle=""10"" name=""Patto del Sangue"" words=""In Jux Xen Mani"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""103"" circle=""10"" name=""Pelle Morta"" words=""In Corp Agle Ylem"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""104"" circle=""10"" name=""Lama del Sangue"" words=""An Sanct Vitae"" flag=""Beneficial"" classname=""Necromancer"" />
	<spell id=""105"" circle=""10"" name=""Presagio Infernale"" words=""Pas An Omen"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""106"" circle=""10"" name=""Forma Bestiale"" words=""Rel Xen Vas Bal"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""107"" circle=""10"" name=""Forma di Lich"" words=""Rel Xen Corp Ort"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""108"" circle=""10"" name=""Mente Corrotta"" words=""Wis An Ben"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""109"" circle=""10"" name=""Supplizio Infernale"" words=""In Sar"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""110"" circle=""10"" name=""Onda Venefica"" words=""In Vas Nox"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""111"" circle=""10"" name=""Strangola"" words=""In Bal Nox"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""112"" circle=""10"" name=""Evoca Famiglio"" words=""Kal Xen Bal"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""113"" circle=""10"" name=""Sguardo Ardente"" words=""Doleo"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""114"" circle=""10"" name=""Vendicatore"" words=""Kal Xen Bal Beh"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""115"" circle=""10"" name=""Nebbia Gelida"" words=""Ethr Kal Des Ylem"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""116"" circle=""10"" name=""Forma Spettrale"" words=""Rel Xen Um"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""117"" circle=""10"" name=""Carne in Pietra"" words=""In Sanct Corp Ylem"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""118"" circle=""10"" name=""Onda del Caos"" words=""In Kal Inferus"" flag=""Harmful"" classname=""Necromancer"" />
	<spell id=""119"" circle=""10"" name=""Scongiuro"" words="""" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""120"" circle=""10"" name=""Armatura d'Ossa"" words=""Kal In Por Inferus"" flag=""Neutral"" classname=""Necromancer"" />
	<spell id=""121"" circle=""10"" name=""Trasmuta Acqua"" words=""An Vitae Nox"" flag=""Neutral"" classname=""Necromancer"" />
    <spell id=""122"" circle=""10"" name=""Negative Force"" words=""Uus An Sanct"" flag=""Beneficial"" classname=""Necromancer"" />

    <!-- CHIERICI -->
	<spell id=""201"" circle=""20"" name=""Forza Divina"" words=""Angelus Terum"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""202"" circle=""20"" name=""Bandire il Male"" words=""Abigo Malus"" flag=""Harmful"" classname=""Cleric"" />
	<spell id=""203"" circle=""20"" name=""Benedici Acqua"" words=""Benedictum"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""204"" circle=""20"" name=""Muro di Spade"" words=""Spatha Terum"" flag=""Harmful"" classname=""Cleric"" />
	<spell id=""205"" circle=""20"" name=""Spirito Infranto"" words=""Abicio Spiritus"" flag=""Harmful"" classname=""Cleric"" />
	<spell id=""206"" circle=""20"" name=""Focus Divino"" words=""Divinium Cogitatus"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""207"" circle=""20"" name=""Martello di Fede"" words=""Malleus Terum"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""208"" circle=""20"" name=""Luce Sacra"" words=""Abigo Tenebrae"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""209"" circle=""20"" name=""Varco Celestiale"" words=""Viam Patefacio"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""210"" circle=""20"" name=""Marchio Divino"" words=""Notae Superna"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""211"" circle=""20"" name=""Preghiera"" words="""" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""212"" circle=""20"" name=""Epurare"" words=""Repurgo"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""213"" circle=""20"" name=""Resurrezione"" words=""Reductio Aetas"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""214"" circle=""20"" name=""Dono Sacro"" words=""Vir Consolatio"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""215"" circle=""20"" name=""Sacrificio"" words=""Adoleo"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""216"" circle=""20"" name=""Punire"" words=""Ferio"" flag=""Harmful"" classname=""Cleric"" />
	<spell id=""217"" circle=""20"" name=""Tocco di Vita"" words=""Tactus Vitalis"" flag=""Beneficial"" classname=""Cleric"" />
	<spell id=""218"" circle=""20"" name=""Scudo di Fuoco"" words=""Temptatio Exsuscito"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""219"" circle=""20"" name=""Guardiano Divino"" words=""Voco Divinum"" flag=""Neutral"" classname=""Cleric"" />
	<spell id=""220"" circle=""20"" name=""Sferzata di Luce"" words=""Lendo"" flag=""Harmful"" classname=""Cleric"" />

	<!-- BARDI -->
	<spell id=""501"" circle=""50"" name=""Armata di Paeon"" words=""Paeonus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""502"" circle=""50"" name=""Etude Incantevole"" words=""Enchantendre"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""503"" circle=""50"" name=""Aria d'Energia"" words=""Energious"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""504"" circle=""50"" name=""Boato d'Energia"" words=""Enerdeficient"" flag=""Harmful"" classname=""Bard"" />
	<spell id=""505"" circle=""50"" name=""Aria del Fuoco"" words=""Inflammabus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""506"" circle=""50"" name=""Boato del Fuoco"" words=""Flammabus"" flag=""Harmful"" classname=""Bard"" />
	<spell id=""507"" circle=""50"" name=""Requiem"" words=""Sonicus"" flag=""Harmful"" classname=""Bard"" />
	<spell id=""508"" circle=""50"" name=""Aria del Gelo"" words=""Insulatus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""509"" circle=""50"" name=""Boato del Gelo"" words=""Chillinum"" flag=""Harmful"" classname=""Bard"" />
	<spell id=""510"" circle=""50"" name=""Danza di Guerra"" words=""Resistus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""511"" circle=""50"" name=""Ballata del Mago"" words=""Mentus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""512"" circle=""50"" name=""Magic Finale"" words=""Dispersus"" flag=""Neutral"" classname=""Bard"" />
	<spell id=""513"" circle=""50"" name=""Aria del Veleno"" words=""Antidotus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""514"" circle=""50"" name=""Boato del Veleno"" words=""Infectus"" flag=""Harmful"" classname=""Bard"" />
	<spell id=""515"" circle=""50"" name=""Ritirata"" words=""Fugitivus"" flag=""Neutral"" classname=""Bard"" />
	<spell id=""516"" circle=""50"" name=""Sonetto Agile"" words=""Facilitus"" flag=""Beneficial"" classname=""Bard"" />
	<spell id=""517"" circle=""50"" name=""Canto dell'Eroe"" words=""Fortitus"" flag=""Beneficial"" classname=""Bard"" />

	<!-- DRUIDI -->
	<spell id=""701"" circle=""70"" name=""Lesser Spring"" words=""En Sepa"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""702"" circle=""70"" name=""Earth Relief"" words=""Ylem An Nox"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""703"" circle=""70"" name=""Fiery Roots"" words=""Ominia Sanguinia"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""704"" circle=""70"" name=""Curse of Yew"" words=""Quas Corp"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""705"" circle=""70"" name=""Spider's Acid"" words=""Ve Nom Us"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""706"" circle=""70"" name=""Hollow Reed"" words=""En Crur Aeta Sec En Ess"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""707"" circle=""70"" name=""Lava Blast"" words=""Kal Flam Ylem"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""708"" circle=""70"" name=""Winter Wind"" words=""Vas Ter"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""709"" circle=""70"" name=""Shield Of Earth"" words=""Kes En Sepa Ohm"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""710"" circle=""70"" name=""Stone Circle"" words=""En Ess Ohm"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""711"" circle=""70"" name=""Swarm Of Insects"" words=""Ess Ohm En Sec Tia"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""712"" circle=""70"" name=""Nature's Passage"" words=""Kes Sec Vauk"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""713"" circle=""70"" name=""Spring Of Life"" words=""En Sepa Aete"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""714"" circle=""70"" name=""Anti Field"" words=""An Grav"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""715"" circle=""70"" name=""Druidic Reflection"" words=""In Jux Sanct"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""716"" circle=""70"" name=""Druidic Shield"" words=""Defense of Nature"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""717"" circle=""70"" name=""Thunder Blast"" words=""Kal Hur Grav"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""718"" circle=""70"" name=""Lure Stone"" words=""En Kes Ohm Crur"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""719"" circle=""70"" name=""Nature Cleansing"" words=""An Disper Dis"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""720"" circle=""70"" name=""Pack Of Beast"" words=""En Sec Ohm Ess Sepa"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""721"" circle=""70"" name=""Leaf Whirlwind"" words=""Ess Lore En Ohm"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""722"" circle=""70"" name=""Grasping Roots"" words=""En Ohm Sepa Tia Kes"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""723"" circle=""70"" name=""Blend With Forest"" words=""Kes Ohm"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""724"" circle=""70"" name=""Tree Form"" words=""Vas Ylem Rel"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""725"" circle=""70"" name=""Druidic Gateway"" words=""Vauk Sepa Ohm"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""726"" circle=""70"" name=""Volcanic Eruption"" words=""Vauk Ohm En Tia Crur"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""727"" circle=""70"" name=""Ignis Fatuus"" words=""Fatuus Flame"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""728"" circle=""70"" name=""Summon Familiar"" words=""Lore Sec En Sepa Ohm"" flag=""Harmful"" classname=""Druid"" />
	<spell id=""729"" circle=""70"" name=""Restorative Soil"" words=""Ohm Sepa Ante"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""730"" circle=""70"" name=""Gift of Life"" words=""Illorae"" flag=""Beneficial"" classname=""Druid"" />
	<spell id=""731"" circle=""70"" name=""Ethereal Voyage"" words=""Orlavdra"" flag=""Neutral"" classname=""Druid"" />
	<spell id=""732"" circle=""70"" name=""Fire Storm"" words=""Kal Des Fatuus Flam"" flag=""Harmful"" classname=""Druid"" />

</spells>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFSPELLS);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadSpellDef: {e}");
                }
            }
            XmlDocument doc = new XmlDocument();
            if(!info.Exists)
            {
                writeSpells();
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"Exception in LoadSpellDef - rewriting file: {e}");
                try
                {
                    writeSpells();
                    doc.Load(info.FullName);
                }
                catch { }
            }
            if (doc == null)
            {
                return;
            }
            XmlElement root = doc["spells"];
            if (root == null)
            {
                return;
            }

            Dictionary<uint, Reagents> reagsdict = new Dictionary<uint, Reagents>()
                {
                    { 0xF78, Reagents.BatWing },
                    { 0xF7A, Reagents.BlackPearl },
                    { 0xF7B, Reagents.Bloodmoss },
                    { 0xF7E, Reagents.Bone },
                    { 0xF7D, Reagents.DaemonBlood },
                    { 0xF80, Reagents.DemonBone },
                    { 0xF82, Reagents.DragonsBlood },
                    { 0x4077, Reagents.DragonsBlood },//new clients
                    { 0xF81, Reagents.FertileDirt },
                    { 0xF84, Reagents.Garlic },
                    { 0xF85, Reagents.Ginseng },
                    { 0xF8F, Reagents.GraveDust },
                    { 0xF86, Reagents.MandrakeRoot },
                    { 0xF88, Reagents.Nightshade },
                    { 0xF8E, Reagents.NoxCrystal },
                    { 0xF8A, Reagents.PigIron },
                    { 0xF8D, Reagents.SpidersSilk },
                    { 0xF8C, Reagents.SulfurousAsh },
                    { 0xF79, Reagents.Blackmoor },
                    { 0xF7C, Reagents.Bloodspawn },
                    { 0xF90, Reagents.DeadWood },
                    { 0xF91, Reagents.WyrmHeart }
                };
            //enum tryparse is performance awful, better to do it this way
            Dictionary<string, TargetType> getTargetFlag = new Dictionary<string, TargetType>()
                {
                    {"neutral", TargetType.Neutral},
                    {"harmful", TargetType.Harmful},
                    {"beneficial", TargetType.Beneficial}
                };
            int id, circle;
            string name, classname;
            Spell.SpellsByID.Clear();
            Spell.SpellsByName.Clear();
            string getClassName(int spellid)
            {
                switch(spellid / 100)
                {
                    case 0:
                        return "Magery";
                    case 1:
                        return "Necromancy";
                    case 2:
                        return "Chivalry";
                    case 3:
                        return "Undefined";
                    case 4:
                        return "Bushido";
                    case 5:
                        return "Ninjisu";
                    case 6:
                        if(spellid < 678)
                            return "Mysticism";
                        return "Spellweaving";
                    case 7:
                        return "Bardic";
                    default:
                        return "Unknown";
                }
            }
            Dictionary<string, List<string>> orderedspells = new Dictionary<string, List<string>>();
            foreach (XmlElement spell in root.GetElementsByTagName("spell"))
            {
                id = (int)GetAttributeUInt(spell, "id");
                circle = (int)GetAttributeUInt(spell, "circle");//this is used on spell categorization
                name = GetAttribute(spell, "name");
                if (id > 0 && !string.IsNullOrEmpty(name))
                {
                    int iconid = (int)GetAttributeUInt(spell, "iconid"), smalliconid = (int)GetAttributeUInt(spell, "smalliconid"),
                        manacost = (int)GetAttributeUInt(spell, "mana"), tithingcost = (int)GetAttributeUInt(spell, "tithing"),
                        minskill = (int)GetAttributeUInt(spell, "minskill");
                    uint timeout = GetAttributeUInt(spell, "timeout");
                    string ttype = GetAttribute(spell, "flag", "neutral"), words = GetAttribute(spell, "words", string.Empty), reagentstring = GetAttribute(spell, "reagents", string.Empty);
                    if (string.IsNullOrWhiteSpace(ttype) || !getTargetFlag.TryGetValue(ttype.Trim().ToLower(), out TargetType targetType))
                        targetType = TargetType.Neutral;
                    string[] reagsarr = reagentstring.Split(',');
                    List<Reagents> reagslist = new List<Reagents>();
                    foreach (string s in reagsarr)
                    {
                        bool hex = s.StartsWith("0x");
                        if (uint.TryParse(hex ? s.Substring(2) : s, hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out uint ru) && reagsdict.TryGetValue(ru, out Reagents reag) && reag != Reagents.None)
                        {
                            reagslist.Add(reag);
                        }
                    }
                    if(string.IsNullOrEmpty(classname = GetAttribute(spell, "classname")))
                        classname = getClassName(id);
                    if (!orderedspells.TryGetValue(classname, out List<string> list))
                        orderedspells[classname] = list = new List<string>();
                    list.Add(name);
                    SpellDefinition.FullIndexSetModifySpell(id, id % 100, iconid, smalliconid, minskill, manacost, tithingcost, name, words, targetType, reagslist.ToArray());
                    name = name.ToLower(Culture);
                    Spell.SpellsByID[id] = Spell.SpellsByName[name] = Spell.SpellsByName[name.Replace(" ", "")] = new Spell((int)targetType, id, circle, words, reagslist);
                }
            }
            foreach(KeyValuePair<string, List<string>> kvp in orderedspells)
            {
                gump.AddSpellsToHotkeys(kvp.Key, kvp.Value);
            }
        }

        internal static void LoadSkillDef(FileInfo info, AssistantGump gump)
        {
            List<SkillEntry> skillEntries = new List<SkillEntry>();
            XmlDocument doc = new XmlDocument();
            if(!info.Exists)
            {
                #region defskills
                const string DEFSKILLS = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<skills>	
	<skill name=""Alchemy"" />
	<skill name=""Anatomy"" passive=""false"" />
	<skill name=""Animal Lore"" passive=""false"" />
	<skill name=""Item Identification"" passive=""false"" />
	<skill name=""Arms Lore"" passive=""false"" />
	<skill name=""Parrying"" />
	<skill name=""Begging"" passive=""false"" />
	<skill name=""Blacksmithy"" />
	<skill name=""Bowcraft"" />
	<skill name=""Peacemaking"" passive=""false"" />
	<skill name=""Camping"" />
	<skill name=""Carpentry"" />
	<skill name=""Cartography"" />
	<skill name=""Cooking"" />
	<skill name=""Detecting Hidden"" passive=""false"" />
	<skill name=""Discordance"" passive=""false"" />
	<skill name=""Evaluating Intelligence"" passive=""false"" />
	<skill name=""Healing"" />
	<skill name=""Fishing"" />
	<skill name=""Forensic Evaluation"" passive=""false"" />
	<skill name=""Herding"" />
	<skill name=""Hiding"" passive=""false"" />
	<skill name=""Provocation"" passive=""false"" />
	<skill name=""Inscription"" />
	<skill name=""Lockpicking"" />
	<skill name=""Magery"" />
	<skill name=""Resisting Spells"" />
	<skill name=""Tactics"" />
	<skill name=""Snooping"" />
	<skill name=""Musicianship"" />
	<skill name=""Poisoning"" passive=""false"" />
	<skill name=""Archery"" />
	<skill name=""Spirit Speak"" passive=""false"" />
	<skill name=""Stealing"" passive=""false"" />
	<skill name=""Tailoring"" />
	<skill name=""Animal Taming"" passive=""false"" />
	<skill name=""Taste Identification"" passive=""false"" />
	<skill name=""Tinkering"" />
	<skill name=""Tracking"" passive=""false"" />
	<skill name=""Veterinary"" />
	<skill name=""Swordsmanship"" />
	<skill name=""Mace Fighting"" />
	<skill name=""Fencing"" />
	<skill name=""Wrestling"" />
	<skill name=""Lumberjacking"" />
	<skill name=""Mining"" />
	<skill name=""Meditation"" passive=""false"" />
	<skill name=""Stealth"" passive=""false"" />
	<skill name=""Remove Trap"" passive=""false"" />
	<skill name=""Necromancy"" />
	<skill name=""Focus"" passive=""false"" />
	<skill name=""Chivalry"" />
	<skill name=""Bushido"" />
	<skill name=""Ninjitsu"" />
	<skill name=""Herboristery"" />
</skills>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFSKILLS);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadSkillDef: {e}");
                }
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"UOSteam -> Exception in LoadSkillDef: {e}");
            }

            int i = 0, count = 0;
            XmlElement root;
            if (doc != null && (root = doc["skills"]) != null)
            {
                foreach (XmlElement skill in root.GetElementsByTagName("skill"))
                {
                    string name = GetAttribute(skill, "name");
                    bool passive = GetAttributeBool(skill, "passive", true);
                    if (!string.IsNullOrEmpty(name))
                        skillEntries.Add(new SkillEntry(i++, name, !passive));
                    count++;
                }
                if (count == i && skillEntries.Count > 0)
                {
                    SetAllSkills(skillEntries);
                }
                else
                    Log.Warn($"Skills count isn't equal to readed skills in LoadSkills: {count} present vs {i} correctly readed");
            }
            else
            {
                skillEntries.AddRange(Client.Game.UO.FileManager.Skills.Skills);
            }

            Dictionary<int, string> skn = new Dictionary<int, string>
            {
                [-1] = "Last"
            };
            for (i = 0; i < skillEntries.Count; i++)
            {
                SkillEntry sk = skillEntries[i];
                if (sk.HasAction)
                    skn[sk.Index] = sk.Name;
            }
            HotKeys.SkillHotKeys.CleanUP();
            gump.SkillsHK.SetItemsValue(skn);
            //ScriptManager.SkillMap.Clear();
            foreach (KeyValuePair<int, string> kvp in skn)
            {
                ScriptManager.SkillMap[kvp.Value.ToLower(Culture)] = kvp.Key;
                ScriptManager.SkillMap[kvp.Value.Replace(" ", "").ToLower(Culture)] = kvp.Key;
            }
            HotKeys.SkillHotKeys.Initialize();
        }

        internal static void LoadBodyDef(FileInfo info)
        {
            XmlDocument doc = new XmlDocument();
            if (!info.Exists)
            {
                #region defbodies
                const string DEFBODIES = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<bodies>
  <humanoid>
    <!-- Main -->
    <body name=""Human Male"" graphic=""0x190"" />
    <body name=""Human Female"" graphic=""0x191"" />
    <body name=""Elf Male"" graphic=""0x25d"" />
    <body name=""Elf Female"" graphic=""0x25e"" />
    <body name=""Dwarf Male"" graphic=""0x29a"" />
    <body name=""Dwarf Female"" graphic=""0x29b"" />
    <!-- Necromancy -->
    <body name=""Vampire Male"" graphic=""0x2e8"" />
    <body name=""Vampire Female"" graphic=""0x2e9"" />
    <!-- Others -->
    <body name=""Tribal Male"" graphic=""0xb7"" />
    <body name=""Tribal Female"" graphic=""0xb8"" />
  </humanoid>
  <transformation>
    <!-- Necromancy -->
    <body name=""Horrific Beast"" graphic=""0x2ea"" />
    <body name=""Wraith Male"" graphic=""0x2ec"" />
    <body name=""Wraith Female"" graphic=""0x2eb"" />
    <body name=""Lich"" graphic=""0x2ed"" />
    <!-- Ninjitsu -->
    <body name=""Llama"" graphic=""0xdc"" />
    <body name=""Wolf"" graphic=""0x19"" />
    <!-- Spellweaving -->
    <body name=""Ethereal Voyage"" graphic=""0x302"" />
    <body name=""Reaper Form"" graphic=""0x11d"" />
    <!-- Mysticism -->
    <body name=""Stone Form"" graphic=""0x2c1"" />
  </transformation>
</bodies>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFBODIES);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadBodyDef: {e}");
                }
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"UOSteam -> Exception in LoadBodyDef: {e}");
            }

            XmlElement root;
            if (doc != null && (root = doc["bodies"]) != null)
            {
                XmlElement basic = root["humanoid"];
                ushort body;
                string bodyname;
                if (basic != null)
                {

                    foreach (XmlElement bodies in basic.GetElementsByTagName("body"))
                    {
                        body = GetAttributeUShort(bodies, "graphic");
                        if(body > 0)
                        {
                            bodyname = GetAttribute(bodies, "name");
                            Targeting.Humanoid.Add(body);
                        }
                    }
                }
                basic = root["transformation"];
                if (basic != null)
                {
                    foreach (XmlElement bodies in basic.GetElementsByTagName("body"))
                    {
                        body = GetAttributeUShort(bodies, "graphic");
                        if (body > 0)
                        {
                            bodyname = GetAttribute(bodies, "name");
                            Targeting.Transformation.Add(body);
                        }
                    }
                }
            }
        }

        internal static void LoadFoodDef(FileInfo info)
        {
            XmlDocument doc = new XmlDocument();
            if (!info.Exists)
            {
                #region deffood
                const string DEFFOOD = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<foods>
	<group name=""Fish"">
		<food name=""Fish Steak"" graphic=""0x97B"" />
		<food name=""Raw Fish Steak"" graphic=""0x097A"" />
	</group>
	<group name=""Fruits and Vegetables"">
		<food name=""Honeydew Melon"" graphic=""0xC74"" />
		<food name=""Yellow Gourd"" graphic=""0xC64"" />
		<food name=""Green Gourd"" graphic=""0xC66"" />
		<food name=""Banana"" graphic=""0x171f"" />
		<food name=""Lemon"" graphic=""0x1728"" />
		<food name=""Lime"" graphic=""0x172a"" />
		<food name=""Grape"" graphic=""0x9D1"" />
		<food name=""Peach"" graphic=""0x9D2"" />
		<food name=""Pear"" graphic=""0x994"" />
		<food name=""Apple"" graphic=""0x9D0"" />
		<food name=""Watermelon"" graphic=""0xC5C"" />
		<food name=""Squash"" graphic=""0xc72"" />
		<food name=""Cantaloupe"" graphic=""0xc79"" />
		<food name=""Carrot"" graphic=""0xc78"" />
		<food name=""Cabbage"" graphic=""0xc7b"" />
		<food name=""Onion"" graphic=""0xc6d"" />
		<food name=""Lettuce"" graphic=""0xc70"" />
		<food name=""Pumpkin"" graphic=""0xC6A"" />
	</group>
	<group name=""Meat"">
		<!-- Cooked -->
		<food name=""Bacon"" graphic=""0x979"" />
		<food name=""Cooked Bird"" graphic=""0x9B7"" />
		<food name=""Sausage"" graphic=""0x9C0"" />
		<food name=""Ham"" graphic=""0x9C9"" />
		<food name=""Ribs"" graphic=""0x9F2"" />
		<food name=""Lamb Leg"" graphic=""0x160a"" />
		<food name=""Chicken Leg"" graphic=""0x1608"" />
		<!-- Uncooked -->
		<food name=""Raw Bird"" graphic=""0x9B9"" />
		<food name=""Raw Ribs"" graphic=""0x9F1"" />
		<food name=""Raw Lamb Leg"" graphic=""0x1609"" />
		<food name=""Raw Chicken Leg"" graphic=""0x1607"" />
		<!-- Body Parts -->
		<food name=""Head"" graphic=""0x1DA0"" />
		<food name=""Left Arm"" graphic=""0x1DA1"" />
		<food name=""Left Leg"" graphic=""0x1DA3"" />
		<food name=""Torso"" graphic=""0x1D9F"" />
		<food name=""Right Arm"" graphic=""0x1DA2"" />
		<food name=""Right Leg"" graphic=""0x1DA4"" />
	</group>
</foods>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFFOOD);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadFoodDef: {e}");
                }
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"UOSteam -> Exception in LoadFoodDef: {e}");
            }

            XmlElement root;
            if (doc != null && (root = doc["foods"]) != null)
            {
                foreach (XmlElement group in root.GetElementsByTagName("group"))
                {
                    foreach (XmlElement food in group.GetElementsByTagName("food"))
                    {
                        Foods.AddFood(GetAttribute(group, "name"), GetAttribute(food, "name"), GetAttributeUShort(food, "graphic"));
                    }
                }
            }
        }

        internal static void LoadRegionDef(FileInfo info)
        {
            XmlDocument doc = new XmlDocument();
            if (!info.Exists)
            {
                #region defregion
                const string DEFREGION = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<regions>
 <Felucca>
  <Towns>
   <region name=""Sede Hilianor"">
    <rectangle x=""2090"" y=""885"" z=""-128"" width=""33"" height=""40"" depth=""256"" />
    <rectangle x=""2100"" y=""862"" z=""-128"" width=""14"" height=""23"" depth=""256"" />
    <rectangle x=""2115"" y=""874"" z=""-128"" width=""13"" height=""11"" depth=""256"" />
    <rectangle x=""2122"" y=""875"" z=""-128"" width=""20"" height=""49"" depth=""256"" />
    <rectangle x=""2128"" y=""921"" z=""-128"" width=""8"" height=""4"" depth=""256"" />
    <rectangle x=""2142"" y=""918"" z=""-128"" width=""4"" height=""6"" depth=""256"" />
    <rectangle x=""2142"" y=""875"" z=""-128"" width=""8"" height=""10"" depth=""256"" />
    <rectangle x=""2125"" y=""844"" z=""-128"" width=""16"" height=""31"" depth=""256"" />
    <rectangle x=""2115"" y=""843"" z=""-128"" width=""10"" height=""9"" depth=""256"" />
    <rectangle x=""2125"" y=""819"" z=""-128"" width=""16"" height=""25"" depth=""256"" />
    <rectangle x=""2141"" y=""843"" z=""-128"" width=""9"" height=""8"" depth=""256"" />
   </region>
   <region name=""Midian"">
    <rectangle x=""5173"" y=""3609"" z=""-128"" width=""73"" height=""76"" depth=""256"" />
    <rectangle x=""5171"" y=""3603"" z=""19"" width=""63"" height=""7"" depth=""109"" />
    <rectangle x=""5166"" y=""3653"" z=""-128"" width=""9"" height=""43"" depth=""256"" />
    <rectangle x=""5188"" y=""3684"" z=""-128"" width=""52"" height=""30"" depth=""256"" />
    <rectangle x=""5243"" y=""3616"" z=""-128"" width=""4"" height=""11"" depth=""256"" />
    <rectangle x=""5246"" y=""3617"" z=""-128"" width=""3"" height=""4"" depth=""256"" />
    <rectangle x=""5246"" y=""3627"" z=""-128"" width=""4"" height=""56"" depth=""256"" />
    <rectangle x=""5248"" y=""3675"" z=""-128"" width=""3"" height=""5"" depth=""256"" />
    <rectangle x=""5244"" y=""3681"" z=""-128"" width=""4"" height=""4"" depth=""256"" />
    <rectangle x=""5247"" y=""3682"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5240"" y=""3685"" z=""-128"" width=""4"" height=""4"" depth=""256"" />
    <rectangle x=""5244"" y=""3685"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5245"" y=""3684"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5243"" y=""3686"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5240"" y=""3689"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5241"" y=""3688"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5239"" y=""3690"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5166"" y=""3624"" z=""-128"" width=""9"" height=""12"" depth=""256"" />
   </region>
   <region name=""Ocllo"">
    <rectangle x=""3607"" y=""2455"" z=""-128"" width=""89"" height=""107"" depth=""256"" />
    <rectangle x=""3607"" y=""2562"" z=""-128"" width=""81"" height=""90"" depth=""256"" />
   </region>
   <region name=""Cove"">
    <rectangle x=""2202"" y=""1160"" z=""-128"" width=""45"" height=""34"" depth=""256"" />
    <rectangle x=""2196"" y=""1194"" z=""-128"" width=""88"" height=""52"" depth=""256"" />
    <rectangle x=""2284"" y=""1219"" z=""-128"" width=""8"" height=""16"" depth=""256"" />
    <rectangle x=""2247"" y=""1181"" z=""-128"" width=""45"" height=""13"" depth=""256"" />
    <rectangle x=""2224"" y=""1246"" z=""-128"" width=""47"" height=""17"" depth=""256"" />
    <rectangle x=""2202"" y=""1104"" z=""-128"" width=""56"" height=""56"" depth=""256"" />
   </region>
   <region name=""Britain"">
    <rectangle x=""1418"" y=""1755"" z=""-5"" width=""225"" height=""22"" depth=""133"" />
    <rectangle x=""1523"" y=""1777"" z=""-5"" width=""80"" height=""18"" depth=""133"" />
    <rectangle x=""1414"" y=""1675"" z=""-5"" width=""4"" height=""31"" depth=""133"" />
    <rectangle x=""1356"" y=""1706"" z=""-5"" width=""62"" height=""7"" depth=""133"" />
    <rectangle x=""1369"" y=""1713"" z=""-5"" width=""49"" height=""10"" depth=""133"" />
    <rectangle x=""1379"" y=""1723"" z=""-5"" width=""39"" height=""10"" depth=""133"" />
    <rectangle x=""1388"" y=""1733"" z=""-5"" width=""30"" height=""31"" depth=""133"" />
    <rectangle x=""1448"" y=""1507"" z=""-5"" width=""94"" height=""25"" depth=""133"" />
    <rectangle x=""1542"" y=""1507"" z=""-5"" width=""26"" height=""25"" depth=""133"" />
    <rectangle x=""1568"" y=""1512"" z=""-5"" width=""70"" height=""20"" depth=""133"" />
    <rectangle x=""1638"" y=""1553"" z=""-5"" width=""47"" height=""119"" depth=""133"" />
    <rectangle x=""1638"" y=""1672"" z=""-5"" width=""50"" height=""25"" depth=""133"" />
    <rectangle x=""1638"" y=""1697"" z=""-5"" width=""40"" height=""20"" depth=""133"" />
    <rectangle x=""1638"" y=""1717"" z=""-5"" width=""24"" height=""28"" depth=""133"" />
    <rectangle x=""1603"" y=""1777"" z=""-5"" width=""30"" height=""5"" depth=""133"" />
    <rectangle x=""1500"" y=""1406"" z=""-5"" width=""49"" height=""69"" depth=""133"" />
    <rectangle x=""1511"" y=""1475"" z=""-5"" width=""23"" height=""32"" depth=""133"" />
    <rectangle x=""1538"" y=""1481"" z=""-5"" width=""26"" height=""26"" depth=""133"" />
    <rectangle x=""1415"" y=""1531"" z=""-5"" width=""3"" height=""51"" depth=""133"" />
    <rectangle x=""1418"" y=""1532"" z=""-5"" width=""220"" height=""223"" depth=""133"" />
    <rectangle x=""1400"" y=""1582"" z=""-5"" width=""18"" height=""93"" depth=""133"" />
   </region>
   <region name=""Cittadella di Britain"">
    <rectangle x=""1296"" y=""1564"" z=""-128"" width=""104"" height=""140"" depth=""256"" />
    <rectangle x=""1400"" y=""1555"" z=""-128"" width=""14"" height=""25"" depth=""256"" />
    <rectangle x=""1400"" y=""1674"" z=""-128"" width=""13"" height=""30"" depth=""256"" />
    <rectangle x=""1295"" y=""1555"" z=""-128"" width=""26"" height=""9"" depth=""256"" />
    <rectangle x=""1387"" y=""1555"" z=""-128"" width=""13"" height=""9"" depth=""256"" />
   </region>
   <region name=""Cimitero di Britain"">
    <rectangle x=""1335"" y=""1443"" z=""-128"" width=""57"" height=""52"" depth=""256"" />
    <rectangle x=""1335"" y=""1495"" z=""-128"" width=""57"" height=""18"" depth=""256"" />
   </region>
   <region name=""Cimitero di Jhelom"">
    <rectangle x=""1271"" y=""3711"" z=""-128"" width=""22"" height=""33"" depth=""256"" />
    <rectangle x=""1293"" y=""3722"" z=""-128"" width=""8"" height=""14"" depth=""256"" />
    <rectangle x=""1279"" y=""3744"" z=""-128"" width=""17"" height=""8"" depth=""256"" />
    <rectangle x=""1293"" y=""3736"" z=""-128"" width=""3"" height=""8"" depth=""256"" />
   </region>
   <region name=""Jhelom"">
    <rectangle x=""1330"" y=""3682"" z=""-20"" width=""148"" height=""188"" depth=""148"" />
    <rectangle x=""1330"" y=""3658"" z=""-20"" width=""50"" height=""24"" depth=""148"" />
    <rectangle x=""1393"" y=""3870"" z=""-20"" width=""90"" height=""41"" depth=""148"" />
    <rectangle x=""1479"" y=""3848"" z=""-20"" width=""16"" height=""22"" depth=""148"" />
    <rectangle x=""1478"" y=""3766"" z=""-20"" width=""1"" height=""82"" depth=""148"" />
    <rectangle x=""1479"" y=""3766"" z=""-20"" width=""3"" height=""8"" depth=""148"" />
    <rectangle x=""1479"" y=""3782"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1479"" y=""3800"" z=""-20"" width=""3"" height=""15"" depth=""148"" />
    <rectangle x=""1479"" y=""3823"" z=""-20"" width=""3"" height=""8"" depth=""148"" />
    <rectangle x=""1479"" y=""3839"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1341"" y=""3870"" z=""-20"" width=""11"" height=""2"" depth=""148"" />
    <rectangle x=""1367"" y=""3870"" z=""-20"" width=""9"" height=""2"" depth=""148"" />
    <rectangle x=""1391"" y=""3870"" z=""-20"" width=""2"" height=""2"" depth=""148"" />
    <rectangle x=""1391"" y=""3887"" z=""-20"" width=""2"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3659"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3675"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3691"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3707"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3731"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3747"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3763"" z=""-20"" width=""3"" height=""9"" depth=""148"" />
    <rectangle x=""1327"" y=""3779"" z=""-20"" width=""3"" height=""10"" depth=""148"" />
    <rectangle x=""1478"" y=""3669"" z=""-20"" width=""2"" height=""9"" depth=""148"" />
    <rectangle x=""1478"" y=""3685"" z=""-20"" width=""2"" height=""19"" depth=""148"" />
    <rectangle x=""1478"" y=""3713"" z=""-20"" width=""2"" height=""11"" depth=""148"" />
    <rectangle x=""1391"" y=""3598"" z=""-20"" width=""87"" height=""84"" depth=""148"" />
   </region>
   <region name=""Jhelom Nord"">
    <rectangle x=""1090"" y=""3556"" z=""-20"" width=""142"" height=""64"" depth=""148"" />
    <rectangle x=""1110"" y=""3620"" z=""-20"" width=""100"" height=""43"" depth=""148"" />
    <rectangle x=""1098"" y=""3650"" z=""-20"" width=""12"" height=""33"" depth=""148"" />
    <rectangle x=""1110"" y=""3663"" z=""-20"" width=""55"" height=""50"" depth=""148"" />
   </region>
   <region name=""Minoc"">
    <rectangle x=""2411"" y=""366"" z=""-128"" width=""135"" height=""241"" depth=""256"" />
    <rectangle x=""2464"" y=""607"" z=""-128"" width=""82"" height=""20"" depth=""256"" />
    <rectangle x=""2476"" y=""348"" z=""-128"" width=""64"" height=""18"" depth=""256"" />
    <rectangle x=""2399"" y=""436"" z=""-128"" width=""12"" height=""135"" depth=""256"" />
    <rectangle x=""2389"" y=""548"" z=""-128"" width=""10"" height=""21"" depth=""256"" />
    <rectangle x=""2546"" y=""574"" z=""-128"" width=""44"" height=""53"" depth=""256"" />
    <rectangle x=""2546"" y=""552"" z=""-128"" width=""13"" height=""22"" depth=""256"" />
   </region>
   <region name=""Trinsic"">
    <rectangle x=""2004"" y=""2813"" z=""-128"" width=""9"" height=""93"" depth=""256"" />
    <rectangle x=""1998"" y=""2900"" z=""-128"" width=""7"" height=""6"" depth=""256"" />
    <rectangle x=""1998"" y=""2906"" z=""40"" width=""9"" height=""1"" depth=""88"" />
    <rectangle x=""1991"" y=""2900"" z=""-128"" width=""7"" height=""7"" depth=""256"" />
    <rectangle x=""1975"" y=""2900"" z=""-128"" width=""16"" height=""24"" depth=""256"" />
    <rectangle x=""1975"" y=""2708"" z=""-128"" width=""29"" height=""192"" depth=""256"" />
    <rectangle x=""2004"" y=""2708"" z=""-128"" width=""3"" height=""105"" depth=""256"" />
    <rectangle x=""1988"" y=""2688"" z=""-128"" width=""19"" height=""20"" depth=""256"" />
    <rectangle x=""2007"" y=""2688"" z=""-128"" width=""47"" height=""59"" depth=""256"" />
    <rectangle x=""2007"" y=""2747"" z=""-128"" width=""16"" height=""27"" depth=""256"" />
    <rectangle x=""2023"" y=""2747"" z=""-128"" width=""7"" height=""24"" depth=""256"" />
    <rectangle x=""2030"" y=""2747"" z=""-128"" width=""3"" height=""22"" depth=""256"" />
    <rectangle x=""2033"" y=""2747"" z=""-128"" width=""2"" height=""19"" depth=""256"" />
    <rectangle x=""2035"" y=""2747"" z=""-128"" width=""2"" height=""15"" depth=""256"" />
    <rectangle x=""2037"" y=""2747"" z=""-128"" width=""2"" height=""11"" depth=""256"" />
    <rectangle x=""2039"" y=""2747"" z=""-128"" width=""4"" height=""10"" depth=""256"" />
    <rectangle x=""2043"" y=""2747"" z=""-128"" width=""2"" height=""7"" depth=""256"" />
    <rectangle x=""2045"" y=""2747"" z=""-128"" width=""3"" height=""5"" depth=""256"" />
    <rectangle x=""2007"" y=""2774"" z=""-128"" width=""6"" height=""10"" depth=""256"" />
    <rectangle x=""1935"" y=""2759"" z=""-128"" width=""40"" height=""112"" depth=""256"" />
    <rectangle x=""1970"" y=""2717"" z=""-128"" width=""5"" height=""39"" depth=""256"" />
    <rectangle x=""1880"" y=""2759"" z=""-128"" width=""55"" height=""136"" depth=""256"" />
    <rectangle x=""1864"" y=""2871"" z=""-128"" width=""16"" height=""24"" depth=""256"" />
    <rectangle x=""1840"" y=""2759"" z=""-128"" width=""40"" height=""112"" depth=""256"" />
    <rectangle x=""1799"" y=""2796"" z=""-128"" width=""41"" height=""75"" depth=""256"" />
    <rectangle x=""1815"" y=""2871"" z=""-128"" width=""49"" height=""16"" depth=""256"" />
    <rectangle x=""1834"" y=""2784"" z=""-128"" width=""6"" height=""12"" depth=""256"" />
    <rectangle x=""1835"" y=""2763"" z=""-128"" width=""5"" height=""14"" depth=""256"" />
    <rectangle x=""1795"" y=""2759"" z=""-128"" width=""45"" height=""4"" depth=""256"" />
    <rectangle x=""1795"" y=""2695"" z=""-128"" width=""130"" height=""64"" depth=""256"" />
    <rectangle x=""1815"" y=""2663"" z=""-128"" width=""110"" height=""32"" depth=""256"" />
    <rectangle x=""1855"" y=""2660"" z=""-128"" width=""70"" height=""3"" depth=""256"" />
    <rectangle x=""1855"" y=""2635"" z=""-128"" width=""70"" height=""25"" depth=""256"" />
    <rectangle x=""1925"" y=""2754"" z=""-128"" width=""26"" height=""5"" depth=""256"" />
    <rectangle x=""1951"" y=""2754"" z=""-128"" width=""9"" height=""5"" depth=""256"" />
    <rectangle x=""1960"" y=""2751"" z=""-128"" width=""9"" height=""8"" depth=""256"" />
    <rectangle x=""2013"" y=""2813"" z=""-128"" width=""5"" height=""94"" depth=""256"" />
    <rectangle x=""2018"" y=""2848"" z=""50"" width=""1"" height=""16"" depth=""78"" />
    <rectangle x=""1950"" y=""2751"" z=""50"" width=""11"" height=""4"" depth=""78"" />
    <rectangle x=""1836"" y=""2777"" z=""39"" width=""4"" height=""7"" depth=""89"" />
   </region>
   <region name=""Vesper"">
    <rectangle x=""2840"" y=""649"" z=""-128"" width=""116"" height=""365"" depth=""256"" />
    <rectangle x=""2864"" y=""634"" z=""-128"" width=""57"" height=""15"" depth=""256"" />
    <rectangle x=""2956"" y=""686"" z=""-128"" width=""39"" height=""290"" depth=""256"" />
    <rectangle x=""2995"" y=""744"" z=""-128"" width=""54"" height=""124"" depth=""256"" />
    <rectangle x=""2820"" y=""782"" z=""-128"" width=""20"" height=""185"" depth=""256"" />
    <rectangle x=""2827"" y=""998"" z=""-128"" width=""13"" height=""9"" depth=""256"" />
    <rectangle x=""2824"" y=""967"" z=""-128"" width=""16"" height=""31"" depth=""256"" />
    <rectangle x=""2814"" y=""951"" z=""-128"" width=""6"" height=""8"" depth=""256"" />
   </region>
   <region name=""Cimitero di Vesper"">
    <rectangle x=""2727"" y=""839"" z=""-128"" width=""57"" height=""57"" depth=""256"" />
   </region>
   <region name=""Sede Idior"">
    <rectangle x=""600"" y=""808"" z=""-128"" width=""24"" height=""24"" depth=""256"" />
    <rectangle x=""648"" y=""808"" z=""-128"" width=""24"" height=""24"" depth=""256"" />
    <rectangle x=""624"" y=""816"" z=""-128"" width=""24"" height=""40"" depth=""256"" />
    <rectangle x=""608"" y=""872"" z=""-128"" width=""16"" height=""24"" depth=""256"" />
    <rectangle x=""621"" y=""854"" z=""20"" width=""3"" height=""18"" depth=""108"" />
   </region>
   <region name=""Yew"">
    <rectangle x=""441"" y=""902"" z=""0"" width=""163"" height=""169"" depth=""128"" />
   </region>
   <region name=""Serpent's Hold"">
    <rectangle x=""2933"" y=""3330"" z=""0"" width=""143"" height=""48"" depth=""128"" />
    <rectangle x=""2933"" y=""3378"" z=""0"" width=""21"" height=""8"" depth=""128"" />
    <rectangle x=""2966"" y=""3378"" z=""0"" width=""21"" height=""6"" depth=""128"" />
    <rectangle x=""2987"" y=""3378"" z=""0"" width=""89"" height=""109"" depth=""128"" />
    <rectangle x=""2957"" y=""3396"" z=""0"" width=""30"" height=""21"" depth=""128"" />
    <rectangle x=""2965"" y=""3417"" z=""0"" width=""22"" height=""83"" depth=""128"" />
    <rectangle x=""2950"" y=""3437"" z=""0"" width=""15"" height=""63"" depth=""128"" />
   </region>
   <region name=""Skara Brae"">
    <rectangle x=""515"" y=""2045"" z=""-128"" width=""173"" height=""62"" depth=""256"" />
    <rectangle x=""514"" y=""2107"" z=""-128"" width=""24"" height=""45"" depth=""256"" />
    <rectangle x=""538"" y=""2107"" z=""-128"" width=""142"" height=""194"" depth=""256"" />
    <rectangle x=""520"" y=""2195"" z=""-128"" width=""18"" height=""53"" depth=""256"" />
    <rectangle x=""680"" y=""2107"" z=""-128"" width=""11"" height=""11"" depth=""256"" />
   </region>
   <region name=""Nujel'm"">
    <rectangle x=""3791"" y=""1216"" z=""-20"" width=""4"" height=""9"" depth=""148"" />
    <rectangle x=""3786"" y=""1172"" z=""-20"" width=""4"" height=""76"" depth=""148"" />
    <rectangle x=""3790"" y=""1178"" z=""-20"" width=""1"" height=""70"" depth=""148"" />
    <rectangle x=""3791"" y=""1178"" z=""-20"" width=""5"" height=""18"" depth=""147"" />
    <rectangle x=""3786"" y=""1082"" z=""-20"" width=""24"" height=""90"" depth=""148"" />
    <rectangle x=""3747"" y=""1111"" z=""-20"" width=""15"" height=""29"" depth=""148"" />
    <rectangle x=""3755"" y=""1151"" z=""-20"" width=""7"" height=""41"" depth=""148"" />
    <rectangle x=""3704"" y=""1178"" z=""-20"" width=""22"" height=""14"" depth=""147"" />
    <rectangle x=""3750"" y=""1170"" z=""-20"" width=""5"" height=""22"" depth=""148"" />
    <rectangle x=""3695"" y=""1175"" z=""-20"" width=""9"" height=""9"" depth=""148"" />
    <rectangle x=""3726"" y=""1173"" z=""-20"" width=""24"" height=""19"" depth=""148"" />
    <rectangle x=""3655"" y=""1184"" z=""-20"" width=""33"" height=""8"" depth=""148"" />
    <rectangle x=""3688"" y=""1185"" z=""-20"" width=""16"" height=""7"" depth=""148"" />
    <rectangle x=""3697"" y=""1184"" z=""-20"" width=""7"" height=""1"" depth=""148"" />
    <rectangle x=""3762"" y=""1082"" z=""-20"" width=""24"" height=""110"" depth=""148"" />
    <rectangle x=""3695"" y=""1192"" z=""-20"" width=""91"" height=""29"" depth=""148"" />
    <rectangle x=""3656"" y=""1192"" z=""-20"" width=""39"" height=""23"" depth=""148"" />
    <rectangle x=""3651"" y=""1218"" z=""-20"" width=""20"" height=""33"" depth=""147"" />
    <rectangle x=""3655"" y=""1215"" z=""-20"" width=""40"" height=""3"" depth=""148"" />
    <rectangle x=""3759"" y=""1272"" z=""-20"" width=""7"" height=""10"" depth=""148"" />
    <rectangle x=""3647"" y=""1231"" z=""-20"" width=""4"" height=""9"" depth=""148"" />
    <rectangle x=""3695"" y=""1221"" z=""-20"" width=""91"" height=""3"" depth=""148"" />
    <rectangle x=""3698"" y=""1224"" z=""-20"" width=""88"" height=""13"" depth=""148"" />
    <rectangle x=""3701"" y=""1237"" z=""-20"" width=""85"" height=""11"" depth=""148"" />
    <rectangle x=""3712"" y=""1248"" z=""-20"" width=""47"" height=""15"" depth=""148"" />
    <rectangle x=""3720"" y=""1263"" z=""-20"" width=""39"" height=""19"" depth=""148"" />
   </region>
   <region name=""Moonglow"">
    <rectangle x=""4376"" y=""1013"" z=""-4"" width=""59"" height=""35"" depth=""132"" />
    <rectangle x=""4376"" y=""1048"" z=""-4"" width=""122"" height=""134"" depth=""132"" />
   </region>
   <region name=""Magincia"">
    <rectangle x=""3662"" y=""1991"" z=""-10"" width=""91"" height=""270"" depth=""138"" />
    <rectangle x=""3647"" y=""2075"" z=""-10"" width=""15"" height=""186"" depth=""138"" />
    <rectangle x=""3753"" y=""2030"" z=""-10"" width=""70"" height=""255"" depth=""138"" />
    <rectangle x=""3687"" y=""2261"" z=""-10"" width=""66"" height=""25"" depth=""138"" />
   </region>
   <region name=""Stirling"">
    <rectangle x=""1816"" y=""2043"" z=""-128"" width=""129"" height=""86"" depth=""256"" />
    <rectangle x=""1862"" y=""2129"" z=""-128"" width=""83"" height=""29"" depth=""256"" />
    <rectangle x=""1945"" y=""2043"" z=""-128"" width=""43"" height=""73"" depth=""256"" />
   </region>
   <region name=""Dregoth"">
    <rectangle x=""2108"" y=""54"" z=""-128"" width=""40"" height=""55"" depth=""256"" />
    <rectangle x=""2130"" y=""109"" z=""-128"" width=""12"" height=""38"" depth=""256"" />
    <rectangle x=""2050"" y=""129"" z=""-128"" width=""80"" height=""31"" depth=""256"" />
    <rectangle x=""2054"" y=""160"" z=""-128"" width=""76"" height=""31"" depth=""256"" />
    <rectangle x=""2067"" y=""191"" z=""-128"" width=""112"" height=""26"" depth=""256"" />
    <rectangle x=""2054"" y=""191"" z=""-128"" width=""13"" height=""13"" depth=""256"" />
    <rectangle x=""2048"" y=""109"" z=""-128"" width=""82"" height=""20"" depth=""256"" />
    <rectangle x=""2130"" y=""147"" z=""-128"" width=""67"" height=""44"" depth=""256"" />
   </region>
   <region name=""Buccaneer's Den"">
    <rectangle x=""2612"" y=""2057"" z=""-128"" width=""164"" height=""210"" depth=""256"" />
    <rectangle x=""2604"" y=""2065"" z=""0"" width=""8"" height=""189"" depth=""128"" />
   </region>
   <region name=""Zona Falsari"">
    <rectangle x=""2830"" y=""2318"" z=""-128"" width=""26"" height=""18"" depth=""256"" />
   </region>
   <region name=""Delucia"">
    <rectangle x=""5123"" y=""3942"" z=""-128"" width=""192"" height=""122"" depth=""256"" />
    <rectangle x=""5147"" y=""4064"" z=""-128"" width=""125"" height=""20"" depth=""256"" />
    <rectangle x=""5235"" y=""3930"" z=""-128"" width=""80"" height=""12"" depth=""256"" />
   </region>
   <region name=""Papua"">
    <rectangle x=""5639"" y=""3095"" z=""-128"" width=""192"" height=""223"" depth=""256"" />
    <rectangle x=""5831"" y=""3237"" z=""-128"" width=""20"" height=""30"" depth=""256"" />
   </region>
   <region name=""Sede Elhoim"">
    <rectangle x=""5767"" y=""3361"" z=""-128"" width=""50"" height=""30"" depth=""256"" />
    <rectangle x=""5805"" y=""3357"" z=""39"" width=""12"" height=""4"" depth=""89"" />
    <rectangle x=""5817"" y=""3354"" z=""39"" width=""25"" height=""14"" depth=""89"" />
    <rectangle x=""5842"" y=""3349"" z=""-128"" width=""31"" height=""14"" depth=""256"" />
   </region>
   <region name=""Serpent's Hold Healer"">
    <rectangle x=""2962"" y=""3416"" z=""11"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Minoc Healer"">
    <rectangle x=""2441"" y=""428"" z=""11"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Moonglow Healer"">
    <rectangle x=""4379"" y=""1075"" z=""-4"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Vesper Healer"">
    <rectangle x=""2873"" y=""715"" z=""-4"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Britain Healer"">
    <rectangle x=""1467"" y=""1602"" z=""16"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Delucia Healer"">
    <rectangle x=""5178"" y=""3985"" z=""33"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Ocllo Healer"">
    <rectangle x=""3670"" y=""2547"" z=""-4"" width=""9"" height=""9"" depth=""10"" />
   </region>
   <region name=""Nujel'm Healer"">
    <rectangle x=""3688"" y=""1189"" z=""-4"" width=""9"" height=""9"" depth=""10"" />
   </region>
   <region name=""Skara Brae Healer"">
    <rectangle x=""624"" y=""2205"" z=""-4"" width=""9"" height=""9"" depth=""10"" />
   </region>
   <region name=""Tempio di Delucia"">
    <rectangle x=""5258"" y=""3931"" z=""-128"" width=""16"" height=""18"" depth=""256"" />
    <rectangle x=""5274"" y=""3939"" z=""-128"" width=""8"" height=""12"" depth=""256"" />
    <rectangle x=""5271"" y=""3949"" z=""-128"" width=""3"" height=""2"" depth=""256"" />
    <rectangle x=""5275"" y=""3934"" z=""-128"" width=""3"" height=""5"" depth=""256"" />
   </region>
   <region name=""Tempio di Sede Elhoim"">
    <rectangle x=""5768"" y=""3361"" z=""-32"" width=""29"" height=""30"" depth=""20"" />
   </region>
   <region name=""Tempio di Sede Idior"">
    <rectangle x=""618"" y=""816"" z=""-128"" width=""34"" height=""12"" depth=""256"" />
   </region>
   <region name=""Tempio di Dregoth"">
    <rectangle x=""2062"" y=""171"" z=""-128"" width=""13"" height=""26"" depth=""256"" />
    <rectangle x=""2075"" y=""177"" z=""-128"" width=""2"" height=""20"" depth=""256"" />
    <rectangle x=""2077"" y=""177"" z=""-128"" width=""2"" height=""19"" depth=""256"" />
    <rectangle x=""2079"" y=""177"" z=""-128"" width=""3"" height=""17"" depth=""256"" />
    <rectangle x=""2079"" y=""177"" z=""-128"" width=""8"" height=""17"" depth=""256"" />
   </region>
   <region name=""Tempio di Vesper"">
    <rectangle x=""2904"" y=""852"" z=""30"" width=""31"" height=""32"" depth=""98"" />
   </region>
   <region name=""Tempio di Moonglow"">
    <rectangle x=""4414"" y=""1150"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
    <rectangle x=""4419"" y=""1146"" z=""-128"" width=""7"" height=""4"" depth=""256"" />
   </region>
   <region name=""Tempio di Britain Elhoim"">
    <rectangle x=""1569"" y=""1552"" z=""-128"" width=""23"" height=""17"" depth=""256"" />
   </region>
   <region name=""Tempio di Britain Idior"">
    <rectangle x=""1538"" y=""1702"" z=""-128"" width=""15"" height=""19"" depth=""256"" />
    <rectangle x=""1542"" y=""1721"" z=""-128"" width=""6"" height=""3"" depth=""256"" />
   </region>
   <region name=""Tempio di Ocllo"">
    <rectangle x=""3670"" y=""2483"" z=""-128"" width=""9"" height=""6"" depth=""256"" />
    <rectangle x=""3666"" y=""2489"" z=""-128"" width=""18"" height=""6"" depth=""256"" />
    <rectangle x=""3670"" y=""2495"" z=""-128"" width=""10"" height=""16"" depth=""256"" />
   </region>
   <region name=""Tempio di Midian"">
    <rectangle x=""5200"" y=""3616"" z=""-128"" width=""22"" height=""14"" depth=""256"" />
   </region>
   <region name=""Tempio di Magincia"">
    <rectangle x=""3656"" y=""2106"" z=""-128"" width=""25"" height=""5"" depth=""256"" />
    <rectangle x=""3659"" y=""2104"" z=""-128"" width=""19"" height=""2"" depth=""256"" />
    <rectangle x=""3660"" y=""2102"" z=""-128"" width=""17"" height=""2"" depth=""256"" />
    <rectangle x=""3661"" y=""2101"" z=""-128"" width=""15"" height=""1"" depth=""256"" />
    <rectangle x=""3662"" y=""2100"" z=""-128"" width=""11"" height=""1"" depth=""256"" />
    <rectangle x=""3664"" y=""2099"" z=""-128"" width=""9"" height=""1"" depth=""256"" />
    <rectangle x=""3666"" y=""2096"" z=""-128"" width=""5"" height=""3"" depth=""256"" />
    <rectangle x=""3659"" y=""2111"" z=""-128"" width=""19"" height=""2"" depth=""256"" />
    <rectangle x=""3660"" y=""2113"" z=""-128"" width=""17"" height=""2"" depth=""256"" />
    <rectangle x=""3661"" y=""2115"" z=""-128"" width=""15"" height=""1"" depth=""256"" />
    <rectangle x=""3662"" y=""2116"" z=""-128"" width=""13"" height=""1"" depth=""256"" />
    <rectangle x=""3666"" y=""2117"" z=""-128"" width=""5"" height=""4"" depth=""256"" />
   </region>
   <region name=""Tempio di Serpent's Hold"">
    <rectangle x=""2936"" y=""3368"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
   </region>
   <region name=""Tempio di Jhelom"">
    <rectangle x=""1424"" y=""3872"" z=""-128"" width=""24"" height=""16"" depth=""256"" />
    <rectangle x=""1432"" y=""3864"" z=""-128"" width=""8"" height=""8"" depth=""256"" />
   </region>
   <region name=""Tempio di Cove"">
    <rectangle x=""2200"" y=""1226"" z=""-128"" width=""18"" height=""15"" depth=""256"" />
    <rectangle x=""2205"" y=""1222"" z=""-128"" width=""7"" height=""4"" depth=""256"" />
   </region>
   <region name=""Tempio di Cittadella di Britain Idior"">
    <rectangle x=""1332"" y=""1669"" z=""28"" width=""29"" height=""15"" depth=""11"" />
    <rectangle x=""1320"" y=""1672"" z=""-128"" width=""12"" height=""12"" depth=""256"" />
   </region>
   <region name=""Tempio di Stirling"">
    <rectangle x=""1955"" y=""2058"" z=""-128"" width=""17"" height=""3"" depth=""256"" />
    <rectangle x=""1951"" y=""2061"" z=""-128"" width=""25"" height=""13"" depth=""256"" />
   </region>
   <region name=""Tempio di Nujel'm"">
    <rectangle x=""3768"" y=""1120"" z=""-128"" width=""23"" height=""16"" depth=""256"" />
   </region>
   <region name=""Tempio di Trinsic"">
    <rectangle x=""1955"" y=""2838"" z=""-128"" width=""23"" height=""11"" depth=""256"" />
   </region>
   <region name=""Tempio di Skara Brae"">
    <rectangle x=""590"" y=""2269"" z=""-128"" width=""4"" height=""19"" depth=""256"" />
    <rectangle x=""601"" y=""2269"" z=""-128"" width=""5"" height=""19"" depth=""256"" />
    <rectangle x=""606"" y=""2274"" z=""-128"" width=""1"" height=""9"" depth=""256"" />
    <rectangle x=""607"" y=""2276"" z=""-128"" width=""2"" height=""5"" depth=""256"" />
    <rectangle x=""594"" y=""2271"" z=""-128"" width=""7"" height=""15"" depth=""256"" />
   </region>
   <region name=""Tempio di Cittadella di Britain Elhoim"">
    <rectangle x=""1312"" y=""1572"" z=""-128"" width=""10"" height=""20"" depth=""256"" />
    <rectangle x=""1322"" y=""1572"" z=""-128"" width=""9"" height=""7"" depth=""256"" />
   </region>
   <region name=""Tempio di Minoc"">
    <rectangle x=""2488"" y=""372"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
   </region>
  </Towns>
  <Dungeons>
   <region name=""Wind"">
    <rectangle x=""5294"" y=""19"" z=""-128"" width=""72"" height=""120"" depth=""256"" />
    <rectangle x=""5132"" y=""58"" z=""-128"" width=""81"" height=""68"" depth=""256"" />
    <rectangle x=""5197"" y=""126"" z=""-128"" width=""55"" height=""78"" depth=""256"" />
    <rectangle x=""5132"" y=""3"" z=""-128"" width=""70"" height=""55"" depth=""256"" />
    <rectangle x=""5252"" y=""112"" z=""-128"" width=""42"" height=""58"" depth=""256"" />
    <rectangle x=""5213"" y=""98"" z=""-128"" width=""39"" height=""28"" depth=""256"" />
    <rectangle x=""5279"" y=""57"" z=""-128"" width=""15"" height=""55"" depth=""256"" />
    <rectangle x=""5252"" y=""170"" z=""-128"" width=""32"" height=""8"" depth=""256"" />
    <rectangle x=""5286"" y=""25"" z=""-128"" width=""8"" height=""32"" depth=""256"" />
    <rectangle x=""5252"" y=""178"" z=""-128"" width=""20"" height=""5"" depth=""256"" />
    <rectangle x=""5252"" y=""183"" z=""-128"" width=""10"" height=""10"" depth=""256"" />
   </region>
   <region name=""Covetous Lago Felucca"">
    <rectangle x=""5394"" y=""1782"" z=""-128"" width=""97"" height=""58"" depth=""256"" />
   </region>
   <region name=""Covetous Felucca"">
    <rectangle x=""5376"" y=""1842"" z=""-128"" width=""142"" height=""102"" depth=""256"" />
    <rectangle x=""5369"" y=""1944"" z=""-128"" width=""53"" height=""18"" depth=""256"" />
   </region>
   <region name=""Covetous 2 Felucca"">
    <rectangle x=""5375"" y=""1964"" z=""-128"" width=""250"" height=""79"" depth=""256"" />
    <rectangle x=""5423"" y=""1945"" z=""-128"" width=""74"" height=""19"" depth=""256"" />
   </region>
   <region name=""Covetous 3 Felucca"">
    <rectangle x=""5532"" y=""1822"" z=""-128"" width=""98"" height=""108"" depth=""256"" />
   </region>
   <region name=""Covetous prigioni Felucca"">
    <rectangle x=""5532"" y=""1822"" z=""-128"" width=""98"" height=""108"" depth=""256"" />
   </region>
   <region name=""Deceit 1"">
    <rectangle x=""5126"" y=""522"" z=""-128"" width=""126"" height=""120"" depth=""256"" />
   </region>
   <region name=""Deceit 2"">
    <rectangle x=""5264"" y=""521"" z=""-128"" width=""106"" height=""121"" depth=""256"" />
   </region>
   <region name=""Despise"">
    <rectangle x=""5377"" y=""516"" z=""-128"" width=""254"" height=""506"" depth=""256"" />
   </region>
   <region name=""Old Khaldun"">
    <rectangle x=""5381"" y=""1284"" z=""-128"" width=""247"" height=""225"" depth=""256"" />
   </region>
   <region name=""Shame 1"">
    <rectangle x=""5505"" y=""0"" z=""-128"" width=""130"" height=""126"" depth=""256"" />
   </region>
   <region name=""Terathan Keep 1"">
    <rectangle x=""5122"" y=""1531"" z=""-128"" width=""222"" height=""187"" depth=""256"" />
    <rectangle x=""5123"" y=""1718"" z=""-128"" width=""65"" height=""16"" depth=""256"" />
    <rectangle x=""5238"" y=""1718"" z=""-128"" width=""106"" height=""24"" depth=""256"" />
    <rectangle x=""5283"" y=""1742"" z=""-128"" width=""96"" height=""51"" depth=""256"" />
    <rectangle x=""5344"" y=""1531"" z=""-128"" width=""31"" height=""211"" depth=""256"" />
   </region>
   <region name=""Labirinto 2 felucca"">
    <rectangle x=""5124"" y=""1406"" z=""-128"" width=""253"" height=""113"" depth=""256"" />
   </region>
   <region name=""Labirinto 3 felucca"">
    <rectangle x=""5532"" y=""1820"" z=""-128"" width=""98"" height=""108"" depth=""256"" />
    <rectangle x=""5494"" y=""1786"" z=""-128"" width=""71"" height=""34"" depth=""256"" />
   </region>
   <region name=""Misc Dungeons"">
    <rectangle x=""5886"" y=""1281"" z=""-128"" width=""257"" height=""254"" depth=""256"" />
   </region>
   <region name=""Palude"">
    <rectangle x=""1112"" y=""2936"" z=""-128"" width=""158"" height=""55"" depth=""256"" />
    <rectangle x=""1090"" y=""2989"" z=""-128"" width=""116"" height=""56"" depth=""256"" />
    <rectangle x=""1090"" y=""2951"" z=""-128"" width=""22"" height=""38"" depth=""256"" />
    <rectangle x=""1080"" y=""2966"" z=""-128"" width=""10"" height=""32"" depth=""256"" />
    <rectangle x=""1106"" y=""2929"" z=""-128"" width=""6"" height=""21"" depth=""256"" />
    <rectangle x=""1112"" y=""2860"" z=""-128"" width=""31"" height=""76"" depth=""256"" />
    <rectangle x=""1142"" y=""2860"" z=""-128"" width=""68"" height=""36"" depth=""256"" />
    <rectangle x=""1142"" y=""2879"" z=""-128"" width=""63"" height=""57"" depth=""256"" />
    <rectangle x=""1098"" y=""2720"" z=""-128"" width=""92"" height=""143"" depth=""256"" />
    <rectangle x=""1188"" y=""2751"" z=""-128"" width=""47"" height=""67"" depth=""256"" />
    <rectangle x=""1817"" y=""2238"" z=""-128"" width=""51"" height=""32"" depth=""256"" />
    <rectangle x=""1817"" y=""2268"" z=""-128"" width=""51"" height=""46"" depth=""256"" />
    <rectangle x=""1806"" y=""2270"" z=""-128"" width=""11"" height=""66"" depth=""256"" />
    <rectangle x=""1816"" y=""2313"" z=""-128"" width=""96"" height=""96"" depth=""256"" />
    <rectangle x=""1803"" y=""2402"" z=""-128"" width=""13"" height=""16"" depth=""256"" />
    <rectangle x=""1815"" y=""2408"" z=""-128"" width=""23"" height=""26"" depth=""256"" />
    <rectangle x=""1837"" y=""2409"" z=""-128"" width=""51"" height=""51"" depth=""256"" />
    <rectangle x=""1887"" y=""2291"" z=""-128"" width=""239"" height=""158"" depth=""256"" />
    <rectangle x=""1898"" y=""2272"" z=""-128"" width=""211"" height=""19"" depth=""256"" />
    <rectangle x=""1867"" y=""2252"" z=""-128"" width=""31"" height=""39"" depth=""256"" />
    <rectangle x=""1867"" y=""2290"" z=""-128"" width=""20"" height=""23"" depth=""256"" />
   </region>
   <region name=""Tunnel di Wind"">
    <rectangle x=""5140"" y=""5"" z=""-128"" width=""235"" height=""249"" depth=""256"" />
    <rectangle x=""5119"" y=""125"" z=""-128"" width=""25"" height=""42"" depth=""256"" />
   </region>
   <region name=""Deserto dei Fremen"">
    <rectangle x=""1802"" y=""814"" z=""-128"" width=""232"" height=""72"" depth=""256"" />
    <rectangle x=""2033"" y=""823"" z=""-128"" width=""8"" height=""27"" depth=""256"" />
    <rectangle x=""1865"" y=""804"" z=""-128"" width=""168"" height=""10"" depth=""256"" />
    <rectangle x=""1847"" y=""809"" z=""-128"" width=""19"" height=""5"" depth=""256"" />
    <rectangle x=""1780"" y=""885"" z=""-128"" width=""225"" height=""15"" depth=""256"" />
    <rectangle x=""1780"" y=""900"" z=""-128"" width=""225"" height=""10"" depth=""256"" />
    <rectangle x=""1780"" y=""910"" z=""-128"" width=""212"" height=""10"" depth=""256"" />
    <rectangle x=""1780"" y=""920"" z=""-128"" width=""200"" height=""20"" depth=""256"" />
    <rectangle x=""1780"" y=""940"" z=""-128"" width=""180"" height=""20"" depth=""256"" />
    <rectangle x=""1780"" y=""960"" z=""-128"" width=""160"" height=""15"" depth=""256"" />
    <rectangle x=""1780"" y=""975"" z=""-128"" width=""130"" height=""10"" depth=""256"" />
   </region>
   <region name=""Custom Region 2"">
    <rectangle x=""6062"" y=""21"" z=""-128"" width=""54"" height=""80"" depth=""256"" />
   </region>
   <region name=""Wrong 2"">
    <rectangle x=""5635"" y=""516"" z=""-128"" width=""109"" height=""71"" depth=""256"" />
   </region>
   <region name=""Antico Lycaeum"">
    <rectangle x=""4286"" y=""948"" z=""-128"" width=""50"" height=""64"" depth=""256"" />
   </region>
   <region name=""Prisma della Luce"">
    <rectangle x=""6441"" y=""13"" z=""-128"" width=""176"" height=""216"" depth=""256"" />
   </region>
   <region name=""Caverne di Khaldun"">
    <rectangle x=""5377"" y=""1281"" z=""-128"" width=""254"" height=""231"" depth=""256"" />
   </region>
   <region name=""Dungeon Newbie"">
    <rectangle x=""5120"" y=""1408"" z=""-128"" width=""254"" height=""107"" depth=""256"" />
   </region>
   <region name=""Stanza Segreta di Prisma"">
    <rectangle x=""6500"" y=""112"" z=""-128"" width=""45"" height=""34"" depth=""256"" />
    <rectangle x=""6506"" y=""145"" z=""-128"" width=""5"" height=""13"" depth=""256"" />
    <rectangle x=""6503"" y=""158"" z=""-128"" width=""17"" height=""31"" depth=""256"" />
   </region>
   <region name=""Covetous Livello 3"">
    <rectangle x=""5528"" y=""1821"" z=""-128"" width=""100"" height=""112"" depth=""256"" />
   </region>
   <region name=""Covetous Livello 5"">
    <rectangle x=""5398"" y=""1789"" z=""-128"" width=""87"" height=""48"" depth=""256"" />
   </region>
   <region name=""Fire Dungeon"">
    <rectangle x=""5635"" y=""1282"" z=""-128"" width=""255"" height=""242"" depth=""256"" />
   </region>
   <region name=""Ingresso Vampire Dungeon"">
    <rectangle x=""139"" y=""1488"" z=""-128"" width=""7"" height=""5"" depth=""256"" />
   </region>
   <region name=""Shame Livello 1"">
    <rectangle x=""5376"" y=""2"" z=""-128"" width=""124"" height=""124"" depth=""256"" />
    <rectangle x=""5395"" y=""125"" z=""-128"" width=""3"" height=""4"" depth=""256"" />
   </region>
   <region name=""Shame Livello 3"">
    <rectangle x=""5376"" y=""132"" z=""-128"" width=""254"" height=""122"" depth=""256"" />
   </region>
   <region name=""Ice Dungeon"">
    <rectangle x=""5661"" y=""133"" z=""-128"" width=""229"" height=""128"" depth=""256"" />
    <rectangle x=""5803"" y=""323"" z=""-128"" width=""61"" height=""62"" depth=""256"" />
   </region>
   <region name=""Sala dei Cristalli"">
    <rectangle x=""5650"" y=""297"" z=""-128"" width=""61"" height=""46"" depth=""256"" />
    <rectangle x=""5633"" y=""318"" z=""-128"" width=""23"" height=""18"" depth=""256"" />
    <rectangle x=""5652"" y=""236"" z=""-128"" width=""60"" height=""58"" depth=""256"" />
    <rectangle x=""5711"" y=""256"" z=""-128"" width=""22"" height=""34"" depth=""256"" />
    <rectangle x=""5642"" y=""274"" z=""-128"" width=""10"" height=""16"" depth=""256"" />
    <rectangle x=""5694"" y=""290"" z=""-128"" width=""11"" height=""8"" depth=""256"" />
   </region>
   <region name=""Percorsi"">
    <rectangle x=""5902"" y=""398"" z=""-128"" width=""101"" height=""101"" depth=""256"" />
   </region>
   <region name=""Orc Village Newbie"">
    <rectangle x=""2178"" y=""1274"" z=""-128"" width=""46"" height=""15"" depth=""256"" />
    <rectangle x=""2178"" y=""1252"" z=""-128"" width=""13"" height=""22"" depth=""256"" />
    <rectangle x=""2224"" y=""1274"" z=""-128"" width=""2"" height=""5"" depth=""256"" />
    <rectangle x=""2210"" y=""1289"" z=""-128"" width=""14"" height=""8"" depth=""256"" />
    <rectangle x=""2210"" y=""1297"" z=""-128"" width=""11"" height=""9"" depth=""256"" />
    <rectangle x=""2210"" y=""1306"" z=""-128"" width=""8"" height=""3"" depth=""256"" />
    <rectangle x=""2209"" y=""1309"" z=""-128"" width=""6"" height=""4"" depth=""256"" />
    <rectangle x=""2177"" y=""1286"" z=""-128"" width=""1"" height=""3"" depth=""256"" />
    <rectangle x=""2175"" y=""1289"" z=""-128"" width=""26"" height=""8"" depth=""256"" />
    <rectangle x=""2170"" y=""1297"" z=""-128"" width=""27"" height=""5"" depth=""256"" />
    <rectangle x=""2167"" y=""1302"" z=""-128"" width=""28"" height=""3"" depth=""256"" />
    <rectangle x=""2164"" y=""1305"" z=""-128"" width=""30"" height=""2"" depth=""256"" />
    <rectangle x=""2149"" y=""1306"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""2151"" y=""1307"" z=""-128"" width=""43"" height=""4"" depth=""256"" />
    <rectangle x=""2154"" y=""1311"" z=""-128"" width=""39"" height=""11"" depth=""256"" />
    <rectangle x=""2151"" y=""1322"" z=""-128"" width=""42"" height=""4"" depth=""256"" />
    <rectangle x=""2149"" y=""1326"" z=""-128"" width=""43"" height=""3"" depth=""256"" />
    <rectangle x=""2146"" y=""1329"" z=""-128"" width=""46"" height=""3"" depth=""256"" />
    <rectangle x=""2142"" y=""1332"" z=""-128"" width=""51"" height=""4"" depth=""256"" />
    <rectangle x=""2137"" y=""1336"" z=""-128"" width=""7"" height=""23"" depth=""256"" />
    <rectangle x=""2167"" y=""1336"" z=""-128"" width=""9"" height=""8"" depth=""256"" />
    <rectangle x=""2151"" y=""1344"" z=""-128"" width=""41"" height=""31"" depth=""256"" />
    <rectangle x=""2151"" y=""1375"" z=""-128"" width=""41"" height=""6"" depth=""256"" />
    <rectangle x=""2157"" y=""1381"" z=""-128"" width=""35"" height=""11"" depth=""256"" />
    <rectangle x=""2167"" y=""1392"" z=""-128"" width=""24"" height=""8"" depth=""256"" />
    <rectangle x=""2171"" y=""1400"" z=""-128"" width=""20"" height=""7"" depth=""256"" />
    <rectangle x=""2192"" y=""1367"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
    <rectangle x=""2199"" y=""1351"" z=""-128"" width=""9"" height=""16"" depth=""256"" />
    <rectangle x=""2208"" y=""1353"" z=""-128"" width=""9"" height=""14"" depth=""256"" />
    <rectangle x=""2208"" y=""1367"" z=""-128"" width=""17"" height=""9"" depth=""256"" />
    <rectangle x=""2208"" y=""1376"" z=""-128"" width=""18"" height=""7"" depth=""256"" />
    <rectangle x=""2192"" y=""1383"" z=""-128"" width=""24"" height=""3"" depth=""256"" />
    <rectangle x=""2216"" y=""1383"" z=""-128"" width=""5"" height=""2"" depth=""256"" />
    <rectangle x=""2192"" y=""1386"" z=""-128"" width=""22"" height=""4"" depth=""256"" />
    <rectangle x=""2192"" y=""1390"" z=""-128"" width=""17"" height=""2"" depth=""256"" />
    <rectangle x=""2191"" y=""1392"" z=""-128"" width=""13"" height=""10"" depth=""256"" />
    <rectangle x=""2191"" y=""1402"" z=""-128"" width=""9"" height=""5"" depth=""256"" />
    <rectangle x=""2193"" y=""1407"" z=""-128"" width=""4"" height=""2"" depth=""256"" />
    <rectangle x=""2201"" y=""1247"" z=""-128"" width=""24"" height=""27"" depth=""256"" />
    <rectangle x=""2225"" y=""1270"" z=""-128"" width=""7"" height=""6"" depth=""256"" />
    <rectangle x=""2177"" y=""1407"" z=""-128"" width=""17"" height=""19"" depth=""256"" />
   </region>
   <region name=""Zoo di Moonglow"">
    <rectangle x=""4487"" y=""1353"" z=""-128"" width=""14"" height=""23"" depth=""256"" />
    <rectangle x=""4501"" y=""1353"" z=""-128"" width=""25"" height=""10"" depth=""256"" />
    <rectangle x=""4506"" y=""1369"" z=""-128"" width=""14"" height=""16"" depth=""256"" />
    <rectangle x=""4480"" y=""1376"" z=""-128"" width=""12"" height=""23"" depth=""256"" />
    <rectangle x=""4491"" y=""1389"" z=""-128"" width=""27"" height=""10"" depth=""256"" />
    <rectangle x=""4525"" y=""1383"" z=""-128"" width=""11"" height=""9"" depth=""256"" />
    <rectangle x=""4527"" y=""1354"" z=""-128"" width=""9"" height=""20"" depth=""256"" />
   </region>
   <region name=""Stanza Wrong 1"">
    <rectangle x=""5849"" y=""537"" z=""-128"" width=""22"" height=""15"" depth=""256"" />
   </region>
   <region name=""Wrong 1"">
    <rectangle x=""5777"" y=""519"" z=""-128"" width=""108"" height=""114"" depth=""256"" />
   </region>
   <region name=""StanzaIngresso"">
    <rectangle x=""5904"" y=""479"" z=""-128"" width=""18"" height=""19"" depth=""256"" />
    <rectangle x=""5983"" y=""479"" z=""-128"" width=""17"" height=""17"" depth=""256"" />
    <rectangle x=""5903"" y=""399"" z=""-128"" width=""17"" height=""16"" depth=""256"" />
    <rectangle x=""5968"" y=""439"" z=""-128"" width=""8"" height=""10"" depth=""256"" />
    <rectangle x=""5983"" y=""399"" z=""-128"" width=""19"" height=""19"" depth=""256"" />
   </region>
   <region name=""Rovine misteriose..."">
    <rectangle x=""6196"" y=""1"" z=""-128"" width=""106"" height=""142"" depth=""256"" />
    <rectangle x=""6236"" y=""-4"" z=""-128"" width=""23"" height=""8"" depth=""256"" />
    <rectangle x=""6218"" y=""-2"" z=""-128"" width=""27"" height=""8"" depth=""256"" />
   </region>
   <region name=""Destard"">
    <rectangle x=""5121"" y=""890"" z=""-128"" width=""63"" height=""61"" depth=""256"" />
    <rectangle x=""5185"" y=""769"" z=""-128"" width=""186"" height=""216"" depth=""256"" />
    <rectangle x=""5154"" y=""951"" z=""-128"" width=""29"" height=""22"" depth=""256"" />
    <rectangle x=""5203"" y=""985"" z=""-128"" width=""101"" height=""28"" depth=""256"" />
    <rectangle x=""5198"" y=""984"" z=""-128"" width=""7"" height=""10"" depth=""256"" />
    <rectangle x=""5305"" y=""984"" z=""-128"" width=""31"" height=""9"" depth=""256"" />
    <rectangle x=""5176"" y=""948"" z=""-128"" width=""12"" height=""13"" depth=""256"" />
    <rectangle x=""5183"" y=""914"" z=""-128"" width=""2"" height=""34"" depth=""256"" />
    <rectangle x=""5123"" y=""797"" z=""-128"" width=""60"" height=""80"" depth=""256"" />
    <rectangle x=""5138"" y=""876"" z=""-128"" width=""31"" height=""8"" depth=""256"" />
    <rectangle x=""5127"" y=""954"" z=""-128"" width=""71"" height=""67"" depth=""256"" />
   </region>
   <region name=""Limbo Unicorni"">
    <rectangle x=""5244"" y=""12"" z=""-128"" width=""39"" height=""69"" depth=""256"" />
    <rectangle x=""5208"" y=""16"" z=""-128"" width=""75"" height=""18"" depth=""256"" />
    <rectangle x=""5209"" y=""33"" z=""-128"" width=""77"" height=""13"" depth=""256"" />
    <rectangle x=""5213"" y=""45"" z=""-128"" width=""67"" height=""12"" depth=""256"" />
    <rectangle x=""5218"" y=""56"" z=""-128"" width=""61"" height=""44"" depth=""256"" />
    <rectangle x=""5202"" y=""15"" z=""-128"" width=""7"" height=""16"" depth=""256"" />
   </region>
   <region name=""REGION TEST"">
    <rectangle x=""5434"" y=""1081"" z=""-128"" width=""85"" height=""37"" depth=""256"" />
   </region>
   <region name=""Shame Livello 4"">
    <rectangle x=""5494"" y=""160"" z=""-128"" width=""33"" height=""34"" depth=""256"" />
   </region>
   <region name=""Caverna Degli Orchi"">
    <rectangle x=""5264"" y=""1276"" z=""-128"" width=""111"" height=""127"" depth=""256"" />
    <rectangle x=""5126"" y=""1942"" z=""-128"" width=""42"" height=""83"" depth=""256"" />
    <rectangle x=""5267"" y=""1950"" z=""-128"" width=""101"" height=""95"" depth=""256"" />
   </region>
   <region name=""Stanza Wrong 1 (2)"">
    <rectangle x=""5786"" y=""537"" z=""-128"" width=""12"" height=""15"" depth=""256"" />
    <rectangle x=""5798"" y=""537"" z=""-128"" width=""2"" height=""7"" depth=""256"" />
   </region>
   <region name=""Terathan Keep 1 Chiuso"">
    <rectangle x=""5241"" y=""1639"" z=""-128"" width=""136"" height=""141"" depth=""256"" />
    <rectangle x=""5163"" y=""1649"" z=""-128"" width=""79"" height=""74"" depth=""256"" />
    <rectangle x=""5135"" y=""1680"" z=""-128"" width=""31"" height=""45"" depth=""256"" />
    <rectangle x=""5319"" y=""1634"" z=""-128"" width=""33"" height=""6"" depth=""256"" />
   </region>
   <region name=""Trappola Wind"">
    <rectangle x=""5192"" y=""65"" z=""-128"" width=""16"" height=""13"" depth=""256"" />
   </region>
   <region name=""Star Room"">
    <rectangle x=""5134"" y=""1754"" z=""-128"" width=""25"" height=""26"" depth=""256"" />
    <rectangle x=""5133"" y=""1773"" z=""-128"" width=""34"" height=""15"" depth=""256"" />
   </region>
   <region name=""Cripta Maledetta"">
    <rectangle x=""1368"" y=""1458"" z=""-128"" width=""10"" height=""11"" depth=""256"" />
    <rectangle x=""809"" y=""2329"" z=""-128"" width=""14"" height=""9"" depth=""256"" />
    <rectangle x=""2759"" y=""847"" z=""-128"" width=""9"" height=""9"" depth=""256"" />
    <rectangle x=""719"" y=""1114"" z=""-128"" width=""9"" height=""12"" depth=""256"" />
    <rectangle x=""2438"" y=""1084"" z=""-128"" width=""10"" height=""9"" depth=""256"" />
   </region>
   <region name=""Piazzola Ocllo"">
    <rectangle x=""3646"" y=""2543"" z=""-128"" width=""5"" height=""13"" depth=""256"" />
    <rectangle x=""3646"" y=""2516"" z=""-128"" width=""4"" height=""13"" depth=""256"" />
   </region>
   <region name=""Covo dei Ladri"">
    <rectangle x=""5739"" y=""319"" z=""-128"" width=""26"" height=""47"" depth=""256"" />
   </region>
   <region name=""Covetous Livello 2"">
    <rectangle x=""5380"" y=""1946"" z=""-128"" width=""239"" height=""99"" depth=""256"" />
    <rectangle x=""2446"" y=""856"" z=""-128"" width=""34"" height=""21"" depth=""256"" />
    <rectangle x=""2478"" y=""855"" z=""-128"" width=""35"" height=""41"" depth=""256"" />
    <rectangle x=""2513"" y=""842"" z=""-128"" width=""48"" height=""35"" depth=""256"" />
    <rectangle x=""2513"" y=""876"" z=""-128"" width=""41"" height=""31"" depth=""256"" />
   </region>
   <region name=""Covetous Livello 4"">
    <rectangle x=""5496"" y=""1794"" z=""-128"" width=""61"" height=""25"" depth=""256"" />
   </region>
   <region name=""Palude di Papua"">
    <rectangle x=""5896"" y=""3338"" z=""-128"" width=""241"" height=""238"" depth=""256"" />
    <rectangle x=""6068"" y=""3331"" z=""-128"" width=""32"" height=""7"" depth=""256"" />
    <rectangle x=""6066"" y=""3336"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
   </region>
   <region name=""Wrong"">
    <rectangle x=""5698"" y=""521"" z=""-128"" width=""13"" height=""14"" depth=""256"" />
   </region>
   <region name=""Wrong 3"">
    <rectangle x=""5684"" y=""621"" z=""-128"" width=""37"" height=""49"" depth=""256"" />
   </region>
   <region name=""Deceit 2."">
    <rectangle x=""5271"" y=""528"" z=""-128"" width=""89"" height=""104"" depth=""256"" />
   </region>
   <region name=""Nave dei Cacciatori"">
    <rectangle x=""3967"" y=""174"" z=""-128"" width=""13"" height=""8"" depth=""256"" />
    <rectangle x=""3965"" y=""136"" z=""-128"" width=""19"" height=""38"" depth=""256"" />
   </region>
   <region name=""Hyth Prova"">
    <rectangle x=""5893"" y=""1"" z=""-128"" width=""248"" height=""509"" depth=""256"" />
   </region>
   <region name=""Ice Dungeon."">
    <rectangle x=""5661"" y=""136"" z=""-128"" width=""75"" height=""68"" depth=""256"" />
    <rectangle x=""5734"" y=""167"" z=""-128"" width=""21"" height=""35"" depth=""256"" />
    <rectangle x=""5733"" y=""151"" z=""-128"" width=""22"" height=""25"" depth=""256"" />
    <rectangle x=""5667"" y=""201"" z=""-128"" width=""58"" height=""7"" depth=""256"" />
   </region>
   <region name=""Casa Della Strega"">
    <rectangle x=""5509"" y=""1475"" z=""-128"" width=""20"" height=""20"" depth=""256"" />
    <rectangle x=""5305"" y=""1898"" z=""-128"" width=""21"" height=""21"" depth=""256"" />
   </region>
   <region name=""Stanza di Ghiaccio"">
    <rectangle x=""6532"" y=""174"" z=""-128"" width=""8"" height=""9"" depth=""256"" />
   </region>
   <region name=""Wrong - Livello 2 [Room]"">
    <rectangle x=""5715"" y=""554"" z=""-128"" width=""20"" height=""13"" depth=""256"" />
   </region>
   <region name=""Passaggio Terre Perdute - Vesper"">
    <rectangle x=""2768"" y=""875"" z=""-25"" width=""16"" height=""23"" depth=""5"" />
    <rectangle x=""5652"" y=""379"" z=""-128"" width=""46"" height=""55"" depth=""256"" />
   </region>
   <region name=""Shame Livello 2"">
    <rectangle x=""5505"" y=""1"" z=""-128"" width=""126"" height=""122"" depth=""256"" />
   </region>
  </Dungeons>
  <Guarded>
   <region name=""Cove"">
    <rectangle x=""2202"" y=""1160"" z=""-128"" width=""45"" height=""34"" depth=""256"" />
    <rectangle x=""2196"" y=""1194"" z=""-128"" width=""88"" height=""52"" depth=""256"" />
    <rectangle x=""2284"" y=""1219"" z=""-128"" width=""8"" height=""16"" depth=""256"" />
    <rectangle x=""2247"" y=""1181"" z=""-128"" width=""45"" height=""13"" depth=""256"" />
    <rectangle x=""2224"" y=""1246"" z=""-128"" width=""47"" height=""17"" depth=""256"" />
    <rectangle x=""2202"" y=""1104"" z=""-128"" width=""56"" height=""56"" depth=""256"" />
   </region>
   <region name=""Britain"">
    <rectangle x=""1418"" y=""1755"" z=""-5"" width=""225"" height=""22"" depth=""133"" />
    <rectangle x=""1523"" y=""1777"" z=""-5"" width=""80"" height=""18"" depth=""133"" />
    <rectangle x=""1414"" y=""1675"" z=""-5"" width=""4"" height=""31"" depth=""133"" />
    <rectangle x=""1356"" y=""1706"" z=""-5"" width=""62"" height=""7"" depth=""133"" />
    <rectangle x=""1369"" y=""1713"" z=""-5"" width=""49"" height=""10"" depth=""133"" />
    <rectangle x=""1379"" y=""1723"" z=""-5"" width=""39"" height=""10"" depth=""133"" />
    <rectangle x=""1388"" y=""1733"" z=""-5"" width=""30"" height=""31"" depth=""133"" />
    <rectangle x=""1448"" y=""1507"" z=""-5"" width=""94"" height=""25"" depth=""133"" />
    <rectangle x=""1542"" y=""1507"" z=""-5"" width=""26"" height=""25"" depth=""133"" />
    <rectangle x=""1568"" y=""1512"" z=""-5"" width=""70"" height=""20"" depth=""133"" />
    <rectangle x=""1638"" y=""1553"" z=""-5"" width=""47"" height=""119"" depth=""133"" />
    <rectangle x=""1638"" y=""1672"" z=""-5"" width=""50"" height=""25"" depth=""133"" />
    <rectangle x=""1638"" y=""1697"" z=""-5"" width=""40"" height=""20"" depth=""133"" />
    <rectangle x=""1638"" y=""1717"" z=""-5"" width=""24"" height=""28"" depth=""133"" />
    <rectangle x=""1603"" y=""1777"" z=""-5"" width=""30"" height=""5"" depth=""133"" />
    <rectangle x=""1500"" y=""1406"" z=""-5"" width=""49"" height=""69"" depth=""133"" />
    <rectangle x=""1511"" y=""1475"" z=""-5"" width=""23"" height=""32"" depth=""133"" />
    <rectangle x=""1538"" y=""1481"" z=""-5"" width=""26"" height=""26"" depth=""133"" />
    <rectangle x=""1415"" y=""1531"" z=""-5"" width=""3"" height=""51"" depth=""133"" />
    <rectangle x=""1418"" y=""1532"" z=""-5"" width=""220"" height=""223"" depth=""133"" />
    <rectangle x=""1400"" y=""1582"" z=""-5"" width=""18"" height=""93"" depth=""133"" />
   </region>
   <region name=""Cittadella di Britain"">
    <rectangle x=""1296"" y=""1564"" z=""-128"" width=""104"" height=""140"" depth=""256"" />
    <rectangle x=""1400"" y=""1555"" z=""-128"" width=""14"" height=""25"" depth=""256"" />
    <rectangle x=""1400"" y=""1674"" z=""-128"" width=""13"" height=""30"" depth=""256"" />
    <rectangle x=""1295"" y=""1555"" z=""-128"" width=""26"" height=""9"" depth=""256"" />
    <rectangle x=""1387"" y=""1555"" z=""-128"" width=""13"" height=""9"" depth=""256"" />
   </region>
   <region name=""Minoc"">
    <rectangle x=""2411"" y=""366"" z=""-128"" width=""135"" height=""241"" depth=""256"" />
    <rectangle x=""2464"" y=""607"" z=""-128"" width=""82"" height=""20"" depth=""256"" />
    <rectangle x=""2476"" y=""348"" z=""-128"" width=""64"" height=""18"" depth=""256"" />
    <rectangle x=""2399"" y=""436"" z=""-128"" width=""12"" height=""135"" depth=""256"" />
    <rectangle x=""2389"" y=""548"" z=""-128"" width=""10"" height=""21"" depth=""256"" />
    <rectangle x=""2546"" y=""574"" z=""-128"" width=""44"" height=""53"" depth=""256"" />
    <rectangle x=""2546"" y=""552"" z=""-128"" width=""13"" height=""22"" depth=""256"" />
   </region>
   <region name=""Trinsic"">
    <rectangle x=""2004"" y=""2813"" z=""-128"" width=""9"" height=""93"" depth=""256"" />
    <rectangle x=""1998"" y=""2900"" z=""-128"" width=""7"" height=""6"" depth=""256"" />
    <rectangle x=""1998"" y=""2906"" z=""40"" width=""9"" height=""1"" depth=""88"" />
    <rectangle x=""1991"" y=""2900"" z=""-128"" width=""7"" height=""7"" depth=""256"" />
    <rectangle x=""1975"" y=""2900"" z=""-128"" width=""16"" height=""24"" depth=""256"" />
    <rectangle x=""1975"" y=""2708"" z=""-128"" width=""29"" height=""192"" depth=""256"" />
    <rectangle x=""2004"" y=""2708"" z=""-128"" width=""3"" height=""105"" depth=""256"" />
    <rectangle x=""1988"" y=""2688"" z=""-128"" width=""19"" height=""20"" depth=""256"" />
    <rectangle x=""2007"" y=""2688"" z=""-128"" width=""47"" height=""59"" depth=""256"" />
    <rectangle x=""2007"" y=""2747"" z=""-128"" width=""16"" height=""27"" depth=""256"" />
    <rectangle x=""2023"" y=""2747"" z=""-128"" width=""7"" height=""24"" depth=""256"" />
    <rectangle x=""2030"" y=""2747"" z=""-128"" width=""3"" height=""22"" depth=""256"" />
    <rectangle x=""2033"" y=""2747"" z=""-128"" width=""2"" height=""19"" depth=""256"" />
    <rectangle x=""2035"" y=""2747"" z=""-128"" width=""2"" height=""15"" depth=""256"" />
    <rectangle x=""2037"" y=""2747"" z=""-128"" width=""2"" height=""11"" depth=""256"" />
    <rectangle x=""2039"" y=""2747"" z=""-128"" width=""4"" height=""10"" depth=""256"" />
    <rectangle x=""2043"" y=""2747"" z=""-128"" width=""2"" height=""7"" depth=""256"" />
    <rectangle x=""2045"" y=""2747"" z=""-128"" width=""3"" height=""5"" depth=""256"" />
    <rectangle x=""2007"" y=""2774"" z=""-128"" width=""6"" height=""10"" depth=""256"" />
    <rectangle x=""1935"" y=""2759"" z=""-128"" width=""40"" height=""112"" depth=""256"" />
    <rectangle x=""1970"" y=""2717"" z=""-128"" width=""5"" height=""39"" depth=""256"" />
    <rectangle x=""1880"" y=""2759"" z=""-128"" width=""55"" height=""136"" depth=""256"" />
    <rectangle x=""1864"" y=""2871"" z=""-128"" width=""16"" height=""24"" depth=""256"" />
    <rectangle x=""1840"" y=""2759"" z=""-128"" width=""40"" height=""112"" depth=""256"" />
    <rectangle x=""1799"" y=""2796"" z=""-128"" width=""41"" height=""75"" depth=""256"" />
    <rectangle x=""1815"" y=""2871"" z=""-128"" width=""49"" height=""16"" depth=""256"" />
    <rectangle x=""1834"" y=""2784"" z=""-128"" width=""6"" height=""12"" depth=""256"" />
    <rectangle x=""1835"" y=""2763"" z=""-128"" width=""5"" height=""14"" depth=""256"" />
    <rectangle x=""1795"" y=""2759"" z=""-128"" width=""45"" height=""4"" depth=""256"" />
    <rectangle x=""1795"" y=""2695"" z=""-128"" width=""130"" height=""64"" depth=""256"" />
    <rectangle x=""1815"" y=""2663"" z=""-128"" width=""110"" height=""32"" depth=""256"" />
    <rectangle x=""1855"" y=""2660"" z=""-128"" width=""70"" height=""3"" depth=""256"" />
    <rectangle x=""1855"" y=""2635"" z=""-128"" width=""70"" height=""25"" depth=""256"" />
    <rectangle x=""1925"" y=""2754"" z=""-128"" width=""26"" height=""5"" depth=""256"" />
    <rectangle x=""1951"" y=""2754"" z=""-128"" width=""9"" height=""5"" depth=""256"" />
    <rectangle x=""1960"" y=""2751"" z=""-128"" width=""9"" height=""8"" depth=""256"" />
    <rectangle x=""2013"" y=""2813"" z=""-128"" width=""5"" height=""94"" depth=""256"" />
    <rectangle x=""2018"" y=""2848"" z=""50"" width=""1"" height=""16"" depth=""78"" />
    <rectangle x=""1950"" y=""2751"" z=""50"" width=""11"" height=""4"" depth=""78"" />
    <rectangle x=""1836"" y=""2777"" z=""39"" width=""4"" height=""7"" depth=""89"" />
   </region>
   <region name=""Dregoth"">
    <rectangle x=""2108"" y=""54"" z=""-128"" width=""40"" height=""55"" depth=""256"" />
    <rectangle x=""2130"" y=""109"" z=""-128"" width=""12"" height=""38"" depth=""256"" />
    <rectangle x=""2050"" y=""129"" z=""-128"" width=""80"" height=""31"" depth=""256"" />
    <rectangle x=""2054"" y=""160"" z=""-128"" width=""76"" height=""31"" depth=""256"" />
    <rectangle x=""2067"" y=""191"" z=""-128"" width=""112"" height=""26"" depth=""256"" />
    <rectangle x=""2054"" y=""191"" z=""-128"" width=""13"" height=""13"" depth=""256"" />
    <rectangle x=""2048"" y=""109"" z=""-128"" width=""82"" height=""20"" depth=""256"" />
    <rectangle x=""2130"" y=""147"" z=""-128"" width=""67"" height=""44"" depth=""256"" />
   </region>
   <region name=""Biblioteca di Skara Brae"">
    <rectangle x=""625"" y=""2123"" z=""-128"" width=""15"" height=""24"" depth=""256"" />
   </region>
   <region name=""NordBritain"">
    <rectangle x=""1470"" y=""1499"" z=""-128"" width=""41"" height=""10"" depth=""256"" />
   </region>
   <region name=""Piazzale del Conservatorio"">
    <rectangle x=""1448"" y=""1561"" z=""-128"" width=""15"" height=""8"" depth=""256"" />
   </region>
   <region name=""Taverna di Britain"">
    <rectangle x=""1424"" y=""1712"" z=""-128"" width=""8"" height=""24"" depth=""256"" />
    <rectangle x=""1432"" y=""1712"" z=""-128"" width=""8"" height=""12"" depth=""256"" />
    <rectangle x=""1432"" y=""1728"" z=""-128"" width=""2"" height=""6"" depth=""256"" />
   </region>
   <region name=""Centro Allenamenti Britain"">
    <rectangle x=""1608"" y=""1584"" z=""-128"" width=""24"" height=""8"" depth=""256"" />
    <rectangle x=""1616"" y=""1576"" z=""-128"" width=""8"" height=""8"" depth=""256"" />
   </region>
   <region name=""Minoc Healer"">
    <rectangle x=""2441"" y=""428"" z=""11"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Britain Healer"">
    <rectangle x=""1467"" y=""1602"" z=""16"" width=""17"" height=""17"" depth=""10"" />
   </region>
   <region name=""Tempio di Dregoth"">
    <rectangle x=""2062"" y=""171"" z=""-128"" width=""13"" height=""26"" depth=""256"" />
    <rectangle x=""2075"" y=""177"" z=""-128"" width=""2"" height=""20"" depth=""256"" />
    <rectangle x=""2077"" y=""177"" z=""-128"" width=""2"" height=""19"" depth=""256"" />
    <rectangle x=""2079"" y=""177"" z=""-128"" width=""3"" height=""17"" depth=""256"" />
    <rectangle x=""2079"" y=""177"" z=""-128"" width=""8"" height=""17"" depth=""256"" />
   </region>
   <region name=""Tempio di Britain Elhoim"">
    <rectangle x=""1569"" y=""1552"" z=""-128"" width=""23"" height=""17"" depth=""256"" />
   </region>
   <region name=""Tempio di Britain Idior"">
    <rectangle x=""1538"" y=""1702"" z=""-128"" width=""15"" height=""19"" depth=""256"" />
    <rectangle x=""1542"" y=""1721"" z=""-128"" width=""6"" height=""3"" depth=""256"" />
   </region>
   <region name=""Tempio di Cove"">
    <rectangle x=""2200"" y=""1226"" z=""-128"" width=""18"" height=""15"" depth=""256"" />
    <rectangle x=""2205"" y=""1222"" z=""-128"" width=""7"" height=""4"" depth=""256"" />
   </region>
   <region name=""Tempio di Cittadella di Britain Idior"">
    <rectangle x=""1332"" y=""1669"" z=""28"" width=""29"" height=""15"" depth=""11"" />
    <rectangle x=""1320"" y=""1672"" z=""-128"" width=""12"" height=""12"" depth=""256"" />
   </region>
   <region name=""Tempio di Trinsic"">
    <rectangle x=""1955"" y=""2838"" z=""-128"" width=""23"" height=""11"" depth=""256"" />
   </region>
   <region name=""Tempio di Cittadella di Britain Elhoim"">
    <rectangle x=""1312"" y=""1572"" z=""-128"" width=""10"" height=""20"" depth=""256"" />
    <rectangle x=""1322"" y=""1572"" z=""-128"" width=""9"" height=""7"" depth=""256"" />
   </region>
   <region name=""Tempio di Minoc"">
    <rectangle x=""2488"" y=""372"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
   </region>
  </Guarded>
  <Forest>
   <region name=""Fuori Ocllo"">
    <rectangle x=""3607"" y=""2419"" z=""-128"" width=""101"" height=""36"" depth=""256"" />
    <rectangle x=""3696"" y=""2455"" z=""-128"" width=""10"" height=""107"" depth=""256"" />
    <rectangle x=""3689"" y=""2562"" z=""-128"" width=""10"" height=""134"" depth=""256"" />
    <rectangle x=""3675"" y=""2652"" z=""-128"" width=""14"" height=""44"" depth=""256"" />
   </region>
   <region name=""Castello dei templari"">
    <rectangle x=""1313"" y=""1717"" z=""-128"" width=""53"" height=""34"" depth=""256"" />
    <rectangle x=""1313"" y=""1705"" z=""-128"" width=""29"" height=""12"" depth=""256"" />
   </region>
   <region name=""Fuori Jhelom"">
    <rectangle x=""1260"" y=""3744"" z=""-128"" width=""19"" height=""14"" depth=""256"" />
    <rectangle x=""1233"" y=""3698"" z=""-128"" width=""38"" height=""46"" depth=""256"" />
    <rectangle x=""1315"" y=""3639"" z=""-128"" width=""12"" height=""83"" depth=""256"" />
    <rectangle x=""1271"" y=""3639"" z=""-128"" width=""22"" height=""72"" depth=""256"" />
    <rectangle x=""1293"" y=""3696"" z=""-128"" width=""34"" height=""26"" depth=""256"" />
    <rectangle x=""1301"" y=""3722"" z=""-128"" width=""26"" height=""30"" depth=""256"" />
    <rectangle x=""1296"" y=""3736"" z=""-128"" width=""5"" height=""16"" depth=""256"" />
    <rectangle x=""1480"" y=""3664"" z=""-128"" width=""19"" height=""58"" depth=""256"" />
    <rectangle x=""1482"" y=""3756"" z=""-128"" width=""31"" height=""91"" depth=""256"" />
    <rectangle x=""1331"" y=""3876"" z=""-128"" width=""34"" height=""20"" depth=""256"" />
   </region>
   <region name=""Fuori Minoc"">
    <rectangle x=""2467"" y=""627"" z=""-128"" width=""32"" height=""25"" depth=""256"" />
    <rectangle x=""2499"" y=""627"" z=""-128"" width=""141"" height=""25"" depth=""256"" />
    <rectangle x=""2590"" y=""598"" z=""-128"" width=""20"" height=""29"" depth=""256"" />
    <rectangle x=""2590"" y=""587"" z=""-128"" width=""6"" height=""11"" depth=""256"" />
    <rectangle x=""2546"" y=""429"" z=""-128"" width=""30"" height=""124"" depth=""256"" />
    <rectangle x=""2639"" y=""457"" z=""-128"" width=""20"" height=""23"" depth=""256"" />
    <rectangle x=""2546"" y=""382"" z=""-128"" width=""47"" height=""47"" depth=""256"" />
    <rectangle x=""2593"" y=""406"" z=""-128"" width=""25"" height=""23"" depth=""256"" />
    <rectangle x=""2529"" y=""336"" z=""-128"" width=""35"" height=""12"" depth=""256"" />
    <rectangle x=""2540"" y=""348"" z=""-128"" width=""30"" height=""18"" depth=""256"" />
    <rectangle x=""2546"" y=""366"" z=""-128"" width=""75"" height=""41"" depth=""256"" />
   </region>
   <region name=""Fuori Trinsic"">
    <rectangle x=""1747"" y=""2585"" z=""-128"" width=""163"" height=""45"" depth=""256"" />
    <rectangle x=""1735"" y=""2630"" z=""-128"" width=""114"" height=""28"" depth=""256"" />
    <rectangle x=""1732"" y=""2658"" z=""-128"" width=""78"" height=""32"" depth=""256"" />
    <rectangle x=""1726"" y=""2690"" z=""-128"" width=""64"" height=""77"" depth=""256"" />
    <rectangle x=""1752"" y=""2768"" z=""-128"" width=""77"" height=""23"" depth=""256"" />
    <rectangle x=""1726"" y=""2792"" z=""-128"" width=""67"" height=""143"" depth=""256"" />
    <rectangle x=""1794"" y=""2876"" z=""-128"" width=""17"" height=""98"" depth=""256"" />
    <rectangle x=""1812"" y=""2901"" z=""-128"" width=""158"" height=""72"" depth=""256"" />
    <rectangle x=""1821"" y=""2893"" z=""-128"" width=""36"" height=""7"" depth=""256"" />
    <rectangle x=""1942"" y=""2877"" z=""-128"" width=""27"" height=""23"" depth=""256"" />
    <rectangle x=""2061"" y=""2637"" z=""-128"" width=""88"" height=""80"" depth=""256"" />
    <rectangle x=""2099"" y=""2717"" z=""-128"" width=""81"" height=""123"" depth=""256"" />
   </region>
   <region name=""Fuori Vesper"">
    <rectangle x=""2716"" y=""940"" z=""-128"" width=""85"" height=""58"" depth=""256"" />
    <rectangle x=""2714"" y=""1008"" z=""-128"" width=""105"" height=""29"" depth=""256"" />
    <rectangle x=""2784"" y=""766"" z=""-128"" width=""35"" height=""129"" depth=""256"" />
    <rectangle x=""2823"" y=""617"" z=""-128"" width=""20"" height=""77"" depth=""256"" />
    <rectangle x=""2800"" y=""694"" z=""-128"" width=""29"" height=""72"" depth=""256"" />
    <rectangle x=""2779"" y=""703"" z=""-128"" width=""21"" height=""63"" depth=""256"" />
    <rectangle x=""2794"" y=""612"" z=""-128"" width=""29"" height=""82"" depth=""256"" />
    <rectangle x=""2947"" y=""604"" z=""-128"" width=""69"" height=""40"" depth=""256"" />
    <rectangle x=""2951"" y=""644"" z=""-128"" width=""76"" height=""38"" depth=""256"" />
    <rectangle x=""2863"" y=""558"" z=""-128"" width=""91"" height=""46"" depth=""256"" />
    <rectangle x=""2895"" y=""605"" z=""-128"" width=""53"" height=""11"" depth=""256"" />
    <rectangle x=""2927"" y=""626"" z=""-128"" width=""20"" height=""18"" depth=""256"" />
    <rectangle x=""2954"" y=""578"" z=""-128"" width=""44"" height=""26"" depth=""256"" />
    <rectangle x=""2846"" y=""564"" z=""-128"" width=""17"" height=""31"" depth=""256"" />
    <rectangle x=""2909"" y=""616"" z=""-128"" width=""38"" height=""10"" depth=""256"" />
    <rectangle x=""2807"" y=""961"" z=""-128"" width=""11"" height=""47"" depth=""256"" />
   </region>
   <region name="" Fuori Serpent's Hold"">
    <rectangle x=""2866"" y=""3452"" z=""0"" width=""78"" height=""84"" depth=""128"" />
    <rectangle x=""2944"" y=""3476"" z=""0"" width=""5"" height=""45"" depth=""128"" />
    <rectangle x=""3001"" y=""3512"" z=""0"" width=""30"" height=""39"" depth=""128"" />
    <rectangle x=""2857"" y=""3330"" z=""0"" width=""76"" height=""111"" depth=""128"" />
    <rectangle x=""2933"" y=""3386"" z=""0"" width=""24"" height=""51"" depth=""128"" />
    <rectangle x=""2957"" y=""3417"" z=""0"" width=""8"" height=""20"" depth=""128"" />
    <rectangle x=""2936"" y=""3437"" z=""0"" width=""13"" height=""15"" depth=""128"" />
    <rectangle x=""2954"" y=""3378"" z=""0"" width=""12"" height=""8"" depth=""128"" />
    <rectangle x=""2957"" y=""3384"" z=""0"" width=""30"" height=""12"" depth=""128"" />
   </region>
   <region name=""Fuori Skara Brae"">
    <rectangle x=""711"" y=""2122"" z=""-128"" width=""56"" height=""127"" depth=""256"" />
    <rectangle x=""711"" y=""2249"" z=""-128"" width=""56"" height=""69"" depth=""256"" />
    <rectangle x=""711"" y=""2318"" z=""-128"" width=""165"" height=""55"" depth=""256"" />
   </region>
   <region name=""Fuori Moonglow"">
    <rectangle x=""4499"" y=""1024"" z=""-128"" width=""30"" height=""177"" depth=""256"" />
    <rectangle x=""4320"" y=""1183"" z=""-128"" width=""179"" height=""40"" depth=""256"" />
    <rectangle x=""4320"" y=""1035"" z=""-128"" width=""56"" height=""147"" depth=""256"" />
    <rectangle x=""4438"" y=""961"" z=""-128"" width=""61"" height=""87"" depth=""256"" />
    <rectangle x=""4320"" y=""1017"" z=""-128"" width=""48"" height=""18"" depth=""256"" />
   </region>
   <region name=""Fuori Magincia"">
    <rectangle x=""3627"" y=""2018"" z=""-128"" width=""35"" height=""57"" depth=""256"" />
    <rectangle x=""3627"" y=""2075"" z=""-128"" width=""20"" height=""157"" depth=""256"" />
   </region>
   <region name=""Fuori Dregoth"">
    <rectangle x=""2085"" y=""217"" z=""-128"" width=""96"" height=""11"" depth=""256"" />
    <rectangle x=""2037"" y=""98"" z=""-128"" width=""71"" height=""11"" depth=""256"" />
    <rectangle x=""2037"" y=""109"" z=""-128"" width=""11"" height=""19"" depth=""256"" />
   </region>
   <region name=""Terathan Keep"">
    <rectangle x=""5400"" y=""3094"" z=""-128"" width=""87"" height=""81"" depth=""256"" />
   </region>
   <region name=""Magic Zone"">
    <rectangle x=""5528"" y=""3727"" z=""-128"" width=""53"" height=""51"" depth=""256"" />
    <rectangle x=""5534"" y=""3726"" z=""-128"" width=""42"" height=""1"" depth=""256"" />
   </region>
   <region name=""PVP Arena"">
    <rectangle x=""1102"" y=""2570"" z=""-128"" width=""67"" height=""48"" depth=""256"" />
    <rectangle x=""1169"" y=""2590"" z=""-128"" width=""1"" height=""11"" depth=""256"" />
   </region>
   <region name=""Zona A"">
    <rectangle x=""5650"" y=""1030"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
   </region>
   <region name=""Wrong0"">
    <rectangle x=""2039"" y=""238"" z=""-128"" width=""0"" height=""0"" depth=""256"" />
   </region>
   <region name=""Fuori  Vesper"">
    <rectangle x=""2803"" y=""616"" z=""-128"" width=""37"" height=""82"" depth=""256"" />
    <rectangle x=""2749"" y=""698"" z=""-128"" width=""64"" height=""96"" depth=""256"" />
    <rectangle x=""2707"" y=""999"" z=""-128"" width=""108"" height=""35"" depth=""256"" />
   </region>
   <region name=""Recinto Nord Jhelom"">
    <rectangle x=""1112"" y=""3568"" z=""-128"" width=""32"" height=""32"" depth=""256"" />
   </region>
   <region name=""Ingresso Wind"">
    <rectangle x=""1465"" y=""963"" z=""-128"" width=""30"" height=""44"" depth=""256"" />
   </region>
   <region name=""Archer"">
    <rectangle x=""3466"" y=""2455"" z=""-128"" width=""85"" height=""37"" depth=""256"" />
   </region>
   <region name=""Destard Entrata"">
    <rectangle x=""1159"" y=""2635"" z=""-128"" width=""39"" height=""27"" depth=""256"" />
    <rectangle x=""1156"" y=""2639"" z=""-128"" width=""3"" height=""23"" depth=""256"" />
   </region>
   <region name=""L'isola dei Panda"">
    <rectangle x=""428"" y=""2037"" z=""-128"" width=""63"" height=""68"" depth=""256"" />
    <rectangle x=""441"" y=""2103"" z=""-128"" width=""46"" height=""16"" depth=""256"" />
    <rectangle x=""445"" y=""2115"" z=""-128"" width=""12"" height=""12"" depth=""256"" />
    <rectangle x=""468"" y=""2022"" z=""-128"" width=""36"" height=""27"" depth=""256"" />
    <rectangle x=""480"" y=""2006"" z=""-128"" width=""28"" height=""22"" depth=""256"" />
   </region>
   <region name=""L'isola misteriosa"">
    <rectangle x=""2083"" y=""3881"" z=""-128"" width=""132"" height=""143"" depth=""256"" />
   </region>
   <region name=""Rebel Island"">
    <rectangle x=""1375"" y=""3953"" z=""-128"" width=""155"" height=""90"" depth=""256"" />
   </region>
   <region name=""AntroDestard"">
    <rectangle x=""5137"" y=""951"" z=""-128"" width=""2"" height=""1"" depth=""256"" />
   </region>
   <region name=""ColonyWar - Ingresso"">
    <rectangle x=""1341"" y=""1826"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
   </region>
   <region name=""Hedge Maze"">
    <rectangle x=""1032"" y=""2159"" z=""-128"" width=""224"" height=""144"" depth=""256"" />
   </region>
   <region name=""Fuori Skara Brae."">
    <rectangle x=""740"" y=""2317"" z=""-128"" width=""136"" height=""76"" depth=""256"" />
   </region>
   <region name=""Limbo Unicorni Uscita"">
    <rectangle x=""5215"" y=""16"" z=""-128"" width=""4"" height=""3"" depth=""256"" />
   </region>
  </Forest>
 </Felucca>
 <Trammel>
  <Dungeons>
   <region name=""Anticamera di Despise"">
    <rectangle x=""5562"" y=""618"" z=""-128"" width=""34"" height=""28"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise 2"">
    <rectangle x=""5182"" y=""1158"" z=""-128"" width=""27"" height=""23"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise 3"">
    <rectangle x=""1618"" y=""963"" z=""-128"" width=""36"" height=""40"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise 4"">
    <rectangle x=""5606"" y=""141"" z=""-128"" width=""15"" height=""26"" depth=""256"" />
    <rectangle x=""5618"" y=""140"" z=""-128"" width=""9"" height=""16"" depth=""256"" />
   </region>
   <region name=""Despise Livello 1"">
    <rectangle x=""5369"" y=""514"" z=""-128"" width=""146"" height=""123"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise 6"">
    <rectangle x=""4582"" y=""3559"" z=""-128"" width=""21"" height=""23"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise       "">
    <rectangle x=""5121"" y=""2433"" z=""-128"" width=""33"" height=""60"" depth=""256"" />
   </region>
   <region name=""Despise Livello 2"">
    <rectangle x=""5380"" y=""633"" z=""-128"" width=""153"" height=""138"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise."">
    <rectangle x=""5672"" y=""2908"" z=""-128"" width=""49"" height=""51"" depth=""256"" />
    <rectangle x=""5666"" y=""2935"" z=""-128"" width=""10"" height=""15"" depth=""256"" />
   </region>
   <region name=""Caverne Profonde di Despise"">
    <rectangle x=""2295"" y=""790"" z=""-128"" width=""364"" height=""176"" depth=""256"" />
   </region>
   <region name=""Despise Livello 3"">
    <rectangle x=""5371"" y=""761"" z=""-128"" width=""260"" height=""255"" depth=""256"" />
   </region>
   <region name=""Caverne Segrete di Despise"">
    <rectangle x=""5292"" y=""1273"" z=""-128"" width=""80"" height=""113"" depth=""256"" />
   </region>
  </Dungeons>
 </Trammel>
 <Ilshenar>
  <Towns>
   <region name=""Tempio di Elhoim"">
    <rectangle x=""1482"" y=""516"" z=""-128"" width=""72"" height=""53"" depth=""256"" />
    <rectangle x=""1494"" y=""492"" z=""-128"" width=""66"" height=""24"" depth=""256"" />
    <rectangle x=""1463"" y=""569"" z=""-128"" width=""84"" height=""10"" depth=""256"" />
    <rectangle x=""1475"" y=""579"" z=""-128"" width=""57"" height=""9"" depth=""256"" />
   </region>
   <region name=""Tempio di Idior"">
    <rectangle x=""1323"" y=""1008"" z=""-128"" width=""84"" height=""120"" depth=""256"" />
   </region>
  </Towns>
  <Dungeons>
   <region name=""Angeli Newbie"">
    <rectangle x=""1938"" y=""1005"" z=""-128"" width=""94"" height=""108"" depth=""256"" />
   </region>
   <region name=""Caverna di Sonagh"">
    <rectangle x=""1262"" y=""1464"" z=""-128"" width=""90"" height=""114"" depth=""256"" />
   </region>
   <region name=""Sala del Fuoco"">
    <rectangle x=""761"" y=""1465"" z=""-128"" width=""7"" height=""30"" depth=""256"" />
    <rectangle x=""768"" y=""1465"" z=""-128"" width=""23"" height=""7"" depth=""256"" />
    <rectangle x=""784"" y=""1472"" z=""-128"" width=""7"" height=""23"" depth=""256"" />
    <rectangle x=""768"" y=""1489"" z=""-128"" width=""16"" height=""6"" depth=""256"" />
    <rectangle x=""768"" y=""1472"" z=""-128"" width=""7"" height=""7"" depth=""256"" />
    <rectangle x=""768"" y=""1482"" z=""-128"" width=""2"" height=""7"" depth=""256"" />
    <rectangle x=""770"" y=""1482"" z=""-128"" width=""11"" height=""4"" depth=""256"" />
    <rectangle x=""773"" y=""1486"" z=""-128"" width=""8"" height=""3"" depth=""256"" />
    <rectangle x=""781"" y=""1488"" z=""-128"" width=""3"" height=""1"" depth=""256"" />
    <rectangle x=""781"" y=""1477"" z=""-128"" width=""3"" height=""8"" depth=""256"" />
    <rectangle x=""778"" y=""1472"" z=""-128"" width=""6"" height=""2"" depth=""256"" />
    <rectangle x=""775"" y=""1475"" z=""-128"" width=""6"" height=""2"" depth=""256"" />
    <rectangle x=""778"" y=""1474"" z=""-128"" width=""3"" height=""1"" depth=""256"" />
    <rectangle x=""775"" y=""1477"" z=""-128"" width=""6"" height=""5"" depth=""256"" />
    <rectangle x=""771"" y=""1479"" z=""-128"" width=""4"" height=""3"" depth=""256"" />
    <rectangle x=""750"" y=""1472"" z=""-128"" width=""8"" height=""16"" depth=""256"" />
   </region>
   <region name=""Covo Della Regina"">
    <rectangle x=""1487"" y=""861"" z=""-128"" width=""40"" height=""35"" depth=""256"" />
    <rectangle x=""1481"" y=""861"" z=""-128"" width=""48"" height=""31"" depth=""256"" />
   </region>
   <region name=""Gilda Dei Ladri"">
    <rectangle x=""919"" y=""1545"" z=""-128"" width=""52"" height=""31"" depth=""256"" />
    <rectangle x=""936"" y=""1458"" z=""-128"" width=""47"" height=""93"" depth=""256"" />
    <rectangle x=""983"" y=""1489"" z=""-128"" width=""32"" height=""38"" depth=""256"" />
    <rectangle x=""950"" y=""1423"" z=""-128"" width=""43"" height=""26"" depth=""256"" />
    <rectangle x=""574"" y=""1150"" z=""-128"" width=""5"" height=""7"" depth=""256"" />
    <rectangle x=""567"" y=""1133"" z=""-128"" width=""20"" height=""18"" depth=""256"" />
   </region>
   <region name=""Sala Neo Necromanti"">
    <rectangle x=""873"" y=""1553"" z=""-128"" width=""31"" height=""31"" depth=""256"" />
   </region>
   <region name=""Mushroom Cave"">
    <rectangle x=""1399"" y=""1458"" z=""-128"" width=""102"" height=""105"" depth=""256"" />
   </region>
   <region name=""Kirin Passage"">
    <rectangle x=""0"" y=""822"" z=""-128"" width=""180"" height=""400"" depth=""256"" />
   </region>
   <region name=""Dungeon Necromanti"">
    <rectangle x=""816"" y=""1464"" z=""-128"" width=""87"" height=""119"" depth=""256"" />
    <rectangle x=""903"" y=""1496"" z=""-128"" width=""7"" height=""22"" depth=""256"" />
    <rectangle x=""903"" y=""1464"" z=""-128"" width=""9"" height=""16"" depth=""256"" />
   </region>
   <region name=""Region Antimuro x Resser"">
    <rectangle x=""1193"" y=""1160"" z=""-128"" width=""10"" height=""8"" depth=""256"" />
   </region>
   <region name=""Minos"">
    <rectangle x=""0"" y=""1250"" z=""-128"" width=""178"" height=""342"" depth=""256"" />
   </region>
   <region name=""Christmas Dungeon"">
    <rectangle x=""199"" y=""1"" z=""-128"" width=""174"" height=""109"" depth=""256"" />
    <rectangle x=""221"" y=""109"" z=""-128"" width=""28"" height=""36"" depth=""256"" />
   </region>
   <region name=""Altari"">
    <rectangle x=""770"" y=""1486"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""781"" y=""1485"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""781"" y=""1474"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""775"" y=""1472"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""768"" y=""1479"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
   </region>
   <region name=""Palude Misteriosa"">
    <rectangle x=""250"" y=""1240"" z=""-128"" width=""364"" height=""153"" depth=""256"" />
    <rectangle x=""302"" y=""1390"" z=""-128"" width=""306"" height=""37"" depth=""256"" />
   </region>
   <region name=""Dungeon Chierici"">
    <rectangle x=""624"" y=""1496"" z=""-128"" width=""103"" height=""79"" depth=""256"" />
   </region>
   <region name=""Casa dei Pirati"">
    <rectangle x=""1711"" y=""874"" z=""-25"" width=""19"" height=""20"" depth=""55"" />
   </region>
   <region name=""Gilda Dei Ladri 4"">
    <rectangle x=""953"" y=""1463"" z=""-128"" width=""23"" height=""33"" depth=""256"" />
   </region>
   <region name=""Custom Region 2"">
    <rectangle x=""1752"" y=""40"" z=""-128"" width=""70"" height=""54"" depth=""256"" />
   </region>
   <region name=""IngressoCollegio"">
    <rectangle x=""365"" y=""17"" z=""-128"" width=""25"" height=""30"" depth=""256"" />
   </region>
   <region name=""Tana della Sfinge"">
    <rectangle x=""745"" y=""1537"" z=""-128"" width=""23"" height=""23"" depth=""256"" />
    <rectangle x=""768"" y=""1545"" z=""-128"" width=""8"" height=""7"" depth=""256"" />
    <rectangle x=""773"" y=""1536"" z=""-128"" width=""19"" height=""24"" depth=""256"" />
    <rectangle x=""792"" y=""1545"" z=""-128"" width=""22"" height=""7"" depth=""256"" />
    <rectangle x=""775"" y=""1554"" z=""-128"" width=""9"" height=""31"" depth=""256"" />
   </region>
   <region name=""Kornak's Outpost"">
    <rectangle x=""1718"" y=""1207"" z=""-128"" width=""62"" height=""30"" depth=""256"" />
    <rectangle x=""1722"" y=""1222"" z=""-128"" width=""58"" height=""21"" depth=""256"" />
    <rectangle x=""1716"" y=""1225"" z=""-128"" width=""28"" height=""20"" depth=""256"" />
    <rectangle x=""1744"" y=""1204"" z=""-128"" width=""38"" height=""50"" depth=""256"" />
    <rectangle x=""1732"" y=""1173"" z=""-128"" width=""27"" height=""33"" depth=""256"" />
    <rectangle x=""1719"" y=""1195"" z=""-128"" width=""26"" height=""22"" depth=""256"" />
    <rectangle x=""1750"" y=""1193"" z=""-128"" width=""18"" height=""18"" depth=""256"" />
   </region>
   <region name=""Boss Collegio"">
    <rectangle x=""873"" y=""1242"" z=""-128"" width=""86"" height=""132"" depth=""256"" />
    <rectangle x=""845"" y=""1293"" z=""-128"" width=""40"" height=""24"" depth=""256"" />
   </region>
   <region name=""Gilda Dei Ladri 2"">
    <rectangle x=""920"" y=""1547"" z=""-128"" width=""51"" height=""29"" depth=""256"" />
   </region>
   <region name=""NOPK Ilshenar"">
    <rectangle x=""1761"" y=""953"" z=""-128"" width=""104"" height=""40"" depth=""256"" />
    <rectangle x=""1760"" y=""958"" z=""-128"" width=""95"" height=""42"" depth=""256"" />
    <rectangle x=""1761"" y=""946"" z=""-128"" width=""95"" height=""40"" depth=""256"" />
   </region>
   <region name=""Chierici Novizi"">
    <rectangle x=""704"" y=""1551"" z=""-128"" width=""24"" height=""25"" depth=""256"" />
   </region>
   <region name=""Castello Ilshenar"">
    <rectangle x=""1101"" y=""620"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
    <rectangle x=""1104"" y=""636"" z=""-128"" width=""8"" height=""33"" depth=""256"" />
    <rectangle x=""1101"" y=""669"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
    <rectangle x=""1068"" y=""672"" z=""-128"" width=""33"" height=""8"" depth=""256"" />
    <rectangle x=""1052"" y=""668"" z=""-128"" width=""16"" height=""17"" depth=""256"" />
    <rectangle x=""1057"" y=""636"" z=""-128"" width=""8"" height=""32"" depth=""256"" />
    <rectangle x=""1052"" y=""620"" z=""-128"" width=""16"" height=""16"" depth=""256"" />
    <rectangle x=""1068"" y=""625"" z=""-128"" width=""33"" height=""8"" depth=""256"" />
    <rectangle x=""1065"" y=""633"" z=""-128"" width=""39"" height=""39"" depth=""256"" />
    <rectangle x=""1090"" y=""680"" z=""-40"" width=""5"" height=""16"" depth=""168"" />
    <rectangle x=""1081"" y=""696"" z=""-128"" width=""23"" height=""16"" depth=""256"" />
   </region>
   <region name=""Gilda Dei Ladri 1"">
    <rectangle x=""953"" y=""1496"" z=""-128"" width=""30"" height=""15"" depth=""256"" />
    <rectangle x=""980"" y=""1493"" z=""-128"" width=""35"" height=""34"" depth=""256"" />
   </region>
   <region name=""Arena TW"">
    <rectangle x=""1162"" y=""1083"" z=""-128"" width=""70"" height=""108"" depth=""256"" />
    <rectangle x=""1231"" y=""1083"" z=""-128"" width=""11"" height=""66"" depth=""256"" />
   </region>
   <region name=""Gilda Dei Ladri 3"">
    <rectangle x=""937"" y=""1503"" z=""-128"" width=""15"" height=""36"" depth=""256"" />
    <rectangle x=""952"" y=""1518"" z=""-128"" width=""16"" height=""25"" depth=""256"" />
    <rectangle x=""937"" y=""1539"" z=""-128"" width=""7"" height=""8"" depth=""256"" />
   </region>
   <region name=""Collegio"">
    <rectangle x=""390"" y=""4"" z=""-128"" width=""79"" height=""100"" depth=""256"" />
    <rectangle x=""420"" y=""102"" z=""-128"" width=""13"" height=""12"" depth=""256"" />
    <rectangle x=""463"" y=""2"" z=""-128"" width=""16"" height=""33"" depth=""256"" />
   </region>
   <region name=""Covo della gilda dei Cacciatori"">
    <rectangle x=""264"" y=""1570"" z=""-128"" width=""72"" height=""26"" depth=""256"" />
   </region>
   <region name=""Covo delle Streghe"">
    <rectangle x=""202"" y=""1337"" z=""-128"" width=""54"" height=""54"" depth=""256"" />
   </region>
   <region name=""Uscita Tana della Sfinge"">
    <rectangle x=""769"" y=""1569"" z=""-128"" width=""8"" height=""15"" depth=""256"" />
   </region>
   <region name=""ELEMENTALI NEWBIE"">
    <rectangle x=""364"" y=""1466"" z=""-128"" width=""200"" height=""135"" depth=""256"" />
   </region>
   <region name=""Vampire Dungeon"">
    <rectangle x=""45"" y=""2"" z=""-128"" width=""141"" height=""129"" depth=""256"" />
    <rectangle x=""554"" y=""404"" z=""-128"" width=""43"" height=""54"" depth=""256"" />
   </region>
   <region name=""Sotterranei Britain"">
    <rectangle x=""2186"" y=""294"" z=""-128"" width=""5"" height=""26"" depth=""256"" />
   </region>
   <region name=""Covo del Signore"">
    <rectangle x=""1592"" y=""533"" z=""-128"" width=""39"" height=""33"" depth=""256"" />
   </region>
   <region name=""Exodus Dungeon"">
    <rectangle x=""1844"" y=""43"" z=""-128"" width=""240"" height=""160"" depth=""256"" />
    <rectangle x=""1908"" y=""19"" z=""-128"" width=""164"" height=""54"" depth=""256"" />
    <rectangle x=""1903"" y=""189"" z=""-128"" width=""56"" height=""23"" depth=""256"" />
   </region>
  </Dungeons>
  <Guarded>
   <region name=""Shantar"">
    <rectangle x=""915"" y=""626"" z=""-128"" width=""11"" height=""11"" depth=""256"" />
    <rectangle x=""914"" y=""647"" z=""-128"" width=""10"" height=""8"" depth=""256"" />
    <rectangle x=""901"" y=""624"" z=""-128"" width=""12"" height=""32"" depth=""256"" />
    <rectangle x=""877"" y=""603"" z=""-128"" width=""24"" height=""76"" depth=""256"" />
    <rectangle x=""853"" y=""576"" z=""-128"" width=""25"" height=""127"" depth=""256"" />
    <rectangle x=""754"" y=""555"" z=""-128"" width=""100"" height=""101"" depth=""256"" />
    <rectangle x=""754"" y=""654"" z=""-128"" width=""100"" height=""74"" depth=""256"" />
    <rectangle x=""912"" y=""629"" z=""-128"" width=""5"" height=""26"" depth=""256"" />
   </region>
  </Guarded>
  <Forest>
   <region name=""Ingresso Minos"">
    <rectangle x=""645"" y=""927"" z=""-128"" width=""18"" height=""14"" depth=""256"" />
    <rectangle x=""663"" y=""928"" z=""-128"" width=""8"" height=""7"" depth=""256"" />
   </region>
   <region name=""Reale Accademia di Britannia"">
    <rectangle x=""933"" y=""228"" z=""-128"" width=""159"" height=""88"" depth=""256"" />
   </region>
   <region name="""">
    <rectangle x=""937"" y=""1567"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""937"" y=""1566"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""936"" y=""1572"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""935"" y=""1570"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""938"" y=""1572"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""933"" y=""1571"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""934"" y=""1569"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""933"" y=""1572"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""933"" y=""1569"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""936"" y=""1570"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""944"" y=""1562"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""939"" y=""1563"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""940"" y=""1568"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""938"" y=""1570"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""944"" y=""1564"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""942"" y=""1562"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""940"" y=""1564"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""938"" y=""1569"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""940"" y=""1571"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""940"" y=""1566"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""937"" y=""1553"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""922"" y=""1561"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""930"" y=""1565"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""924"" y=""1558"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""932"" y=""1567"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""938"" y=""1568"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""931"" y=""1571"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""928"" y=""1566"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""932"" y=""1566"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""944"" y=""1560"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""937"" y=""1568"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
   <region name="""">
    <rectangle x=""926"" y=""1562"" z=""-28"" width=""1"" height=""1"" depth=""10"" />
   </region>
  </Forest>
 </Ilshenar>
 <Dungeon_semi-chiusi>
  <Dungeons>
   <region name=""Shame 2"">
    <rectangle x=""5376"" y=""131"" z=""-128"" width=""261"" height=""129"" depth=""256"" />
   </region>
   <region name=""Destard 1"">
    <rectangle x=""5185"" y=""768"" z=""-128"" width=""189"" height=""219"" depth=""256"" />
    <rectangle x=""5205"" y=""987"" z=""-128"" width=""137"" height=""28"" depth=""256"" />
    <rectangle x=""5196"" y=""987"" z=""-128"" width=""9"" height=""8"" depth=""256"" />
    <rectangle x=""5120"" y=""889"" z=""-128"" width=""65"" height=""63"" depth=""256"" />
    <rectangle x=""5154"" y=""952"" z=""-128"" width=""31"" height=""23"" depth=""256"" />
   </region>
   <region name=""Destard 2"">
    <rectangle x=""5121"" y=""788"" z=""-128"" width=""64"" height=""98"" depth=""256"" />
   </region>
   <region name=""Destard 3"">
    <rectangle x=""5121"" y=""953"" z=""-128"" width=""33"" height=""70"" depth=""256"" />
    <rectangle x=""5154"" y=""975"" z=""-128"" width=""30"" height=""47"" depth=""256"" />
    <rectangle x=""5184"" y=""995"" z=""-128"" width=""16"" height=""29"" depth=""256"" />
   </region>
   <region name=""Ice 2"">
    <rectangle x=""5800"" y=""319"" z=""-128"" width=""63"" height=""65"" depth=""256"" />
   </region>
   <region name=""Deceit 3"">
    <rectangle x=""5125"" y=""644"" z=""-128"" width=""115"" height=""125"" depth=""256"" />
   </region>
   <region name=""Bottega Maledetta"">
    <rectangle x=""5687"" y=""564"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5645"" y=""536"" z=""-128"" width=""58"" height=""43"" depth=""256"" />
    <rectangle x=""5669"" y=""513"" z=""-128"" width=""49"" height=""33"" depth=""256"" />
    <rectangle x=""5672"" y=""522"" z=""-128"" width=""15"" height=""15"" depth=""256"" />
    <rectangle x=""5684"" y=""558"" z=""-128"" width=""51"" height=""18"" depth=""256"" />
    <rectangle x=""5710"" y=""551"" z=""-128"" width=""28"" height=""21"" depth=""256"" />
   </region>
   <region name=""Deceit 3."">
    <rectangle x=""5123"" y=""644"" z=""-128"" width=""117"" height=""123"" depth=""256"" />
   </region>
   <region name=""Antro di Glacior"">
    <rectangle x=""4010"" y=""304"" z=""-128"" width=""39"" height=""40"" depth=""256"" />
    <rectangle x=""4010"" y=""336"" z=""-128"" width=""9"" height=""12"" depth=""256"" />
    <rectangle x=""4019"" y=""331"" z=""-128"" width=""13"" height=""17"" depth=""256"" />
    <rectangle x=""4016"" y=""301"" z=""-128"" width=""36"" height=""23"" depth=""256"" />
    <rectangle x=""4008"" y=""302"" z=""-128"" width=""8"" height=""8"" depth=""256"" />
    <rectangle x=""4008"" y=""308"" z=""-128"" width=""20"" height=""18"" depth=""256"" />
   </region>
   <region name=""LAMA-Emblema"">
    <rectangle x=""5901"" y=""397"" z=""-128"" width=""43"" height=""102"" depth=""256"" />
    <rectangle x=""5959"" y=""397"" z=""-128"" width=""44"" height=""102"" depth=""256"" />
    <rectangle x=""5901"" y=""397"" z=""-128"" width=""102"" height=""43"" depth=""256"" />
    <rectangle x=""5901"" y=""455"" z=""-128"" width=""102"" height=""44"" depth=""256"" />
   </region>
   <region name=""Passaggio Terre Perdute 2"">
    <rectangle x=""5890"" y=""1287"" z=""-128"" width=""44"" height=""78"" depth=""256"" />
    <rectangle x=""5934"" y=""1283"" z=""-128"" width=""102"" height=""129"" depth=""256"" />
    <rectangle x=""6035"" y=""1301"" z=""-128"" width=""11"" height=""21"" depth=""256"" />
   </region>
   <region name=""Ocelot Chamber"">
    <rectangle x=""4582"" y=""3562"" z=""-128"" width=""27"" height=""21"" depth=""256"" />
    <rectangle x=""4586"" y=""3583"" z=""-128"" width=""18"" height=""4"" depth=""256"" />
   </region>
   <region name=""Pit of Kornak"">
    <rectangle x=""5803"" y=""338"" z=""-128"" width=""60"" height=""46"" depth=""256"" />
    <rectangle x=""5822"" y=""336"" z=""-128"" width=""27"" height=""5"" depth=""256"" />
    <rectangle x=""5826"" y=""319"" z=""-128"" width=""18"" height=""18"" depth=""256"" />
   </region>
   <region name=""Tana di Eostar"">
    <rectangle x=""5130"" y=""956"" z=""-128"" width=""70"" height=""64"" depth=""256"" />
    <rectangle x=""5119"" y=""895"" z=""-128"" width=""70"" height=""70"" depth=""256"" />
    <rectangle x=""5149"" y=""815"" z=""-128"" width=""140"" height=""156"" depth=""256"" />
    <rectangle x=""5203"" y=""821"" z=""-128"" width=""83"" height=""80"" depth=""256"" />
   </region>
   <region name=""Eostar"">
    <rectangle x=""5656"" y=""1324"" z=""-128"" width=""15"" height=""12"" depth=""256"" />
    <rectangle x=""5645"" y=""1299"" z=""-128"" width=""50"" height=""52"" depth=""256"" />
    <rectangle x=""5635"" y=""1285"" z=""-128"" width=""65"" height=""23"" depth=""256"" />
    <rectangle x=""5654"" y=""1285"" z=""-128"" width=""46"" height=""66"" depth=""256"" />
    <rectangle x=""5636"" y=""1309"" z=""-128"" width=""22"" height=""24"" depth=""256"" />
   </region>
   <region name=""Rissone di Natale"">
    <rectangle x=""2488"" y=""435"" z=""-128"" width=""2"" height=""4"" depth=""256"" />
    <rectangle x=""2393"" y=""440"" z=""-128"" width=""200"" height=""190"" depth=""256"" />
    <rectangle x=""2396"" y=""353"" z=""-128"" width=""147"" height=""98"" depth=""256"" />
    <rectangle x=""2524"" y=""356"" z=""-128"" width=""24"" height=""46"" depth=""256"" />
    <rectangle x=""2488"" y=""346"" z=""-128"" width=""45"" height=""21"" depth=""256"" />
   </region>
  </Dungeons>
  <Forest>
   <region name=""ApprodoOscuro"">
    <rectangle x=""2138"" y=""3599"" z=""-128"" width=""3"" height=""4"" depth=""256"" />
   </region>
   <region name=""BonusFazioneAlberi"">
    <rectangle x=""417"" y=""2020"" z=""-128"" width=""86"" height=""86"" depth=""256"" />
    <rectangle x=""417"" y=""2106"" z=""-128"" width=""86"" height=""45"" depth=""256"" />
    <rectangle x=""466"" y=""1982"" z=""-128"" width=""51"" height=""38"" depth=""256"" />
   </region>
  </Forest>
 </Dungeon_semi-chiusi>
 <Dungeon_chiusi>
  <Dungeons>
   <region name=""Shame 3"">
    <rectangle x=""5637"" y=""0"" z=""-128"" width=""261"" height=""128"" depth=""256"" />
   </region>
   <region name=""Labirinto"">
    <rectangle x=""5282"" y=""1276"" z=""-128"" width=""92"" height=""114"" depth=""256"" />
   </region>
   <region name=""Ice 3"">
    <rectangle x=""5654"" y=""300"" z=""-128"" width=""54"" height=""40"" depth=""256"" />
   </region>
   <region name=""Hythloth 1"">
    <rectangle x=""5902"" y=""12"" z=""-128"" width=""128"" height=""43"" depth=""256"" />
    <rectangle x=""5902"" y=""55"" z=""-128"" width=""19"" height=""12"" depth=""256"" />
    <rectangle x=""5982"" y=""55"" z=""-128"" width=""25"" height=""17"" depth=""256"" />
   </region>
   <region name=""Hythloth 2"">
    <rectangle x=""5898"" y=""72"" z=""-128"" width=""132"" height=""43"" depth=""256"" />
    <rectangle x=""5924"" y=""55"" z=""-128"" width=""53"" height=""17"" depth=""256"" />
   </region>
   <region name=""Hythloth 3"">
    <rectangle x=""6017"" y=""136"" z=""-128"" width=""125"" height=""101"" depth=""256"" />
   </region>
   <region name=""Hythloth 4"">
    <rectangle x=""5883"" y=""393"" z=""-128"" width=""122"" height=""108"" depth=""256"" />
   </region>
   <region name=""Hythloth 5"">
    <rectangle x=""6042"" y=""0"" z=""-128"" width=""86"" height=""109"" depth=""256"" />
   </region>
   <region name=""Hythloth 6"">
    <rectangle x=""6025"" y=""3930"" z=""-128"" width=""105"" height=""105"" depth=""256"" />
   </region>
   <region name=""Dungeon Niubbi"">
    <rectangle x=""0"" y=""0"" z=""-128"" width=""0"" height=""0"" depth=""256"" />
   </region>
   <region name=""Deceit 4"">
    <rectangle x=""5244"" y=""644"" z=""-128"" width=""113"" height=""124"" depth=""256"" />
   </region>
   <region name=""Khaldun"">
    <rectangle x=""5381"" y=""1284"" z=""-128"" width=""247"" height=""225"" depth=""256"" />
   </region>
   <region name=""Fire Dungeon 2"">
    <rectangle x=""5635"" y=""1282"" z=""-128"" width=""255"" height=""242"" depth=""256"" />
   </region>
   <region name=""Squab Area"">
    <rectangle x=""6638"" y=""64"" z=""-128"" width=""60"" height=""134"" depth=""256"" />
    <rectangle x=""6632"" y=""128"" z=""-128"" width=""6"" height=""10"" depth=""256"" />
   </region>
   <region name=""La Profezia"">
    <rectangle x=""1271"" y=""1548"" z=""-128"" width=""250"" height=""168"" depth=""256"" />
   </region>
   <region name=""AntroOscuro2"">
    <rectangle x=""935"" y=""684"" z=""-128"" width=""35"" height=""43"" depth=""256"" />
   </region>
   <region name=""Hythoth 3"">
    <rectangle x=""5912"" y=""143"" z=""-128"" width=""88"" height=""97"" depth=""256"" />
    <rectangle x=""5900"" y=""156"" z=""-128"" width=""5"" height=""5"" depth=""256"" />
   </region>
   <region name=""Tana Di Vladimir"">
    <rectangle x=""5590"" y=""1326"" z=""-128"" width=""35"" height=""35"" depth=""256"" />
   </region>
   <region name=""Dungeon Newbie1"">
    <rectangle x=""5379"" y=""5"" z=""-128"" width=""121"" height=""121"" depth=""256"" />
    <rectangle x=""5392"" y=""125"" z=""-128"" width=""11"" height=""9"" depth=""256"" />
   </region>
   <region name=""Covo di Jack"">
    <rectangle x=""1328"" y=""1436"" z=""-128"" width=""68"" height=""82"" depth=""256"" />
   </region>
   <region name=""Tunnel Sotteraneo Di Wrong"">
    <rectangle x=""5295"" y=""1803"" z=""-128"" width=""48"" height=""60"" depth=""256"" />
   </region>
   <region name=""Shame Livello 5"">
    <rectangle x=""5637"" y=""0"" z=""-128"" width=""261"" height=""124"" depth=""256"" />
   </region>
   <region name=""Mausoleo Onirico"">
    <rectangle x=""1732"" y=""2737"" z=""-128"" width=""44"" height=""48"" depth=""256"" />
   </region>
   <region name=""Arena Minax"">
    <rectangle x=""1099"" y=""2572"" z=""-128"" width=""74"" height=""51"" depth=""256"" />
    <rectangle x=""1160"" y=""2592"" z=""-128"" width=""9"" height=""14"" depth=""256"" />
    <rectangle x=""1168"" y=""2588"" z=""-128"" width=""19"" height=""14"" depth=""256"" />
   </region>
   <region name=""LAMA-Custode"">
    <rectangle x=""5941"" y=""328"" z=""-128"" width=""22"" height=""26"" depth=""256"" />
    <rectangle x=""5958"" y=""309"" z=""-128"" width=""30"" height=""22"" depth=""256"" />
    <rectangle x=""5956"" y=""323"" z=""-128"" width=""1"" height=""3"" depth=""256"" />
    <rectangle x=""5954"" y=""326"" z=""-128"" width=""4"" height=""5"" depth=""256"" />
    <rectangle x=""5955"" y=""324"" z=""-128"" width=""3"" height=""7"" depth=""256"" />
    <rectangle x=""5939"" y=""283"" z=""-128"" width=""21"" height=""25"" depth=""256"" />
    <rectangle x=""5915"" y=""306"" z=""-128"" width=""27"" height=""22"" depth=""256"" />
    <rectangle x=""5941"" y=""312"" z=""-128"" width=""3"" height=""6"" depth=""256"" />
    <rectangle x=""5943"" y=""312"" z=""-128"" width=""2"" height=""5"" depth=""256"" />
    <rectangle x=""5945"" y=""312"" z=""-128"" width=""0"" height=""4"" depth=""256"" />
    <rectangle x=""5944"" y=""312"" z=""-128"" width=""2"" height=""4"" depth=""256"" />
    <rectangle x=""5944"" y=""312"" z=""-128"" width=""2"" height=""5"" depth=""256"" />
    <rectangle x=""5945"" y=""312"" z=""-128"" width=""3"" height=""5"" depth=""256"" />
    <rectangle x=""5947"" y=""312"" z=""-128"" width=""1"" height=""2"" depth=""256"" />
    <rectangle x=""5947"" y=""312"" z=""-128"" width=""2"" height=""1"" depth=""256"" />
    <rectangle x=""5951"" y=""307"" z=""-128"" width=""8"" height=""5"" depth=""256"" />
    <rectangle x=""5952"" y=""311"" z=""-128"" width=""7"" height=""2"" depth=""256"" />
    <rectangle x=""5952"" y=""314"" z=""-128"" width=""7"" height=""0"" depth=""256"" />
    <rectangle x=""5952"" y=""312"" z=""-128"" width=""6"" height=""1"" depth=""256"" />
    <rectangle x=""5952"" y=""312"" z=""-128"" width=""7"" height=""2"" depth=""256"" />
    <rectangle x=""5954"" y=""314"" z=""-128"" width=""5"" height=""2"" depth=""256"" />
    <rectangle x=""5954"" y=""315"" z=""-128"" width=""5"" height=""1"" depth=""256"" />
    <rectangle x=""5954"" y=""315"" z=""-128"" width=""2"" height=""1"" depth=""256"" />
    <rectangle x=""5954"" y=""314"" z=""-128"" width=""5"" height=""3"" depth=""256"" />
    <rectangle x=""5956"" y=""316"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
    <rectangle x=""5956"" y=""322"" z=""-128"" width=""1"" height=""5"" depth=""256"" />
    <rectangle x=""5956"" y=""322"" z=""-128"" width=""1"" height=""2"" depth=""256"" />
    <rectangle x=""5956"" y=""322"" z=""-128"" width=""2"" height=""3"" depth=""256"" />
    <rectangle x=""5943"" y=""326"" z=""-128"" width=""7"" height=""1"" depth=""256"" />
    <rectangle x=""5943"" y=""327"" z=""-128"" width=""8"" height=""1"" depth=""256"" />
    <rectangle x=""5943"" y=""325"" z=""-128"" width=""6"" height=""1"" depth=""256"" />
    <rectangle x=""5943"" y=""324"" z=""-128"" width=""5"" height=""1"" depth=""256"" />
    <rectangle x=""5943"" y=""323"" z=""-128"" width=""4"" height=""1"" depth=""256"" />
    <rectangle x=""5943"" y=""322"" z=""-128"" width=""4"" height=""1"" depth=""256"" />
    <rectangle x=""5942"" y=""319"" z=""-128"" width=""3"" height=""3"" depth=""256"" />
    <rectangle x=""5946"" y=""322"" z=""-128"" width=""2"" height=""2"" depth=""256"" />
   </region>
   <region name=""AntroOscuro1"">
    <rectangle x=""5130"" y=""956"" z=""-128"" width=""66"" height=""67"" depth=""256"" />
   </region>
   <region name=""Hythloth Finale"">
    <rectangle x=""6027"" y=""395"" z=""-128"" width=""100"" height=""100"" depth=""256"" />
   </region>
   <region name=""Blighted Grove"">
    <rectangle x=""6427"" y=""811"" z=""-128"" width=""178"" height=""175"" depth=""256"" />
   </region>
   <region name=""Caverna"">
    <rectangle x=""6549"" y=""140"" z=""-128"" width=""36"" height=""34"" depth=""256"" />
    <rectangle x=""6542"" y=""146"" z=""-128"" width=""29"" height=""26"" depth=""256"" />
    <rectangle x=""6536"" y=""151"" z=""-128"" width=""10"" height=""16"" depth=""256"" />
    <rectangle x=""6550"" y=""113"" z=""-128"" width=""45"" height=""18"" depth=""256"" />
    <rectangle x=""6544"" y=""110"" z=""-128"" width=""10"" height=""21"" depth=""256"" />
    <rectangle x=""6562"" y=""104"" z=""-128"" width=""26"" height=""14"" depth=""256"" />
    <rectangle x=""6522"" y=""69"" z=""-128"" width=""47"" height=""30"" depth=""256"" />
    <rectangle x=""6565"" y=""71"" z=""-128"" width=""11"" height=""17"" depth=""256"" />
    <rectangle x=""6535"" y=""87"" z=""-128"" width=""22"" height=""17"" depth=""256"" />
    <rectangle x=""6500"" y=""112"" z=""-128"" width=""46"" height=""27"" depth=""256"" />
   </region>
   <region name=""Wrong diff - Livello 2 [Room]"">
    <rectangle x=""5715"" y=""554"" z=""-128"" width=""20"" height=""13"" depth=""256"" />
   </region>
   <region name=""Passaggio Terre Perdute"">
    <rectangle x=""5892"" y=""1371"" z=""-128"" width=""30"" height=""48"" depth=""256"" />
    <rectangle x=""5920"" y=""1375"" z=""-128"" width=""17"" height=""33"" depth=""256"" />
   </region>
   <region name=""I Canali di Montor"">
    <rectangle x=""5124"" y=""1402"" z=""-128"" width=""247"" height=""109"" depth=""256"" />
    <rectangle x=""5144"" y=""1509"" z=""-128"" width=""23"" height=""7"" depth=""256"" />
   </region>
  </Dungeons>
  <Guarded>
   <region name=""Sala Teletrasporti"">
    <rectangle x=""1295"" y=""1679"" z=""-128"" width=""17"" height=""17"" depth=""256"" />
    <rectangle x=""1303"" y=""1666"" z=""-128"" width=""5"" height=""14"" depth=""256"" />
    <rectangle x=""1307"" y=""1667"" z=""-128"" width=""13"" height=""16"" depth=""256"" />
   </region>
  </Guarded>
  <Forest>
   <region name=""Boss Fire"">
    <rectangle x=""5652"" y=""1443"" z=""-128"" width=""0"" height=""0"" depth=""256"" />
   </region>
   <region name=""Lavori Forzati"">
    <rectangle x=""2161"" y=""1956"" z=""-128"" width=""54"" height=""58"" depth=""256"" />
    <rectangle x=""2932"" y=""737"" z=""-128"" width=""18"" height=""21"" depth=""256"" />
   </region>
   <region name=""Città dei Pazzi"">
    <rectangle x=""2840"" y=""588"" z=""-128"" width=""214"" height=""184"" depth=""256"" />
   </region>
   <region name=""BonusFazioniAlberi"">
    <rectangle x=""417"" y=""2020"" z=""-128"" width=""86"" height=""86"" depth=""256"" />
    <rectangle x=""417"" y=""2106"" z=""-128"" width=""86"" height=""45"" depth=""256"" />
    <rectangle x=""466"" y=""1982"" z=""-128"" width=""51"" height=""38"" depth=""256"" />
   </region>
   <region name=""Interno Arena Minax"">
    <rectangle x=""1110"" y=""2583"" z=""-128"" width=""50"" height=""27"" depth=""256"" />
   </region>
   <region name=""MonsterHunter"">
    <rectangle x=""5898"" y=""2779"" z=""-128"" width=""244"" height=""324"" depth=""256"" />
   </region>
  </Forest>
 </Dungeon_chiusi>
 <Eventi>
  <Dungeons>
   <region name=""Anticamera Arena Campione"">
    <rectangle x=""5425"" y=""1416"" z=""-128"" width=""3"" height=""12"" depth=""256"" />
    <rectangle x=""5396"" y=""1400"" z=""-128"" width=""2"" height=""15"" depth=""256"" />
    <rectangle x=""5409"" y=""1417"" z=""-128"" width=""5"" height=""5"" depth=""256"" />
    <rectangle x=""5410"" y=""1374"" z=""-128"" width=""8"" height=""7"" depth=""256"" />
    <rectangle x=""5409"" y=""1447"" z=""-128"" width=""7"" height=""10"" depth=""256"" />
    <rectangle x=""5416"" y=""1444"" z=""-128"" width=""5"" height=""11"" depth=""256"" />
    <rectangle x=""5427"" y=""1417"" z=""-128"" width=""13"" height=""2"" depth=""256"" />
    <rectangle x=""5389"" y=""1401"" z=""-128"" width=""7"" height=""2"" depth=""256"" />
    <rectangle x=""5401"" y=""1389"" z=""-128"" width=""16"" height=""1"" depth=""256"" />
    <rectangle x=""5417"" y=""1389"" z=""-128"" width=""16"" height=""9"" depth=""256"" />
    <rectangle x=""5433"" y=""1397"" z=""-128"" width=""6"" height=""9"" depth=""256"" />
    <rectangle x=""5390"" y=""1414"" z=""-128"" width=""8"" height=""1"" depth=""256"" />
    <rectangle x=""5390"" y=""1414"" z=""-128"" width=""1"" height=""19"" depth=""256"" />
    <rectangle x=""5390"" y=""1433"" z=""-128"" width=""8"" height=""6"" depth=""256"" />
   </region>
   <region name=""ArenaTW"">
    <rectangle x=""5380"" y=""1284"" z=""-128"" width=""200"" height=""55"" depth=""256"" />
    <rectangle x=""5904"" y=""270"" z=""-128"" width=""95"" height=""99"" depth=""256"" />
    <rectangle x=""5503"" y=""1338"" z=""-128"" width=""67"" height=""15"" depth=""256"" />
    <rectangle x=""5378"" y=""1284"" z=""-128"" width=""245"" height=""231"" depth=""256"" />
   </region>
   <region name=""Esterno RoyalRumble"">
    <rectangle x=""1103"" y=""2576"" z=""-128"" width=""66"" height=""44"" depth=""256"" />
   </region>
   <region name=""AtrioRissone"">
    <rectangle x=""5785"" y=""1222"" z=""-128"" width=""34"" height=""26"" depth=""256"" />
    <rectangle x=""5792"" y=""1242"" z=""-128"" width=""18"" height=""23"" depth=""256"" />
   </region>
  </Dungeons>
  <Forest>
   <region name=""Monster Ranch"">
    <rectangle x=""5124"" y=""877"" z=""-128"" width=""242"" height=""139"" depth=""256"" />
   </region>
  </Forest>
 </Eventi>
</regions>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFREGION);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadRegionDef: {e}");
                }
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"UOSteam -> Exception in LoadRegionDef: {e}");
            }

            XmlElement root;
            if (doc != null && (root = doc["regions"]) != null)
            {
                foreach (XmlElement group in root)
                {
                    foreach (XmlElement food in group.GetElementsByTagName("food"))
                    {
                        Foods.AddFood(GetAttribute(group, "name"), GetAttribute(food, "name"), GetAttributeUShort(food, "graphic"));
                    }
                }
            }
        }

        internal static void LoadBuffDef(FileInfo info, AssistantGump gump)
        {
            XmlDocument doc = new XmlDocument();
            if (!info.Exists)
            {
                #region defbuffico
                const string DEFBUFFICO = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<bufficons>
	<icon id=""1001"" name=""Dismount""/>
	<icon id=""1002"" name=""Vampire Regen""/>
	<icon id=""1005"" name=""Night Sight""/>
	<icon id=""1006"" name=""Vampire Speed""/> 
	<icon id=""1007"" name=""Evil Omen""/> 
	<icon id=""1009"" name=""Prayer""/>
	<icon id=""1010"" name=""Divine Fury""/> 
	<icon id=""1011"" name=""Bard Buff""/> 
	<icon id=""1012"" name=""Hiding""/>
	<icon id=""1013"" name=""Meditation""/>
	<icon id=""1014"" name=""Blood Oath Caster""/> 
	<icon id=""1015"" name=""Blood Oath""/> 
	<icon id=""1016"" name=""Corpse Skin""/> 
	<icon id=""1017"" name=""Mind rot""/> 
	<icon id=""1018"" name=""Pain Spike""/> 
	<icon id=""1019"" name=""Strangle""/> 
	<icon id=""1020"" name=""Gift of Renewal""/> 
	<icon id=""1021"" name=""Attune Weapon""/> 
	<icon id=""1022"" name=""Thunderstorm""/> 
	<icon id=""1023"" name=""Essence of Wind""/> 
	<icon id=""1024"" name=""Sacred Flames""/> 
	<icon id=""1025"" name=""Sacrifice""/> 
	<icon id=""1026"" name=""Arcane Empowerment""/> 
	<icon id=""1027"" name=""Bard Debuff""/> 
	<icon id=""1028"" name=""Reactive Armor""/>
	<icon id=""1029"" name=""Protection""/>
	<icon id=""1030"" name=""Bard Protections""/>
	<icon id=""1031"" name=""Magic Reflection""/>
	<icon id=""1032"" name=""Incognito""/>
	<icon id=""1033"" name=""Disguised""/> 
	<icon id=""1034"" name=""Animal Form""/> 
	<icon id=""1035"" name=""Polymorph""/>
	<icon id=""1036"" name=""Invisibility""/>
	<icon id=""1037"" name=""Paralyze""/>
	<icon id=""1038"" name=""Poison""/>
	<icon id=""1039"" name=""Bleed""/>
	<icon id=""1040"" name=""Clumsy""/>
	<icon id=""1041"" name=""Feeblemind""/>
	<icon id=""1042"" name=""Weaken""/>
	<icon id=""1043"" name=""Curse""/>
	<icon id=""1044"" name=""Mass Curse""/>
	<icon id=""1045"" name=""Agility""/>
	<icon id=""1046"" name=""Cunning""/>
	<icon id=""1047"" name=""Strength""/>
	<icon id=""1048"" name=""Bless""/>
	<icon id=""1049"" name=""Sleep""/>
	<icon id=""1051"" name=""Spell Plague""/>
</bufficons>
";
                #endregion
                try
                {
                    using (StreamWriter w = new StreamWriter(info.FullName, false))
                    {
                        w.Write(DEFBUFFICO);
                        w.Flush();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"UOSteam -> Exception in LoadBuffDef: {e}");
                }
            }
            try
            {
                doc.Load(info.FullName);
            }
            catch (Exception e)
            {
                Log.Warn($"UOSteam -> Exception in LoadBuffDef: {e}");
            }

            XmlElement root;
            
            if (doc != null && (root = doc["bufficons"]) != null)
            {
                foreach(XmlElement icons in root.GetElementsByTagName("icon"))
                {
                    int icon = (int)GetAttributeUInt(icons, "id");
                    if(icon > 0)
                    {
                        string name = GetAttribute(icons, "name");
                        if(!string.IsNullOrEmpty(name))
                        {
                            name = name.ToLower(Culture);
                            PlayerData.BuffNames[name] = PlayerData.BuffNames[name.Replace(" ", "")] = icon;
                        }
                    }
                }
            }
        }

        private static void SetAllSkills(List<SkillEntry> entries)
        {
            Client.Game.UO.FileManager.Skills.Skills.Clear();
            Client.Game.UO.FileManager.Skills.Skills.AddRange(entries);
            Client.Game.UO.FileManager.Skills.SortedSkills.Clear();
            Client.Game.UO.FileManager.Skills.SortedSkills.AddRange(entries);
            Client.Game.UO.FileManager.Skills.SortedSkills.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

        internal static void SaveData(string profile = null)
        {
            AssistantGump gump = UOSObjects.Gump;
            if (gump == null)
                return;
            SaveConfig(gump);
            if(profile == null)
                profile = gump.LastProfile;
            if (!string.IsNullOrWhiteSpace(profile))
                SaveProfile(profile);
        }

        internal static void SaveConfig(AssistantGump gump)
        {
            try
            {
                FileInfo info = new FileInfo(Path.Combine(Engine.DataPath, $"assistant.xml"));
                bool exists = info.Exists;
                using (FileStream op = new FileStream(info.FullName, FileMode.OpenOrCreate))
                {
                    using(XmlTextWriter xml = new XmlTextWriter(op, Encoding.UTF8) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 1 })
                    {
                        xml.WriteStartDocument(true);
                        xml.WriteStartElement("config");

                        xml.WriteStartElement("data");
                        xml.WriteAttributeString("name", "LastProfile");
                        xml.WriteString($"{gump.LastProfile}.xml");
                        xml.WriteEndElement();

                        xml.WriteStartElement("data");
                        xml.WriteAttributeString("name", "SmartProfile");
                        xml.WriteString($"{gump.SmartProfile}");
                        xml.WriteEndElement();

                        xml.WriteStartElement("snapshots");
                        xml.WriteStartElement("ownDeath");
                        xml.WriteString(UOSObjects.Gump.SnapOwnDeath.ToString());
                        xml.WriteEndElement();
                        xml.WriteStartElement("othersDeath");
                        xml.WriteString(UOSObjects.Gump.SnapOtherDeath.ToString());
                        xml.WriteEndElement();
                        xml.WriteEndElement();
                        /*xml.WriteStartElement("data");
                        xml.WriteAttributeString("name", "NegotiateFeatures");
                        xml.WriteString($"{gump.NegotiateFeatures}");
                        xml.WriteEndElement();*/
                        if (gump.ItemsToLoot.Count > 0)
                        {
                            xml.WriteStartElement("autoloot");
                            foreach (KeyValuePair<ushort, (ushort, string)> kvp in gump.ItemsToLoot)
                            {
                                xml.WriteStartElement("item");
                                xml.WriteAttributeString("type", $"{kvp.Key}");
                                xml.WriteAttributeString("limit", $"{kvp.Value.Item1}");
                                xml.WriteAttributeString("name", $"{kvp.Value.Item2}");
                                xml.WriteEndElement();
                            }
                            xml.WriteEndElement();
                        }

                        xml.WriteStartElement("gump");
                        xml.WriteAttributeString("x", gump.X.ToString());
                        xml.WriteAttributeString("y", gump.Y.ToString());
                        xml.WriteEndElement();

                        xml.WriteEndElement();
                    }
                }
                if (exists)
                {
                    try
                    {
                        //keep the backup if they are not older than 7/14 days
                        FileInfo binfo = new FileInfo(Path.Combine(Engine.DataPath, $"assistant_backup"));
                        if (binfo.Exists)
                        {
                            if (binfo.LastWriteTimeUtc.AddDays(7) <= DateTime.UtcNow)
                            {
                                binfo.CopyTo(Path.Combine(Engine.DataPath, $"assistant_backup2"));
                                info.CopyTo(Path.Combine(Engine.DataPath, $"assistant_backup"));
                            }
                        }
                        else
                        {
                            info.CopyTo(binfo.FullName);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        internal static bool SaveProfile(string name = null)
        {
            AssistantGump gump = UOSObjects.Gump;
            if (gump == null)
                return false;
            SaveConfig(gump);
            SavePrivate(gump);
            string filename = name ?? gump.LastProfile;
            if (!string.IsNullOrWhiteSpace(filename))
            {
                try
                {
                    bool exists = false;
                    FileInfo info = new FileInfo(Path.Combine(Engine.ProfilePath, $"{filename}.xml"));
                    exists = info.Exists;
                    using (FileStream op = new FileStream(info.FullName, FileMode.OpenOrCreate))
                    {
                        using (XmlTextWriter xml = new XmlTextWriter(op, Encoding.UTF8) { Formatting = Formatting.Indented, IndentChar = ' ', Indentation = 1 })
                        {
                            xml.WriteStartDocument(true);
                            //BEGIN PROFILE
                            xml.WriteStartElement("profile");
                            for (int i = 0; i < Filter.List.Count; i++)
                            {
                                Filter f = Filter.List[i];
                                xml.WriteStartElement("data");
                                xml.WriteAttributeString("name", f.XmlName);
                                xml.WriteString(f.Enabled.ToString());
                                xml.WriteEndElement();
                            }
                            void writeData(string mname, string vval)
                            {
                                xml.WriteStartElement("data");
                                xml.WriteAttributeString("name", mname);
                                xml.WriteString(vval);
                                xml.WriteEndElement();
                            }
                            writeData("UseObjectsQueue", gump.UseObjectsQueue.ToString());
                            //writeData("UseTargetQueue", gump.UseTargetQueue.ToString());
                            writeData("ShowBandageTimerStart", gump.ShowBandageTimerStart.ToString());
                            writeData("ShowBandageTimerEnd", gump.ShowBandageTimerEnd.ToString());
                            writeData("ShowBandageTimerOverhead", gump.ShowBandageTimerOverhead.ToString());
                            writeData("ShowCorpseNames", gump.ShowCorpseNames.ToString());
                            //WriteData("OpenCorpses", gump.OpenCorpses.ToString());
                            //writeData("ShowMobileHits", gump.ShowMobileHits.ToString());
                            writeData("HandsBeforePotions", gump.HandsBeforePotions.ToString());
                            writeData("HandsBeforeCasting", gump.HandsBeforeCasting.ToString());
                            writeData("HighlightCurrentTarget", gump.HighlightCurrentTarget.ToString());
                            writeData("HighlightCurrentTargetHue", gump.HighlightCurrentTargetHue.ToString());
                            writeData("BlockInvalidHeal", gump.BlockInvalidHeal.ToString());
                            writeData("SharedTargetInAliasEnemy", gump.SharedTargetInAliasEnemy.ToString());
                            //WriteData("BoneCutter", gump.BoneCutter.ToString());
                            writeData("AutoMount", gump.AutoMount.ToString());
                            writeData("AutoBandage", gump.AutoBandage.ToString());
                            writeData("AutoBandageScale", gump.AutoBandageScale.ToString());
                            writeData("AutoBandageCount", gump.AutoBandageCount.ToString());
                            writeData("AutoBandageStart", gump.AutoBandageStart.ToString());
                            writeData("AutoBandageFormula", gump.AutoBandageFormula.ToString());
                            writeData("AutoBandageHidden", gump.AutoBandageHidden.ToString());
                            writeData("OpenDoors", gump.OpenDoors.ToString());
                            writeData("UseDoors", gump.UseDoors.ToString());
                            writeData("ShowMobileFlags", gump.ShowMobileFlags.ToString());
                            writeData("CountStealthSteps", gump.CountStealthSteps.ToString());
                            writeData("FriendsListOnly", gump.FriendsListOnly.ToString());
                            writeData("FriendsParty", gump.FriendsParty.ToString());
                            writeData("MoveConflictingItems", gump.MoveConflictingItems.ToString());
                            writeData("PreventDismount", gump.PreventDismount.ToString());
                            writeData("PreventAttackFriends", gump.PreventAttackFriends.ToString());
                            writeData("AutoSearchContainers", gump.AutoSearchContainers.ToString());
                            writeData("AutoAcceptParty", gump.AutoAcceptParty.ToString());

                            //WriteData("OpenCorpsesRange", $"0x{gump.OpenCorpsesRange:X2}");
                            //WriteData("UseObjectsLimit", $"0x{gump.UseObjectsLimit:X2}");
                            writeData("SmartTargetRange", gump.SmartTargetRange.ToString());
                            writeData("SmartTargetRangeValue", $"0x{gump.SmartTargetRangeValue:X2}");
                            //WriteData("FixedSeason", $"0x{gump.FixedSeason:X2}");
                            writeData("SmartTarget", $"0x{gump.SmartTarget:X2}");
                            writeData("TargetShare", $"0x{gump.EnemyTargetShare:X2}");
                            writeData("AutoBandageStartValue", $"0x{gump.AutoBandageStartValue:X2}");
                            writeData("SpellsTargetShare", $"0x{gump.SpellsTargetShare:X2}");
                            writeData("OpenDoorsMode", $"0x{gump.OpenDoorsMode:X2}");
                            //WriteData("OpenCorpsesMode", $"0x{gump.OpenCorpsesMode:X2}");
                            writeData("CustomCaptionMode", $"0x{gump.CustomCaptionMode:X2}");
                            writeData("GrabHotBag", $"0x{gump.GrabHotBag:X8}");
                            writeData("MountSerial", $"0x{gump.MountSerial:X8}");
                            writeData("BladeSerial", $"0x{gump.BladeSerial:X8}");
                            writeData("AutoBandageTarget", $"0x{gump.AutoBandageTarget:X8}");
                            writeData("AutoBandageDelay", $"{gump.AutoBandageDelay}");
                            writeData("ActionDelay", $"{gump.ActionDelay}");
                            writeData("DressTypeDefault", gump.TypeDress.ToString());
                            writeData("ReturnToParentScript", gump.ReturnToParentScript.ToString());
                            writeData("StartStopMacroMessages", gump.StartStopMacroMessages.ToString());

                            #region friends
                            if (FriendsManager.FriendDictionary.Count > 0)
                            {
                                xml.WriteStartElement("friends");
                                foreach (KeyValuePair<uint, string> kvp in FriendsManager.FriendDictionary)
                                {
                                    xml.WriteStartElement("friend");
                                    xml.WriteAttributeString("name", kvp.Value);
                                    xml.WriteString($"0x{kvp.Key:X}");
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion

                            #region macros
                            if (ScriptManager.MacroDictionary.Count > 0)
                            {
                                xml.WriteStartElement("macros");
                                foreach (KeyValuePair<string, HotKeyOpts> kvp in ScriptManager.MacroDictionary)
                                {
                                    xml.WriteStartElement("macro");
                                    xml.WriteAttributeString("loop", kvp.Value.Loop.ToString());
                                    xml.WriteAttributeString("name", kvp.Key);
                                    xml.WriteAttributeString("interrupt", (!kvp.Value.NoAutoInterrupt).ToString());
                                    xml.WriteString(kvp.Value.Macro.Replace('\n', ';'));
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion
                            
                            #region hotkeys
                            if (HotKeys.AllHotKeys.Count > 0)
                            {
                                xml.WriteStartElement("hotkeys");
                                foreach (KeyValuePair<uint, HotKeyOpts> kvp in HotKeys.AllHotKeys)
                                {
                                    xml.WriteStartElement("hotkey");
                                    xml.WriteAttributeString("action", kvp.Value.Action);
                                    xml.WriteAttributeString("key", $"0x{kvp.Key:X}");
                                    xml.WriteAttributeString("pass", kvp.Value.PassToUO.ToString());
                                    if (!string.IsNullOrEmpty(kvp.Value.Param))
                                    {
                                        xml.WriteAttributeString("param", kvp.Value.Param);
                                    }
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion

                            #region autoloot
                            xml.WriteStartElement("autoloot");

                            xml.WriteStartElement("enabled");
                            xml.WriteString(gump.AutoLoot.ToString());
                            xml.WriteEndElement();

                            xml.WriteStartElement("container");
                            xml.WriteString($"0x{gump.AutoLootContainer:X}");
                            xml.WriteEndElement();

                            xml.WriteStartElement("guards");
                            xml.WriteString(gump.NoAutoLootInGuards.ToString());
                            xml.WriteEndElement();

                            xml.WriteEndElement();
                            #endregion

                            #region dresslist
                            foreach (DressList dl in DressList.DressLists)
                            {
                                if (dl == null)
                                    continue;
                                xml.WriteStartElement("dresslist");
                                xml.WriteAttributeString("name", dl.Name);
                                if (SerialHelper.IsItem(dl.UndressBag))
                                    xml.WriteAttributeString("container", $"0x{dl.UndressBag:X8}");
                                foreach (KeyValuePair<Layer, DressItem> kvp in dl.LayerItems)
                                {
                                    xml.WriteStartElement("item");
                                    xml.WriteAttributeString("layer", ((int)kvp.Key).ToString());
                                    if (kvp.Value.ObjType > 0)
                                        xml.WriteAttributeString("type", $"0x{kvp.Value.ObjType:X4}");
                                    xml.WriteAttributeString("usetype", kvp.Value.UsesType.ToString());
                                    xml.WriteString($"0x{kvp.Value.Serial:X8}");
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion

                            #region organizers
                            if (Organizer.Organizers.Count > 0)
                            {
                                xml.WriteStartElement("organizer");
                                for (int i = 0; i < Organizer.Organizers.Count; ++i)
                                {
                                    Organizer org = Organizer.Organizers[i];
                                    if (org == null)
                                        continue;

                                    xml.WriteStartElement("group");
                                    xml.WriteAttributeString("source", $"0x{org.SourceCont:X}");
                                    xml.WriteAttributeString("stack", $"{org.Stack}");
                                    xml.WriteAttributeString("target", $"0x{org.TargetCont:X}");
                                    xml.WriteAttributeString("loop", $"{org.Loop}");
                                    xml.WriteAttributeString("complete", $"{org.Complete}");
                                    xml.WriteAttributeString("name", org.Name);
                                    if (org.Items.Count > 0)
                                    {
                                        foreach (ItemDisplay oi in org.Items)
                                        {
                                            xml.WriteStartElement("item");
                                            xml.WriteAttributeString("amount", $"0x{oi.Amount:X}");
                                            xml.WriteAttributeString("graphic", $"0x{oi.Graphic:X4}");
                                            xml.WriteAttributeString("hue", $"0x{oi.Hue:X4}");
                                            xml.WriteAttributeString("name", oi.Name);
                                            xml.WriteEndElement();
                                        }
                                    }
                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion

                            #region scavenger
                            xml.WriteStartElement("scavenger");
                            xml.WriteAttributeString("enabled", Scavenger.Enabled.ToString());
                            xml.WriteAttributeString("stack", Scavenger.Stack.ToString());
                            var scav = Scavenger.ItemIDsHues;
                            if (scav.Count > 0)
                            {
                                foreach (List<ItemDisplay> list in scav.Values)
                                {
                                    foreach (ItemDisplay id in list)
                                    {
                                        xml.WriteStartElement("scavenge");
                                        xml.WriteAttributeString("graphic", $"0x{id.Graphic:X4}");
                                        xml.WriteAttributeString("color", $"0x{id.Hue:X4}");
                                        xml.WriteAttributeString("enabled", id.Enabled.ToString());
                                        xml.WriteAttributeString("name", id.Name);
                                        xml.WriteEndElement();
                                    }
                                }
                            }
                            xml.WriteEndElement();
                            #endregion

                            #region vendors
                            if (Vendors.Buy.BuyList.Count > 0 || Vendors.Sell.SellList.Count > 0)
                            {
                                xml.WriteStartElement("vendors");
                                if (Vendors.Buy.BuySelected != null)
                                {
                                    xml.WriteStartElement("buystate");
                                    xml.WriteAttributeString("enabled", Vendors.Buy.BuySelected.Enabled.ToString());
                                    xml.WriteAttributeString("list", Vendors.Buy.BuySelected.Name);
                                    xml.WriteEndElement();
                                }
                                if (Vendors.Sell.SellSelected != null)
                                {
                                    xml.WriteStartElement("sellstate");
                                    xml.WriteAttributeString("enabled", Vendors.Sell.SellSelected.Enabled.ToString());
                                    xml.WriteAttributeString("list", Vendors.Sell.SellSelected.Name);
                                    xml.WriteEndElement();
                                }
                                foreach (var kvp in Vendors.Buy.BuyList)
                                {
                                    xml.WriteStartElement("shoppinglist");
                                    xml.WriteAttributeString("limit", kvp.Key.MaxAmount.ToString());
                                    xml.WriteAttributeString("name", kvp.Key.Name);
                                    xml.WriteAttributeString("type", "Buy");
                                    xml.WriteAttributeString("complete", kvp.Key.Complete.ToString());

                                    foreach (BuySellEntry bse in kvp.Value)
                                    {
                                        xml.WriteStartElement("item");
                                        xml.WriteAttributeString("graphic", $"0x{bse.ItemID:X}");
                                        xml.WriteAttributeString("amount", bse.Amount.ToString());
                                        xml.WriteEndElement();
                                    }

                                    xml.WriteEndElement();
                                }
                                foreach (var kvp in Vendors.Sell.SellList)
                                {
                                    xml.WriteStartElement("shoppinglist");
                                    xml.WriteAttributeString("limit", kvp.Key.MaxAmount.ToString());
                                    xml.WriteAttributeString("name", kvp.Key.Name);
                                    xml.WriteAttributeString("type", "Sell");
                                    xml.WriteAttributeString("complete", kvp.Key.Complete.ToString());

                                    foreach (BuySellEntry bse in kvp.Value)
                                    {
                                        xml.WriteStartElement("item");
                                        xml.WriteAttributeString("graphic", $"0x{bse.ItemID:X}");
                                        xml.WriteAttributeString("amount", bse.Amount.ToString());
                                        xml.WriteEndElement();
                                    }

                                    xml.WriteEndElement();
                                }
                                xml.WriteEndElement();
                            }
                            #endregion

                            #region search_exemptions
                            xml.WriteStartElement("autosearchexemptions");
                            foreach (string exempt in SearchExemption.ActivatedExemptions())
                            {
                                xml.WriteStartElement("exemption");
                                xml.WriteAttributeString("group", exempt);
                                xml.WriteEndElement();
                            }
                            xml.WriteEndElement();
                            #endregion
                            //END PROFILE

                            xml.WriteEndElement();
                        }
                    }
                    if (exists)
                    {
                        try
                        {
                            //keep the backup if they are not older than 7/14 days
                            FileInfo binfo = new FileInfo(Path.Combine(Engine.ProfilePath, $"{filename}_backup"));
                            if (binfo.Exists)
                            {
                                if (binfo.LastWriteTimeUtc.AddDays(7) <= DateTime.UtcNow)
                                {
                                    binfo.CopyTo(Path.Combine(Engine.ProfilePath, $"{filename}_backup2"));
                                    info.CopyTo(Path.Combine(Engine.ProfilePath, $"{filename}_backup"));
                                }
                            }
                            else
                            {
                                info.CopyTo(binfo.FullName);
                            }
                        }
                        catch { }
                    }
                }
                catch
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        #region SDL-VK_KEY_CONVERTER
        internal static readonly Dictionary<SDL.SDL_Keycode, (uint, string)> SDLkeyToVK = new Dictionary<SDL.SDL_Keycode, (uint, string)>()
            {
                {SDL.SDL_Keycode.SDLK_UNKNOWN, (0x00, "NONE")},
                //0x01 -> 0x03 left - right - control break on MOUSE
                {(SDL.SDL_Keycode)1, (0x04, "Middle Mouse")},//MOUSE MIDDLE
                {(SDL.SDL_Keycode)3, (0x0103, "X1 Mouse")},//mouse X1
                {(SDL.SDL_Keycode)4, (0x0104, "X2 Mouse")},//mouse X2
                {(SDL.SDL_Keycode)0x101, (0x0101, "Scroll UP")},//mouse scroll UP
                {(SDL.SDL_Keycode)0x102, (0x0102, "Scroll DOWN")},//mouse scroll DOWN
                {SDL.SDL_Keycode.SDLK_BACKSPACE, (0x08, "Backspace")},
                {SDL.SDL_Keycode.SDLK_TAB, (0x09, "Tab")},
                //0x0A -> 0x0B RESERVED
                {SDL.SDL_Keycode.SDLK_CLEAR, (0x0C, "Clear")},
                {SDL.SDL_Keycode.SDLK_KP_ENTER, (0x0D, "Return")},
                {SDL.SDL_Keycode.SDLK_RETURN, (0x0D, "Return")},
                //0x0E -> 0x0F UNDEFINED
                //0x10 -> Shift
                //0x11 -> CTRL
                //0x12 -> ALT
                {SDL.SDL_Keycode.SDLK_PAUSE, (0x13, "Pause")},
                {SDL.SDL_Keycode.SDLK_CAPSLOCK, (0x14, "CAPS")},
                //0x15 IME Kana/Hanguel mode
                //0x16 UNDEFINED
                //0x17 IME Junja mode
                //0x18 IME final mode
                //0x19 IME Hanja mode
                //0x1A UNDEFINED
                {SDL.SDL_Keycode.SDLK_ESCAPE, (0x1B, "Esc")},
                //0x1C IME Convert
                //0x1D IME NonConvert
                //0x1E IME Accept
                //0x1F IME mode change request
                {SDL.SDL_Keycode.SDLK_SPACE, (0x20, "Space")},
                {SDL.SDL_Keycode.SDLK_PAGEUP, (0x21, "Page UP")},
                {SDL.SDL_Keycode.SDLK_PAGEDOWN, (0x22, "Page DOWN")},
                {SDL.SDL_Keycode.SDLK_END, (0x23, "END")},
                {SDL.SDL_Keycode.SDLK_HOME, (0x24, "HOME")},
                {SDL.SDL_Keycode.SDLK_LEFT, (0x25, "Left")},
                {SDL.SDL_Keycode.SDLK_UP, (0x26, "Up")},
                {SDL.SDL_Keycode.SDLK_RIGHT, (0x27, "Right")},
                {SDL.SDL_Keycode.SDLK_DOWN, (0x28, "Down")},
                {SDL.SDL_Keycode.SDLK_SELECT, (0x29, "Select")},
                //0x2A Print KEY (no print SCREEN)
                {SDL.SDL_Keycode.SDLK_EXECUTE, (0x2B, "Execute")},
                {SDL.SDL_Keycode.SDLK_PRINTSCREEN, (0x2C, "Stamp")},
                {SDL.SDL_Keycode.SDLK_INSERT, (0x2D, "INS")},
                {SDL.SDL_Keycode.SDLK_DELETE, (0x2E, "DEL")},
                {SDL.SDL_Keycode.SDLK_HELP, (0x2F, "Help")},
                {SDL.SDL_Keycode.SDLK_0, (0x30, "0")},
                {SDL.SDL_Keycode.SDLK_1, (0x31, "1")},
                {SDL.SDL_Keycode.SDLK_2, (0x32, "2")},
                {SDL.SDL_Keycode.SDLK_3, (0x33, "3")},
                {SDL.SDL_Keycode.SDLK_4, (0x34, "4")},
                {SDL.SDL_Keycode.SDLK_5, (0x35, "5")},
                {SDL.SDL_Keycode.SDLK_6, (0x36, "6")},
                {SDL.SDL_Keycode.SDLK_7, (0x37, "7")},
                {SDL.SDL_Keycode.SDLK_8, (0x38, "8")},
                {SDL.SDL_Keycode.SDLK_9, (0x39, "9")},
                //0x40 UNDEFINED
                {SDL.SDL_Keycode.SDLK_a, (0x41, "A")},
                {SDL.SDL_Keycode.SDLK_b, (0x42, "B")},
                {SDL.SDL_Keycode.SDLK_c, (0x43, "C")},
                {SDL.SDL_Keycode.SDLK_d, (0x44, "D")},
                {SDL.SDL_Keycode.SDLK_e, (0x45, "E")},
                {SDL.SDL_Keycode.SDLK_f, (0x46, "F")},
                {SDL.SDL_Keycode.SDLK_g, (0x47, "G")},
                {SDL.SDL_Keycode.SDLK_h, (0x48, "H")},
                {SDL.SDL_Keycode.SDLK_i, (0x49, "I")},
                {SDL.SDL_Keycode.SDLK_j, (0x4A, "J")},
                {SDL.SDL_Keycode.SDLK_k, (0x4B, "K")},
                {SDL.SDL_Keycode.SDLK_l, (0x4C, "L")},
                {SDL.SDL_Keycode.SDLK_m, (0x4D, "M")},
                {SDL.SDL_Keycode.SDLK_n, (0x4E, "N")},
                {SDL.SDL_Keycode.SDLK_o, (0x4F, "O")},
                {SDL.SDL_Keycode.SDLK_p, (0x50, "P")},
                {SDL.SDL_Keycode.SDLK_q, (0x51, "Q")},
                {SDL.SDL_Keycode.SDLK_r, (0x52, "R")},
                {SDL.SDL_Keycode.SDLK_s, (0x53, "S")},
                {SDL.SDL_Keycode.SDLK_t, (0x54, "T")},
                {SDL.SDL_Keycode.SDLK_u, (0x55, "U")},
                {SDL.SDL_Keycode.SDLK_v, (0x56, "V")},
                {SDL.SDL_Keycode.SDLK_w, (0x57, "W")},
                {SDL.SDL_Keycode.SDLK_x, (0x58, "X")},
                {SDL.SDL_Keycode.SDLK_y, (0x59, "Y")},
                {SDL.SDL_Keycode.SDLK_z, (0x5A, "Z")},
                {SDL.SDL_Keycode.SDLK_LGUI, (0x5B, "Left Win")},
                {SDL.SDL_Keycode.SDLK_RGUI, (0x5C, "Right Win")},
                {SDL.SDL_Keycode.SDLK_APPLICATION, (0x5D, "Application")},
                //0x5E RESERVED
                {SDL.SDL_Keycode.SDLK_SLEEP, (0x5F, "Sleep")},
                {SDL.SDL_Keycode.SDLK_KP_0, (0x60, "KP 0")},
                {SDL.SDL_Keycode.SDLK_KP_1, (0x61, "KP 1")},
                {SDL.SDL_Keycode.SDLK_KP_2, (0x62, "KP 2")},
                {SDL.SDL_Keycode.SDLK_KP_3, (0x63, "KP 3")},
                {SDL.SDL_Keycode.SDLK_KP_4, (0x64, "KP 4")},
                {SDL.SDL_Keycode.SDLK_KP_5, (0x65, "KP 5")},
                {SDL.SDL_Keycode.SDLK_KP_6, (0x66, "KP 6")},
                {SDL.SDL_Keycode.SDLK_KP_7, (0x67, "KP 7")},
                {SDL.SDL_Keycode.SDLK_KP_8, (0x68, "KP 8")},
                {SDL.SDL_Keycode.SDLK_KP_9, (0x69, "KP 9")},
                {SDL.SDL_Keycode.SDLK_KP_MULTIPLY, (0x6A, "KP *")},
                {SDL.SDL_Keycode.SDLK_KP_PLUS, (0x6B, "KP +")},
                {SDL.SDL_Keycode.SDLK_SEPARATOR, (0x6C, "Separator")},
                {SDL.SDL_Keycode.SDLK_KP_MINUS, (0x6D, "KP -")},
                {SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR, (0x6E, "Decimal")},
                {SDL.SDL_Keycode.SDLK_KP_DIVIDE, (0x6F, "KP /")},
                {SDL.SDL_Keycode.SDLK_F1, (0x70, "F1")},
                {SDL.SDL_Keycode.SDLK_F2, (0x71, "F2")},
                {SDL.SDL_Keycode.SDLK_F3, (0x72, "F3")},
                {SDL.SDL_Keycode.SDLK_F4, (0x73, "F4")},
                {SDL.SDL_Keycode.SDLK_F5, (0x74, "F5")},
                {SDL.SDL_Keycode.SDLK_F6, (0x75, "F6")},
                {SDL.SDL_Keycode.SDLK_F7, (0x76, "F7")},
                {SDL.SDL_Keycode.SDLK_F8, (0x77, "F8")},
                {SDL.SDL_Keycode.SDLK_F9, (0x78, "F9")},
                {SDL.SDL_Keycode.SDLK_F10, (0x79, "F10")},
                {SDL.SDL_Keycode.SDLK_F11, (0x7A, "F11")},
                {SDL.SDL_Keycode.SDLK_F12, (0x7B, "F12")},
                {SDL.SDL_Keycode.SDLK_F13, (0x7C, "F13")},
                {SDL.SDL_Keycode.SDLK_F14, (0x7D, "F14")},
                {SDL.SDL_Keycode.SDLK_F15, (0x7E, "F15")},
                {SDL.SDL_Keycode.SDLK_F16, (0x7F, "F16")},
                {SDL.SDL_Keycode.SDLK_F17, (0x80, "F17")},
                {SDL.SDL_Keycode.SDLK_F18, (0x81, "F18")},
                {SDL.SDL_Keycode.SDLK_F19, (0x82, "F19")},
                {SDL.SDL_Keycode.SDLK_F20, (0x83, "F20")},
                {SDL.SDL_Keycode.SDLK_F21, (0x84, "F21")},
                {SDL.SDL_Keycode.SDLK_F22, (0x85, "F22")},
                {SDL.SDL_Keycode.SDLK_F23, (0x86, "F23")},
                {SDL.SDL_Keycode.SDLK_F24, (0x87, "F24")},
                //0x88 is UNASSIGNED
                {SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR, (0x90, "Num Lock")},
                {SDL.SDL_Keycode.SDLK_SCROLLLOCK, (0x91, "Scroll Lock")},
                //0x92 -> 0x96 OEM SPECIFIC
                //0x97 -> 0x9f UNASSIGNED
                {SDL.SDL_Keycode.SDLK_MENU, (0xA4, "Menu")},
                //0xA4 is Left Menu, while 0xA5 is Right Menu
                {SDL.SDL_Keycode.SDLK_AC_BACK, (0xA6, "App Back")},
                {SDL.SDL_Keycode.SDLK_AC_FORWARD, (0xA7, "App Forward")},
                {SDL.SDL_Keycode.SDLK_AC_REFRESH, (0xA8, "App Refresh")},
                {SDL.SDL_Keycode.SDLK_AC_STOP, (0xA9, "App Stop")},
                {SDL.SDL_Keycode.SDLK_AC_SEARCH, (0xAA, "App Search")},
                {SDL.SDL_Keycode.SDLK_AC_BOOKMARKS, (0xAB, "App Bookmark")},
                {SDL.SDL_Keycode.SDLK_AC_HOME, (0xAC, "App Home")},
                {SDL.SDL_Keycode.SDLK_MUTE, (0xAD, "Mute")},
                {SDL.SDL_Keycode.SDLK_VOLUMEDOWN, (0xAE, "Volume DOWN")},
                {SDL.SDL_Keycode.SDLK_VOLUMEUP, (0xAF, "Volume UP")},
                {SDL.SDL_Keycode.SDLK_AUDIONEXT, (0xB0, "Next Track")},
                {SDL.SDL_Keycode.SDLK_AUDIOPREV, (0xB1, "Prev Track")},
                {SDL.SDL_Keycode.SDLK_AUDIOSTOP, (0xB2, "Stop Track")},
                {SDL.SDL_Keycode.SDLK_AUDIOPLAY, (0xB3, "Play Track")},
                {SDL.SDL_Keycode.SDLK_MAIL, (0xB4, "Mail")},
                {SDL.SDL_Keycode.SDLK_MEDIASELECT, (0xB5, "Sel Track")},
                {SDL.SDL_Keycode.SDLK_APP1, (0xB6, "Lauch App1")},
                {SDL.SDL_Keycode.SDLK_APP2, (0xB7, "Lauch App2")},
                //0xB8 -> 0xB9 RESERVED
                {SDL.SDL_Keycode.SDLK_SEMICOLON, (0xBA, ";")},
                {SDL.SDL_Keycode.SDLK_COMMA, (0xBB, ",")},
                {SDL.SDL_Keycode.SDLK_PLUS, (0xBC, "+")},
                {SDL.SDL_Keycode.SDLK_MINUS, (0xBD, "-")},
                {SDL.SDL_Keycode.SDLK_PERIOD, (0xBE, ".")},
                {SDL.SDL_Keycode.SDLK_KP_PERIOD, (0xBE, ".")},
                {SDL.SDL_Keycode.SDLK_SLASH, (0xBF, "/")},
                //{SDL.SDL_Keycode.SDLK_BACKQUOTE, (0xC0, "`")},
                //0xC1 -> 0xD7 RESERVED
                //0xD8 -> 0xDA UNASSIGNED
                {SDL.SDL_Keycode.SDLK_LEFTBRACKET, (0xDB, "[")},
                {SDL.SDL_Keycode.SDLK_BACKSLASH, (0xDC, "\\")},
                {SDL.SDL_Keycode.SDLK_RIGHTBRACKET, (0xDD, "]")},
                //{SDL.SDL_Keycode.SDLK_QUOTE, (0xDE, "'")},
                // 0xDF - no use -- 0xE0 RESERVED -- 0xE1 OEM Specific
                {SDL.SDL_Keycode.SDLK_LESS, (0xE2, "<")}
            };

        internal static uint GetVKfromSDLmod(int mod)
        {
            uint vkmod = 0;
            AssistHotkeyBox.GetModType((SDL.SDL_Keymod)mod, out bool isshift, out bool isctrl, out bool isalt);
            if (isshift)
                vkmod |= 0x200;
            if (isctrl)
                vkmod |= 0x400;
            if (isalt)
                vkmod |= 0x800;
            return vkmod;
        }
        #endregion
        #region VK_KEY_CONVERTER
        internal static Dictionary<uint, SDL.SDL_Keycode> vkToSDLkey = new Dictionary<uint, SDL.SDL_Keycode>()
            {
                {0x00, SDL.SDL_Keycode.SDLK_UNKNOWN},
                {0x04, (SDL.SDL_Keycode)0x02},
                {0x103, (SDL.SDL_Keycode)0x03},
                {0x104, (SDL.SDL_Keycode)0x04},
                {0x101, (SDL.SDL_Keycode)0x101},
                {0x102, (SDL.SDL_Keycode)0x102},
                {0x08, SDL.SDL_Keycode.SDLK_BACKSPACE},
                {0x09, SDL.SDL_Keycode.SDLK_TAB},
                {0x0C, SDL.SDL_Keycode.SDLK_CLEAR},
                {0x0D, SDL.SDL_Keycode.SDLK_RETURN},
                {0x13, SDL.SDL_Keycode.SDLK_PAUSE},
                {0x14, SDL.SDL_Keycode.SDLK_CAPSLOCK},
                {0x1B, SDL.SDL_Keycode.SDLK_ESCAPE},
                {0x20, SDL.SDL_Keycode.SDLK_SPACE},
                {0x21, SDL.SDL_Keycode.SDLK_PAGEUP},
                {0x22, SDL.SDL_Keycode.SDLK_PAGEDOWN},
                {0x23, SDL.SDL_Keycode.SDLK_END},
                {0x24, SDL.SDL_Keycode.SDLK_HOME},
                {0x25, SDL.SDL_Keycode.SDLK_LEFT},
                {0x26, SDL.SDL_Keycode.SDLK_UP},
                {0x27, SDL.SDL_Keycode.SDLK_RIGHT},
                {0x28, SDL.SDL_Keycode.SDLK_DOWN},
                {0x29, SDL.SDL_Keycode.SDLK_SELECT},
                {0x2B, SDL.SDL_Keycode.SDLK_EXECUTE},
                {0x2C, SDL.SDL_Keycode.SDLK_PRINTSCREEN},
                {0x2D, SDL.SDL_Keycode.SDLK_INSERT},
                {0x2E, SDL.SDL_Keycode.SDLK_DELETE},
                {0x2F, SDL.SDL_Keycode.SDLK_HELP},
                {0x30, SDL.SDL_Keycode.SDLK_0},
                {0x31, SDL.SDL_Keycode.SDLK_1},
                {0x32, SDL.SDL_Keycode.SDLK_2},
                {0x33, SDL.SDL_Keycode.SDLK_3},
                {0x34, SDL.SDL_Keycode.SDLK_4},
                {0x35, SDL.SDL_Keycode.SDLK_5},
                {0x36, SDL.SDL_Keycode.SDLK_6},
                {0x37, SDL.SDL_Keycode.SDLK_7},
                {0x38, SDL.SDL_Keycode.SDLK_8},
                {0x39, SDL.SDL_Keycode.SDLK_9},
                {0x41, SDL.SDL_Keycode.SDLK_a},
                {0x42, SDL.SDL_Keycode.SDLK_b},
                {0x43, SDL.SDL_Keycode.SDLK_c},
                {0x44, SDL.SDL_Keycode.SDLK_d},
                {0x45, SDL.SDL_Keycode.SDLK_e},
                {0x46, SDL.SDL_Keycode.SDLK_f},
                {0x47, SDL.SDL_Keycode.SDLK_g},
                {0x48, SDL.SDL_Keycode.SDLK_h},
                {0x49, SDL.SDL_Keycode.SDLK_i},
                {0x4A, SDL.SDL_Keycode.SDLK_j},
                {0x4B, SDL.SDL_Keycode.SDLK_k},
                {0x4C, SDL.SDL_Keycode.SDLK_l},
                {0x4D, SDL.SDL_Keycode.SDLK_m},
                {0x4E, SDL.SDL_Keycode.SDLK_n},
                {0x4F, SDL.SDL_Keycode.SDLK_o},
                {0x50, SDL.SDL_Keycode.SDLK_p},
                {0x51, SDL.SDL_Keycode.SDLK_q},
                {0x52, SDL.SDL_Keycode.SDLK_r},
                {0x53, SDL.SDL_Keycode.SDLK_s},
                {0x54, SDL.SDL_Keycode.SDLK_t},
                {0x55, SDL.SDL_Keycode.SDLK_u},
                {0x56, SDL.SDL_Keycode.SDLK_v},
                {0x57, SDL.SDL_Keycode.SDLK_w},
                {0x58, SDL.SDL_Keycode.SDLK_x},
                {0x59, SDL.SDL_Keycode.SDLK_y},
                {0x5A, SDL.SDL_Keycode.SDLK_z},
                {0x5B, SDL.SDL_Keycode.SDLK_LGUI},
                {0x5C, SDL.SDL_Keycode.SDLK_RGUI},
                {0x5D, SDL.SDL_Keycode.SDLK_APPLICATION},
                {0x5F, SDL.SDL_Keycode.SDLK_SLEEP},
                {0x60, SDL.SDL_Keycode.SDLK_KP_0},
                {0x61, SDL.SDL_Keycode.SDLK_KP_1},
                {0x62, SDL.SDL_Keycode.SDLK_KP_2},
                {0x63, SDL.SDL_Keycode.SDLK_KP_3},
                {0x64, SDL.SDL_Keycode.SDLK_KP_4},
                {0x65, SDL.SDL_Keycode.SDLK_KP_5},
                {0x66, SDL.SDL_Keycode.SDLK_KP_6},
                {0x67, SDL.SDL_Keycode.SDLK_KP_7},
                {0x68, SDL.SDL_Keycode.SDLK_KP_8},
                {0x69, SDL.SDL_Keycode.SDLK_KP_9},
                {0x6A, SDL.SDL_Keycode.SDLK_KP_MULTIPLY},
                {0x6B, SDL.SDL_Keycode.SDLK_KP_PLUS},
                {0x6C, SDL.SDL_Keycode.SDLK_SEPARATOR},
                {0x6D, SDL.SDL_Keycode.SDLK_KP_MINUS},
                {0x6E, SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR},
                {0x6F, SDL.SDL_Keycode.SDLK_KP_DIVIDE},
                {0x70, SDL.SDL_Keycode.SDLK_F1},
                {0x71, SDL.SDL_Keycode.SDLK_F2},
                {0x72, SDL.SDL_Keycode.SDLK_F3},
                {0x73, SDL.SDL_Keycode.SDLK_F4},
                {0x74, SDL.SDL_Keycode.SDLK_F5},
                {0x75, SDL.SDL_Keycode.SDLK_F6},
                {0x76, SDL.SDL_Keycode.SDLK_F7},
                {0x77, SDL.SDL_Keycode.SDLK_F8},
                {0x78, SDL.SDL_Keycode.SDLK_F9},
                {0x79, SDL.SDL_Keycode.SDLK_F10},
                {0x7A, SDL.SDL_Keycode.SDLK_F11},
                {0x7B, SDL.SDL_Keycode.SDLK_F12},
                {0x7C, SDL.SDL_Keycode.SDLK_F13},
                {0x7D, SDL.SDL_Keycode.SDLK_F14},
                {0x7E, SDL.SDL_Keycode.SDLK_F15},
                {0x7F, SDL.SDL_Keycode.SDLK_F16},
                {0x80, SDL.SDL_Keycode.SDLK_F17},
                {0x81, SDL.SDL_Keycode.SDLK_F18},
                {0x82, SDL.SDL_Keycode.SDLK_F19},
                {0x83, SDL.SDL_Keycode.SDLK_F20},
                {0x84, SDL.SDL_Keycode.SDLK_F21},
                {0x85, SDL.SDL_Keycode.SDLK_F22},
                {0x86, SDL.SDL_Keycode.SDLK_F23},
                {0x87, SDL.SDL_Keycode.SDLK_F24},
                {0x90, SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR},
                {0x91, SDL.SDL_Keycode.SDLK_SCROLLLOCK},
                {0xA4, SDL.SDL_Keycode.SDLK_MENU},
                {0xA6, SDL.SDL_Keycode.SDLK_AC_BACK},
                {0xA7, SDL.SDL_Keycode.SDLK_AC_FORWARD},
                {0xA8, SDL.SDL_Keycode.SDLK_AC_REFRESH},
                {0xA9, SDL.SDL_Keycode.SDLK_AC_STOP},
                {0xAA, SDL.SDL_Keycode.SDLK_AC_SEARCH},
                {0xAB, SDL.SDL_Keycode.SDLK_AC_BOOKMARKS},
                {0xAC, SDL.SDL_Keycode.SDLK_AC_HOME},
                {0xAD, SDL.SDL_Keycode.SDLK_MUTE},
                {0xAE, SDL.SDL_Keycode.SDLK_VOLUMEDOWN},
                {0xAF, SDL.SDL_Keycode.SDLK_VOLUMEUP},
                {0xB0, SDL.SDL_Keycode.SDLK_AUDIONEXT},
                {0xB1, SDL.SDL_Keycode.SDLK_AUDIOPREV},
                {0xB2, SDL.SDL_Keycode.SDLK_AUDIOSTOP},
                {0xB3, SDL.SDL_Keycode.SDLK_AUDIOPLAY},
                {0xB4, SDL.SDL_Keycode.SDLK_MAIL},
                {0xB5, SDL.SDL_Keycode.SDLK_MEDIASELECT},
                {0xB6, SDL.SDL_Keycode.SDLK_APP1},
                {0xB7, SDL.SDL_Keycode.SDLK_APP2},
                {0xBA, SDL.SDL_Keycode.SDLK_SEMICOLON},
                {0xBB, SDL.SDL_Keycode.SDLK_COMMA},
                {0xBC, SDL.SDL_Keycode.SDLK_PLUS},
                {0xBD, SDL.SDL_Keycode.SDLK_MINUS},
                {0xBE, SDL.SDL_Keycode.SDLK_PERIOD},
                {0xBF, SDL.SDL_Keycode.SDLK_SLASH},
                //{0xC0, SDL.SDL_Keycode.SDLK_BACKQUOTE},
                {0xDB, SDL.SDL_Keycode.SDLK_LEFTBRACKET},
                {0xDC, SDL.SDL_Keycode.SDLK_BACKSLASH},
                {0xDD, SDL.SDL_Keycode.SDLK_RIGHTBRACKET},
                //{0xDE, SDL.SDL_Keycode.SDLK_QUOTE},
                {0xE2, SDL.SDL_Keycode.SDLK_LESS}
            };
        internal static Dictionary<uint, SDL.SDL_Keymod> vkmToSDLmod = new Dictionary<uint, SDL.SDL_Keymod>()
            {
                { 0x200, SDL.SDL_Keymod.KMOD_SHIFT },
                { 0x400, SDL.SDL_Keymod.KMOD_CTRL },
                { 0x600, SDL.SDL_Keymod.KMOD_CTRL | SDL.SDL_Keymod.KMOD_SHIFT },
                { 0x800, SDL.SDL_Keymod.KMOD_ALT },
                { 0xA00, SDL.SDL_Keymod.KMOD_ALT | SDL.SDL_Keymod.KMOD_SHIFT },
                { 0xC00, SDL.SDL_Keymod.KMOD_ALT | SDL.SDL_Keymod.KMOD_CTRL },
                { 0xE00, SDL.SDL_Keymod.KMOD_ALT | SDL.SDL_Keymod.KMOD_SHIFT | SDL.SDL_Keymod.KMOD_CTRL }
            };
        #endregion

        #region SubClassesUtilities
        private class ShoppingList
        {
            internal Dictionary<ushort, uint> GraphicAmount { get; }
            internal string Name { get; set; }
            private uint m_Limit = 999;
            internal uint Limit { get => m_Limit; set => m_Limit = Math.Min(999, value); }
            internal bool Complete { get; set; }

            internal ShoppingList(uint limit, string name, bool complete, Dictionary<ushort, uint> itemlist)
            {
                Name = name;
                Limit = limit;
                Complete = complete;
                GraphicAmount = itemlist;
            }
        }

        private class Counter
        {
            internal bool Image { get; set; }
            internal ushort Graphic { get; set; }
            internal ushort Color { get; set; }
            internal string Name { get; set; }
            internal bool Enabled { get; set; }
            internal Counter(bool image, ushort graphic, ushort color, string name, bool enabled)
            {
                Image = image;
                Graphic = graphic;
                Color = color;
                Name = name;
                Enabled = enabled;
            }
        }
        #endregion

        //this is a mere sample configuration
        #region defprof
        internal const string DEFPROF = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<profile>
 <data name=""FilterBardsMusic"">False</data>
 <data name=""FilterDogSounds"">False</data>
 <data name=""FilterCatSounds"">False</data>
 <data name=""FilterHorseSounds"">False</data>
 <data name=""FilterSheepSounds"">False</data>
 <data name=""FilterSpiritSpeakSound"">False</data>
 <data name=""FilterFizzleSound"">False</data>
 <data name=""FilterBackpackSounds"">False</data>
 <data name=""FilterDeerSounds"">False</data>
 <data name=""FilterCyclopTitanSounds"">False</data>
 <data name=""FilterBullSounds"">False</data>
 <data name=""FilterDragonSounds"">False</data>
 <data name=""FilterChickenSounds"">False</data>
 <data name=""FilterEmoteSounds"">False</data>
 <data name=""FilterDeath"">False</data>
 <data name=""FilterStaffItems"">False</data>
 <data name=""FilterSnoopingMessages"">False</data>
 <data name=""FilterTradeWindow"">False</data>
 <data name=""UseObjectsQueue"">True</data>
 <data name=""UseTargetQueue"">True</data>
 <data name=""ShowBandageTimerStart"">False</data>
 <data name=""ShowBandageTimerEnd"">False</data>
 <data name=""ShowBandageTimerOverhead"">False</data>
 <data name=""ShowCorpseNames"">False</data>
 <data name=""ShowMobileHits"">False</data>
 <data name=""HandsBeforePotions"">False</data>
 <data name=""HandsBeforeCasting"">False</data>
 <data name=""HighlightCurrentTarget"">False</data>
 <data name=""HighlightCurrentTargetHue"">0</data>
 <data name=""BlockInvalidHeal"">False</data>
 <data name=""AutoMount"">False</data>
 <data name=""AutoBandage"">False</data>
 <data name=""AutoBandageScale"">False</data>
 <data name=""AutoBandageCount"">False</data>
 <data name=""AutoBandageStart"">False</data>
 <data name=""AutoBandageFormula"">False</data>
 <data name=""AutoBandageHidden"">False</data>
 <data name=""OpenDoors"">True</data>
 <data name=""UseDoors"">True</data>
 <data name=""ShowMobileFlags"">False</data>
 <data name=""CountStealthSteps"">False</data>
 <data name=""FriendsListOnly"">False</data>
 <data name=""FriendsParty"">False</data>
 <data name=""MoveConflictingItems"">True</data>
 <data name=""PreventDismount"">False</data>
 <data name=""PreventAttackFriends"">False</data>
 <data name=""AutoSearchContainers"">True</data>
 <data name=""AutoAcceptParty"">False</data>
 <data name=""SmartTargetRange"">False</data>
 <data name=""SmartTargetRangeValue"">0x12</data>
 <data name=""SmartTarget"">0x00</data>
 <data name=""TargetShare"">0x00</data>
 <data name=""AutoBandageStartValue"">0x5F</data>
 <data name=""SpellsTargetShare"">0x00</data>
 <data name=""OpenDoorsMode"">0x00</data>
 <data name=""CustomCaptionMode"">0x01</data>
 <data name=""GrabHotBag"">0x00000000</data>
 <data name=""MountSerial"">0x00000000</data>
 <data name=""BladeSerial"">0xFFFFFFFF</data>
 <data name=""AutoBandageTarget"">0x00000010</data>
 <data name=""AutoBandageDelay"">2500</data>
 <data name=""ActionDelay"">150</data>
 <data name=""DressTypeDefault"">True</data>
 <data name=""ReturnToParentScript"">False</data>
 <macros>
 </macros>
 <hotkeys>
 </hotkeys>
 <autoloot>
  <enabled>True</enabled>
  <container>0x0</container>
  <guards>False</guards>
 </autoloot>
 <organizer>
 </organizer>
 <scavenger enabled=""False"" />
 <vendors>
 </vendors>
 <autosearchexemptions>
 </autosearchexemptions>
</profile>
";
        #endregion

        internal static void LoadProfile(AssistantGump gump, string filename)
        {
            if (!string.IsNullOrWhiteSpace(filename))
            {
                FileInfo info = new FileInfo(Path.Combine(Engine.ProfilePath, $"{filename}.xml"));
                if (!info.Exists)
                {
                    try
                    {
                        using (StreamWriter w = new StreamWriter(info.FullName, false))
                        {
                            w.Write(DEFPROF);
                            w.Flush();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"UOSteam -> Exception in LoadProfile: {e}");
                        return;
                    }
                }
                XmlDocument doc = new XmlDocument();
                bool retried = false;
                retry:
                try
                {
                    doc.Load(info.FullName);
                }
                catch (Exception e)
                {
                    if (!retried)
                    {
                        retried = true;
                        try
                        {
                            string str = File.ReadAllText(info.FullName);
                            StringBuilder sb = new StringBuilder();
                            using (StringReader sr = new StringReader(str))
                            {
                                string s;
                                while ((s = sr.ReadLine()) != null)
                                {
                                    if (s.Contains("</profile>"))
                                    {
                                        sb.AppendLine("</profile>");
                                        break;
                                    }
                                    else
                                        sb.AppendLine(s);
                                }
                            }
                            if (sb.Length > 10)
                            {
                                File.WriteAllText(info.FullName, sb.ToString());
                            }
                        }
                        catch
                        {
                            return;
                        }
                        goto retry;
                    }
                    Log.Warn($"Exception in LoadProfile: {e}");
                    return;
                }
                if (doc == null)
                {
                    return;
                }

                XmlElement root = doc["profile"];
                if (root == null)
                {
                    return;
                }

                foreach (XmlElement data in root.GetElementsByTagName("data"))
                {
                    string name = GetAttribute(data, "name");
                    if(name.StartsWith("Filter"))
                    {
                        for(int i = 0; i < Filter.List.Count; i++)
                        {
                            Filter f = Filter.List[i];
                            if (f.XmlName == name)
                            {
                                bool.TryParse(GetText(data, "False"), out bool enabled);
                                AssistantGump.FiltersCB[i].IsChecked = enabled;
                            }
                        }
                    }
                    else
                    {
                        switch(name.ToLower(Culture))
                        {
                            case "useobjectsqueue":
                                gump.UseObjectsQueue = GetBool(data, true);
                                break;
                            /*case "usetargetqueue":
                                gump.UseTargetQueue = GetBool(data, true);
                                break;*/
                            case "showbandagetimerstart":
                                gump.ShowBandageTimerStart = GetBool(data, false);
                                break;
                            case "showbandagetimerend":
                                gump.ShowBandageTimerEnd = GetBool(data, false);
                                break;
                            case "showbandagetimeroverhead":
                                gump.ShowBandageTimerOverhead = GetBool(data, false);
                                break;
                            case "showcorpsenames":
                                gump.ShowCorpseNames = GetBool(data, false);
                                break;
                            /*case "opencorpses":
                                gump.OpenCorpses = GetBool(data, false);
                                break;*/
                            /*case "showmobilehits":
                                gump.ShowMobileHits = GetBool(data, false);
                                break;*/
                            case "handsbeforepotions":
                                gump.HandsBeforePotions = GetBool(data, false);
                                break;
                            case "handsbeforecasting":
                                gump.HandsBeforeCasting = GetBool(data, false);
                                break;
                            case "highlightcurrenttarget":
                                gump.HighlightCurrentTarget = GetBool(data, false);
                                break;
                            case "highlightcurrenttargethue":
                                gump.HighlightCurrentTargetHue = GetUShort(data, 53);
                                break;
                            case "blockinvalidheal":
                                gump.BlockInvalidHeal = GetBool(data, false);
                                break;
                            case "sharedtargetinaliasenemy":
                                gump.SharedTargetInAliasEnemy = GetBool(data, false);
                                break;
                            /*case "bonecutter":
                                gump.BoneCutter = GetBool(data, false);
                                break;*/
                            case "automount":
                                gump.AutoMount = GetBool(data, false);
                                break;
                            case "autobandage":
                                gump.AutoBandage = GetBool(data, false);
                                break;
                            case "autobandagescale":
                                gump.AutoBandageScale = GetBool(data, false);
                                break;
                            case "autobandagecount":
                                gump.AutoBandageCount = GetBool(data, false);
                                break;
                            case "autobandagestart":
                                gump.AutoBandageStart = GetBool(data, false);
                                break;
                            case "autobandageformula":
                                gump.AutoBandageFormula = GetBool(data, false);
                                break;
                            case "autobandagehidden":
                                gump.AutoBandageHidden = GetBool(data, false);
                                break;
                            case "opendoors":
                                gump.OpenDoors = GetBool(data, false);
                                break;
                            case "usedoors":
                                gump.UseDoors = GetBool(data, false);
                                break;
                            case "showmobileflags":
                                gump.ShowMobileFlags = GetBool(data, true);
                                break;
                            case "countstealthsteps":
                                gump.CountStealthSteps = GetBool(data, true);
                                break;
                            case "friendslistonly":
                                gump.FriendsListOnly = GetBool(data, false);
                                break;
                            case "friendsparty":
                                gump.FriendsParty = GetBool(data, true);
                                break;
                            case "moveconflictingitems":
                                gump.MoveConflictingItems = GetBool(data, false);
                                break;
                            case "preventdismount":
                                gump.PreventDismount = GetBool(data, true);
                                break;
                            case "preventattackfriends":
                                gump.PreventAttackFriends = GetBool(data, true);
                                break;
                            case "autosearchcontainers":
                                gump.AutoSearchContainers = GetBool(data, true);
                                break;
                            case "autoacceptparty":
                                gump.AutoAcceptParty = GetBool(data, false);
                                break;
                            /*case "opencorpsesrange":
                                gump.OpenCorpsesRange = GetByte(data);
                                break;
                            case "useobjectslimit":
                                gump.UseObjectsLimit = GetByte(data);
                                break;*/
                            case "smarttargetrange":
                                gump.SmartTargetRange = GetBool(data, true);
                                break;
                            case "smarttargetrangevalue":
                                gump.SmartTargetRangeValue = GetByte(data);
                                break;
                            /*case "fixedseason":
                                gump.FixedSeason = GetByte(data);
                                break;*/
                            case "smarttarget":
                                gump.SmartTarget = GetByte(data);
                                break;
                            case "targetshare":
                                gump.EnemyTargetShare = GetByte(data);
                                break;
                            case "autobandagestartvalue":
                                gump.AutoBandageStartValue = GetByte(data);
                                break;
                            case "spellstargetshare":
                                gump.SpellsTargetShare = GetByte(data);
                                break;
                            case "opendoorsmode":
                                gump.OpenDoorsMode = GetByte(data);
                                break;
                            /*case "opencorpsesmode":
                                gump.OpenCorpsesMode = GetByte(data);
                                break;*/
                            case "customcaptionmode":
                                gump.CustomCaptionMode = GetByte(data);
                                break;
                            case "grabhotbag":
                                gump.GrabHotBag = GetUInt(data);
                                break;
                            case "mountserial":
                                gump.MountSerial = GetUInt(data);
                                break;
                            case "bladeserial":
                                gump.BladeSerial = GetUInt(data);
                                break;
                            case "autobandagetarget":
                                gump.AutoBandageTarget = GetUInt(data);
                                break;
                            case "autobandagedelay":
                                gump.AutoBandageDelay = GetUInt(data);
                                break;
                            case "actiondelay":
                                gump.ActionDelay = GetUInt(data);
                                break;
                            case "dresstypedefault":
                                gump.TypeDress = GetBool(data, false);
                                break;
                            case "returntoparentscript":
                                gump.ReturnToParentScript = GetBool(data, false);
                                break;
                            case "startstopmacromessages":
                                gump.StartStopMacroMessages = GetBool(data, false);
                                break;
                            default:
                                break;
                        }
                    }
                }

                XmlElement sub = root["friends"];
                FriendsManager.FriendDictionary.Clear();
                if (sub != null)
                {
                    foreach (XmlElement friend in sub.ChildNodes)//.GetElementsByTagName("friend"))
                    {
                        if (friend.Name != "friend")
                        {
                            continue;
                        }

                        string name = GetAttribute(friend, "name", "(unknown)");
                        uint serial = GetUInt(friend);
                        if (SerialHelper.IsMobile(serial))
                        {
                            FriendsManager.FriendDictionary[serial] = name;
                        }
                    }
                }
                gump.UpdateFriendListGump();

                sub = root["autoloot"];
                if (sub != null)
                {
                    foreach (XmlElement lootitem in sub.ChildNodes)
                    {
                        if (lootitem.Name == "enabled")
                        {
                            gump.AutoLoot = GetBool(lootitem, false);
                            continue;
                        }
                        else if(lootitem.Name == "container")
                        {
                            gump.AutoLootContainer = GetUInt(lootitem, 0);
                            continue;
                        }
                        else if(lootitem.Name == "guards")
                        {
                            gump.NoAutoLootInGuards = GetBool(lootitem, false);
                            continue;
                        }
                    }
                }
                else
                {
                    gump.AutoLoot = false;
                    gump.AutoLootContainer = 0;
                    gump.NoAutoLootInGuards = false;
                }

                sub = root["scavenger"];
                var dict = Scavenger.ItemIDsHues;
                dict.Clear();
                if (sub != null)
                {
                    bool active = GetAttributeBool(sub, "enabled", false), stack = GetAttributeBool(sub, "stack", false);
                    string name;
                    foreach (XmlElement counter in sub.ChildNodes)
                    {
                        if (counter.Name != "scavenge")
                        {
                            continue;
                        }

                        ushort graphic = (ushort)GetAttributeUInt(counter, "graphic");
                        short color = (short)GetAttributeUShort(counter, "color");
                        name = GetAttribute(counter, "name");
                        if (string.IsNullOrEmpty(name))
                            name = UOSObjects.GetDefaultItemName(graphic);
                        if (!dict.TryGetValue(graphic, out var list))
                            dict[(ushort)graphic] = list = new List<ItemDisplay>();
                         list.Add(new ItemDisplay(graphic, name, color, GetAttributeBool(counter, "enabled")));
                    }
                    gump.UpdateScavengerItemsGump();
                    gump.EnabledScavenger.IsChecked = active;
                    gump.StackOresAtFeet.IsChecked = stack;
                }
                SearchExemption.ClearAll();
                sub = root["autosearchexemptions"];
                List<string> exempts = new List<string>();
                if (sub != null)
                {
                    foreach (XmlElement exempt in sub.ChildNodes)
                    {
                        if (exempt.Name != "exemption")
                        {
                            continue;
                        }

                        string s = GetAttribute(exempt, "group", string.Empty);
                        exempts.Add(s);
                    }
                    if(exempts.Count > 0)
                    {
                        SearchExemption.AddExemptions(exempts);
                    }
                }
                sub = root["objects"];
                Dictionary<string, uint> objects = new Dictionary<string, uint>();
                if (sub != null)
                {
                    foreach (XmlElement obj in sub.ChildNodes)
                    {
                        if (obj.Name != "obj")
                        {
                            continue;
                        }

                        string s = GetAttribute(obj, "name");
                        if (!string.IsNullOrEmpty(s))
                        {
                            uint ser = GetUInt(obj);
                            if (SerialHelper.IsItem(ser))
                            {
                                objects[s] = ser;
                            }
                        }
                    }
                }
                sub = root["macros"];
                ScriptManager.MacroDictionary.Clear();
                if (sub != null)
                {
                    foreach (XmlElement key in sub.ChildNodes)
                    {
                        if (key.Name != "macro")
                        {
                            continue;
                        }

                        string name = GetAttribute(key, "name"), macro = GetText(key, null);
                        if (name != null && macro != null)
                        {
                            MacroAction ac = MacroAction.None;
                            if (!GetAttributeBool(key, "interrupt", true))
                                ac |= MacroAction.NoInterrupt;
                            if (GetAttributeBool(key, "loop"))
                                ac |= MacroAction.Loop;
                            if (macro.IndexOf('\r') >= 0)
                                macro = macro.Replace("\r", "");
                            ScriptManager.MacroDictionary.Add(name, new HotKeyOpts(ac, "macro.play", name) { Macro = macro.Replace(';', '\n') });
                        }
                    }
                }
                gump.UpdateMacroListGump();

                sub = root["vendors"];
                Vendors.Buy.BuyList.Clear();
                Vendors.Sell.SellList.Clear();
                if (sub != null)
                {
                    bool foundbuy = false, foundsell = false;
                    (string, bool) buystate = (null, false);
                    (string, bool) sellstate = (null, false);
                    foreach (XmlElement list in sub.ChildNodes)
                    {
                        if (list.Name == "buystate")
                        {
                            buystate.Item1 = GetAttribute(list, "list");
                            buystate.Item2 = GetAttributeBool(list, "enabled");
                            foundbuy = !string.IsNullOrEmpty(buystate.Item1);
                        }
                        else if(list.Name == "sellstate")
                        {
                            sellstate.Item1 = GetAttribute(list, "list");
                            sellstate.Item2 = GetAttributeBool(list, "enabled");
                            foundsell = !string.IsNullOrEmpty(sellstate.Item1);
                        }
                        if (foundbuy && foundsell)
                            break;
                    }
                    ushort bnum = 0, snum = 0;
                    foreach (XmlElement list in sub.ChildNodes)
                    {
                        if (list.Name != "shoppinglist")
                        {
                            continue;
                        }
                        string name = GetAttribute(list, "name");
                        string type = GetAttribute(list, "type");
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                        {
                            Vendors.IBuySell ibs = null;
                            List<BuySellEntry> bselist = null;
                            if (type == "Buy")
                            {
                                if (!Vendors.Buy.BuyList.Keys.Any(l => l.Name == name))
                                {
                                    ibs = new Vendors.Buy(bnum, name);
                                    Vendors.Buy.BuyList[ibs] = bselist = new List<BuySellEntry>();
                                    if (ibs.Name == buystate.Item1)
                                    {
                                        ibs.Selected = ibs;
                                        ibs.Enabled = buystate.Item2;
                                    }
                                    ++bnum;
                                }
                            }
                            else if (type == "Sell")
                            {
                                if (!Vendors.Sell.SellList.Keys.Any(l => l.Name == name))
                                {
                                    ibs = new Vendors.Sell(snum, name);
                                    Vendors.Sell.SellList[ibs] = bselist = new List<BuySellEntry>();
                                    if (ibs.Name == sellstate.Item1)
                                    {
                                        ibs.Selected = ibs;
                                        ibs.Enabled = sellstate.Item2;
                                    }
                                    ++snum;
                                }
                            }
                            if (ibs != null && bselist != null)
                            {
                                ibs.MaxAmount = GetAttributeUShort(list, "limit", 0x1);
                                ibs.Complete = GetAttributeBool(list, "complete");
                                foreach (XmlElement items in list.GetElementsByTagName("item"))
                                {
                                    ushort graphic = GetAttributeUShort(items, "graphic");
                                    if (graphic > 0)
                                    {
                                        bselist.Add(new BuySellEntry(graphic, Math.Max((ushort)1, Math.Min((ushort)999, GetAttributeUShort(items, "amount")))));
                                    }
                                }
                            }
                        }
                        gump.UpdateVendorsListGump();
                        gump.AddVendorsListToHotkeys();
                    }
                }

                ushort max = 0;
                XmlNodeList nodelist = root.GetElementsByTagName("dresslist");
                DressList.DressLists.Clear();
                if (nodelist.Count > 0)
                {
                    SortedDictionary<int, DressList> lists = new SortedDictionary<int, DressList>();
                    ushort num = 0;
                    foreach (XmlElement list in root.GetElementsByTagName("dresslist"))
                    {
                        string name = GetAttribute(list, "name");
                        uint undressbag = GetAttributeUInt(list, "container");
                        if (!string.IsNullOrWhiteSpace(name) && !lists.Values.Any(dl => dl.Name == name))
                        {
                            if (num <= ushort.MaxValue)
                            {
                                Dictionary<Layer, DressItem> dresses = new Dictionary<Layer, DressItem>();
                                foreach (XmlElement item in list.ChildNodes)
                                {
                                    if (item.Name != "item")
                                    {
                                        continue;
                                    }

                                    Layer layer = (Layer)GetAttributeUInt(item, "layer");
                                    ushort type = (ushort)GetAttributeUInt(item, "type");
                                    bool usetype = GetAttributeBool(item, "usetype", gump.TypeDress);
                                    if (layer > Layer.Invalid && layer < Layer.Mount && layer != Layer.Backpack && layer != Layer.Beard && layer != Layer.Hair)
                                    {
                                        uint serial = GetUInt(item);
                                        if (SerialHelper.IsItem(serial))//Max Item Value - MaxItemValue
                                        {
                                            if (type >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                                            {
                                                //we have an invalid loaded type? reset to zero, so we allow the subsystem to recalculate it on next dress action
                                                type = 0;
                                            }
                                            dresses[layer] = new DressItem(serial, type, usetype);
                                        }
                                    }
                                }
                                lists[num] = new DressList(name, dresses, undressbag);
                                if (num > max)
                                    max = num;
                            }
                            else
                                break;
                            ++num;
                        }
                    }
                    if (lists.Count > 0)
                    {
                        for (int i = 0; i <= max; ++i)
                        {
                            DressList.DressLists.Add(null);
                        }
                        foreach (KeyValuePair<int, DressList> kvp in lists)
                        {
                            DressList.DressLists[kvp.Key] = kvp.Value;
                        }
                    }
                }
                gump.UpdateDressListGump();
                gump.AddDressListToHotkeys();

                sub = root["organizer"];
                Organizer.Organizers.Clear();
                if (sub != null)
                {
                    max = 0;
                    SortedDictionary<int, Organizer> organizers = new SortedDictionary<int, Organizer>();
                    ushort num = 0;
                    foreach (XmlElement group in sub.ChildNodes)
                    {
                        if (group.Name != "group")
                        {
                            continue;
                        }

                        string name = GetAttribute(group, "name");
                        if (!string.IsNullOrWhiteSpace(name) && !organizers.Values.Any(o => o.Name == name))
                        {
                            if (num <= ushort.MaxValue)
                            {
                                bool stack = GetAttributeBool(group, "stack"), complete = GetAttributeBool(group, "complete"), loop = GetAttributeBool(group, "loop");
                                uint source = GetAttributeUInt(group, "source"), target = GetAttributeUInt(group, "target");
                                Organizer org = new Organizer(name) { Stack = stack, Loop = loop, Complete = complete, SourceCont = source, TargetCont = target };
                                foreach (XmlElement item in group.GetElementsByTagName("item"))
                                {
                                    ushort graphic = (ushort)GetAttributeUInt(item, "graphic");
                                    if (graphic > 0)
                                    {
                                        uint amt = GetAttributeUInt(item, "amount");
                                        short hue = (short)GetAttributeUShort(item, "hue", ushort.MaxValue);
                                        name = GetAttribute(item, "name");
                                        if (string.IsNullOrEmpty(name))
                                            name = UOSObjects.GetDefaultItemName(graphic);
                                        ItemDisplay oi = new ItemDisplay(graphic, name, hue) { Amount = amt };
                                        if (!org.Items.Contains(oi))
                                            org.Items.Add(oi);
                                    }
                                }
                                organizers[num] = org;
                                if (num > max)
                                    max = num;
                            }
                            else
                                break;
                            ++num;
                        }
                    }
                    if (organizers.Count > 0)
                    {
                        for (int i = 0; i <= max; ++i)
                        {
                            Organizer.Organizers.Add(null);
                        }
                        foreach (KeyValuePair<int, Organizer> kvp in organizers)
                        {
                            Organizer.Organizers[kvp.Key] = kvp.Value;
                        }
                    }
                }
                gump.UpdateOrganizerListGump();
                gump.AddOrganizerListToHotkeys();
                sub = root["hotkeys"];
                HotKeys.ClearHotkeys();
                if (sub != null)
                {
                    foreach (XmlElement key in sub.ChildNodes)
                    {
                        if (key.Name != "hotkey")
                        {
                            continue;
                        }

                        string action = GetAttribute(key, "action"), param = GetAttribute(key, "param");
                        uint keyval = GetAttributeUInt(key, "key");
                        if (action != null)//param could be missing
                        {
                            if (keyval > 0)
                            {
                                HotKeys.AddHotkey(keyval, new HotKeyOpts(GetAttributeBool(key, "pass") ? MacroAction.PassToUO : MacroAction.None, action, param), null, ref action, gump, true);
                            }
                        }
                    }
                }

                gump.OnProfileChanged();
            }
        }
    }
    #endregion
}
