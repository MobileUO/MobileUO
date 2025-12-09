using System;
using System.Runtime.InteropServices;
using System.Text;
using static SDL3.SDL;

namespace SDL3
{
    public static class SDL
    {
        public enum SDL_MessageBoxFlags
        {
            SDL_MESSAGEBOX_ERROR
        }

        public enum SDL_bool
        {
            SDL_FALSE,
            SDL_TRUE
        }

        public enum SDL_EventType /*: uint*/
        {
            SDL_WINDOWEVENT = 512, // 0x00000200
            SDL_KEYDOWN = 768, // 0x00000300
            SDL_KEYUP = 769, // 0x00000301
            SDL_TEXTINPUT = 771, // 0x00000303
            SDL_MOUSEMOTION = 1024, // 0x00000400
            SDL_MOUSEBUTTONDOWN = 1025, // 0x00000401
            SDL_MOUSEBUTTONUP = 1026, // 0x00000402
            SDL_MOUSEWHEEL = 1027, // 0x00000403
            SDL_AUDIODEVICEADDED = 4352, // 0x00001100
            SDL_AUDIODEVICEREMOVED = 4353, // 0x00001101
            // SDL3:
            SDL_EVENT_FIRST = 0,
            SDL_EVENT_QUIT = 256,
            SDL_EVENT_TERMINATING = 257,
            SDL_EVENT_LOW_MEMORY = 258,
            SDL_EVENT_WILL_ENTER_BACKGROUND = 259,
            SDL_EVENT_DID_ENTER_BACKGROUND = 260,
            SDL_EVENT_WILL_ENTER_FOREGROUND = 261,
            SDL_EVENT_DID_ENTER_FOREGROUND = 262,
            SDL_EVENT_LOCALE_CHANGED = 263,
            SDL_EVENT_SYSTEM_THEME_CHANGED = 264,
            SDL_EVENT_DISPLAY_ORIENTATION = 337,
            SDL_EVENT_DISPLAY_ADDED = 338,
            SDL_EVENT_DISPLAY_REMOVED = 339,
            SDL_EVENT_DISPLAY_MOVED = 340,
            SDL_EVENT_DISPLAY_DESKTOP_MODE_CHANGED = 341,
            SDL_EVENT_DISPLAY_CURRENT_MODE_CHANGED = 342,
            SDL_EVENT_DISPLAY_CONTENT_SCALE_CHANGED = 343,
            SDL_EVENT_DISPLAY_FIRST = 337,
            SDL_EVENT_DISPLAY_LAST = 343,
            SDL_EVENT_WINDOW_SHOWN = 514,
            SDL_EVENT_WINDOW_HIDDEN = 515,
            SDL_EVENT_WINDOW_EXPOSED = 516,
            SDL_EVENT_WINDOW_MOVED = 517,
            SDL_EVENT_WINDOW_RESIZED = 518,
            SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED = 519,
            SDL_EVENT_WINDOW_METAL_VIEW_RESIZED = 520,
            SDL_EVENT_WINDOW_MINIMIZED = 521,
            SDL_EVENT_WINDOW_MAXIMIZED = 522,
            SDL_EVENT_WINDOW_RESTORED = 523,
            SDL_EVENT_WINDOW_MOUSE_ENTER = 524,
            SDL_EVENT_WINDOW_MOUSE_LEAVE = 525,
            SDL_EVENT_WINDOW_FOCUS_GAINED = 526,
            SDL_EVENT_WINDOW_FOCUS_LOST = 527,
            SDL_EVENT_WINDOW_CLOSE_REQUESTED = 528,
            SDL_EVENT_WINDOW_HIT_TEST = 529,
            SDL_EVENT_WINDOW_ICCPROF_CHANGED = 530,
            SDL_EVENT_WINDOW_DISPLAY_CHANGED = 531,
            SDL_EVENT_WINDOW_DISPLAY_SCALE_CHANGED = 532,
            SDL_EVENT_WINDOW_SAFE_AREA_CHANGED = 533,
            SDL_EVENT_WINDOW_OCCLUDED = 534,
            SDL_EVENT_WINDOW_ENTER_FULLSCREEN = 535,
            SDL_EVENT_WINDOW_LEAVE_FULLSCREEN = 536,
            SDL_EVENT_WINDOW_DESTROYED = 537,
            SDL_EVENT_WINDOW_HDR_STATE_CHANGED = 538,
            SDL_EVENT_WINDOW_FIRST = 514,
            SDL_EVENT_WINDOW_LAST = 538,
            SDL_EVENT_KEY_DOWN = 768,
            SDL_EVENT_KEY_UP = 769,
            SDL_EVENT_TEXT_EDITING = 770,
            SDL_EVENT_TEXT_INPUT = 771,
            SDL_EVENT_KEYMAP_CHANGED = 772,
            SDL_EVENT_KEYBOARD_ADDED = 773,
            SDL_EVENT_KEYBOARD_REMOVED = 774,
            SDL_EVENT_TEXT_EDITING_CANDIDATES = 775,
            SDL_EVENT_MOUSE_MOTION = 1024,
            SDL_EVENT_MOUSE_BUTTON_DOWN = 1025,
            SDL_EVENT_MOUSE_BUTTON_UP = 1026,
            SDL_EVENT_MOUSE_WHEEL = 1027,
            SDL_EVENT_MOUSE_ADDED = 1028,
            SDL_EVENT_MOUSE_REMOVED = 1029,
            SDL_EVENT_JOYSTICK_AXIS_MOTION = 1536,
            SDL_EVENT_JOYSTICK_BALL_MOTION = 1537,
            SDL_EVENT_JOYSTICK_HAT_MOTION = 1538,
            SDL_EVENT_JOYSTICK_BUTTON_DOWN = 1539,
            SDL_EVENT_JOYSTICK_BUTTON_UP = 1540,
            SDL_EVENT_JOYSTICK_ADDED = 1541,
            SDL_EVENT_JOYSTICK_REMOVED = 1542,
            SDL_EVENT_JOYSTICK_BATTERY_UPDATED = 1543,
            SDL_EVENT_JOYSTICK_UPDATE_COMPLETE = 1544,
            SDL_EVENT_GAMEPAD_AXIS_MOTION = 1616,
            SDL_EVENT_GAMEPAD_BUTTON_DOWN = 1617,
            SDL_EVENT_GAMEPAD_BUTTON_UP = 1618,
            SDL_EVENT_GAMEPAD_ADDED = 1619,
            SDL_EVENT_GAMEPAD_REMOVED = 1620,
            SDL_EVENT_GAMEPAD_REMAPPED = 1621,
            SDL_EVENT_GAMEPAD_TOUCHPAD_DOWN = 1622,
            SDL_EVENT_GAMEPAD_TOUCHPAD_MOTION = 1623,
            SDL_EVENT_GAMEPAD_TOUCHPAD_UP = 1624,
            SDL_EVENT_GAMEPAD_SENSOR_UPDATE = 1625,
            SDL_EVENT_GAMEPAD_UPDATE_COMPLETE = 1626,
            SDL_EVENT_GAMEPAD_STEAM_HANDLE_UPDATED = 1627,
            SDL_EVENT_FINGER_DOWN = 1792,
            SDL_EVENT_FINGER_UP = 1793,
            SDL_EVENT_FINGER_MOTION = 1794,
            SDL_EVENT_FINGER_CANCELED = 1795,
            SDL_EVENT_CLIPBOARD_UPDATE = 2304,
            SDL_EVENT_DROP_FILE = 4096,
            SDL_EVENT_DROP_TEXT = 4097,
            SDL_EVENT_DROP_BEGIN = 4098,
            SDL_EVENT_DROP_COMPLETE = 4099,
            SDL_EVENT_DROP_POSITION = 4100,
            SDL_EVENT_AUDIO_DEVICE_ADDED = 4352,
            SDL_EVENT_AUDIO_DEVICE_REMOVED = 4353,
            SDL_EVENT_AUDIO_DEVICE_FORMAT_CHANGED = 4354,
            SDL_EVENT_SENSOR_UPDATE = 4608,
            SDL_EVENT_PEN_PROXIMITY_IN = 4864,
            SDL_EVENT_PEN_PROXIMITY_OUT = 4865,
            SDL_EVENT_PEN_DOWN = 4866,
            SDL_EVENT_PEN_UP = 4867,
            SDL_EVENT_PEN_BUTTON_DOWN = 4868,
            SDL_EVENT_PEN_BUTTON_UP = 4869,
            SDL_EVENT_PEN_MOTION = 4870,
            SDL_EVENT_PEN_AXIS = 4871,
            SDL_EVENT_CAMERA_DEVICE_ADDED = 5120,
            SDL_EVENT_CAMERA_DEVICE_REMOVED = 5121,
            SDL_EVENT_CAMERA_DEVICE_APPROVED = 5122,
            SDL_EVENT_CAMERA_DEVICE_DENIED = 5123,
            SDL_EVENT_RENDER_TARGETS_RESET = 8192,
            SDL_EVENT_RENDER_DEVICE_RESET = 8193,
            SDL_EVENT_RENDER_DEVICE_LOST = 8194,
            SDL_EVENT_PRIVATE0 = 16384,
            SDL_EVENT_PRIVATE1 = 16385,
            SDL_EVENT_PRIVATE2 = 16386,
            SDL_EVENT_PRIVATE3 = 16387,
            SDL_EVENT_POLL_SENTINEL = 32512,
            SDL_EVENT_USER = 32768,
            SDL_EVENT_LAST = 65535,
            SDL_EVENT_ENUM_PADDING = 2147483647,
        }

