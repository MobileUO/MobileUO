using ClassicUO.LegionScripting;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class SimpleProgressBar : Control
    {
        private readonly string _backgroundColor;
        private readonly string _foregroundColor;
        private AlphaBlendControl _background, _foreground;

        public override bool AcceptMouseInput { get; set; } = true;

        public SimpleProgressBar(string backgroundColor, string foregroundColor, int width, int height)
        {
            CanMove = true;

            Width = width;
            Height = height;
            _backgroundColor = backgroundColor;
            _foregroundColor = foregroundColor;

            Build();
        }

        public void SetProgress(float value, float max)
        {
            if (_foreground == null)
                return;

            if (max <= 0)
            {
                Log.Warn("[SimpleProgressBar] Attempting to set progress with a negative or zero max.");
                return;
            }

            float percent = value / max;

            if (percent < 0f) percent = 0f;
            else if (percent > 1f) percent = 1f;

            _foreground.Width = (int)(Width * percent);
        }

        private void Build()
        {
            _background = new(1f)
            {
                BaseColor = LegionScripting.Utility.GetColorFromHex(_backgroundColor),
                Width = Width,
                Height = Height
            };

            Add(_background);

            _foreground = new(1f)
            {
                BaseColor = LegionScripting.Utility.GetColorFromHex(_foregroundColor),
                Width = Width,
                Height = Height
            };

            Add(_foreground);
        }
    }
}