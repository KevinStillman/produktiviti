using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using produKtiviti.Models;

namespace produKtiviti.Services
{
    public class RoboMouseService : IDisposable
    {
        // ── Win32 ────────────────────────────────────────────────────────────

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        // ── State ────────────────────────────────────────────────────────────

        private RoboMouseConfig? _config;
        private CancellationTokenSource? _cts;
        private Task? _clickTask;
        private Task? _hotkeyTask;
        private CancellationTokenSource? _hotkeyCts;
        private bool _running;
        private readonly Random _rng = new();

        public bool IsRunning => _running;
        public event Action? StateChanged;

        // ── Public API ───────────────────────────────────────────────────────

        public void Configure(RoboMouseConfig config)
        {
            _config = config;
        }

        public void StartHotkeyListener()
        {
            if (_hotkeyCts != null) return;
            _hotkeyCts = new CancellationTokenSource();
            var token = _hotkeyCts.Token;
            _hotkeyTask = Task.Run(async () =>
            {
                bool startWasDown = false;
                bool stopWasDown = false;
                while (!token.IsCancellationRequested)
                {
                    if (_config == null) { await Task.Delay(100, token).ConfigureAwait(false); continue; }

                    int startVk = KeyNameToVk(_config.StartKey);
                    int stopVk = KeyNameToVk(_config.StopKey);

                    bool startDown = startVk != 0 && (GetAsyncKeyState(startVk) & 0x8000) != 0;
                    bool stopDown = stopVk != 0 && (GetAsyncKeyState(stopVk) & 0x8000) != 0;

                    if (startDown && !startWasDown && !_running)
                        StartClicking();
                    if (stopDown && !stopWasDown && _running)
                        StopClicking();

                    startWasDown = startDown;
                    stopWasDown = stopDown;

                    await Task.Delay(50, token).ConfigureAwait(false);
                }
            }, token);
        }

        public void StopHotkeyListener()
        {
            _hotkeyCts?.Cancel();
            _hotkeyCts = null;
            StopClicking();
        }

        public void Dispose()
        {
            StopHotkeyListener();
        }

        // ── Click loop ───────────────────────────────────────────────────────

        private void StartClicking()
        {
            if (_config == null || _running) return;
            _running = true;
            StateChanged?.Invoke();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _clickTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    Click();
                    int delay = _config.RandomizeInterval
                        ? _rng.Next(_config.RandomMinMs, _config.RandomMaxMs + 1)
                        : _config.IntervalMs;
                    await Task.Delay(Math.Max(1, delay), token).ConfigureAwait(false);
                }
            }, token);
        }

        private void StopClicking()
        {
            if (!_running) return;
            _cts?.Cancel();
            _cts = null;
            _running = false;
            StateChanged?.Invoke();
        }

        private void Click()
        {
            if (_config == null) return;
            if (_config.UseRightButton)
            {
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, IntPtr.Zero);
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);
            }
            else
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
            }
        }

        // ── Key name → VK ────────────────────────────────────────────────────

        public static int KeyNameToVk(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return 0;
            if (Enum.TryParse<Key>(keyName, true, out var key))
            {
                return KeyInterop.VirtualKeyFromKey(key);
            }
            return 0;
        }
    }
}