        public enum SDL_Keycode : uint
        {
            //SDLK_UNKNOWN = 0,
            //SDLK_BACKSPACE = 8,
            //SDLK_TAB = 9,
            //SDLK_RETURN = 13, // 0x0000000D
            //SDLK_ESCAPE = 27, // 0x0000001B
            //SDLK_SPACE = 32, // 0x00000020
            //SDLK_EXCLAIM = 33, // 0x00000021
            //SDLK_QUOTEDBL = 34, // 0x00000022
            //SDLK_HASH = 35, // 0x00000023
            //SDLK_DOLLAR = 36, // 0x00000024
            //SDLK_PERCENT = 37, // 0x00000025
            //SDLK_AMPERSAND = 38, // 0x00000026
            SDLK_QUOTE = 39, // 0x00000027
            //SDLK_LEFTPAREN = 40, // 0x00000028
            //SDLK_RIGHTPAREN = 41, // 0x00000029
            //SDLK_ASTERISK = 42, // 0x0000002A
            //SDLK_PLUS = 43, // 0x0000002B
            //SDLK_COMMA = 44, // 0x0000002C
            //SDLK_MINUS = 45, // 0x0000002D
            //SDLK_PERIOD = 46, // 0x0000002E
            //SDLK_SLASH = 47, // 0x0000002F
            //SDLK_0 = 48, // 0x00000030
            //SDLK_1 = 49, // 0x00000031
            //SDLK_2 = 50, // 0x00000032
            //SDLK_3 = 51, // 0x00000033
            //SDLK_4 = 52, // 0x00000034
            //SDLK_5 = 53, // 0x00000035
            //SDLK_6 = 54, // 0x00000036
            //SDLK_7 = 55, // 0x00000037
            //SDLK_8 = 56, // 0x00000038
            //SDLK_9 = 57, // 0x00000039
            //SDLK_COLON = 58, // 0x0000003A
            //SDLK_SEMICOLON = 59, // 0x0000003B
            //SDLK_LESS = 60, // 0x0000003C
            //SDLK_EQUALS = 61, // 0x0000003D
            //SDLK_GREATER = 62, // 0x0000003E
            //SDLK_QUESTION = 63, // 0x0000003F
            //SDLK_AT = 64, // 0x00000040
            //SDLK_LEFTBRACKET = 91, // 0x0000005B
            //SDLK_BACKSLASH = 92, // 0x0000005C
            //SDLK_RIGHTBRACKET = 93, // 0x0000005D
            //SDLK_CARET = 94, // 0x0000005E
            //SDLK_UNDERSCORE = 95, // 0x0000005F
            SDLK_BACKQUOTE = 96, // 0x00000060
            SDLK_a = 97, // 0x00000061
            SDLK_b = 98, // 0x00000062
            SDLK_c = 99, // 0x00000063
            SDLK_d = 100, // 0x00000064
            SDLK_e = 101, // 0x00000065
            SDLK_f = 102, // 0x00000066
            SDLK_g = 103, // 0x00000067
            SDLK_h = 104, // 0x00000068
            SDLK_i = 105, // 0x00000069
            SDLK_j = 106, // 0x0000006A
            SDLK_k = 107, // 0x0000006B
            SDLK_l = 108, // 0x0000006C
            SDLK_m = 109, // 0x0000006D
            SDLK_n = 110, // 0x0000006E
            SDLK_o = 111, // 0x0000006F
            SDLK_p = 112, // 0x00000070
            SDLK_q = 113, // 0x00000071
            SDLK_r = 114, // 0x00000072
            SDLK_s = 115, // 0x00000073
            SDLK_t = 116, // 0x00000074
            SDLK_u = 117, // 0x00000075
            SDLK_v = 118, // 0x00000076
            SDLK_w = 119, // 0x00000077
            SDLK_x = 120, // 0x00000078
            SDLK_y = 121, // 0x00000079
            SDLK_z = 122, // 0x0000007A
            //SDLK_DELETE = 127, // 0x0000007F
            //SDLK_CAPSLOCK = 1073741881, // 0x40000039
            //SDLK_F1 = 1073741882, // 0x4000003A
            //SDLK_F2 = 1073741883, // 0x4000003B
            //SDLK_F3 = 1073741884, // 0x4000003C
            //SDLK_F4 = 1073741885, // 0x4000003D
            //SDLK_F5 = 1073741886, // 0x4000003E
            //SDLK_F6 = 1073741887, // 0x4000003F
            //SDLK_F7 = 1073741888, // 0x40000040
            //SDLK_F8 = 1073741889, // 0x40000041
            //SDLK_F9 = 1073741890, // 0x40000042
            //SDLK_F10 = 1073741891, // 0x40000043
            //SDLK_F11 = 1073741892, // 0x40000044
            //SDLK_F12 = 1073741893, // 0x40000045
            //SDLK_PRINTSCREEN = 1073741894, // 0x40000046
            //SDLK_SCROLLLOCK = 1073741895, // 0x40000047
            //SDLK_PAUSE = 1073741896, // 0x40000048
            //SDLK_INSERT = 1073741897, // 0x40000049
            //SDLK_HOME = 1073741898, // 0x4000004A
            //SDLK_PAGEUP = 1073741899, // 0x4000004B
            //SDLK_END = 1073741901, // 0x4000004D
            //SDLK_PAGEDOWN = 1073741902, // 0x4000004E
            //SDLK_RIGHT = 1073741903, // 0x4000004F
            //SDLK_LEFT = 1073741904, // 0x40000050
            //SDLK_DOWN = 1073741905, // 0x40000051
            //SDLK_UP = 1073741906, // 0x40000052
            //SDLK_NUMLOCKCLEAR = 1073741907, // 0x40000053
            //SDLK_KP_DIVIDE = 1073741908, // 0x40000054
            //SDLK_KP_MULTIPLY = 1073741909, // 0x40000055
            //SDLK_KP_MINUS = 1073741910, // 0x40000056
            //SDLK_KP_PLUS = 1073741911, // 0x40000057
            //SDLK_KP_ENTER = 1073741912, // 0x40000058
            //SDLK_KP_1 = 1073741913, // 0x40000059
            //SDLK_KP_2 = 1073741914, // 0x4000005A
            //SDLK_KP_3 = 1073741915, // 0x4000005B
            //SDLK_KP_4 = 1073741916, // 0x4000005C
            //SDLK_KP_5 = 1073741917, // 0x4000005D
            //SDLK_KP_6 = 1073741918, // 0x4000005E
            //SDLK_KP_7 = 1073741919, // 0x4000005F
            //SDLK_KP_8 = 1073741920, // 0x40000060
            //SDLK_KP_9 = 1073741921, // 0x40000061
            //SDLK_KP_0 = 1073741922, // 0x40000062
            //SDLK_KP_PERIOD = 1073741923, // 0x40000063
            //SDLK_APPLICATION = 1073741925, // 0x40000065
            //SDLK_POWER = 1073741926, // 0x40000066
            //SDLK_KP_EQUALS = 1073741927, // 0x40000067
            //SDLK_F13 = 1073741928, // 0x40000068
            //SDLK_F14 = 1073741929, // 0x40000069
            //SDLK_F15 = 1073741930, // 0x4000006A
            //SDLK_F16 = 1073741931, // 0x4000006B
            //SDLK_F17 = 1073741932, // 0x4000006C
            //SDLK_F18 = 1073741933, // 0x4000006D
            //SDLK_F19 = 1073741934, // 0x4000006E
            //SDLK_F20 = 1073741935, // 0x4000006F
            //SDLK_F21 = 1073741936, // 0x40000070
            //SDLK_F22 = 1073741937, // 0x40000071
            //SDLK_F23 = 1073741938, // 0x40000072
            //SDLK_F24 = 1073741939, // 0x40000073
            //SDLK_EXECUTE = 1073741940, // 0x40000074
            //SDLK_HELP = 1073741941, // 0x40000075
            //SDLK_MENU = 1073741942, // 0x40000076
            //SDLK_SELECT = 1073741943, // 0x40000077
            //SDLK_STOP = 1073741944, // 0x40000078
            //SDLK_AGAIN = 1073741945, // 0x40000079
            //SDLK_UNDO = 1073741946, // 0x4000007A
            //SDLK_CUT = 1073741947, // 0x4000007B
            //SDLK_COPY = 1073741948, // 0x4000007C
            //SDLK_PASTE = 1073741949, // 0x4000007D
            //SDLK_FIND = 1073741950, // 0x4000007E
            //SDLK_MUTE = 1073741951, // 0x4000007F
            //SDLK_VOLUMEUP = 1073741952, // 0x40000080
            //SDLK_VOLUMEDOWN = 1073741953, // 0x40000081
            //SDLK_KP_COMMA = 1073741957, // 0x40000085
            //SDLK_KP_EQUALSAS400 = 1073741958, // 0x40000086
            //SDLK_ALTERASE = 1073741977, // 0x40000099
            //SDLK_SYSREQ = 1073741978, // 0x4000009A
            //SDLK_CANCEL = 1073741979, // 0x4000009B
            //SDLK_CLEAR = 1073741980, // 0x4000009C
            //SDLK_PRIOR = 1073741981, // 0x4000009D
            //SDLK_RETURN2 = 1073741982, // 0x4000009E
            //SDLK_SEPARATOR = 1073741983, // 0x4000009F
            //SDLK_OUT = 1073741984, // 0x400000A0
            //SDLK_OPER = 1073741985, // 0x400000A1
            //SDLK_CLEARAGAIN = 1073741986, // 0x400000A2
            //SDLK_CRSEL = 1073741987, // 0x400000A3
            //SDLK_EXSEL = 1073741988, // 0x400000A4
            //SDLK_KP_00 = 1073742000, // 0x400000B0
            //SDLK_KP_000 = 1073742001, // 0x400000B1
            //SDLK_THOUSANDSSEPARATOR = 1073742002, // 0x400000B2
            //SDLK_DECIMALSEPARATOR = 1073742003, // 0x400000B3
            //SDLK_CURRENCYUNIT = 1073742004, // 0x400000B4
            //SDLK_CURRENCYSUBUNIT = 1073742005, // 0x400000B5
            //SDLK_KP_LEFTPAREN = 1073742006, // 0x400000B6
            //SDLK_KP_RIGHTPAREN = 1073742007, // 0x400000B7
            //SDLK_KP_LEFTBRACE = 1073742008, // 0x400000B8
            //SDLK_KP_RIGHTBRACE = 1073742009, // 0x400000B9
            //SDLK_KP_TAB = 1073742010, // 0x400000BA
            //SDLK_KP_BACKSPACE = 1073742011, // 0x400000BB
            //SDLK_KP_A = 1073742012, // 0x400000BC
            //SDLK_KP_B = 1073742013, // 0x400000BD
            //SDLK_KP_C = 1073742014, // 0x400000BE
            //SDLK_KP_D = 1073742015, // 0x400000BF
            //SDLK_KP_E = 1073742016, // 0x400000C0
            //SDLK_KP_F = 1073742017, // 0x400000C1
            //SDLK_KP_XOR = 1073742018, // 0x400000C2
            //SDLK_KP_POWER = 1073742019, // 0x400000C3
            //SDLK_KP_PERCENT = 1073742020, // 0x400000C4
            //SDLK_KP_LESS = 1073742021, // 0x400000C5
            //SDLK_KP_GREATER = 1073742022, // 0x400000C6
            //SDLK_KP_AMPERSAND = 1073742023, // 0x400000C7
            //SDLK_KP_DBLAMPERSAND = 1073742024, // 0x400000C8
            //SDLK_KP_VERTICALBAR = 1073742025, // 0x400000C9
            //SDLK_KP_DBLVERTICALBAR = 1073742026, // 0x400000CA
            //SDLK_KP_COLON = 1073742027, // 0x400000CB
            //SDLK_KP_HASH = 1073742028, // 0x400000CC
            //SDLK_KP_SPACE = 1073742029, // 0x400000CD
            //SDLK_KP_AT = 1073742030, // 0x400000CE
            //SDLK_KP_EXCLAM = 1073742031, // 0x400000CF
            //SDLK_KP_MEMSTORE = 1073742032, // 0x400000D0
            //SDLK_KP_MEMRECALL = 1073742033, // 0x400000D1
            //SDLK_KP_MEMCLEAR = 1073742034, // 0x400000D2
            //SDLK_KP_MEMADD = 1073742035, // 0x400000D3
            //SDLK_KP_MEMSUBTRACT = 1073742036, // 0x400000D4
            //SDLK_KP_MEMMULTIPLY = 1073742037, // 0x400000D5
            //SDLK_KP_MEMDIVIDE = 1073742038, // 0x400000D6
            //SDLK_KP_PLUSMINUS = 1073742039, // 0x400000D7
            //SDLK_KP_CLEAR = 1073742040, // 0x400000D8
            //SDLK_KP_CLEARENTRY = 1073742041, // 0x400000D9
            //SDLK_KP_BINARY = 1073742042, // 0x400000DA
            //SDLK_KP_OCTAL = 1073742043, // 0x400000DB
            //SDLK_KP_DECIMAL = 1073742044, // 0x400000DC
            //SDLK_KP_HEXADECIMAL = 1073742045, // 0x400000DD
            //SDLK_LCTRL = 1073742048, // 0x400000E0
            //SDLK_LSHIFT = 1073742049, // 0x400000E1
            //SDLK_LALT = 1073742050, // 0x400000E2
            //SDLK_LGUI = 1073742051, // 0x400000E3
            //SDLK_RCTRL = 1073742052, // 0x400000E4
            //SDLK_RSHIFT = 1073742053, // 0x400000E5
            //SDLK_RALT = 1073742054, // 0x400000E6
            //SDLK_RGUI = 1073742055, // 0x400000E7
            //SDLK_MODE = 1073742081, // 0x40000101
            SDLK_AUDIONEXT = 1073742082, // 0x40000102
            SDLK_AUDIOPREV = 1073742083, // 0x40000103
            SDLK_AUDIOSTOP = 1073742084, // 0x40000104
            SDLK_AUDIOPLAY = 1073742085, // 0x40000105
            SDLK_AUDIOMUTE = 1073742086, // 0x40000106
            SDLK_MEDIASELECT = 1073742087, // 0x40000107
            //SDLK_WWW = 1073742088, // 0x40000108
            SDLK_MAIL = 1073742089, // 0x40000109
            //SDLK_CALCULATOR = 1073742090, // 0x4000010A
            //SDLK_COMPUTER = 1073742091, // 0x4000010B
            //SDLK_AC_SEARCH = 1073742092, // 0x4000010C
            //SDLK_AC_HOME = 1073742093, // 0x4000010D
            //SDLK_AC_BACK = 1073742094, // 0x4000010E
            //SDLK_AC_FORWARD = 1073742095, // 0x4000010F
            //SDLK_AC_STOP = 1073742096, // 0x40000110
            //SDLK_AC_REFRESH = 1073742097, // 0x40000111
            //SDLK_AC_BOOKMARKS = 1073742098, // 0x40000112
            //SDLK_BRIGHTNESSDOWN = 1073742099, // 0x40000113
            //SDLK_BRIGHTNESSUP = 1073742100, // 0x40000114
            //SDLK_DISPLAYSWITCH = 1073742101, // 0x40000115
            //SDLK_KBDILLUMTOGGLE = 1073742102, // 0x40000116
            //SDLK_KBDILLUMDOWN = 1073742103, // 0x40000117
            //SDLK_KBDILLUMUP = 1073742104, // 0x40000118
            //SDLK_EJECT = 1073742105, // 0x40000119
            //SDLK_SLEEP = 1073742106, // 0x4000011A
            SDLK_APP1 = 1073742107, // 0x4000011B
            SDLK_APP2 = 1073742108, // 0x4000011C
            //SDLK_AUDIOREWIND = 1073742109, // 0x4000011D
            //SDLK_AUDIOFASTFORWARD = 1073742110, // 0x4000011E
            // SDL3:
            SDLK_SCANCODE_MASK = 0x40000000,
            SDLK_UNKNOWN = 0x00000000u,
            SDLK_RETURN = 0x0000000du,
            SDLK_ESCAPE = 0x0000001bu,
            SDLK_BACKSPACE = 0x00000008u,
            SDLK_TAB = 0x00000009u,
            SDLK_SPACE = 0x00000020u,
            SDLK_EXCLAIM = 0x00000021u,
            SDLK_DBLAPOSTROPHE = 0x00000022u,
            SDLK_HASH = 0x00000023u,
            SDLK_DOLLAR = 0x00000024u,
            SDLK_PERCENT = 0x00000025u,
            SDLK_AMPERSAND = 0x00000026u,
            SDLK_APOSTROPHE = 0x00000027u,
            SDLK_LEFTPAREN = 0x00000028u,
            SDLK_RIGHTPAREN = 0x00000029u,
            SDLK_ASTERISK = 0x0000002au,
            SDLK_PLUS = 0x0000002bu,
            SDLK_COMMA = 0x0000002cu,
            SDLK_MINUS = 0x0000002du,
            SDLK_PERIOD = 0x0000002eu,
            SDLK_SLASH = 0x0000002fu,
            SDLK_0 = 0x00000030u,
            SDLK_1 = 0x00000031u,
            SDLK_2 = 0x00000032u,
            SDLK_3 = 0x00000033u,
            SDLK_4 = 0x00000034u,
            SDLK_5 = 0x00000035u,
            SDLK_6 = 0x00000036u,
            SDLK_7 = 0x00000037u,
            SDLK_8 = 0x00000038u,
            SDLK_9 = 0x00000039u,
            SDLK_COLON = 0x0000003au,
            SDLK_SEMICOLON = 0x0000003bu,
            SDLK_LESS = 0x0000003cu,
            SDLK_EQUALS = 0x0000003du,
            SDLK_GREATER = 0x0000003eu,
            SDLK_QUESTION = 0x0000003fu,
            SDLK_AT = 0x00000040u,
            SDLK_LEFTBRACKET = 0x0000005bu,
            SDLK_BACKSLASH = 0x0000005cu,
            SDLK_RIGHTBRACKET = 0x0000005du,
            SDLK_CARET = 0x0000005eu,
            SDLK_UNDERSCORE = 0x0000005fu,
            SDLK_GRAVE = 0x00000060u,
            SDLK_A = 0x00000061u,
            SDLK_B = 0x00000062u,
            SDLK_C = 0x00000063u,
            SDLK_D = 0x00000064u,
            SDLK_E = 0x00000065u,
            SDLK_F = 0x00000066u,
            SDLK_G = 0x00000067u,
            SDLK_H = 0x00000068u,
            SDLK_I = 0x00000069u,
            SDLK_J = 0x0000006au,
            SDLK_K = 0x0000006bu,
            SDLK_L = 0x0000006cu,
            SDLK_M = 0x0000006du,
            SDLK_N = 0x0000006eu,
            SDLK_O = 0x0000006fu,
            SDLK_P = 0x00000070u,
            SDLK_Q = 0x00000071u,
            SDLK_R = 0x00000072u,
            SDLK_S = 0x00000073u,
            SDLK_T = 0x00000074u,
            SDLK_U = 0x00000075u,
            SDLK_V = 0x00000076u,
            SDLK_W = 0x00000077u,
            SDLK_X = 0x00000078u,
            SDLK_Y = 0x00000079u,
            SDLK_Z = 0x0000007au,
            SDLK_LEFTBRACE = 0x0000007bu,
            SDLK_PIPE = 0x0000007cu,
            SDLK_RIGHTBRACE = 0x0000007du,
            SDLK_TILDE = 0x0000007eu,
            SDLK_DELETE = 0x0000007fu,
            SDLK_PLUSMINUS = 0x000000b1u,
            SDLK_CAPSLOCK = 0x40000039u,
            SDLK_F1 = 0x4000003au,
            SDLK_F2 = 0x4000003bu,
            SDLK_F3 = 0x4000003cu,
            SDLK_F4 = 0x4000003du,
            SDLK_F5 = 0x4000003eu,
            SDLK_F6 = 0x4000003fu,
            SDLK_F7 = 0x40000040u,
            SDLK_F8 = 0x40000041u,
            SDLK_F9 = 0x40000042u,
            SDLK_F10 = 0x40000043u,
            SDLK_F11 = 0x40000044u,
            SDLK_F12 = 0x40000045u,
            SDLK_PRINTSCREEN = 0x40000046u,
            SDLK_SCROLLLOCK = 0x40000047u,
            SDLK_PAUSE = 0x40000048u,
            SDLK_INSERT = 0x40000049u,
            SDLK_HOME = 0x4000004au,
            SDLK_PAGEUP = 0x4000004bu,
            SDLK_END = 0x4000004du,
            SDLK_PAGEDOWN = 0x4000004eu,
            SDLK_RIGHT = 0x4000004fu,
            SDLK_LEFT = 0x40000050u,
            SDLK_DOWN = 0x40000051u,
            SDLK_UP = 0x40000052u,
            SDLK_NUMLOCKCLEAR = 0x40000053u,
            SDLK_KP_DIVIDE = 0x40000054u,
            SDLK_KP_MULTIPLY = 0x40000055u,
            SDLK_KP_MINUS = 0x40000056u,
            SDLK_KP_PLUS = 0x40000057u,
            SDLK_KP_ENTER = 0x40000058u,
            SDLK_KP_1 = 0x40000059u,
            SDLK_KP_2 = 0x4000005au,
            SDLK_KP_3 = 0x4000005bu,
            SDLK_KP_4 = 0x4000005cu,
            SDLK_KP_5 = 0x4000005du,
            SDLK_KP_6 = 0x4000005eu,
            SDLK_KP_7 = 0x4000005fu,
            SDLK_KP_8 = 0x40000060u,
            SDLK_KP_9 = 0x40000061u,
            SDLK_KP_0 = 0x40000062u,
            SDLK_KP_PERIOD = 0x40000063u,
            SDLK_APPLICATION = 0x40000065u,
            SDLK_POWER = 0x40000066u,
            SDLK_KP_EQUALS = 0x40000067u,
            SDLK_F13 = 0x40000068u,
            SDLK_F14 = 0x40000069u,
            SDLK_F15 = 0x4000006au,
            SDLK_F16 = 0x4000006bu,
            SDLK_F17 = 0x4000006cu,
            SDLK_F18 = 0x4000006du,
            SDLK_F19 = 0x4000006eu,
            SDLK_F20 = 0x4000006fu,
            SDLK_F21 = 0x40000070u,
            SDLK_F22 = 0x40000071u,
            SDLK_F23 = 0x40000072u,
            SDLK_F24 = 0x40000073u,
            SDLK_EXECUTE = 0x40000074u,
            SDLK_HELP = 0x40000075u,
            SDLK_MENU = 0x40000076u,
            SDLK_SELECT = 0x40000077u,
            SDLK_STOP = 0x40000078u,
            SDLK_AGAIN = 0x40000079u,
            SDLK_UNDO = 0x4000007au,
            SDLK_CUT = 0x4000007bu,
            SDLK_COPY = 0x4000007cu,
            SDLK_PASTE = 0x4000007du,
            SDLK_FIND = 0x4000007eu,
            SDLK_MUTE = 0x4000007fu,
            SDLK_VOLUMEUP = 0x40000080u,
            SDLK_VOLUMEDOWN = 0x40000081u,
            SDLK_KP_COMMA = 0x40000085u,
            SDLK_KP_EQUALSAS400 = 0x40000086u,
            SDLK_ALTERASE = 0x40000099u,
            SDLK_SYSREQ = 0x4000009au,
            SDLK_CANCEL = 0x4000009bu,
            SDLK_CLEAR = 0x4000009cu,
            SDLK_PRIOR = 0x4000009du,
            SDLK_RETURN2 = 0x4000009eu,
            SDLK_SEPARATOR = 0x4000009fu,
            SDLK_OUT = 0x400000a0u,
            SDLK_OPER = 0x400000a1u,
            SDLK_CLEARAGAIN = 0x400000a2u,
            SDLK_CRSEL = 0x400000a3u,
            SDLK_EXSEL = 0x400000a4u,
            SDLK_KP_00 = 0x400000b0u,
            SDLK_KP_000 = 0x400000b1u,
            SDLK_THOUSANDSSEPARATOR = 0x400000b2u,
            SDLK_DECIMALSEPARATOR = 0x400000b3u,
            SDLK_CURRENCYUNIT = 0x400000b4u,
            SDLK_CURRENCYSUBUNIT = 0x400000b5u,
            SDLK_KP_LEFTPAREN = 0x400000b6u,
            SDLK_KP_RIGHTPAREN = 0x400000b7u,
            SDLK_KP_LEFTBRACE = 0x400000b8u,
            SDLK_KP_RIGHTBRACE = 0x400000b9u,
            SDLK_KP_TAB = 0x400000bau,
            SDLK_KP_BACKSPACE = 0x400000bbu,
            SDLK_KP_A = 0x400000bcu,
            SDLK_KP_B = 0x400000bdu,
            SDLK_KP_C = 0x400000beu,
            SDLK_KP_D = 0x400000bfu,
            SDLK_KP_E = 0x400000c0u,
            SDLK_KP_F = 0x400000c1u,
            SDLK_KP_XOR = 0x400000c2u,
            SDLK_KP_POWER = 0x400000c3u,
            SDLK_KP_PERCENT = 0x400000c4u,
            SDLK_KP_LESS = 0x400000c5u,
            SDLK_KP_GREATER = 0x400000c6u,
            SDLK_KP_AMPERSAND = 0x400000c7u,
            SDLK_KP_DBLAMPERSAND = 0x400000c8u,
            SDLK_KP_VERTICALBAR = 0x400000c9u,
            SDLK_KP_DBLVERTICALBAR = 0x400000cau,
            SDLK_KP_COLON = 0x400000cbu,
            SDLK_KP_HASH = 0x400000ccu,
            SDLK_KP_SPACE = 0x400000cdu,
            SDLK_KP_AT = 0x400000ceu,
            SDLK_KP_EXCLAM = 0x400000cfu,
            SDLK_KP_MEMSTORE = 0x400000d0u,
            SDLK_KP_MEMRECALL = 0x400000d1u,
            SDLK_KP_MEMCLEAR = 0x400000d2u,
            SDLK_KP_MEMADD = 0x400000d3u,
            SDLK_KP_MEMSUBTRACT = 0x400000d4u,
            SDLK_KP_MEMMULTIPLY = 0x400000d5u,
            SDLK_KP_MEMDIVIDE = 0x400000d6u,
            SDLK_KP_PLUSMINUS = 0x400000d7u,
            SDLK_KP_CLEAR = 0x400000d8u,
            SDLK_KP_CLEARENTRY = 0x400000d9u,
            SDLK_KP_BINARY = 0x400000dau,
            SDLK_KP_OCTAL = 0x400000dbu,
            SDLK_KP_DECIMAL = 0x400000dcu,
            SDLK_KP_HEXADECIMAL = 0x400000ddu,
            SDLK_LCTRL = 0x400000e0u,
            SDLK_LSHIFT = 0x400000e1u,
            SDLK_LALT = 0x400000e2u,
            SDLK_LGUI = 0x400000e3u,
            SDLK_RCTRL = 0x400000e4u,
            SDLK_RSHIFT = 0x400000e5u,
            SDLK_RALT = 0x400000e6u,
            SDLK_RGUI = 0x400000e7u,
            SDLK_MODE = 0x40000101u,
            SDLK_SLEEP = 0x40000102u,
            SDLK_WAKE = 0x40000103u,
            SDLK_CHANNEL_INCREMENT = 0x40000104u,
            SDLK_CHANNEL_DECREMENT = 0x40000105u,
            SDLK_MEDIA_PLAY = 0x40000106u,
            SDLK_MEDIA_PAUSE = 0x40000107u,
            SDLK_MEDIA_RECORD = 0x40000108u,
            SDLK_MEDIA_FAST_FORWARD = 0x40000109u,
            SDLK_MEDIA_REWIND = 0x4000010au,
            SDLK_MEDIA_NEXT_TRACK = 0x4000010bu,
            SDLK_MEDIA_PREVIOUS_TRACK = 0x4000010cu,
            SDLK_MEDIA_STOP = 0x4000010du,
            SDLK_MEDIA_EJECT = 0x4000010eu,
            SDLK_MEDIA_PLAY_PAUSE = 0x4000010fu,
            SDLK_MEDIA_SELECT = 0x40000110u,
            SDLK_AC_NEW = 0x40000111u,
            SDLK_AC_OPEN = 0x40000112u,
            SDLK_AC_CLOSE = 0x40000113u,
            SDLK_AC_EXIT = 0x40000114u,
            SDLK_AC_SAVE = 0x40000115u,
            SDLK_AC_PRINT = 0x40000116u,
            SDLK_AC_PROPERTIES = 0x40000117u,
            SDLK_AC_SEARCH = 0x40000118u,
            SDLK_AC_HOME = 0x40000119u,
            SDLK_AC_BACK = 0x4000011au,
            SDLK_AC_FORWARD = 0x4000011bu,
            SDLK_AC_STOP = 0x4000011cu,
            SDLK_AC_REFRESH = 0x4000011du,
            SDLK_AC_BOOKMARKS = 0x4000011eu,
            SDLK_SOFTLEFT = 0x4000011fu,
            SDLK_SOFTRIGHT = 0x40000120u,
            SDLK_CALL = 0x40000121u,
            SDLK_ENDCALL = 0x40000122u,
            SDLK_LEFT_TAB = 0x20000001u,
            SDLK_LEVEL5_SHIFT = 0x20000002u,
            SDLK_MULTI_KEY_COMPOSE = 0x20000003u,
            SDLK_LMETA = 0x20000004u,
            SDLK_RMETA = 0x20000005u,
            SDLK_LHYPER = 0x20000006u,
            SDLK_RHYPER = 0x20000007u,
        }

