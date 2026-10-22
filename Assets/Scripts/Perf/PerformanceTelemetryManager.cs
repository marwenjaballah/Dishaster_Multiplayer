using System;
using System.Collections.Generic;
using UnityEngine;

namespace Perf
{
    /// <summary>
    /// Production performance profiling and frame rate manager.
    /// Locks 120 FPS target on PC and 60 FPS on mobile.
    /// Tracks frame times, 1% low FPS, and memory allocation telemetry in real-time.
    /// Toggle overlay with F3.
    /// </summary>
    public class PerformanceTelemetryManager : MonoBehaviour
    {
        public static PerformanceTelemetryManager Instance { get; private set; }

        public static event EventHandler<TelemetryUpdatedEventArgs> OnTelemetryUpdated;

        public static void ResetStaticData()
        {
            OnTelemetryUpdated = null;
        }

        public class TelemetryUpdatedEventArgs : EventArgs
        {
            public float currentFps;
            public float onePercentLowFps;
            public float frameTimeMs;
            public long totalAllocatedMemoryMb;
        }

        [Header("Target Framerates")]
        [SerializeField] private int pcTargetFps = 120;
        [SerializeField] private int mobileTargetFps = 60;
        [SerializeField] private bool showOverlayOnStart = false;

        private bool _showOverlay;
        private readonly float[] _frameTimeBuffer = new float[120];
        private int _bufferIndex;
        private float _fpsAccumulator;
        private int _fpsCount;
        private float _currentFps = 120f;
        private float _onePercentLow = 100f;
        private float _frameTimeMs = 8.33f;
        private long _allocatedMemoryMb;
        private float _telemetryTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _showOverlay = showOverlayOnStart;
            ApplyTargetFramerates();
        }

        private void ApplyTargetFramerates()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Application.isMobilePlatform ? mobileTargetFps : pcTargetFps;
            Debug.Log($"[PerformanceTelemetryManager] Configured target frame rate: {Application.targetFrameRate} FPS (VSync: Off, Mobile: {Application.isMobilePlatform})");
        }

        private void Update()
        {
            // Toggle performance HUD with F3
            if (Input.GetKeyDown(KeyCode.F3))
            {
                _showOverlay = !_showOverlay;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;

            // Record rolling frame time
            _frameTimeBuffer[_bufferIndex] = dt;
            _bufferIndex = (_bufferIndex + 1) % _frameTimeBuffer.Length;

            _fpsAccumulator += (1f / dt);
            _fpsCount++;

            _telemetryTimer += dt;
            if (_telemetryTimer >= 0.5f)
            {
                _currentFps = _fpsAccumulator / _fpsCount;
                _frameTimeMs = (_telemetryTimer / _fpsCount) * 1000f;
                _fpsAccumulator = 0f;
                _fpsCount = 0;
                _telemetryTimer = 0f;

                // Calculate 1% Low FPS
                float[] sorted = (float[])_frameTimeBuffer.Clone();
                Array.Sort(sorted);
                int onePercentIndex = Mathf.FloorToInt(sorted.Length * 0.99f);
                float worstFrameTime = sorted[Mathf.Clamp(onePercentIndex, 0, sorted.Length - 1)];
                _onePercentLow = worstFrameTime > 0.0001f ? (1f / worstFrameTime) : _currentFps;

                _allocatedMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024);

                OnTelemetryUpdated?.Invoke(this, new TelemetryUpdatedEventArgs
                {
                    currentFps = _currentFps,
                    onePercentLowFps = _onePercentLow,
                    frameTimeMs = _frameTimeMs,
                    totalAllocatedMemoryMb = _allocatedMemoryMb
                });
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 13;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;

            Color fpsColor = _currentFps >= 58f ? Color.green : (_currentFps >= 30f ? Color.yellow : Color.red);
            string fpsColorHex = ColorUtility.ToHtmlStringRGB(fpsColor);

            string text = $"<b>PERFORMANCE TELEMETRY (F3)</b>\n" +
                          $"FPS: <color=#{fpsColorHex}><b>{_currentFps:F0} FPS</b></color> (1% Low: {_onePercentLow:F0} FPS)\n" +
                          $"Frame Time: {_frameTimeMs:F2} ms\n" +
                          $"Memory (GC): {_allocatedMemoryMb} MB\n" +
                          $"Target: {Application.targetFrameRate} FPS";

            GUILayout.BeginArea(new Rect(15, Screen.height - 110, 260, 95), style);
            GUILayout.Label(text, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });
            GUILayout.EndArea();
        }

        public void SetTargetFramerate(int targetFps)
        {
            Application.targetFrameRate = targetFps;
        }

        public float GetCurrentFps() => _currentFps;
        public float GetOnePercentLow() => _onePercentLow;
        public long GetAllocatedMemoryMb() => _allocatedMemoryMb;
    }
}
