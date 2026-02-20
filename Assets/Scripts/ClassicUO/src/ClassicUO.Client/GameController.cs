// Sascha Puligheddu mods inside

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
// MobileUO: import
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Network.Encryption;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PreferenceEnums;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using static SDL2.SDL;
using Lean.Touch;

namespace ClassicUO
{
    internal unsafe class GameController : Microsoft.Xna.Framework.Game
    {

        private bool _ignoreNextTextInput;
        private readonly float[] _intervalFixedUpdate = new float[2];
        private double _statisticsTimer;
        private double _totalElapsed, _currentFpsTime;
        private uint _totalFrames;
        private UltimaBatcher2D _uoSpriteBatch;
        private bool _suppressedDraw;
        private Texture2D _background;
        private bool _pluginsInitialized = false;

        // MobileUO: Batcher and TouchScreenKeyboard
        public UltimaBatcher2D Batcher => _uoSpriteBatch;
        public static UnityEngine.TouchScreenKeyboard TouchScreenKeyboard;

        public GameController(IPluginHost pluginHost)
        {
            GraphicManager = new GraphicsDeviceManager(this);

            GraphicManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

            Window.ClientSizeChanged += WindowOnClientSizeChanged;
            Window.AllowUserResizing = true;
            Window.Title = $"ClassicUO - {CUOEnviroment.Version}";
            IsMouseVisible = Settings.GlobalSettings.RunMouseInASeparateThread;

            IsFixedTimeStep = false; // Settings.GlobalSettings.FixedTimeStep;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / 250.0);
            InactiveSleepTime = TimeSpan.Zero;
            foreach(SDL_Keycode sdl in _keyCodeEnumValues.Values)
            {
                _RepeatedKeys[sdl] = 0;
            }
            //DEBUGGING CODE FOR KEYCODES
            /*string[] arr = Enum.GetNames(typeof(SDL_Keycode));
            SDL_Keycode[] skc = (SDL_Keycode[])Enum.GetValues(typeof(SDL_Keycode));
            for(int i = 0; i < arr.Length; ++i)
            {
                string s = arr[i].Remove(0, 5).Replace("KP_", "Keypad");
                if (int.TryParse(s, out _))
                    s = "Alpha" + s;
                if (Enum.TryParse<UnityEngine.KeyCode>(s, true, out UnityEngine.KeyCode ukc))
                {
                    _keyCodeEnumValuesNew.Add(ukc, skc[i]);
                }
            }
            using (StreamWriter sw = new StreamWriter("ukc_skc.txt", false))
            {
                sw.WriteLine("        private readonly Dictionary<UnityEngine.KeyCode, SDL_Keycode> _keyCodeEnumValuesNew = new Dictionary<UnityEngine.KeyCode, SDL_Keycode>()");
                sw.WriteLine("        {");
                foreach (var kvp in _keyCodeEnumValuesNew)
                {
                    //private readonly Dictionary<UnityEngine.KeyCode, SDL_Keycode> _keyCodeEnumValuesNew = new Dictionary<UnityEngine.KeyCode, SDL_Keycode>();
                    sw.WriteLine($"            {{ UnityEngine.KeyCode.{kvp.Key.ToString()}, SDL_Keycode.{kvp.Value.ToString()} }},");
                }
                sw.WriteLine("        };");
            }*/
        }

        public Scene Scene { get; private set; }
        public AudioManager Audio { get; private set; }
        public UltimaOnline UO { get; } = new UltimaOnline();
        public IPluginHost PluginHost { get; private set; }
        public GraphicsDeviceManager GraphicManager { get; }
        public readonly uint[] FrameDelay = new uint[2];

        private readonly List<(uint, Action)> _queuedActions = new ();

        public void EnqueueAction(uint time, Action action)
        {
            _queuedActions.Add((Time.Ticks + time, action));
        }

        protected override void Initialize()
        {
            // MobileUO: commented out
            //if (GraphicManager.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
            //{
            //    GraphicManager.GraphicsProfile = GraphicsProfile.HiDef;
            //}

            GraphicManager.ApplyChanges();

            SetRefreshRate(Settings.GlobalSettings.FPS);
            _uoSpriteBatch = new UltimaBatcher2D(GraphicsDevice);

            //UNNECESSARY CODE IN UNITY, DON'T EMULATE
            /*_filter = HandleSdlEvent;
            SDL_SetEventFilter(_filter, IntPtr.Zero);*/

            base.Initialize();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Fonts.Initialize(GraphicsDevice);
            SolidColorTextureCache.Initialize(GraphicsDevice);
            Audio = new AudioManager();

            // MobileUO: commented out
            //var bytes = Loader.GetBackgroundImage().ToArray();
            //using var ms = new MemoryStream(bytes);
            //_background = Texture2D.FromStream(GraphicsDevice, ms);

#if false
            SetScene(new MainScene(this));
#else
            UO.Load(this);
            Audio.Initialize();
            // TODO: temporary fix to avoid crash when laoding plugins
            Settings.GlobalSettings.Encryption = (byte) NetClient.Socket.Load(UO.FileManager.Version, (EncryptionType) Settings.GlobalSettings.Encryption);

            Log.Trace("Loading plugins...");
            PluginHost?.Initialize();

            foreach (string p in Settings.GlobalSettings.Plugins)
            {
                Plugin.Create(p);
            }
            _pluginsInitialized = true;

            Log.Trace("Done!");

            SetScene(new LoginScene(UO.World));
#endif
            SetWindowPositionBySettings();
        }

        // MobileUO: makes public
        public override void UnloadContent()
        {
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out int bottom, out int right);
            Settings.GlobalSettings.WindowPosition = new Point(Math.Max(0, Window.ClientBounds.X - left), Math.Max(0, Window.ClientBounds.Y - top));

            Audio?.StopMusic();
            Settings.GlobalSettings.Save();
            Plugin.OnClosing();

            UO.Unload();

            // MobileUO: NOTE: My dispose related changes, see if they're still necessary
            // MobileUO: TODO: hueSamplers were moved to Client.cs
            //_hueSamplers[0]?.Dispose();
            //_hueSamplers[0] = null;
            //_hueSamplers[1]?.Dispose();
            //_hueSamplers[1] = null;
            Scene?.Dispose();
            //AuraManager.Dispose();
            UIManager.Dispose();
            SolidColorTextureCache.Dispose();
            RenderedText.Dispose();

            // MobileUO: NOTE: We force the sockets to disconnect in case they haven't already been disposed
            //This is good practice since the Client can be quit while the socket is still active
            // MobileUO: TODO: version 1.0.0.0 drops IsDisposed property
            //if (NetClient.Socket.IsDisposed == false)
            //{
            //    NetClient.Socket.Disconnect();
            //}
            LeanTouch.OnFingerTap -= LeanTouch_OnFingerTap;
            LeanTouch.OnFingerDown -= LeanTouch_OnFingerDown;
            LeanTouch.OnFingerUp -= LeanTouch_OnFingerUp;
            LeanTouch.OnFingerExpired -= LeanTouch_OnFingerExpired;
            LeanTouch.OnGesture -= LeanTouch_OnGesture;
            _Init = false;

            base.UnloadContent();
        }