        /// <summary>
        /// The list of buttons available on a gamepad
        /// </summary>
        /// <remarks>
        /// Refer to the official <see href="https://wiki.libsdl.org/SDL3/SDL_GamepadButton">documentation</see> for more details.
        /// </remarks>
        public enum SDL_GamepadButton
        {
            /// <summary>
            /// Invalid button.
            /// </summary>
            Invalid = -1,

            /// <summary>
            /// Bottom face button (e.g. Xbox A button).
            /// </summary>
            South,

            /// <summary>
            /// Right face button (e.g. Xbox B button).
            /// </summary>
            East,

            /// <summary>
            /// Left face button (e.g. Xbox X button).
            /// </summary>
            West,

            /// <summary>
            /// Top face button (e.g. Xbox Y button).
            /// </summary>
            North,
            Back,
            Guide,
            Start,
            LeftStick,
            RightStick,
            LeftShoulder,
            RightShoulder,
            DPadUp,
            DPadDown,
            DPadLeft,
            DPadRight,

            /// <summary>
            /// Additional button (e.g. Xbox Series X share button, PS5 microphone button, Nintendo Switch Pro capture button,
            /// Amazon Luna microphone button, Google Stadia capture button)
            /// </summary>
            Misc1,

            /// <summary>
            /// Upper or primary paddle, under your right hand (e.g. Xbox Elite paddle P1).
            /// </summary>
            RightPaddle1,

