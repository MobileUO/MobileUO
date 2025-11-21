using UnityEngine;

namespace FPSOptimization
{
    public class FPSMonitor : MonoBehaviour
    {
        [Header("Display Settings")]
        public bool showFPS = true;
        public Color textColor = Color.green;
        public int fontSize = 24;
        
        private float deltaTime = 0.0f;
        private GUIStyle style;
        
        private void Start()
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = fontSize;
            style.normal.textColor = textColor;
        }
        
        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
        
        private void OnGUI()
        {
            if (!showFPS) return;
            
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.} FPS", fps);
            
            Rect rect = new Rect(10, 10, 100, 50);
            GUI.Label(rect, text, style);
        }
    }
}