        public void SetWindowTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
#if DEV_BUILD
                Window.Title = $"ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"ClassicUO - {CUOEnviroment.Version}";
#endif
            }
            else
            {
#if DEV_BUILD
                Window.Title = $"{title} - ClassicUO [dev] - {CUOEnviroment.Version}";
#else
                Window.Title = $"{title} - ClassicUO - {CUOEnviroment.Version}";
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetScene<T>() where T : Scene
        {
            return Scene as T;
        }

        public void SetScene(Scene scene)
        {
            Scene?.Dispose();
            Scene = scene;

            // MobileUO: NOTE: Added this to be able to react to scene changes, mainly for calculating render scale factor
            Client.InvokeSceneChanged();

            Scene?.Load();
            if (Scene is not GameScene)
            {
                int disp = Math.Max(0, (int)((UnityEngine.Screen.width / Client.Game.Batcher.scale) - 640) / 2);
                foreach (Gump g in UIManager.Gumps)
                {
                    g.X = disp;
                }
            }
        }

        public void SetVSync(bool value)
        {
            GraphicManager.SynchronizeWithVerticalRetrace = value;
        }

        public void SetRefreshRate(int rate)
        {
            if (rate < Constants.MIN_FPS)
            {
                rate = Constants.MIN_FPS;
            }
            else if (rate > Constants.MAX_FPS)
            {
                rate = Constants.MAX_FPS;
            }

            float frameDelay;

            if (rate == Constants.MIN_FPS)
            {
                // The "real" UO framerate is 12.5. Treat "12" as "12.5" to match.
                frameDelay = 80;
            }
            else
            {
                frameDelay = 1000.0f / rate;
            }

            FrameDelay[0] = FrameDelay[1] = (uint)frameDelay;
            FrameDelay[1] = FrameDelay[1] >> 1;

            Settings.GlobalSettings.FPS = rate;

            _intervalFixedUpdate[0] = frameDelay;
            _intervalFixedUpdate[1] = 217; // 5 FPS

            // MobileUO: Use this frame rate if we aren't capped by MobileUO FPS settings
            if(UserPreferences.TargetFrameRate.CurrentValue == (int)TargetFrameRates.InGameFPS)
            {
                UnityEngine.Application.targetFrameRate = rate;
            }
        }

        private void SetWindowPosition(int x, int y)
        {
            SDL_SetWindowPosition(Window.Handle, x, y);
        }

        public void SetWindowSize(int width, int height)
        {
            //width = (int) ((double) width * Client.Game.GraphicManager.PreferredBackBufferWidth / Client.Game.Window.ClientBounds.Width);
            //height = (int) ((double) height * Client.Game.GraphicManager.PreferredBackBufferHeight / Client.Game.Window.ClientBounds.Height);

            /*if (CUOEnviroment.IsHighDPI)
            {
                width *= 2;
                height *= 2;
            }
            */

            GraphicManager.PreferredBackBufferWidth = width;
            GraphicManager.PreferredBackBufferHeight = height;
            GraphicManager.ApplyChanges();
        }

        public void SetWindowBorderless(bool borderless)
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0 && borderless)
            {
                return;
            }

            if ((flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) == 0 && !borderless)
            {
                return;
            }

            SDL_SetWindowBordered(
                Window.Handle,
                borderless ? SDL_bool.SDL_FALSE : SDL_bool.SDL_TRUE
            );
            SDL_GetCurrentDisplayMode(
                SDL_GetWindowDisplayIndex(Window.Handle),
                out SDL_DisplayMode displayMode
            );

            int width = displayMode.w;
            int height = displayMode.h;

            if (borderless)
            {
                SetWindowSize(width, height);
                SDL_GetDisplayUsableBounds(
                    SDL_GetWindowDisplayIndex(Window.Handle),
                    out SDL_Rect rect
                );
                SDL_SetWindowPosition(Window.Handle, rect.x, rect.y);
            }
            else
            {
                SDL_GetWindowBordersSize(Window.Handle, out int top, out _, out int bottom, out _);

                SetWindowSize(width, height - (top - bottom));
                SetWindowPositionBySettings();
            }

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }

        public void MaximizeWindow()
        {
            SDL_MaximizeWindow(Window.Handle);

            GraphicManager.PreferredBackBufferWidth = Client.Game.Window.ClientBounds.Width;
            GraphicManager.PreferredBackBufferHeight = Client.Game.Window.ClientBounds.Height;
            GraphicManager.ApplyChanges();
        }

        public bool IsWindowMaximized()
        {
            SDL_WindowFlags flags = (SDL_WindowFlags)SDL_GetWindowFlags(Window.Handle);

            return (flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
        }

        public void RestoreWindow()
        {
            SDL_RestoreWindow(Window.Handle);
        }

        public void SetWindowPositionBySettings()
        {
            SDL_GetWindowBordersSize(Window.Handle, out int top, out int left, out _, out _);

            if (Settings.GlobalSettings.WindowPosition.HasValue)
            {
                int x = left + Settings.GlobalSettings.WindowPosition.Value.X;
                int y = top + Settings.GlobalSettings.WindowPosition.Value.Y;
                x = Math.Max(0, x);
                y = Math.Max(0, y);

                SetWindowPosition(x, y);
            }
        }

        private static bool _Init = false;
        protected override void Update(GameTime gameTime)
        {
            if (Profiler.InContext("OutOfContext"))
            {
                Profiler.ExitContext("OutOfContext");
            }

            UnityInputUpdate(Time.Ticks = (uint)gameTime.TotalGameTime.TotalMilliseconds);
            Time.Delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (IsUsingFingers)
            {
                if (!_Init)
                {
                    _Init = true;
                    LeanTouch.OnFingerTap += LeanTouch_OnFingerTap;
                    LeanTouch.OnFingerDown += LeanTouch_OnFingerDown;
                    LeanTouch.OnFingerUp += LeanTouch_OnFingerUp;
                    LeanTouch.OnFingerExpired += LeanTouch_OnFingerExpired;
                    LeanTouch.OnGesture += LeanTouch_OnGesture;

                    //LeanTouch.OnFingerSwipe += LeanTouch_OnFingerSwipe;
                    //LeanTouch.OnFingerSet += LeanTouch_OnFingerSwipe;
                }
                //Settings.GlobalSettings.RunMouseInASeparateThread = !TargetManager.IsTargeting;
            }
            else
            {
                if (_Init)
                {
                    _Init = false;
                    LeanTouch.OnFingerTap -= LeanTouch_OnFingerTap;
                    LeanTouch.OnFingerDown -= LeanTouch_OnFingerDown;
                    LeanTouch.OnFingerUp -= LeanTouch_OnFingerUp;
                    LeanTouch.OnFingerExpired -= LeanTouch_OnFingerExpired;
                    LeanTouch.OnGesture -= LeanTouch_OnGesture;

                    //LeanTouch.OnFingerSwipe -= LeanTouch_OnFingerSwipe;
                    //LeanTouch.OnFingerSet -= LeanTouch_OnFingerSwipe;
                    //Settings.GlobalSettings.RunMouseInASeparateThread = true;
                }
                UnityMouseUpdate();
            }


            var data = NetClient.Socket.CollectAvailableData();
            var packetsCount = PacketHandlers.Handler.ParsePackets(NetClient.Socket, UO.World, data);

            NetClient.Socket.Statistics.TotalPacketsReceived += (uint)packetsCount;
            NetClient.Socket.Flush();
            Plugin.Tick();

            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                Profiler.EnterContext("Update");
                Scene.Update();
                Profiler.ExitContext("Update");
            }

            UIManager.Update();

            _totalElapsed += gameTime.ElapsedGameTime.TotalMilliseconds;
            _currentFpsTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_currentFpsTime >= 1000)
            {
                CUOEnviroment.CurrentRefreshRate = _totalFrames;

                _totalFrames = 0;
                _currentFpsTime = 0;
            }

            double x = _intervalFixedUpdate[
                !IsActive
                && ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.ReduceFPSWhenInactive
                    ? 1
                    : 0
            ];
            _suppressedDraw = false;

            if (_totalElapsed > x)
            {
                _totalElapsed %= x;
            }
            else
            {
                _suppressedDraw = true;
                SuppressDraw();

                if (!gameTime.IsRunningSlowly)
                {
                    Thread.Sleep(1);
                }
            }

            UO.GameCursor?.Update();
            Audio?.Update();


            for (var i = _queuedActions.Count - 1; i >= 0; i--)
            {
                (var time, var fn) = _queuedActions[i];

                if (Time.Ticks > time)
                {
                    fn();
                    _queuedActions.RemoveAt(i);
                    break;
                }
            }

             base.Update(gameTime);
        }

        private void LeanTouch_OnGesture(List<LeanFinger> objs)
        {
            if (objs.Count > 0)
            {
                LeanFinger obj = objs[0];
                if (obj.StartedOverGui)
                    return;

                if (obj.ScreenPosition != obj.LastScreenPosition)
                {
                    if (!Mouse.IsDragging)
                    {
                        obj.Age += LeanTouch.CurrentTapThreshold;
                        UnityFingersRefresh(true);
                    }
                    else
                    {
                        UnityFingersRefresh(false);
                        if (!Scene.OnMouseDragging())
                            UIManager.OnMouseDragging();
                    }
                }
            }
        }

        private static bool _AlreadyDown = false;
        private void LeanTouch_OnFingerUp(LeanFinger obj)
        {
            if (obj.StartedOverGui)
                return;
            _AlreadyDown = false;
            var list = LeanTouch.GetFingers(true, false);
            if (list.Count == 1)
            {
                LeftMouse(false);
            }
        }

        private void LeanTouch_OnFingerDown(LeanFinger obj)
        {
            if (obj.StartedOverGui || _AlreadyDown)
                return;
            UnityFingersRefresh();
        }

        private void LeanTouch_OnFingerTap(LeanFinger obj)
        {
            if (obj.StartedOverGui)
                return;
            UnityFingersUpdate();
        }

        private void LeanTouch_OnFingerExpired(LeanFinger obj)
        {
            if (obj.StartedOverGui)
                return;
            var list = LeanTouch.GetFingers(true, false);
            if (list.Count == 0 && !Mouse.IsDragging && !(Client.Game.UO?.World?.TargetManager?.IsTargeting ?? false))
            {
                Mouse.Position = Point.Zero;
            }
        }

        private void UnityFingersRefresh(bool leftclick = false)
        {
            List<LeanFinger> fingers = LeanTouch.GetFingers(true, false);

            //Only process one finger that has not started over gui because using multiple fingers with UIManager
            //causes issues due to the assumption that there's only one pointer, such as on finger "stealing"
            //a dragged gump from another
            if (fingers.Count > 0)
            {
                LeanFinger finger = fingers[0];

                /*bool leftMouseDown = finger.Down;
                bool leftMouseHeld = finger.Set;*/

                Mouse.Position = ConvertUnityMousePosition(finger.ScreenPosition, 1f / Batcher.scale);
                /*Mouse.LButtonPressed = leftMouseDown || leftMouseHeld;
                Mouse.RButtonPressed = false;
                Mouse.Update();*/
                if (finger.Down || finger.Set)
                {
                    if (fingers.Count >= 2 && (fingers[1].Down || fingers[1].Set))
                    {
                        _AlreadyDown = true;
                        //Point firstMousePositionPoint = ConvertUnityMousePosition(fingers[0].ScreenPosition, oneOverScale);
                        Point secondMousePositionPoint = ConvertUnityMousePosition(fingers[1].ScreenPosition, 1f / Batcher.scale);
                        Control firstControlUnderFinger = UIManager.GetMouseOverControl(Mouse.Position);
                        Control secondControlUnderFinger = UIManager.GetMouseOverControl(secondMousePositionPoint);
                        //We prefer to get the root parent but sometimes it can be null (like with GridLootGump), in which case we revert to the initially found control
                        firstControlUnderFinger = firstControlUnderFinger?.RootParent ?? firstControlUnderFinger;
                        secondControlUnderFinger = secondControlUnderFinger?.RootParent ?? secondControlUnderFinger;
                        if (firstControlUnderFinger != null && firstControlUnderFinger == secondControlUnderFinger)
                        {
                            RightMouse(true, true);
                            RightMouse(false, true);
                        }
                    }
                    else if (leftclick)
                    {
                        _AlreadyDown = true;
                        LeftMouse(true);
                    }
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.EndFrame();
            Profiler.BeginFrame();

            if (Profiler.InContext("OutOfContext"))
            {
                Profiler.ExitContext("OutOfContext");
            }

            Profiler.EnterContext("RenderFrame");

            _totalFrames++;

            GraphicsDevice.Clear(Color.Black);

            _uoSpriteBatch.Begin();
            // MobileUO: commented out
            //var rect = new Rectangle(
            //    0,
            //    0,
            //    GraphicManager.PreferredBackBufferWidth,
            //    GraphicManager.PreferredBackBufferHeight
            //);
            //_uoSpriteBatch.DrawTiled(
            //    _background,
            //    rect,
            //    _background.Bounds,
            //    new Vector3(0, 0, 0.1f)
            //);
            _uoSpriteBatch.End();

            if (Scene != null && Scene.IsLoaded && !Scene.IsDestroyed)
            {
                Scene.Draw(_uoSpriteBatch);
            }

            UIManager.Draw(_uoSpriteBatch);

            if ((UO.World?.InGame ?? false) && SelectedObject.Object is TextObject t)
            {
                if (t.IsTextGump)
                {
                    t.ToTopD();
                }
                else
                {
                    UO.World.WorldTextManager?.MoveToTop(t);
                }
            }

            SelectedObject.HealthbarObject = null;
            SelectedObject.SelectedContainer = null;

            _uoSpriteBatch.Begin();
            UO.GameCursor?.Draw(_uoSpriteBatch);
            _uoSpriteBatch.End();

            Profiler.ExitContext("RenderFrame");
            Profiler.EnterContext("OutOfContext");

            Plugin.ProcessDrawCmdList(GraphicsDevice);

            base.Draw(gameTime);
        }

        // MobileUO: commented out
        // MobileUO: TODO: do we need to implement it?
        //protected override bool BeginDraw()
        //{
        //    return !_suppressedDraw && base.BeginDraw();
        //}

        private void WindowOnClientSizeChanged(object sender, EventArgs e)
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;

            if (!IsWindowMaximized())
            {
                if (ProfileManager.CurrentProfile != null)
                    ProfileManager.CurrentProfile.WindowClientBounds = new Point(width, height);
            }

            SetWindowSize(width, height);

            WorldViewportGump viewport = UIManager.GetGump<WorldViewportGump>();

            if (viewport != null && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.GameWindowFullSize)
            {
                viewport.ResizeGameWindow(new Point(width, height));
                viewport.X = -5;
                viewport.Y = -5;
            }
        }

        // MobileUO: NOTE: SDL events are not handled in Unity! This function will NOT be hit!
        private int HandleSdlEvent(IntPtr userData, IntPtr ptr)
        {
            SDL_Event* sdlEvent = (SDL_Event*)ptr;

            // Don't pass SDL events to the plugin host before the plugins are initialized
            // or the garbage collector can get screwed up
            if (_pluginsInitialized && Plugin.ProcessWndProc(sdlEvent) != 0)
            {
                if (sdlEvent->type == SDL_EventType.SDL_MOUSEMOTION)
                {
                    if (UO.GameCursor != null)
                    {
                        UO.GameCursor.AllowDrawSDLCursor = false;
                    }
                }

                return 1;
            }

            switch (sdlEvent->type)
            {
                case SDL_EventType.SDL_AUDIODEVICEADDED:
                    Console.WriteLine("AUDIO ADDED: {0}", sdlEvent->adevice.which);

                    break;

                case SDL_EventType.SDL_AUDIODEVICEREMOVED:
                    Console.WriteLine("AUDIO REMOVED: {0}", sdlEvent->adevice.which);

                    break;

                case SDL_EventType.SDL_WINDOWEVENT:

                    switch (sdlEvent->window.windowEvent)
                    {
                        case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                            Mouse.MouseInWindow = true;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                            Mouse.MouseInWindow = false;

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                            Plugin.OnFocusGained();

                            break;

                        case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                            Plugin.OnFocusLost();

                            break;
                    }

                    break;

                case SDL_EventType.SDL_KEYDOWN:

                    Keyboard.OnKeyDown(sdlEvent->key);

                    if (
                        Plugin.ProcessHotkeys(
                            (int)sdlEvent->key.keysym.sym,
                            (int)sdlEvent->key.keysym.mod,
                            true
                        )
                    )
                    {
                        _ignoreNextTextInput = false;

                        UIManager.KeyboardFocusControl?.InvokeKeyDown(
                            sdlEvent->key.keysym.sym,
                            sdlEvent->key.keysym.mod
                        );

                        Scene.OnKeyDown(sdlEvent->key);
                    }
                    else
                    {
                        _ignoreNextTextInput = true;
                    }

                    break;

                case SDL_EventType.SDL_KEYUP:

                    Keyboard.OnKeyUp(sdlEvent->key);
                    UIManager.KeyboardFocusControl?.InvokeKeyUp(
                        sdlEvent->key.keysym.sym,
                        sdlEvent->key.keysym.mod
                    );
                    Scene.OnKeyUp(sdlEvent->key);
                    Plugin.ProcessHotkeys(0, 0, false);

                    if (sdlEvent->key.keysym.sym == SDL_Keycode.SDLK_PRINTSCREEN)
                    {
                        // MobileUO: commented out
                        // TakeScreenshot();
                    }

                    break;

                case SDL_EventType.SDL_TEXTINPUT:

                    if (_ignoreNextTextInput)
                    {
                        break;
                    }

                    // Fix for linux OS: https://github.com/andreakarasho/ClassicUO/pull/1263
                    // Fix 2: SDL owns this behaviour. Cheating is not a real solution.
                    /*if (!Utility.Platforms.PlatformHelper.IsWindows)
                    {
                        if (Keyboard.Alt || Keyboard.Ctrl)
                        {
                            break;
                        }
                    }*/

                    string s = UTF8_ToManaged((IntPtr)sdlEvent->text.text, false);

                    if (!string.IsNullOrEmpty(s))
                    {
                        UIManager.KeyboardFocusControl?.InvokeTextInput(s);
                        Scene.OnTextInput(s);
                    }

                    break;

                case SDL_EventType.SDL_MOUSEMOTION:

                    if (UO.GameCursor != null && !UO.GameCursor.AllowDrawSDLCursor)
                    {
                        UO.GameCursor.AllowDrawSDLCursor = true;
                        UO.GameCursor.Graphic = 0xFFFF;
                    }

                    Mouse.Update();

                    if (Mouse.IsDragging)
                    {
                        if (!Scene.OnMouseDragging())
                        {
                            UIManager.OnMouseDragging();
                        }
                    }

                    break;

                case SDL_EventType.SDL_MOUSEWHEEL:
                    Mouse.Update();
                    bool isScrolledUp = sdlEvent->wheel.y > 0;

                    Plugin.ProcessMouse(0, sdlEvent->wheel.y);

                    if (!Scene.OnMouseWheel(isScrolledUp))
                    {
                        UIManager.OnMouseWheel(isScrolledUp);
                    }

                    break;

                case SDL_EventType.SDL_MOUSEBUTTONDOWN:
                {
                    SDL_MouseButtonEvent mouse = sdlEvent->button;

                    // The values in MouseButtonType are chosen to exactly match the SDL values
                    MouseButtonType buttonType = (MouseButtonType)mouse.button;

                    uint lastClickTime = 0;

                    switch (buttonType)
                    {
                        case MouseButtonType.Left:
                            lastClickTime = Mouse.LastLeftButtonClickTime;

                            break;

                        case MouseButtonType.Middle:
                            lastClickTime = Mouse.LastMidButtonClickTime;

                            break;

                        case MouseButtonType.Right:
                            lastClickTime = Mouse.LastRightButtonClickTime;

                            break;

                        case MouseButtonType.XButton1:
                        case MouseButtonType.XButton2:
                            break;

                        default:
                            Log.Warn($"No mouse button handled: {mouse.button}");

                            break;
                    }

                    Mouse.ButtonPress(buttonType);
                    Mouse.Update();

                    uint ticks = Time.Ticks;

                    if (lastClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                    {
                        lastClickTime = 0;

                        bool res =
                            Scene.OnMouseDoubleClick(buttonType)
                            || UIManager.OnMouseDoubleClick(buttonType);

                        if (!res)
                        {
                            if (!Scene.OnMouseDown(buttonType))
                            {
                                UIManager.OnMouseButtonDown(buttonType);
                            }
                        }
                        else
                        {
                            lastClickTime = 0xFFFF_FFFF;
                        }
                    }
                    else
                    {
                        if (
                            buttonType != MouseButtonType.Left
                            && buttonType != MouseButtonType.Right
                        )
                        {
                            Plugin.ProcessMouse(sdlEvent->button.button, 0);
                        }

                        if (!Scene.OnMouseDown(buttonType))
                        {
                            UIManager.OnMouseButtonDown(buttonType);
                        }

                        lastClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
                    }

                    switch (buttonType)
                    {
                        case MouseButtonType.Left:
                            Mouse.LastLeftButtonClickTime = lastClickTime;

                            break;

                        case MouseButtonType.Middle:
                            Mouse.LastMidButtonClickTime = lastClickTime;

                            break;

                        case MouseButtonType.Right:
                            Mouse.LastRightButtonClickTime = lastClickTime;

                            break;
                    }

                    break;
                }

                case SDL_EventType.SDL_MOUSEBUTTONUP:
                {
                    SDL_MouseButtonEvent mouse = sdlEvent->button;

                    // The values in MouseButtonType are chosen to exactly match the SDL values
                    MouseButtonType buttonType = (MouseButtonType)mouse.button;

                    uint lastClickTime = 0;

                    switch (buttonType)
                    {
                        case MouseButtonType.Left:
                            lastClickTime = Mouse.LastLeftButtonClickTime;

                            break;

                        case MouseButtonType.Middle:
                            lastClickTime = Mouse.LastMidButtonClickTime;

                            break;

                        case MouseButtonType.Right:
                            lastClickTime = Mouse.LastRightButtonClickTime;

                            break;

                        default:
                            Log.Warn($"No mouse button handled: {mouse.button}");

                            break;
                    }

                    if (lastClickTime != 0xFFFF_FFFF)
                    {
                        if (
                            !Scene.OnMouseUp(buttonType)
                            || UIManager.LastControlMouseDown(buttonType) != null
                        )
                        {
                            UIManager.OnMouseButtonUp(buttonType);
                        }
                    }

                    Mouse.ButtonRelease(buttonType);
                    Mouse.Update();

                    break;
                }
            }

            return 1;
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Scene?.Dispose();

            base.OnExiting(sender, args);
        }

        // MobileUO: commented out
        //private void TakeScreenshot()
        //{
        //    string screenshotsFolder = FileSystemHelper.CreateFolderIfNotExists
        //        (CUOEnviroment.ExecutablePath, "Data", "Client", "Screenshots");

        //    string path = Path.Combine(screenshotsFolder, $"screenshot_{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.png");

        //    Color[] colors =
        //        new Color[GraphicManager.PreferredBackBufferWidth * GraphicManager.PreferredBackBufferHeight];

        //    GraphicsDevice.GetBackBufferData(colors);

        //    using (Texture2D texture = new Texture2D
        //    (
        //        GraphicsDevice, GraphicManager.PreferredBackBufferWidth, GraphicManager.PreferredBackBufferHeight,
        //        false, SurfaceFormat.Color
        //    ))
        //    using (FileStream fileStream = File.Create(path))
        //    {
        //        texture.SetData(colors);
        //        texture.SaveAsPng(fileStream, texture.Width, texture.Height);
        //        string message = string.Format(ResGeneral.ScreenshotStoredIn0, path);

        //        if (ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.HideScreenshotStoredInMessage)
        //        {
        //            Log.Info(message);
        //        }
        //        else
        //        {
        //            GameActions.Print(message, 0x44, MessageType.System);
        //        }
        //    }
        //}

        // MobileUO: here to end of file for Unity functions to help support inputs
        private void MouseWheel(bool isup)
        {
            Plugin.ProcessMouse(0, isup ? 1 : -1);

            if (!Scene.OnMouseWheel(isup))
                UIManager.OnMouseWheel(isup);
        }

        private void LeftMouse(bool isDown)
        {
            if (isDown)
            {
                //Mouse.Begin();
                Mouse.LButtonPressed = true;
                Mouse.LClickPosition = Mouse.Position;
                Mouse.CancelDoubleClick = false;
                Mouse.Update();
                uint ticks = Time.Ticks;

                if (Mouse.LastLeftButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                {
                    Mouse.LastLeftButtonClickTime = 0;

                    bool res = Scene.OnMouseDoubleClick(MouseButtonType.Left) || UIManager.OnMouseDoubleClick(MouseButtonType.Left);

                    if (!res)
                    {
                        if (!Scene.OnMouseDown(MouseButtonType.Left))
                            UIManager.OnMouseButtonDown(MouseButtonType.Left);
                    }
                    else
                    {
                        Mouse.LastLeftButtonClickTime = 0xFFFF_FFFF;
                    }

                    return;
                }

                if (!Scene.OnMouseDown(MouseButtonType.Left))
                    UIManager.OnMouseButtonDown(MouseButtonType.Left);

                Mouse.LastLeftButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
            }
            else
            {
                if (Mouse.LastLeftButtonClickTime != 0xFFFF_FFFF)
                {
                    if (!Scene.OnMouseUp(MouseButtonType.Left) || UIManager.LastControlMouseDown(MouseButtonType.Left) != null)
                        UIManager.OnMouseButtonUp(MouseButtonType.Left);
                }
                Mouse.LButtonPressed = false;
                Mouse.Update();
                //Mouse.End();
            }
        }

        private void MiddleMouse(bool isDown)
        {
            if (isDown)
            {
                //Mouse.Begin();
                Mouse.MButtonPressed = true;
                Mouse.MClickPosition = Mouse.Position;
                Mouse.CancelDoubleClick = false;
                Mouse.Update();
                uint ticks = Time.Ticks;

                if (Mouse.LastMidButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                {
                    Mouse.LastMidButtonClickTime = 0;

                    bool res = Scene.OnMouseDoubleClick(MouseButtonType.Middle) || UIManager.OnMouseDoubleClick(MouseButtonType.Middle);

                    if (!res)
                    {
                        if (!Scene.OnMouseDown(MouseButtonType.Middle))
                            UIManager.OnMouseButtonDown(MouseButtonType.Middle);
                    }
                    else
                    {
                        Mouse.LastMidButtonClickTime = 0xFFFF_FFFF;
                    }

                    return;
                }

                Plugin.ProcessMouse((int)MouseButtonType.Middle, 0);

                if (!Scene.OnMouseDown(MouseButtonType.Middle))
                    UIManager.OnMouseButtonDown(MouseButtonType.Middle);

                Mouse.LastMidButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
            }
            else
            {
                if (Mouse.LastMidButtonClickTime != 0xFFFF_FFFF)
                {
                    if (!Scene.OnMouseUp(MouseButtonType.Middle))
                        UIManager.OnMouseButtonUp(MouseButtonType.Middle);
                }

                Mouse.MButtonPressed = false;
                Mouse.Update();
                //Mouse.End();
            }
        }

        private void RightMouse(bool isDown, bool skipscene = false)
        {
            if (isDown)
            {
                //Mouse.Begin();
                Mouse.RButtonPressed = true;
                Mouse.RClickPosition = Mouse.Position;
                Mouse.CancelDoubleClick = false;
                Mouse.Update();
                uint ticks = Time.Ticks;

                if (Mouse.LastRightButtonClickTime + Mouse.MOUSE_DELAY_DOUBLE_CLICK >= ticks)
                {
                    Mouse.LastRightButtonClickTime = 0;

                    bool res = skipscene ? UIManager.OnMouseDoubleClick(MouseButtonType.Right) : Scene.OnMouseDoubleClick(MouseButtonType.Right) || UIManager.OnMouseDoubleClick(MouseButtonType.Right);

                    if (!res)
                    {
                        if (skipscene || !Scene.OnMouseDown(MouseButtonType.Right))
                            UIManager.OnMouseButtonDown(MouseButtonType.Right);
                    }
                    else
                    {
                        Mouse.LastRightButtonClickTime = 0xFFFF_FFFF;
                    }

                    return;
                }

                if (skipscene || !Scene.OnMouseDown(MouseButtonType.Right))
                    UIManager.OnMouseButtonDown(MouseButtonType.Right);

                Mouse.LastRightButtonClickTime = Mouse.CancelDoubleClick ? 0 : ticks;
            }
            else
            {
                if (Mouse.LastRightButtonClickTime != 0xFFFF_FFFF)
                {
                    if (skipscene || !Scene.OnMouseUp(MouseButtonType.Right))
                        UIManager.OnMouseButtonUp(MouseButtonType.Right);
                }
                Mouse.RButtonPressed = false;
                Mouse.Update();
                //Mouse.End();
            }
        }

        private void ExtraMouse(bool isDown, int button)
        {
            if (isDown)
            {
                //Mouse.Begin();
                Mouse.XButtonPressed = true;
                Mouse.CancelDoubleClick = false;
                Plugin.ProcessMouse(button, 0);
                if (!Scene.OnMouseDown((MouseButtonType)button))
                    UIManager.OnMouseButtonDown((MouseButtonType)button);

                // TODO: doubleclick?
            }
            else
            {
                if (!Scene.OnMouseUp((MouseButtonType)button))
                    UIManager.OnMouseButtonUp((MouseButtonType)button);

                Mouse.XButtonPressed = false;
                //Mouse.End();
            }
        }

        private readonly Dictionary<UnityEngine.KeyCode, SDL_Keycode> _keyCodeEnumValues = new Dictionary<UnityEngine.KeyCode, SDL_Keycode>()
        {
            { UnityEngine.KeyCode.Backspace, SDL_Keycode.SDLK_BACKSPACE },
            { UnityEngine.KeyCode.Tab, SDL_Keycode.SDLK_TAB },
            { UnityEngine.KeyCode.Return, SDL_Keycode.SDLK_RETURN },
            { UnityEngine.KeyCode.Escape, SDL_Keycode.SDLK_ESCAPE },
            { UnityEngine.KeyCode.Space, SDL_Keycode.SDLK_SPACE },
            { UnityEngine.KeyCode.Exclaim, SDL_Keycode.SDLK_EXCLAIM },
            { UnityEngine.KeyCode.Hash, SDL_Keycode.SDLK_HASH },
            { UnityEngine.KeyCode.Dollar, SDL_Keycode.SDLK_DOLLAR },
            { UnityEngine.KeyCode.Percent, SDL_Keycode.SDLK_PERCENT },
            { UnityEngine.KeyCode.Ampersand, SDL_Keycode.SDLK_AMPERSAND },
            { UnityEngine.KeyCode.Quote, SDL_Keycode.SDLK_QUOTE },
            { UnityEngine.KeyCode.LeftParen, SDL_Keycode.SDLK_LEFTPAREN },
            { UnityEngine.KeyCode.RightParen, SDL_Keycode.SDLK_RIGHTPAREN },
            { UnityEngine.KeyCode.Asterisk, SDL_Keycode.SDLK_ASTERISK },
            { UnityEngine.KeyCode.Plus, SDL_Keycode.SDLK_PLUS },
            { UnityEngine.KeyCode.Comma, SDL_Keycode.SDLK_COMMA },
            { UnityEngine.KeyCode.Minus, SDL_Keycode.SDLK_MINUS },
            { UnityEngine.KeyCode.Period, SDL_Keycode.SDLK_PERIOD },
            { UnityEngine.KeyCode.Slash, SDL_Keycode.SDLK_SLASH },
            { UnityEngine.KeyCode.Alpha0, SDL_Keycode.SDLK_0 },
            { UnityEngine.KeyCode.Alpha1, SDL_Keycode.SDLK_1 },
            { UnityEngine.KeyCode.Alpha2, SDL_Keycode.SDLK_2 },
            { UnityEngine.KeyCode.Alpha3, SDL_Keycode.SDLK_3 },
            { UnityEngine.KeyCode.Alpha4, SDL_Keycode.SDLK_4 },
            { UnityEngine.KeyCode.Alpha5, SDL_Keycode.SDLK_5 },
            { UnityEngine.KeyCode.Alpha6, SDL_Keycode.SDLK_6 },
            { UnityEngine.KeyCode.Alpha7, SDL_Keycode.SDLK_7 },
            { UnityEngine.KeyCode.Alpha8, SDL_Keycode.SDLK_8 },
            { UnityEngine.KeyCode.Alpha9, SDL_Keycode.SDLK_9 },
            { UnityEngine.KeyCode.Colon, SDL_Keycode.SDLK_COLON },
            { UnityEngine.KeyCode.Semicolon, SDL_Keycode.SDLK_SEMICOLON },
            { UnityEngine.KeyCode.Less, SDL_Keycode.SDLK_LESS },
            { UnityEngine.KeyCode.Equals, SDL_Keycode.SDLK_EQUALS },
            { UnityEngine.KeyCode.Greater, SDL_Keycode.SDLK_GREATER },
            { UnityEngine.KeyCode.Question, SDL_Keycode.SDLK_QUESTION },
            { UnityEngine.KeyCode.At, SDL_Keycode.SDLK_AT },
            { UnityEngine.KeyCode.LeftBracket, SDL_Keycode.SDLK_LEFTBRACKET },
            { UnityEngine.KeyCode.Backslash, SDL_Keycode.SDLK_BACKSLASH },
            { UnityEngine.KeyCode.RightBracket, SDL_Keycode.SDLK_RIGHTBRACKET },
            { UnityEngine.KeyCode.Caret, SDL_Keycode.SDLK_CARET },
            { UnityEngine.KeyCode.Underscore, SDL_Keycode.SDLK_UNDERSCORE },
            { UnityEngine.KeyCode.BackQuote, SDL_Keycode.SDLK_BACKQUOTE },
            { UnityEngine.KeyCode.A, SDL_Keycode.SDLK_a },
            { UnityEngine.KeyCode.B, SDL_Keycode.SDLK_b },
            { UnityEngine.KeyCode.C, SDL_Keycode.SDLK_c },
            { UnityEngine.KeyCode.D, SDL_Keycode.SDLK_d },
            { UnityEngine.KeyCode.E, SDL_Keycode.SDLK_e },
            { UnityEngine.KeyCode.F, SDL_Keycode.SDLK_f },
            { UnityEngine.KeyCode.G, SDL_Keycode.SDLK_g },
            { UnityEngine.KeyCode.H, SDL_Keycode.SDLK_h },
            { UnityEngine.KeyCode.I, SDL_Keycode.SDLK_i },
            { UnityEngine.KeyCode.J, SDL_Keycode.SDLK_j },
            { UnityEngine.KeyCode.K, SDL_Keycode.SDLK_k },
            { UnityEngine.KeyCode.L, SDL_Keycode.SDLK_l },
            { UnityEngine.KeyCode.M, SDL_Keycode.SDLK_m },
            { UnityEngine.KeyCode.N, SDL_Keycode.SDLK_n },
            { UnityEngine.KeyCode.O, SDL_Keycode.SDLK_o },
            { UnityEngine.KeyCode.P, SDL_Keycode.SDLK_p },
            { UnityEngine.KeyCode.Q, SDL_Keycode.SDLK_q },
            { UnityEngine.KeyCode.R, SDL_Keycode.SDLK_r },
            { UnityEngine.KeyCode.S, SDL_Keycode.SDLK_s },
            { UnityEngine.KeyCode.T, SDL_Keycode.SDLK_t },
            { UnityEngine.KeyCode.U, SDL_Keycode.SDLK_u },
            { UnityEngine.KeyCode.V, SDL_Keycode.SDLK_v },
            { UnityEngine.KeyCode.W, SDL_Keycode.SDLK_w },
            { UnityEngine.KeyCode.X, SDL_Keycode.SDLK_x },
            { UnityEngine.KeyCode.Y, SDL_Keycode.SDLK_y },
            { UnityEngine.KeyCode.Z, SDL_Keycode.SDLK_z },
            { UnityEngine.KeyCode.Delete, SDL_Keycode.SDLK_DELETE },
            { UnityEngine.KeyCode.CapsLock, SDL_Keycode.SDLK_CAPSLOCK },
            { UnityEngine.KeyCode.F1, SDL_Keycode.SDLK_F1 },
            { UnityEngine.KeyCode.F2, SDL_Keycode.SDLK_F2 },
            { UnityEngine.KeyCode.F3, SDL_Keycode.SDLK_F3 },
            { UnityEngine.KeyCode.F4, SDL_Keycode.SDLK_F4 },
            { UnityEngine.KeyCode.F5, SDL_Keycode.SDLK_F5 },
            { UnityEngine.KeyCode.F6, SDL_Keycode.SDLK_F6 },
            { UnityEngine.KeyCode.F7, SDL_Keycode.SDLK_F7 },
            { UnityEngine.KeyCode.F8, SDL_Keycode.SDLK_F8 },
            { UnityEngine.KeyCode.F9, SDL_Keycode.SDLK_F9 },
            { UnityEngine.KeyCode.F10, SDL_Keycode.SDLK_F10 },
            { UnityEngine.KeyCode.F11, SDL_Keycode.SDLK_F11 },
            { UnityEngine.KeyCode.F12, SDL_Keycode.SDLK_F12 },
            { UnityEngine.KeyCode.ScrollLock, SDL_Keycode.SDLK_SCROLLLOCK },
            { UnityEngine.KeyCode.Pause, SDL_Keycode.SDLK_PAUSE },
            { UnityEngine.KeyCode.Insert, SDL_Keycode.SDLK_INSERT },
            { UnityEngine.KeyCode.Home, SDL_Keycode.SDLK_HOME },
            { UnityEngine.KeyCode.PageUp, SDL_Keycode.SDLK_PAGEUP },
            { UnityEngine.KeyCode.End, SDL_Keycode.SDLK_END },
            { UnityEngine.KeyCode.PageDown, SDL_Keycode.SDLK_PAGEDOWN },
            { UnityEngine.KeyCode.KeypadDivide, SDL_Keycode.SDLK_KP_DIVIDE },
            { UnityEngine.KeyCode.KeypadMultiply, SDL_Keycode.SDLK_KP_MULTIPLY },
            { UnityEngine.KeyCode.KeypadMinus, SDL_Keycode.SDLK_KP_MINUS },
            { UnityEngine.KeyCode.KeypadPlus, SDL_Keycode.SDLK_KP_PLUS },
            { UnityEngine.KeyCode.KeypadEnter, SDL_Keycode.SDLK_KP_ENTER },
            { UnityEngine.KeyCode.Keypad1, SDL_Keycode.SDLK_KP_1 },
            { UnityEngine.KeyCode.Keypad2, SDL_Keycode.SDLK_KP_2 },
            { UnityEngine.KeyCode.Keypad3, SDL_Keycode.SDLK_KP_3 },
            { UnityEngine.KeyCode.Keypad4, SDL_Keycode.SDLK_KP_4 },
            { UnityEngine.KeyCode.Keypad5, SDL_Keycode.SDLK_KP_5 },
            { UnityEngine.KeyCode.Keypad6, SDL_Keycode.SDLK_KP_6 },
            { UnityEngine.KeyCode.Keypad7, SDL_Keycode.SDLK_KP_7 },
            { UnityEngine.KeyCode.Keypad8, SDL_Keycode.SDLK_KP_8 },
            { UnityEngine.KeyCode.Keypad9, SDL_Keycode.SDLK_KP_9 },
            { UnityEngine.KeyCode.Keypad0, SDL_Keycode.SDLK_KP_0 },
            { UnityEngine.KeyCode.KeypadPeriod, SDL_Keycode.SDLK_KP_PERIOD },
            { UnityEngine.KeyCode.KeypadEquals, SDL_Keycode.SDLK_KP_EQUALS },
            { UnityEngine.KeyCode.F13, SDL_Keycode.SDLK_F13 },
            { UnityEngine.KeyCode.F14, SDL_Keycode.SDLK_F14 },
            { UnityEngine.KeyCode.F15, SDL_Keycode.SDLK_F15 },
            { UnityEngine.KeyCode.Help, SDL_Keycode.SDLK_HELP },
            { UnityEngine.KeyCode.Menu, SDL_Keycode.SDLK_MENU },
            { UnityEngine.KeyCode.SysReq, SDL_Keycode.SDLK_SYSREQ },
            { UnityEngine.KeyCode.Clear, SDL_Keycode.SDLK_CLEAR },
            { UnityEngine.KeyCode.LeftArrow, SDL_Keycode.SDLK_LEFT },
            { UnityEngine.KeyCode.RightArrow, SDL_Keycode.SDLK_RIGHT },
            { UnityEngine.KeyCode.UpArrow, SDL_Keycode.SDLK_UP },
            { UnityEngine.KeyCode.DownArrow, SDL_Keycode.SDLK_DOWN }
        };
        private readonly Dictionary<SDL_Keycode, uint> _RepeatedKeys = new Dictionary<SDL_Keycode, uint>();

        private UnityEngine.Vector3 lastMousePosition;
        public SDL_Keymod KeymodOverride;
        public bool EscOverride;

        private bool IsUsingFingers => UnityEngine.Application.isMobilePlatform && UserPreferences.UseMouseOnMobile.CurrentValue == 0;

        private void UnityFingersUpdate()
        {
            float oneOverScale = 1f / Batcher.scale;
            List<LeanFinger> fingers = LeanTouch.GetFingers(true, false);

            if (fingers.Count == 1)
            {
                Mouse.Position = ConvertUnityMousePosition(fingers[0].ScreenPosition, oneOverScale);

                //Detect two finger tap gesture for closing gumps, only when one of the fingers' state is Down
                /*if (fingers.Count >= 2 && (fingers[0].Down || fingers[1].Down))
                {
                    //Point firstMousePositionPoint = ConvertUnityMousePosition(fingers[0].ScreenPosition, oneOverScale);
                    Point secondMousePositionPoint = ConvertUnityMousePosition(fingers[1].ScreenPosition, oneOverScale);
                    Control firstControlUnderFinger = UIManager.GetMouseOverControl(Mouse.Position);
                    Control secondControlUnderFinger = UIManager.GetMouseOverControl(secondMousePositionPoint);
                    //We prefer to get the root parent but sometimes it can be null (like with GridLootGump), in which case we revert to the initially found control
                    firstControlUnderFinger = firstControlUnderFinger?.RootParent ?? firstControlUnderFinger;
                    secondControlUnderFinger = secondControlUnderFinger?.RootParent ?? secondControlUnderFinger;
                    if (firstControlUnderFinger != null && firstControlUnderFinger == secondControlUnderFinger)
                    {
                        RightMouse(true, true);
                        RightMouse(false, true);
                    }
                }
                //Only process one finger that has not started over gui because using multiple fingers with UIManager
                //causes issues due to the assumption that there's only one pointer, such as one finger "stealing" a
                //dragged gump from another
                else*/
                {
                    LeanFinger finger = fingers[0];

                    if (finger.Tap)
                    {
                        LeftMouse(true);
                    }
                }
            }
        }

        private void UnityMouseUpdate()
        {
            float oneOverScale = 1f / Batcher.scale;

            UnityEngine.Vector3 mousePosition = UnityEngine.Input.mousePosition;
            if (LeanTouch.PointOverGui(mousePosition))
            {
                Mouse.Position.X = 0;
                Mouse.Position.Y = 0;
                return;
            }
            else
            {
                Mouse.Position = ConvertUnityMousePosition(mousePosition, oneOverScale);
            }

            if (mousePosition != lastMousePosition)
            {
                if (Mouse.IsDragging)
                {
                    if (!Scene.OnMouseDragging())
                        UIManager.OnMouseDragging();
                }
            }
            lastMousePosition = mousePosition;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                LeftMouse(true);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                LeftMouse(false);
            }
            else if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                RightMouse(true);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                RightMouse(false);
            }
            else if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                MiddleMouse(true);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                MiddleMouse(false);
            }
            else if (UnityEngine.Input.GetMouseButtonDown(3))
            {
                ExtraMouse(true, (int)MouseButtonType.XButton1);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(3))
            {
                ExtraMouse(false, (int)MouseButtonType.XButton1);
            }
            else if (UnityEngine.Input.GetMouseButtonDown(4))
            {
                ExtraMouse(true, (int)MouseButtonType.XButton2);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(4))
            {
                ExtraMouse(false, (int)MouseButtonType.XButton2);
            }

            float w = UnityEngine.Input.mouseScrollDelta.y * 10;
            if (w != .0f)
            {
                MouseWheel(w > .0f);
            }
        }

        private void UnityInputUpdate(uint ticks)
        {
            //Keyboard handling
            var keymod = KeymodOverride;
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftAlt))
            {
                keymod |= SDL_Keymod.KMOD_LALT;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightAlt))
            {
                keymod |= SDL_Keymod.KMOD_RALT;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift))
            {
                keymod |= SDL_Keymod.KMOD_LSHIFT;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift))
            {
                keymod |= SDL_Keymod.KMOD_RSHIFT;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl))
            {
                keymod |= SDL_Keymod.KMOD_LCTRL;
            }
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl))
            {
                keymod |= SDL_Keymod.KMOD_RCTRL;
            }

            Keyboard.Shift = (keymod & SDL_Keymod.KMOD_SHIFT) != SDL_Keymod.KMOD_NONE;
            Keyboard.Alt = (keymod & SDL_Keymod.KMOD_ALT) != SDL_Keymod.KMOD_NONE;
            Keyboard.Ctrl = (keymod & SDL_Keymod.KMOD_CTRL) != SDL_Keymod.KMOD_NONE;
            foreach (var kvp in _keyCodeEnumValues)
            {
                if (UnityEngine.Input.GetKeyDown(kvp.Key) || (UnityEngine.Input.GetKey(kvp.Key) && _RepeatedKeys[kvp.Value] <= ticks))
                {
                    SDL_KeyboardEvent key = new SDL_KeyboardEvent { keysym = new SDL_Keysym { sym = kvp.Value, mod = keymod } };
                    Keyboard.OnKeyDown(key);

                    if (Plugin.ProcessHotkeys((int)key.keysym.sym, (int)key.keysym.mod, true))
                    {
                        _ignoreNextTextInput = Keyboard.Ctrl || Keyboard.Alt;
                        UIManager.KeyboardFocusControl?.InvokeKeyDown(key.keysym.sym, key.keysym.mod);

                        Scene.OnKeyDown(key);
                    }
                    else
                        _ignoreNextTextInput = true;

                    _RepeatedKeys[kvp.Value] = ticks + (_RepeatedKeys[kvp.Value] == 0 ? ((kvp.Key == UnityEngine.KeyCode.Backspace || kvp.Key == UnityEngine.KeyCode.Delete) ? 200u : 400u) : (kvp.Key == UnityEngine.KeyCode.Backspace || kvp.Key == UnityEngine.KeyCode.Delete) ? 50u : 200u);
                }
                else if (UnityEngine.Input.GetKeyUp(kvp.Key))
                {
                    SDL_KeyboardEvent key = new SDL_KeyboardEvent { keysym = new SDL_Keysym { sym = kvp.Value, mod = keymod } };
                    Keyboard.OnKeyUp(key);
                    UIManager.KeyboardFocusControl?.InvokeKeyUp(key.keysym.sym, key.keysym.mod);
                    Scene.OnKeyUp(key);
                    Plugin.ProcessHotkeys(0, 0, false);
                    _RepeatedKeys[kvp.Value] = 0;
                }
            }

            //WARNING - NOTE: DONT ENABLE THIS, AS IT ALLOCATES AND USES A LOT OR RESOURCES; ONLY FOR TESTS! 
            /*HashSet<UnityEngine.KeyCode> excluded = new HashSet<UnityEngine.KeyCode>() { UnityEngine.KeyCode.Mouse0, UnityEngine.KeyCode.Mouse1, UnityEngine.KeyCode.Mouse2, UnityEngine.KeyCode.Mouse3, UnityEngine.KeyCode.Mouse4, UnityEngine.KeyCode.Mouse5, UnityEngine.KeyCode.Mouse6 };
            foreach(var key in (UnityEngine.KeyCode[])Enum.GetValues(typeof(UnityEngine.KeyCode)))
            {
                if (UnityEngine.Input.GetKeyDown(key) && !_keyCodeEnumValues.ContainsKey(key) && !excluded.Contains(key))
                {

                }
            }*/

            if (EscOverride)
            {
                EscOverride = false;
                var key = new SDL_KeyboardEvent { keysym = new SDL_Keysym { sym = (SDL_Keycode)UnityEngine.KeyCode.Escape, mod = keymod } };
                // if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    Keyboard.OnKeyDown(key);

                    if (Plugin.ProcessHotkeys((int)key.keysym.sym, (int)key.keysym.mod, true))
                    {
                        _ignoreNextTextInput = false;
                        UIManager.KeyboardFocusControl?.InvokeKeyDown(key.keysym.sym, key.keysym.mod);
                        Scene.OnKeyDown(key);
                    }
                    else
                        _ignoreNextTextInput = true;
                }
                // if (UnityEngine.Input.GetKeyUp(KeyCode.Escape))
                {
                    Keyboard.OnKeyUp(key);
                    UIManager.KeyboardFocusControl?.InvokeKeyUp(key.keysym.sym, key.keysym.mod);
                    Scene.OnKeyUp(key);
                    Plugin.ProcessHotkeys(0, 0, false);
                }
            }

            //Input text handling
            if (UnityEngine.Application.isMobilePlatform && TouchScreenKeyboard != null)
            {
                if (_ignoreNextTextInput == false && TouchScreenKeyboard.status == UnityEngine.TouchScreenKeyboard.Status.Done)
                {
                    var text = StringHelper.Asciify(TouchScreenKeyboard.text.AsSpan());
                    //Clear the text of TouchScreenKeyboard, otherwise it stays there and is re-evaluated every frame
                    TouchScreenKeyboard.text = string.Empty;

                    //Set keyboard to null so we process its text only once when its status is set to Done
                    TouchScreenKeyboard = null;

                    //Need to clear the existing text in textbox before "pasting" new text from TouchScreenKeyboard
                    if (UIManager.KeyboardFocusControl is StbTextBox stbTextBox)
                    {
                        stbTextBox.SetText(string.Empty);
                    }

                    UIManager.KeyboardFocusControl?.InvokeTextInput(text);
                    Scene.OnTextInput(text);

                    //When targeting SystemChat textbox, "auto-press" return key so that the text entered on the TouchScreenKeyboard is submitted right away
                    if (UIManager.KeyboardFocusControl != null && UIManager.KeyboardFocusControl == UIManager.SystemChat?.TextBoxControl)
                    {
                        //Handle different chat modes
                        HandleChatMode(text);
                        //"Press" return
                        UIManager.KeyboardFocusControl.InvokeKeyDown(SDL_Keycode.SDLK_RETURN, SDL_Keymod.KMOD_NONE);
                        //Revert chat mode to default
                        UIManager.SystemChat.Mode = ChatMode.Default;
                    }
                }
            }
            else
            {
                var text = UnityEngine.Input.inputString;
                //Backspace character should not be sent as text input
                text = text.Replace("\b", "");
                if (_ignoreNextTextInput == false && string.IsNullOrEmpty(text) == false)
                {
                    UIManager.KeyboardFocusControl?.InvokeTextInput(text);
                    Scene.OnTextInput(text);
                }
            }
        }

        private void HandleChatMode(string text)
        {
            if (text.Length > 0)
            {
                switch (text[0])
                {
                    case '/':
                        UIManager.SystemChat.Mode = ChatMode.Party;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(1));
                        break;
                    case '\\':
                        UIManager.SystemChat.Mode = ChatMode.Guild;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(1));
                        break;
                    case '|':
                        UIManager.SystemChat.Mode = ChatMode.Alliance;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(1));
                        break;
                    case '-':
                        UIManager.SystemChat.Mode = ChatMode.ClientCommand;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(1));
                        break;
                    case ',' when UO.World.ChatManager.ChatIsEnabled == ChatStatus.Enabled:
                        UIManager.SystemChat.Mode = ChatMode.UOChat;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(1));
                        break;
                    case ':' when text.Length > 1 && text[1] == ' ':
                        UIManager.SystemChat.Mode = ChatMode.Emote;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(2));
                        break;
                    case ';' when text.Length > 1 && text[1] == ' ':
                        UIManager.SystemChat.Mode = ChatMode.Whisper;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(2));
                        break;
                    case '!' when text.Length > 1 && text[1] == ' ':
                        UIManager.SystemChat.Mode = ChatMode.Yell;
                        //Textbox text has been cleared, set it again
                        UIManager.SystemChat.TextBoxControl.InvokeTextInput(text.Substring(2));
                        break;
                }
            }
        }

        private static Point ConvertUnityMousePosition(UnityEngine.Vector2 screenPosition, float oneOverScale)
        {
            var x = UnityEngine.Mathf.RoundToInt(screenPosition.x * oneOverScale);
            var y = UnityEngine.Mathf.RoundToInt((UnityEngine.Screen.height - screenPosition.y) * oneOverScale);
            return new Point(x, y);
        }
    }
}