            /// <summary>
            /// Upper or primary paddle, under your left hand (e.g. Xbox Elite paddle P3).
            /// </summary>
            LeftPaddle1,

            /// <summary>
            /// Lower or secondary paddle, under your right hand (e.g. Xbox Elite paddle P2).
            /// </summary>
            RightPaddle2,

            /// <summary>
            /// Lower or secondary paddle, under your left hand (e.g. Xbox Elite paddle P4).
            /// </summary>
            LeftPaddle2,

            /// <summary>
            /// PS4/PS5 touchpad button.
            /// </summary>
            Touchpad,

            /// <summary>
            /// Additional button.
            /// </summary>
            Misc2,

            /// <summary>
            /// Additional button.
            /// </summary>
            Misc3,

            /// <summary>
            /// Additional button.
            /// </summary>
            Misc4,

            /// <summary>
            /// Additional button.
            /// </summary>
            Misc5,

            /// <summary>
            /// Additional button.
            /// </summary>
            Misc6,

            /// <summary>
            /// The number of constants defined by the enumeration.
            /// </summary>
            Count,
            // SDL3:
            SDL_GAMEPAD_BUTTON_INVALID = -1,
            SDL_GAMEPAD_BUTTON_SOUTH = 0,
            SDL_GAMEPAD_BUTTON_EAST = 1,
            SDL_GAMEPAD_BUTTON_WEST = 2,
            SDL_GAMEPAD_BUTTON_NORTH = 3,
            SDL_GAMEPAD_BUTTON_BACK = 4,
            SDL_GAMEPAD_BUTTON_GUIDE = 5,
            SDL_GAMEPAD_BUTTON_START = 6,
            SDL_GAMEPAD_BUTTON_LEFT_STICK = 7,
            SDL_GAMEPAD_BUTTON_RIGHT_STICK = 8,
            SDL_GAMEPAD_BUTTON_LEFT_SHOULDER = 9,
            SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER = 10,
            SDL_GAMEPAD_BUTTON_DPAD_UP = 11,
            SDL_GAMEPAD_BUTTON_DPAD_DOWN = 12,
            SDL_GAMEPAD_BUTTON_DPAD_LEFT = 13,
            SDL_GAMEPAD_BUTTON_DPAD_RIGHT = 14,
            SDL_GAMEPAD_BUTTON_MISC1 = 15,
            SDL_GAMEPAD_BUTTON_RIGHT_PADDLE1 = 16,
            SDL_GAMEPAD_BUTTON_LEFT_PADDLE1 = 17,
            SDL_GAMEPAD_BUTTON_RIGHT_PADDLE2 = 18,
            SDL_GAMEPAD_BUTTON_LEFT_PADDLE2 = 19,
            SDL_GAMEPAD_BUTTON_TOUCHPAD = 20,
            SDL_GAMEPAD_BUTTON_MISC2 = 21,
            SDL_GAMEPAD_BUTTON_MISC3 = 22,
            SDL_GAMEPAD_BUTTON_MISC4 = 23,
            SDL_GAMEPAD_BUTTON_MISC5 = 24,
            SDL_GAMEPAD_BUTTON_MISC6 = 25,
            SDL_GAMEPAD_BUTTON_COUNT = 26,
        }

