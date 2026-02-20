namespace PreferenceEnums
{
    public enum ScaleSizes
    {
        Fifty = 50,
        SeventyFive = 75,
        One = 100,
        OneTwentyFive = 125,
        OneHalf = 150,
        OneSeventyFive = 175,
        Two = 200
    }

    public enum TargetFrameRates
    {
        _30 = 30,
        _60 = 60,
        InGameFPS = -1
    }


    public enum JoystickSizes
    {
        Small = 0,
        Normal = 1,
        Large = 2,
        Custom = 3
    }

    public enum UseMouseOnMobile
    {
        Off = 0,
        On = 1
    }
    
    public enum ShowCloseButtons
    {
        Off = 0,
        On = 1
    }

    public enum TextureFilterMode
    {
        Sharp = 0,
        Smooth = 1
    }

    public enum JoystickOpacity
    {
        VeryLow = 0,
        Low = 1,
        Normal = 2,
        High = 3
    }
    
    public enum JoystickDeadZone
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
    
    public enum JoystickRunThreshold
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum ContainerItemSelection
    {
        Coarse = 0,
        Fine = 1
    }

    public enum ForceUseXbr
    {
        Off = 0,
        On = 1
    }

    public enum VisualizeFingerInput
    {
        Off = 0,
        On = 1
    }

    public enum ShowModifierKeyButtons
    {
        Off = 0,
        On = 1
    }

    public enum DisableTouchscreenKeyboardOnMobile
    {
        Off = 0,
        On = 1
    }
    
    public enum EnableAssistant
    {
        Off = 0,
        On = 1
    }
    
    public enum ShowDebugConsole
    {
        Off = 0,
        On = 1
    }
    
    public enum AssistantMinimized
    {
        Off = 0,
        On = 1
    }
    
    public enum EnlargeSmallButtons
    {
        Off = 0,
        On = 1
    }
    
    public enum UseLegacyJoystick
    {
        Off = 0,
        On = 1
    }
    
    public enum JoystickCancelsFollow
    {
        Off = 0,
        On = 1
    }

    // MobileUO: TODO: only for master branch, comment these out as they aren't used yet (requires newer CUO changes from dev branch)
    //public enum UseDrawTexture
    //{
    //    Off = 0,
    //    On = 1
    //}

    //public enum UseSpriteSheet
    //{
    //    Off = 0,
    //    On = 1
    //}

    //public enum SpriteSheetSize
    //{
    //    Small = 1024,
    //    Medium = 2048,
    //    Large = 4096,
    //}

    //public enum UseProfiler
    //{
    //    Off = 0,
    //    On = 1
    //}

    public enum ShowErrorDetails
    {
        Off = 0,
        On = 1
    }
}