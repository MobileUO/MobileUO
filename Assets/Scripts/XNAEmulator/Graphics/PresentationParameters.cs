using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public class PresentationParameters
    {
        public PresentationParameters()
        {
            settings = new Settings(0);
        }

        public PresentationParameters Clone()
        {
            return new PresentationParameters
            {
                settings = this.settings,
                BackBufferWidth = this.BackBufferWidth ,
                BackBufferHeight = this.BackBufferHeight,
        };
        }

        public int BackBufferWidth
        {
            get
            {
                return this.settings.BackBufferWidth;
            }
            set
            {
                this.settings.BackBufferWidth = value;
            }
        }

        public int BackBufferHeight
        {
            get
            {
                return this.settings.BackBufferHeight;
            }
            set
            {
                this.settings.BackBufferHeight = value;
            }
        }

        public SurfaceFormat BackBufferFormat
        {
            get
            {
                return this.settings.BackBufferFormat;
            }
            set
            {
                this.settings.BackBufferFormat = value;
            }
        }

        public DepthFormat DepthStencilFormat
        {
            get
            {
                return this.settings.DepthStencilFormat;
            }
            set
            {
                this.settings.DepthStencilFormat = value;
            }
        }

        public int MultiSampleCount
        {
            get
            {
                return this.settings.MultiSampleCount;
            }
            set
            {
                this.settings.MultiSampleCount = value;
            }
        }

        public DisplayOrientation DisplayOrientation
        {
            get
            {
                return this.settings.DisplayOrientation;
            }
            set
            {
                this.settings.DisplayOrientation = value;
            }
        }

        public PresentInterval PresentationInterval
        {
            get
            {
                return this.settings.PresentationInterval;
            }
            set
            {
                this.settings.PresentationInterval = value;
            }
        }

        public RenderTargetUsage RenderTargetUsage
        {
            get
            {
                return this.settings.RenderTargetUsage;
            }
            set
            {
                this.settings.RenderTargetUsage = value;
            }
        }

        public IntPtr DeviceWindowHandle
        {
            get
            {
                return this.settings.DeviceWindowHandle;
            }
            set
            {
                this.settings.DeviceWindowHandle = value;
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return this.settings.IsFullScreen != 0;
            }
            set
            {
                this.settings.IsFullScreen = (value ? 1 : 0);
            }
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(0, 0, this.settings.BackBufferWidth, this.settings.BackBufferHeight);
            }
        }

        internal PresentationParameters.Settings settings;

        internal struct Settings
        {
            public Settings (int a)
            {
                BackBufferWidth = UnityEngine.Screen.width;
                BackBufferHeight = UnityEngine.Screen.height;
                BackBufferFormat = SurfaceFormat.Rgba32;
                DepthStencilFormat = DepthFormat.Depth32;
                MultiSampleCount = 0;
                DisplayOrientation = DisplayOrientation.Default;
                PresentationInterval = PresentInterval.Default;
                RenderTargetUsage = RenderTargetUsage.PlatformContents;
                DeviceWindowHandle = IntPtr.Zero;
                IsFullScreen = 1;
            }
            public int BackBufferWidth;

            public int BackBufferHeight;

            public SurfaceFormat BackBufferFormat;

            public DepthFormat DepthStencilFormat;

            public int MultiSampleCount;

            public DisplayOrientation DisplayOrientation;

            public PresentInterval PresentationInterval;

            public RenderTargetUsage RenderTargetUsage;

            public IntPtr DeviceWindowHandle;

            public int IsFullScreen;
        }
    }
}