        [Flags]
        public enum SDL_Keymod : ushort
        {
            KMOD_NONE = 0,
            KMOD_LSHIFT = 1,
            KMOD_RSHIFT = 2,
            KMOD_LCTRL = 64, // 0x0040
            KMOD_RCTRL = 128, // 0x0080
            KMOD_LALT = 256, // 0x0100
            KMOD_RALT = 512, // 0x0200
            KMOD_LGUI = 1024, // 0x0400
            KMOD_RGUI = 2048, // 0x0800
            KMOD_NUM = 4096, // 0x1000
            KMOD_CAPS = 8192, // 0x2000
            KMOD_MODE = 16384, // 0x4000
            KMOD_RESERVED = 32768, // 0x8000
            KMOD_CTRL = KMOD_RCTRL | KMOD_LCTRL, // 0x00C0
            KMOD_SHIFT = KMOD_RSHIFT | KMOD_LSHIFT, // 0x0003
            KMOD_ALT = KMOD_RALT | KMOD_LALT, // 0x0300
            KMOD_GUI = KMOD_RGUI | KMOD_LGUI, // 0x0C00
            // same as above but renamed during SDL3 conversion
            SDL_KMOD_NONE = 0x0000,
            SDL_KMOD_LSHIFT = 0x0001,
            SDL_KMOD_RSHIFT = 0x0002,
            SDL_KMOD_LCTRL = 0x0040,
            SDL_KMOD_RCTRL = 0x0080,
            SDL_KMOD_LALT = 0x0100,
            SDL_KMOD_RALT = 0x0200,
            SDL_KMOD_LGUI = 0x0400,
            SDL_KMOD_RGUI = 0x0800,
            SDL_KMOD_NUM = 0x1000,
            SDL_KMOD_CAPS = 0x2000,
            SDL_KMOD_MODE = 0x4000,
            SDL_KMOD_SCROLL = 0x8000,
            SDL_KMOD_CTRL = SDL_KMOD_LCTRL | SDL_KMOD_RCTRL,
            SDL_KMOD_SHIFT = SDL_KMOD_LSHIFT | SDL_KMOD_RSHIFT,
            SDL_KMOD_ALT = SDL_KMOD_RALT | SDL_KMOD_LALT,
            SDL_KMOD_GUI = SDL_KMOD_RGUI | SDL_KMOD_LGUI,
        }

