using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class VersionHistory : NineSliceGump
    {
        private static string[] updateTexts =
        {
            // MobileUO: raw string literals not available in Unity
            @"
            /c[white][4.11.0]/cd
            # Misc
            - Autoloot now provides backups
            - Finish up info section in assistant window
            - Added ToggleMount to macros
            - Added Bandage Agent to global queue system with priority
            - Added hud options to imgui assistant
            - Added titlebar to imgui assistant
            - Added spell indicator to imgui assistant
            - Added journal filters to imgui assistant
            - Added friends list to imgui assistant
            - Added spell bar settings to imgui assistant
            - Allow healing friends with bandage agent
            - Added item database to keep track of all known items
            - Added dress agent to imgui assistant
            - Moved old assistant to new menu
            - Legion assistant and new style script manager reopen now
            - ImGui windows can be closed with right click now
            - Grid highlight selects best matching rule and show on item tooltips now(Can be toggled in options) ( <@397429006657519617> )

            # Legion Py
            - Better fake py api gen
            - Begin conversion to Pywrapper for controls
            - Added dropdown box to API
            - Added `.Destroy()` to Entity objects(Items, Mobiles)
            - Added new Modern UI gump option `API.CreateModernGump`
            - Added events to API.Events(See https://TazUO.org docs)
            - Fixed various Que spellings to Queue
            - Added `.GetAvailableDressOutfits()`
            - Added IsTree and Name to PyStatic ( <@188729541307531264> )
            - Added `GetAllGumps()` (Server side gumps)
            - Added Persistent Vars manager
            - Convert persistent vars to use SQL
            - `.GumpContains` should search better now
            - Added MatchingHighlightName and MatchesHighlight bool to PyItem

            # Bug fixes
            - Fix autoloot loading incorrect profile
            - Fixed a rare crash when pathfinding on the world map
            - Fixed a rare crash with nameplates
            - Fixed several potential rare crashes related to paperdolls
            - Fix close corpse macro to also close GridContainer corpses
            - Spiked whip now correctly shows abilities, thank you <@719588654913159190>
            - Audio switches when an audio device is removed/added
            - Grid container locked slots and autosort fixes
            - No more double input in new assistant window
            - Disable pooling for textboxes
            ",

            @"
            /c[white][4.10.0]/cd
            # Misc
            - Add clear journal button
            - Add VSync toggle
            - Async map loading for better performance
            - Add auto buy to new assistant window
            - Add Async map loading toggle
            - Default profiles to no reduced fps while inactive
            - Default profiles to use vsync
            - Add graphic to autoloot assistant window
            - Moved graphic replacement settings to new assistant window

            # Legion Py
            - Fixed Legion Py script stopping bug
            - Added API.StopRequested to check if your script should stop
            - Added several friends methods
            - Limit API.Pause to 0-30 seconds
            - Check for stop requests after API.Pause internally
            - Added API.Dress()
            - API.Pause now uses a cancellation token to end early when hitting stop on a script mid pause

            # Bug fixes
            - Fixed crash when world is somehow null
            - Fixed a rare crash on control hit test
            - Fix for grid container borders not properly hued
            - Fix for lag in high traffic areas
            - Fix for viewport not showing border
            - Fix for grid containers sorting when they shouldn't be
            - Fix background color on comparison tooltips
            ",

            @"
            /c[white][4.9.0]/cd
            ## Misc
            - Moved auto sell to new assistant window

            ## Legion Py
            - Changed how script stopping works slightly, it should indicate now if the script did not stop at least.

            ## Bugs
            - Fixed a couple crashes when re-logging quickly
            - Fixed a high cpu usage bug
            ",

            "/c[white][4.8.0]/cd\n" +
            @"
            # Misc
            - Vastly improved grid container performance
            - Better profiler info for testing and debugging
            - Remove need for cuoapi.dll(Mostly affects mac arm users)
            - Grid containers for corpses will now only show the items in there instead of the full grid
            - Added Top menu -> More -> Tools -> Retrieve gumps to find lost gumps(Outside your screen)
            - Added Lichtblitz color picker to color picker gumps
            - Single click coords on world map to copy to clipboard
            - Performance improvements for tiles, land, mobiles
            - Added new low fps reminder and `-syncfps` command to sync your fps limit to your monitor

            # Legion Py
            - Fix for buffexists method to make sure a valid string was put in and null checks
            - Add API.SetWarMode()
            - Add optional notoriety list to GetAllMobiles()
            - Add API.GlobalMsg() to send global chat messages

            # Bug fixes
            - Check if cuoapi is found/loaded, if not don't try to load plugins and disable pluginhost
            - Check if sdl event is null before processing
            - Fix buff icon text
            - Fixed a rare infobar crash
            "+
            "\n",

            "/c[white][4.7.0]/cd\n" +
            @"
            - Upgraded to latest FNA and SDL(Major changes on backend)
            - Added missing shortblade abilities
            - Autoloot is now in the new assistant menu

            ## Other
            - Better crash report information regarding OS and .Net frameworks
            - Better url handling for opening links in the browser
            - Added action delay config to autoloot menu
            - Added option to block dismount in warmode

            ## LegionPy
            - Added disable module cache option to menu
            - Minor back-end changes to python engine
            - Fix for legion py imports - nesci
            - Added optional graphic and distance to GetAllMobiles method
            - Changed Legion Py journals to use a py specific journal entry
            - Enforce max journal entries limitation on legion py journal entries(User-chosen max journal entries)

            - Fixed script recorder using API.Cast instead of CastSpell

            ## Bug fixes
            - Added some safety net for a rare crash while saving anchored gumps
            - Fix for ImGui display size occasional crash
            - Fix for a crash during python script execution
            - Fix for parrots on OSI - BCrowly
            - Potential high memory usage with a lot of UI controls open
            - Fix login gump still showing after logging in sometimes
            - Fix books not allowing you to type when you first open them
            " +
            "\n",

            "/c[white][4.5.0]/cd\n" +
            @"
            - Check for yellow hits before bandaging
            - Added some missing OSI mounts
            - Show other drives in file selector(When you navigate to root)
            - Grid highlights will re-check items when making changes to configs
            - Minor improvements to grid container ui interactions
            - Several new python API changes

            ## Bug fixes
            - Fix for in-game file selector without a parent folder
            - Improved nameplate jumping when moving and using avoid overlap
            - Fix chat command hue
            " +
            "\n",

            "/c[white][4.4.0]/cd\n" +
            @"
            - Improved draw times for mobiles
            - Better tooltip override handling in settings
            - Auto complete(press tab) while typing
            - Added a Journal Filter assistant feature([wiki](<https://github.com/PlayTazUO/TazUO/wiki/Journal-Filters>))
            - Reworked grid container highlight menu to be resizable
            - Can move grid highlights up and down the list now
            - You can now type unlimited length in the chat box, anything past 100 characters will be sent the next time you hit enter
            - Added a title bar status feature([wiki](<https://github.com/PlayTazUO/TazUO/wiki/Title-Bar-Status>))
            - Added auto bandaging ([wiki](<https://github.com/PlayTazUO/TazUO/wiki/Auto-Bandage>))
            - Added Mount and Setmount macros
            - Added Dress and Undress agent
            - Added friends list manager(For future use with api and other things)
            - Added organizer agent
            - Auto loot from grid highlight matches

            ### Minor changes
            - Removed old options gump
            - Some currency formatting for trade currencies(1,000,000)
            - Grid containers now show more than 125 items if the container has more items in it
            - Added a couple missing mounts from OSI
            - Added a new scroll bar control for future gump usage
            - Can now sort by name in grid containers(When names are available to the client)
            - Can now deselect items from the multi move gump( @nesci0471 )
            - Scavenger now works without moving, also checks if an item is movable first and has a delay before retrying the same item again.
            - Journal entries where the name and text are the same will only show the text(For example: a bird: a bird is now just a  bird)
            - World map now also loads map files from UO folder, and TazUO/Data/ServerName
            - Can now pathfind to objects instead of just surfaces( Lichz )
            - Added more gumps to hide hud feature
            - Zoom changes(Increase max, lower min, finer stepping for more precise control)
            - Nameplates have an optional no-overlap toggle
            - Autoloot now has import/export buttons
            - Grid highlighting will now user colors intead of hues
            - Bug fixes

            ### Python API Changes
            - Added `.Impassible` to PyGameObject
            - Fixed FindLayer returning an empty item when no item was found
            - Added `GetMultisAt`, `GetMultisInArea`, `PreTarget` `CancelPreTarget`
            - Fixed a bug where API calls that don't need to wait for a return value were still waiting
            " +
            "\n",

            "/c[white][4.3.0]/cd\n" +
            "- Added hud toggle to macros and assistant menu\n" +
            "- Added resync macro\n" +
            "- Added a new macro type to toggle forced house transparency\n" +
            "- Added some backend UI improvements for future use\n" +
            "- Added option to not make enemies gray\n" +
            "- Reopening sub container on login should work now\n" +
            "- Better profile and grid container save systems with backups\n" +
            "- Improved draw times slightly\n" +
            "- Discord item sharing\n" +
            "- Extended grid highlighting system\n" +
            "- Extended python api\n" +
            "- Other small changes" +
            "\n",
            "/c[white][4.0.0]/cd\n" +
            "- Prevent autoloot, move item queue from moving items while you are holding something.\n" +
            "- Change multi item move to use shared move item queue\n" +
            "- Prevent closing containers when changing facets\n" +
            "- Added Create macro button for legion scripts\n" +
            "- Potential Crash fix from CUO\n" +
            "- Python API changes\n" +
            "- Change how skill message frequency option works - fuzzlecutter\n" +
            "- Added an option to default to old container style with the option to switch to grid container style\n" +
            "- Added option to remove System prefix in journal\n" +
            "- Minor bug fixes\n" +
            "- Spellbar!\n" +
            "- Implemented Async networking\n",
            "/c[white][3.32.0]/cd\n" +
            "- Added simple progress bar control for Python API gumps.\n" +
            "- Generate user friendly html crash logs and open them on crash\n" +
            "- Some fixes for nearby corpse loot gump\n" +
            "- Very slightly increased minimum distance to start dragging a gump. Hopefully it should prevent accidental drags instead of clicks\n" +
            "- Nearby loot gump now stays open after relogging\n" +
            "- Moved some assistant-like options to their own menu.\n" +
            "- XML Gumps save locked status now(Ctrl + Alt + Click to lock)\n" +
            "- Python API created gumps will automatically close when the script stops, unless marked keep open." +
            "- Various bug fixes\n",
            "/c[white][3.31.0]/cd\n" +
            "- Fix for Python API EquipItem\n" +
            "- Fix for legion scripting useability commands\n" +
            "- Added basic scavenger agent(Uses autoloot)\n" +
            "- Nearby item gump and grid container quick loot now use move item queue\n" +
            "- Combine duplicate system messages\n" +
            "- Default visual spell indicator setup embedded now\n" +
            "- Various bug fixes\n",
            "/c[white][3.30.0]/cd\n" +
            "- Implementing Discord Social features\n" +
            "- Added more python API methods\n" +
            "- Better python API error handling\n" +
            "- Other minor bug fixes",
            "/c[white][3.29.0]/cd\n" +
            "- Moved tooltip override options into main menu\n" +
            "- Expanded Python API\n" +
            "- Prevent moving gumps outside the client window\n" +
            "- Reworked internal TTF fonts for better performance\n" +
            "- Fixed a bug in tooltips, likely not noticable but should be a significant performance boost while a tooltip is shown.\n" +
            "- Added -artbrowser and -animbrowser commands\n" +
            "- Added option to disable targeting grid containers directly\n" +
            "- Added some new fonts in\n" +
            "- Added option to disable controller\n" +
            "- Added some standard python libs in for python scripting\n" +
            "- Various bug fixes\n",
            "/c[white][3.28.0]/cd\n" +
            "- Added auto buy and sell agents\n" +
            "- Added Python scripting language support to legion scripting\n" +
            "- Added graphic replacement option\n" +
            "- Better item stacking in original containers while using grid containers \n" +
            "- Added a hotkeys page in options\n" +
            "- Improved autolooting\n",
            "/c[white][3.27.0]/cd\n" +
            "- Added forced tooltip option for pre-tooltip servers\n" +
            "- Added global scaling\n" +
            "- Add regex matching for autoloot\n" +
            "- Improved modern shop gump asthetics\n" +
            "- Counter bars can now be assigned spells\n" +
            "- Removed unused scripting system\n" +
            "- Added adjustable turn delay\n",
            "/c[white][3.26.1]/cd\n" +
            "- Fix for replygump command in legion scripting\n" +
            "/c[white][3.26.0]/cd\n" +
            "- Added optional regex to tooltip overrides\n" +
            "- Minor improvements in tooltip overrides\n" +
            "- Fix whitespace error during character creation\n" +
            "- Nearby item gump will close if moved, or 30 seconds has passed\n" +
            "/c[white][3.25.2]/cd\n" +
            "- Nearby item gump moved to macros\n" +
            "/c[white][3.25.1]/cd\n" +
            "- Added DPS meter\n" +
            "- Legion Scripting bug fix\n" +
            "/c[white][3.25.0]/cd\n" +
            "- Added the Legion scripting engine, see wiki for details\n" +
            "- Updated some common default settings that are usually used\n" +
            "- More controller QOL improvements\n" +
            "- Added tooltips for counterbar items\n" +
            "- Added a nearby items feature(See wiki for details)\n" +
            "- Various bug fixes\n" +
            "/c[white][3.24.2]/cd\n" +
            "- Fix Invisible items in Osi New Legacy Server\n" +
            "- Fix added more slots for show items layer in paperdoll \n" +
            "- Add scrollbar to cooldowns in options  \n" +
            "- Created progress bar for auto loot \n" +
            "- Fix skill progress bars \n" +
            "- Fix scroll area in autoloot options \n" +
            "- Create gump toggle mcros gumps for controller gameplay \n" +
            "- Save position of durability gump while in game \n" +
            "/c[white][3.24.2]/cd\n" +
            "- Fix Render Maps for Server Osi New Legacy\n" +
            "- Fix Ignore List \n" +
            "- Fix Big Tags in Weapons props \n" +
            "- Fix Pathfinding algorithm using Z more efficiently from ghzatomic \n" +
            "/c[white][3.24.1]/cd\n" +
            "- Fix for Modern Paperdoll not loading\n" +
            "- Fix Using Weapons Abilitys\n" +
            "/c[white][3.24.0]/cd\n" +
            "- Updated the algorithm for reading mul encryption\n" +
            "- Fix scrolling in the infobar manager\n" +
            "- Fix ignoring player in chat too\n" +
            "- Add auto avoid obstacles",
            "/c[white][3.23.2]/cd\n" +
            "- Fixed Disarm and Stun ability AOS",
            "/c[white][3.23.1]/cd\n" +
            "- Fixed Weird lines with nameplate",
            "/c[white][3.23.0]/cd\n" +
            "- Nameplate healthbar poison and invul/paralyzed colors from Elderwyn\n" +
            "- Target indiciator option from original client from Elderwyn\n" +
            "- Advanced skill gump improvements from Elderwyn",
            "/c[white][3.22.0]/cd\n" +
            "- Spell book icon fix\n" +
            "- Add option to add treasure maps as map markers instead of goto only\n" +
            "- Added the same option for SOS messages\n" +
            "- Fix text height for nameplates\n" +
            "- Added option to disable auto follow",
            "/c[white][3.21.4]/cd\n" +
            "- Various bug fixes\n" +
            "- Removed gump closing animation. Too many unforeseen issues with it.",
            "/c[white][3.21.3]/cd\n" +
            "- Changes to improve gump closing animations",
            "/c[white][3.21.2]/cd\n" +
            "- A bugfix release for 3.21 causing crashes",
            "/c[white][3.21.0]/cd\n" +
            "- A few bug fixes\n" +
            "- A few fixes from CUO\n" +
            "- Converted nameplates to use TTF fonts\n" +
            "- Added an available client commands gump\n" +
            "- World map alt lock now works, and middle mouse click will toggle freeview",
            "/c[white][3.20.0]/cd\n" +
            "- Being frozen wont cancel auto follow\n" +
            "- Fix from CUO for buffs\n" +
            "- Add ability to load custom spell definitions from an external file\n" +
            "- Customize the options gump via ui file\n" +
            "- Added saveposition tag for xml gumps\n" +
            "- Can now open multiple journals\n",
            "/c[white][3.19.0]/cd\n" +
            "- SOS Gump ID configurable in settings\n" +
            "- Added macro option to execute a client-side command\n" +
            "- Added a command doesn't exist message\n" +
            "- Follow party members on world map option\n" +
            "- Added option to override party member body hues\n" +
            "- Bug fix",
            "/c[white][3.18.0]/cd\n" +
            "- Added a language file that will contain UI text for easy language translations\n",
            "/c[white][3.17.0]/cd\n" +
            "- Added original paperdoll to customizable gump system\n" +
            "- Imroved script loading time",
            "/c[white][3.16.0]/cd\n" +
            "- Some small improvements for input boxes and the new option menu\n" +
            "- Added player position offset option in TazUO->Misc\n" +
            "- Fix for health indicator percentage\n" +
            "- Fix tooltip centered text\n" +
            "- Added a modding system almost identical to ServUO's script system\n" +
            "- Added macros to use items from your counter bar\n" +
            "- Simple auto loot improvements\n" +
            "- Hold ctrl and drop an item anywhere on the game window to drop it",
            "/c[white][3.15.0]/cd\n" +
            "- Mouse interaction for overhead text can be disabled\n" +
            "- Visable layers option added in Options->TazUO\n" +
            "- Added custom XML Gumps -> see wiki\n" +
            "- Added some controller support for movement and macros",
            "/c[white][3.14.0]/cd\n" +
            "- New options menu\n" +
            "- Small null ref bug fix\n" +
            "- No max width on item count text for smaller scaling\n" +
            "- Auto loot shift-click will no long work if you have shift for context menu or split stacks.\n" +
            "- Skill progress bars will save their position if you move them\n" +
            "- Changed skill progress bars to a queue instead of all showing at once\n" +
            "- Fix art png loading\n" +
            "- Added /c[green]-paperdoll/cd command\n" +
            "- Added an auto resync option under Options->TazUO->Misc\n" +
            "- Alt + Click paperdoll preview in modern paperdoll to copy a screenshot of it\n" +
            "- Added `both` option to auto close gumps range or dead\n" +
            "- Added shift + double click to advanced shop gump to buy/sell all of that item\n" +
            "- Added use one health bar for last attack option\n" +
            "- Added `-optlink` command",

            "\n\n/c[white]For further history please visit our discord."
        };

        private ScrollArea _scrollArea;
        private VBoxContainer _vBoxContainer;

        public VersionHistory(World world) : base(world, 0, 0, 400, 500, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, true, 200, 200)
        {
            CanCloseWithRightClick = true;
            CanMove = true;

            Build();

            CenterXInViewPort();
            CenterYInViewPort();
        }

        private void Build()
        {
            Clear();

            Positioner pos = new(13, 13);

            Add(pos.Position(TextBox.GetOne(Language.Instance.TazuoVersionHistory, TrueTypeLoader.EMBEDDED_FONT, 30, Color.White, TextBox.RTLOptions.DefaultCentered(Width))));

            Add(pos.Position(TextBox.GetOne(Language.Instance.CurrentVersion + CUOEnviroment.Version, TrueTypeLoader.EMBEDDED_FONT, 20, Color.Orange, TextBox.RTLOptions.DefaultCentered(Width))));

            _scrollArea = new ScrollArea(0, 0, Width - 26, Height - (pos.LastY + pos.LastHeight) - 32, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways };
            _vBoxContainer = new VBoxContainer(_scrollArea.Width - _scrollArea.ScrollBarWidth());
            _scrollArea.Add(_vBoxContainer);

            foreach (string s in updateTexts)
            {
                _vBoxContainer.Add(TextBox.GetOne(s, TrueTypeLoader.EMBEDDED_FONT, 15, Color.Orange, TextBox.RTLOptions.Default(_scrollArea.Width - _scrollArea.ScrollBarWidth())));
            }

            Add(pos.Position(_scrollArea));

            Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUOWiki, "https://github.com/PlayTazUO/TazUO/wiki", Color.Orange, 15), 25, Height - 20));
            Add(pos.PositionExact(new HttpClickableLink(Language.Instance.TazUODiscord, "https://discord.gg/QvqzkB95G4", Color.Orange, 15), Width - 110, Height - 20));
        }

        protected override void OnResize(int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            base.OnResize(oldWidth, oldHeight, newWidth, newHeight);
            Build();
        }
    }
}