        public enum SDL_PACKEDLAYOUT_ENUM
        {
            SDL_PACKEDLAYOUT_1555
        }

        public enum SDL_PIXELORDER_ENUM
        {
            SDL_PACKEDORDER_ARGB = 3
        }

        public enum SDL_PIXELTYPE_ENUM
        {
            SDL_PIXELTYPE_PACKED16
        }

        public enum SDL_SYSWM_TYPE
        {
            SDL_SYSWM_WINDOWS
        }

        public enum SDL_WindowEventID : byte
        {
            SDL_WINDOWEVENT_ENTER,
            SDL_WINDOWEVENT_LEAVE,
            SDL_WINDOWEVENT_FOCUS_GAINED,
            SDL_WINDOWEVENT_FOCUS_LOST
        }

        public enum SDL_PixelFormat
        {
            SDL_PIXELFORMAT_UNKNOWN = 0,
            SDL_PIXELFORMAT_INDEX1LSB = 286261504,
            SDL_PIXELFORMAT_INDEX1MSB = 287310080,
            SDL_PIXELFORMAT_INDEX2LSB = 470811136,
            SDL_PIXELFORMAT_INDEX2MSB = 471859712,
            SDL_PIXELFORMAT_INDEX4LSB = 303039488,
            SDL_PIXELFORMAT_INDEX4MSB = 304088064,
            SDL_PIXELFORMAT_INDEX8 = 318769153,
            SDL_PIXELFORMAT_RGB332 = 336660481,
            SDL_PIXELFORMAT_XRGB4444 = 353504258,
            SDL_PIXELFORMAT_XBGR4444 = 357698562,
            SDL_PIXELFORMAT_XRGB1555 = 353570562,
            SDL_PIXELFORMAT_XBGR1555 = 357764866,
            SDL_PIXELFORMAT_ARGB4444 = 355602434,
            SDL_PIXELFORMAT_RGBA4444 = 356651010,
            SDL_PIXELFORMAT_ABGR4444 = 359796738,
            SDL_PIXELFORMAT_BGRA4444 = 360845314,
            SDL_PIXELFORMAT_ARGB1555 = 355667970,
            SDL_PIXELFORMAT_RGBA5551 = 356782082,
            SDL_PIXELFORMAT_ABGR1555 = 359862274,
            SDL_PIXELFORMAT_BGRA5551 = 360976386,
            SDL_PIXELFORMAT_RGB565 = 353701890,
            SDL_PIXELFORMAT_BGR565 = 357896194,
            SDL_PIXELFORMAT_RGB24 = 386930691,
            SDL_PIXELFORMAT_BGR24 = 390076419,
            SDL_PIXELFORMAT_XRGB8888 = 370546692,
            SDL_PIXELFORMAT_RGBX8888 = 371595268,
            SDL_PIXELFORMAT_XBGR8888 = 374740996,
            SDL_PIXELFORMAT_BGRX8888 = 375789572,
            SDL_PIXELFORMAT_ARGB8888 = 372645892,
            SDL_PIXELFORMAT_RGBA8888 = 373694468,
            SDL_PIXELFORMAT_ABGR8888 = 376840196,
            SDL_PIXELFORMAT_BGRA8888 = 377888772,
            SDL_PIXELFORMAT_XRGB2101010 = 370614276,
            SDL_PIXELFORMAT_XBGR2101010 = 374808580,
            SDL_PIXELFORMAT_ARGB2101010 = 372711428,
            SDL_PIXELFORMAT_ABGR2101010 = 376905732,
            SDL_PIXELFORMAT_RGB48 = 403714054,
            SDL_PIXELFORMAT_BGR48 = 406859782,
            SDL_PIXELFORMAT_RGBA64 = 404766728,
            SDL_PIXELFORMAT_ARGB64 = 405815304,
            SDL_PIXELFORMAT_BGRA64 = 407912456,
            SDL_PIXELFORMAT_ABGR64 = 408961032,
            SDL_PIXELFORMAT_RGB48_FLOAT = 437268486,
            SDL_PIXELFORMAT_BGR48_FLOAT = 440414214,
            SDL_PIXELFORMAT_RGBA64_FLOAT = 438321160,
            SDL_PIXELFORMAT_ARGB64_FLOAT = 439369736,
            SDL_PIXELFORMAT_BGRA64_FLOAT = 441466888,
            SDL_PIXELFORMAT_ABGR64_FLOAT = 442515464,
            SDL_PIXELFORMAT_RGB96_FLOAT = 454057996,
            SDL_PIXELFORMAT_BGR96_FLOAT = 457203724,
            SDL_PIXELFORMAT_RGBA128_FLOAT = 455114768,
            SDL_PIXELFORMAT_ARGB128_FLOAT = 456163344,
            SDL_PIXELFORMAT_BGRA128_FLOAT = 458260496,
            SDL_PIXELFORMAT_ABGR128_FLOAT = 459309072,
            SDL_PIXELFORMAT_YV12 = 842094169,
            SDL_PIXELFORMAT_IYUV = 1448433993,
            SDL_PIXELFORMAT_YUY2 = 844715353,
            SDL_PIXELFORMAT_UYVY = 1498831189,
            SDL_PIXELFORMAT_YVYU = 1431918169,
            SDL_PIXELFORMAT_NV12 = 842094158,
            SDL_PIXELFORMAT_NV21 = 825382478,
            SDL_PIXELFORMAT_P010 = 808530000,
            SDL_PIXELFORMAT_EXTERNAL_OES = 542328143,
            SDL_PIXELFORMAT_RGBA32 = 376840196,
            SDL_PIXELFORMAT_ARGB32 = 377888772,
            SDL_PIXELFORMAT_BGRA32 = 372645892,
            SDL_PIXELFORMAT_ABGR32 = 373694468,
            SDL_PIXELFORMAT_RGBX32 = 374740996,
            SDL_PIXELFORMAT_XRGB32 = 375789572,
            SDL_PIXELFORMAT_BGRX32 = 370546692,
            SDL_PIXELFORMAT_XBGR32 = 371595268,
        }

        [Flags]
        public enum SDL_MouseButtonFlags : uint
        {
            SDL_BUTTON_LMASK = 0x1,
            SDL_BUTTON_MMASK = 0x2,
            SDL_BUTTON_RMASK = 0x4,
            SDL_BUTTON_X1MASK = 0x08,
            SDL_BUTTON_X2MASK = 0x10,
        }

        public const string SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH = "SDL_MOUSE_FOCUS_CLICKTHROUGH";
        public const string SDL_HINT_ENABLE_SCREEN_KEYBOARD = "SDL_ENABLE_SCREEN_KEYBOARD";
        public const uint SDL_BUTTON_LEFT = 1;
        public const uint SDL_BUTTON_MIDDLE = 2;
        public const uint SDL_BUTTON_RIGHT = 3;
        public const uint SDL_BUTTON_X1 = 4;
        public const uint SDL_BUTTON_X2 = 5;

        public static readonly uint SDL_PIXELFORMAT_ARGB1555 = SDL_DEFINE_PIXELFORMAT(
            SDL_PIXELTYPE_ENUM.SDL_PIXELTYPE_PACKED16, SDL_PIXELORDER_ENUM.SDL_PACKEDORDER_ARGB,
            SDL_PACKEDLAYOUT_ENUM.SDL_PACKEDLAYOUT_1555, 16, 2);

        public static readonly uint SDL_PIXELFORMAT_ABGR8888;

        internal static byte[] UTF8_ToNative(string s)
        {
            if (s == null)
                return null;
            return Encoding.UTF8.GetBytes(s + "\0");
        }

        public static unsafe string UTF8_ToManaged(IntPtr s, bool freePtr = false)
        {
            if (s == IntPtr.Zero)
                return null;
            var numPtr = (byte*)(void*)s;
            while (*numPtr != 0)
                ++numPtr;
            var num = (int)(numPtr - (byte*)(void*)s);
            if (num == 0)
                return string.Empty;
            var chars1 = stackalloc char[num];
            var chars2 = Encoding.UTF8.GetChars((byte*)(void*)s, num, chars1, num);
            var str = new string(chars1, 0, chars2);
            if (!freePtr)
                return str;
            SDL_free(s);
            return str;
        }

        internal static void SDL_free(IntPtr memblock)
        {
        }

        public static void SDL_VERSION(out SDL_version x)
        {
            x.major = 2;
            x.minor = 0;
            x.patch = 9;
        }

        private static IntPtr INTERNAL_SDL_GL_GetProcAddress(byte[] proc)
        {
            return IntPtr.Zero;
        }

        public static IntPtr SDL_GL_GetProcAddress(string proc)
        {
            return INTERNAL_SDL_GL_GetProcAddress(UTF8_ToNative(proc));
        }

        public static uint SDL_DEFINE_PIXELFORMAT(
            SDL_PIXELTYPE_ENUM type,
            SDL_PIXELORDER_ENUM order,
            SDL_PACKEDLAYOUT_ENUM layout,
            byte bits,
            byte bytes)
        {
            return (uint)(268435456 | ((byte)type << 24) | ((byte)order << 20) | ((byte)layout << 16) |
                           (bits << 8)) | bytes;
        }

        private const uint SDL_PREALLOC = 0x00000001;  // Surface uses preallocated memory

        public static IntPtr SDL_CreateRGBSurfaceWithFormatFrom(
            IntPtr pixels,
            int width,
            int height,
            int depth,
            int pitch,
            uint format)
        {
            // Parameter validation
            //if (pixels == IntPtr.Zero)
            //{
            //    return IntPtr.Zero;
            //}

            //if (width < 0)
            //{
            //    return IntPtr.Zero;
            //}

            //if (height < 0)
            //{
            //    return IntPtr.Zero;
            //}

            //// Create a new SDL_Surface structure
            //SDL_Surface surface = new SDL_Surface
            //{
            //    flags = (uint)SDL_PREALLOC, // Mark as using external pixel data
            //    pixels = pixels,
            //    w = width,
            //    h = height,
            //    pitch = pitch,
            //    locked = 0,
            //    refcount = 1
            //};

            //// Create format structure
            //SDL_PixelFormat pixelFormat = new SDL_PixelFormat
            //{
            //    format = format
            //};

            //// Allocate memory for the surface structure and copy the data
            //IntPtr surfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<SDL_Surface>());
            //IntPtr formatPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SDL_PixelFormat>());

            //// Copy the format structure to unmanaged memory
            //Marshal.StructureToPtr(pixelFormat, formatPtr, false);

            //// Set the format pointer in the surface
            //surface.format = formatPtr;

            //// Set default clip rectangle
            //surface.clip_rect = new SDL_Rect
            //{
            //    x = 0,
            //    y = 0,
            //    w = width,
            //    h = height
            //};

            //// Copy the surface structure to unmanaged memory
            //Marshal.StructureToPtr(surface, surfacePtr, false);

            //return surfacePtr;
            return IntPtr.Zero;
        }

        public static void SDL_FreeSurface(IntPtr surface)
        {
            if (surface == IntPtr.Zero)
                return;

            // Get the surface structure
            SDL_Surface surfaceStruct = Marshal.PtrToStructure<SDL_Surface>(surface);

            // Free the pixel format if it exists
            if (surfaceStruct.format != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(surfaceStruct.format);
            }

            // Free the surface structure itself
            Marshal.FreeHGlobal(surface);
        }

        public static void SDL_DestroySurface(IntPtr surface)
        {

        }

        public static SDLBool SDL_HasClipboardText()
        {
            return new SDLBool();
            //return SDL_bool.SDL_FALSE;
        }

        private static IntPtr INTERNAL_SDL_GetClipboardText()
        {
            return IntPtr.Zero;
        }

        public static string SDL_GetClipboardText()
        {
            return UTF8_ToManaged(INTERNAL_SDL_GetClipboardText());
        }

        private static int INTERNAL_SDL_SetClipboardText(byte[] text)
        {
            return 1;
        }

        public static int SDL_SetClipboardText(string text)
        {
            return INTERNAL_SDL_SetClipboardText(UTF8_ToNative(text));
        }

        public static SDLBool SDL_CaptureMouse(SDLBool enabled)
        {
            return new SDLBool();
        }

        public static IntPtr SDL_CreateColorCursor(IntPtr surface, int hot_x, int hot_y)
        {
            return IntPtr.Zero;
        }

        public static void SDL_SetCursor(IntPtr cursor)
        {
        }

        public static void SDL_FreeCursor(IntPtr cursor) { }

        public static uint SDL_GetTicks()
        {
            return 0;
        }

        public static SDL_bool SDL_GetWindowWMInfo(
            IntPtr window,
            ref SDL_SysWMinfo info)
        {
            return SDL_bool.SDL_TRUE;
        }

        public static IntPtr SDL_GL_GetCurrentWindow()
        {
            return IntPtr.Zero;
        }

        public struct SDL_version
        {
            public byte major;
            public byte minor;
            public byte patch;
        }

        public struct SDL_DisplayEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint display;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public int data1;
        }

        public struct SDL_WindowEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public SDL_WindowEventID windowEvent;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public int data1;
            public int data2;
        }

        public struct SDL_KeyboardEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public byte state;
            //public byte repeat;
            public SDLBool repeat;
            private byte padding2;
            private byte padding3;
            public SDL_Keysym keysym;
            public uint key;
            public SDL_Keymod mod;
        }

        public struct SDL_TextEditingEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public unsafe fixed byte text[32];
            public int start;
            public int length;
        }

        public struct SDL_TextInputEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public unsafe fixed byte text[32];
        }

        public struct SDL_MouseMotionEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public uint which;
            public byte state;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public int x;
            public int y;
            public int xrel;
            public int yrel;
        }

        public struct SDL_MouseButtonEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public uint which;
            public byte button;
            public byte state;
            public byte clicks;
            private byte padding1;
            public int x;
            public int y;
        }

        public struct SDL_MouseWheelEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public uint windowID;
            public uint which;
            public int x;
            public int y;
            public uint direction;
        }

        public struct SDL_JoyAxisEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte axis;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public short axisValue;
            public ushort padding4;
        }

        public struct SDL_JoyBallEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte ball;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public short xrel;
            public short yrel;
        }

        public struct SDL_JoyHatEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte hat;
            public byte hatValue;
            private byte padding1;
            private byte padding2;
        }

        public struct SDL_JoyButtonEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte button;
            public byte state;
            private byte padding1;
            private byte padding2;
        }

        public struct SDL_JoyDeviceEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
        }

        public struct SDL_ControllerAxisEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte axis;
            private byte padding1;
            private byte padding2;
            private byte padding3;
            public short axisValue;
            private ushort padding4;
        }

        public struct SDL_ControllerButtonEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public byte button;
            public byte state;
            private byte padding1;
            private byte padding2;
        }

        public struct SDL_ControllerDeviceEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
        }

        public struct SDL_AudioDeviceEvent
        {
            public uint type;
            public uint timestamp;
            public uint which;
            public byte iscapture;
            private byte padding1;
            private byte padding2;
            private byte padding3;
        }

        public struct SDL_TouchFingerEvent
        {
            public uint type;
            public uint timestamp;
            public long touchId;
            public long fingerId;
            public float x;
            public float y;
            public float dx;
            public float dy;
            public float pressure;
        }

        public struct SDL_MultiGestureEvent
        {
            public uint type;
            public uint timestamp;
            public long touchId;
            public float dTheta;
            public float dDist;
            public float x;
            public float y;
            public ushort numFingers;
            public ushort padding;
        }

        public struct SDL_DollarGestureEvent
        {
            public uint type;
            public uint timestamp;
            public long touchId;
            public long gestureId;
            public uint numFingers;
            public float error;
            public float x;
            public float y;
        }

        public struct SDL_DropEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public IntPtr file;
            public uint windowID;
        }

        public struct SDL_SensorEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public int which;
            public unsafe fixed float data[6];
        }

        public struct SDL_QuitEvent
        {
            public SDL_EventType type;
            public uint timestamp;
        }

        public struct SDL_UserEvent
        {
            public uint type;
            public uint timestamp;
            public uint windowID;
            public int code;
            public IntPtr data1;
            public IntPtr data2;
        }

        public struct SDL_SysWMEvent
        {
            public SDL_EventType type;
            public uint timestamp;
            public IntPtr msg;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SDL_GamepadButtonEvent
        {
            public SDL_EventType type;
            public uint reserved;
            public ulong timestamp;
            public uint which;
            public byte button;
            public SDLBool down;
            public byte padding1;
            public byte padding2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SDL_GamepadAxisEvent
        {
            public SDL_EventType type;
            public uint reserved;
            public ulong timestamp;
            public uint which;
            public byte axis;
            public byte padding1;
            public byte padding2;
            public byte padding3;
            public short value;
            public ushort padding4;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SDL_Event
        {
            [FieldOffset(0)]
            public uint type;
            //[FieldOffset(0)] public SDL_EventType type;
            [FieldOffset(0)] public SDL_DisplayEvent display;
            [FieldOffset(0)] public SDL_WindowEvent window;
            [FieldOffset(0)] public SDL_KeyboardEvent key;
            [FieldOffset(0)] public SDL_TextEditingEvent edit;
            [FieldOffset(0)] public SDL_TextInputEvent text;
            [FieldOffset(0)] public SDL_MouseMotionEvent motion;
            [FieldOffset(0)] public SDL_MouseButtonEvent button;
            [FieldOffset(0)] public SDL_MouseWheelEvent wheel;
            [FieldOffset(0)] public SDL_JoyAxisEvent jaxis;
            [FieldOffset(0)] public SDL_JoyBallEvent jball;
            [FieldOffset(0)] public SDL_JoyHatEvent jhat;
            [FieldOffset(0)] public SDL_JoyButtonEvent jbutton;
            [FieldOffset(0)] public SDL_JoyDeviceEvent jdevice;
            [FieldOffset(0)] public SDL_ControllerAxisEvent caxis;
            [FieldOffset(0)] public SDL_ControllerButtonEvent cbutton;
            [FieldOffset(0)] public SDL_ControllerDeviceEvent cdevice;
            [FieldOffset(0)] public SDL_AudioDeviceEvent adevice;
            [FieldOffset(0)] public SDL_SensorEvent sensor;
            [FieldOffset(0)] public SDL_QuitEvent quit;
            [FieldOffset(0)] public SDL_UserEvent user;
            [FieldOffset(0)] public SDL_SysWMEvent syswm;
            [FieldOffset(0)] public SDL_TouchFingerEvent tfinger;
            [FieldOffset(0)] public SDL_MultiGestureEvent mgesture;
            [FieldOffset(0)] public SDL_DollarGestureEvent dgesture;
            [FieldOffset(0)] public SDL_DropEvent drop;
            [FieldOffset(0)] public SDL_GamepadButtonEvent gbutton;
            [FieldOffset(0)] public SDL_GamepadAxisEvent gaxis;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool SDL_EventFilter(IntPtr userdata, IntPtr sdlevent);

        public static void SDL_AddEventWatch(SDL.SDL_EventFilter filter, IntPtr userdata)
        { }

        public static void SDL_SetEventFilter(SDL.SDL_EventFilter filter, IntPtr userdata)
        { }

        public struct SDL_Keysym
        {
            public SDL_Keycode sym;
            public SDL_Keymod mod;
        }

        public struct INTERNAL_windows_wminfo
        {
            public IntPtr window;
            public IntPtr hdc;
        }

        public struct INTERNAL_winrt_wminfo
        {
            public IntPtr window;
        }

        public struct INTERNAL_x11_wminfo
        {
            public IntPtr display;
            public IntPtr window;
        }

        public struct INTERNAL_directfb_wminfo
        {
            public IntPtr dfb;
            public IntPtr window;
            public IntPtr surface;
        }

        public struct INTERNAL_cocoa_wminfo
        {
            public IntPtr window;
        }

        public struct INTERNAL_uikit_wminfo
        {
            public IntPtr window;
            public uint framebuffer;
            public uint colorbuffer;
            public uint resolveFramebuffer;
        }

        public struct INTERNAL_wayland_wminfo
        {
            public IntPtr display;
            public IntPtr surface;
            public IntPtr shell_surface;
        }

        public struct INTERNAL_mir_wminfo
        {
            public IntPtr connection;
            public IntPtr surface;
        }

        public struct INTERNAL_android_wminfo
        {
            public IntPtr window;
            public IntPtr surface;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INTERNAL_SysWMDriverUnion
        {
            [FieldOffset(0)] public INTERNAL_windows_wminfo win;
            [FieldOffset(0)] public INTERNAL_winrt_wminfo winrt;
            [FieldOffset(0)] public INTERNAL_x11_wminfo x11;
            [FieldOffset(0)] public INTERNAL_directfb_wminfo dfb;
            [FieldOffset(0)] public INTERNAL_cocoa_wminfo cocoa;
            [FieldOffset(0)] public INTERNAL_uikit_wminfo uikit;
            [FieldOffset(0)] public INTERNAL_wayland_wminfo wl;
            [FieldOffset(0)] public INTERNAL_mir_wminfo mir;
            [FieldOffset(0)] public INTERNAL_android_wminfo android;
        }

        public struct SDL_SysWMinfo
        {
            public SDL_version version;
            public SDL_SYSWM_TYPE subsystem;
            public INTERNAL_SysWMDriverUnion info;
        }

        // Taken from https://github.com/ppy/SDL3-CS
        // C# bools are not blittable, so we need this workaround
        public struct SDLBool
        {
            private readonly byte value;

            internal const byte FALSE_VALUE = 0;
            internal const byte TRUE_VALUE = 1;

            internal SDLBool(byte value)
            {
                this.value = value;
            }

            public static implicit operator bool(SDLBool b)
            {
                return b.value != FALSE_VALUE;
            }

            public static implicit operator SDLBool(bool b)
            {
                return new SDLBool(b ? TRUE_VALUE : FALSE_VALUE);
            }

            public bool Equals(SDLBool other)
            {
                return other.value == value;
            }

            public override bool Equals(object rhs)
            {
                if (rhs is bool)
                {
                    return Equals((SDLBool)(bool)rhs);
                }
                else if (rhs is SDLBool)
                {
                    return Equals((SDLBool)rhs);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return value.GetHashCode();
            }
        }

        public static void SDL_ShowSimpleMessageBox(SDL_MessageBoxFlags sdlMessageboxError, string error, string msg, IntPtr windowHandle)
        {
        }

        public static SDL_MouseButtonFlags SDL_GetGlobalMouseState(out float i, out float i1)
        {
            i = 0;
            i1 = 0;

            return new SDL_MouseButtonFlags();
        }

        public static void SDL_GetWindowPosition(IntPtr windowHandle, out int i, out int i1)
        {
            i = 0;
            i1 = 0;
        }

        public static SDL_MouseButtonFlags SDL_GetMouseState(out float positionX, out float positionY)
        {
            positionX = 0;
            positionY = 0;

            return new SDL_MouseButtonFlags();
        }

        public static void SDL_GetWindowBordersSize(IntPtr windowHandle, out int i, out int i1, out int i2, out int i3)
        {
            i = 0;
            i1 = 0;
            i2 = 0;
            i3 = 0;
        }

        public static void SDL_SetWindowPosition(IntPtr windowHandle, int i, int i1)
        {
        }

        public enum SDL_WindowFlags
        {
            SDL_WINDOW_BORDERLESS = 0,
            SDL_WINDOW_MAXIMIZED = 1
        }

        public static SDL_WindowFlags SDL_GetWindowFlags(IntPtr windowHandle)
        {
            return SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
        }

        public static void SDL_SetWindowBordered(IntPtr window, SDLBool bordered)
        {
        }

        public static void SDL_SetWindowMinimumSize(IntPtr windowHandle, int width, int height)
        {

        }

        public static void SDL_GetCurrentDisplayMode(int i, out SDL_DisplayMode sdlDisplayMode)
        {
            sdlDisplayMode = new SDL_DisplayMode();
        }

        public static int SDL_GetWindowDisplayIndex(IntPtr windowHandle)
        {
            return 0;
        }

        public static int SDL_GetDisplayUsableBounds(uint displayID, out SDL_Rect rect)
        {
            rect = new SDL_Rect();
            rect.x = 0;
            rect.y = 0;

            return 0;
        }

        public struct SDL_DisplayMode
        {
            public int w;
            public int h;
            public float refresh_rate;
        }

        public struct SDL_Surface
        {
            public uint flags;
            public IntPtr format;
            public int w;
            public int h;
            public int pitch;
            public IntPtr pixels;
            public IntPtr userdata;
            public int locked;
            public IntPtr lock_data;
            public SDL.SDL_Rect clip_rect;
            public IntPtr map;
            public int refcount;
        }

        public struct SDL_Rect
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }

        public static void SDL_MaximizeWindow(IntPtr windowHandle)
        {
        }

        public static void SDL_RestoreWindow(IntPtr windowHandle)
        {
        }

        //NOTE: Not implemented properly on purpose. Any usages so far are not relevant to MobileUO
        public static string SDL_GetPlatform()
        {
            return string.Empty;
        }

        public static unsafe SDL_Surface* SDL_CreateRGBSurface(int i, int biWidth, int biHeight, int i1, int i2, int i3, int i4, uint u)
        {
            throw new NotImplementedException();
        }

        //public struct SDL_PixelFormat
        //{
        //    public uint format;
        //}

        public static IntPtr SDL_ConvertSurfaceFormat(IntPtr surface, uint sdlPixelformatAbgr8888, int i)
        {
            throw new NotImplementedException();
        }

        public static IntPtr SDL_CreateSurface(int width, int height, SDL_PixelFormat format)
        {
            return IntPtr.Zero;
        }

        public static IntPtr SDL_CreateSurfaceFrom(int width, int height, SDL_PixelFormat format, IntPtr pixels, int pitch)
        {
            return IntPtr.Zero;
        }

        public static SDLBool SDL_SetHint(string name, string value)
        {
            return new SDLBool();
        }

        public static SDLBool SDL_StartTextInput(IntPtr window)
        {
            return new SDLBool();
        }

        public static uint SDL_GetDisplayForWindow(IntPtr window)
        {
            return 0;
        }

        public static IntPtr SDL_GetCurrentDisplayMode(uint displayID)
        {
            return IntPtr.Zero;
        }

        public static SDLBool SDL_GetDisplayBounds(uint displayID, out SDL_Rect rect)
        {
            rect = new SDL_Rect();
            return new SDLBool();
        }

        public static SDLBool SDL_PushEvent(ref SDL_Event @event)
        {
            return new SDLBool();
        }

        public static void SDL_WarpMouseInWindow(IntPtr window, float x, float y)
        {

        }
    }
}