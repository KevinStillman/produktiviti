using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace produKtiviti.Services
{
    /// <summary>
    /// Moves a newly-foregrounded window to whichever monitor the user last
    /// clicked a taskbar button on.
    ///
    /// Strategy:
    ///   1. A low-level mouse hook (WH_MOUSE_LL) fires on every left-click.
    ///      If the click lands on a taskbar window class, we snapshot the
    ///      cursor position.
    ///   2. A WinEvent hook (EVENT_SYSTEM_FOREGROUND) fires when any window
    ///      becomes foreground. We use the snapshotted position to decide
    ///      which monitor was intended and move the window there.
    /// </summary>
    public class SmartTabsService : IDisposable
    {
        // ── Win32 ────────────────────────────────────────────────────────────

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime);
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
            public bool Contains(int x, int y) =>
                x >= Left && x < Right && y >= Top && y < Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        // ── State ────────────────────────────────────────────────────────────

        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _winEventHook = IntPtr.Zero;
        private LowLevelMouseProc? _mouseProc;
        private WinEventDelegate? _winEventProc;

        private bool _enabled;
        private readonly uint _ownPid;
        private POINT? _lastTaskbarClickPoint;
        private DateTime _lastTaskbarClickTime = DateTime.MinValue;
        private DateTime _lastMoveTime = DateTime.MinValue;
        private static readonly TimeSpan ClickExpiry = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan MoveCooldown = TimeSpan.FromMilliseconds(800);

        public SmartTabsService()
        {
            _ownPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Enable()
        {
            if (_enabled) return;

            _mouseProc = MouseHookCallback;
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                GetModuleHandle(curModule.ModuleName!), 0);

            _winEventProc = OnForegroundChanged;
            _winEventHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _winEventProc,
                0, 0, WINEVENT_OUTOFCONTEXT);

            _enabled = true;
        }

        public void Disable()
        {
            if (!_enabled) return;

            if (_mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
            if (_winEventHook != IntPtr.Zero) { UnhookWinEvent(_winEventHook); _winEventHook = IntPtr.Zero; }

            _mouseProc = null;
            _winEventProc = null;
            _lastTaskbarClickPoint = null;
            _enabled = false;
        }

        public void Dispose() => Disable();

        // ── Mouse hook ───────────────────────────────────────────────────────

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var clicked = WindowFromPoint(info.pt);

                if (IsTaskbarWindow(clicked))
                {
                    _lastTaskbarClickPoint = info.pt;
                    _lastTaskbarClickTime = DateTime.UtcNow;
                    _lastMoveTime = DateTime.MinValue;
                }
                else if (_lastTaskbarClickPoint.HasValue)
                {
                    _lastTaskbarClickPoint = null;
                }
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private static bool IsTaskbarWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            var cur = hwnd;
            int depth = 0;
            while (cur != IntPtr.Zero && depth++ < 6)
            {
                var sb = new StringBuilder(256);
                GetClassName(cur, sb, sb.Capacity);
                var cls = sb.ToString();
                if (cls is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd"
                    or "MSTaskListWClass" or "MSTaskSwWClass" or "ReBarWindow32"
                    or "TaskListButtonWnd" or "TaskListThumbnailWnd"
                    or "Windows.UI.Composition.DesktopWindowContentBridge")
                    return true;
                cur = GetParent(cur);
            }
            return false;
        }

        private static bool IsNonMainWindow(string cls)
        {
            if (string.IsNullOrEmpty(cls)) return true;
            if (cls.EndsWith("_0", StringComparison.Ordinal) &&
                !cls.Contains("SSNSkinWindow", StringComparison.OrdinalIgnoreCase)) return true;
            return cls is "Chrome_WidgetWin_0"
                or "ApplicationFrameInputSinkWindow"
                or "Windows.UI.Core.CoreWindow"
                or "ForegroundStaging"
                or "tooltips_class32"
                or "SysShadow";
        }

        // ── WinEvent hook ────────────────────────────────────────────────────

        private void OnForegroundChanged(
            IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == _ownPid) return;

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return;
            if ((exStyle & WS_EX_NOACTIVATE) != 0) return;

            if (!_lastTaskbarClickPoint.HasValue) return;

            if (DateTime.UtcNow - _lastTaskbarClickTime > ClickExpiry)
            {
                _lastTaskbarClickPoint = null;
                return;
            }

            if (DateTime.UtcNow - _lastMoveTime < MoveCooldown)
            {
                _lastTaskbarClickPoint = null;
                return;
            }

            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            var fgClass = sb.ToString();

            if (IsNonMainWindow(fgClass)) return;

            var clickPoint = _lastTaskbarClickPoint.Value;
            _lastTaskbarClickPoint = null;

            var monitors = GetAllMonitors();
            var targetMon = GetMonitorAtPoint(monitors, clickPoint.X, clickPoint.Y);
            if (targetMon == null) return;

            bool wasMinimized = IsIconic(hwnd);
            var capturedTarget = targetMon.Value;

            if (wasMinimized)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(200);
                    MoveWindowToMonitor(hwnd, capturedTarget, GetAllMonitors());
                    await Task.Delay(400);
                    MoveWindowToMonitor(hwnd, capturedTarget, GetAllMonitors());
                });
                _lastMoveTime = DateTime.UtcNow;
                return;
            }

            if (!GetWindowRect(hwnd, out RECT winRect)) return;

            var currentMon = GetMonitorAtPoint(monitors,
                winRect.Left + winRect.Width / 2,
                winRect.Top + winRect.Height / 2);

            if (currentMon != null &&
                currentMon.Value.rcWork.Left == targetMon.Value.rcWork.Left &&
                currentMon.Value.rcWork.Top == targetMon.Value.rcWork.Top)
                return;

            PerformMove(hwnd, winRect, capturedTarget, currentMon);
            Task.Run(async () =>
            {
                await Task.Delay(400);
                MoveWindowToMonitor(hwnd, capturedTarget, GetAllMonitors());
            });
            _lastMoveTime = DateTime.UtcNow;
        }

        // ── Monitor helpers ──────────────────────────────────────────────────

        private static List<MONITORINFO> GetAllMonitors()
        {
            var list = new List<MONITORINFO>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMon, hdc, ref rect, data) =>
            {
                var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMon, ref mi))
                    list.Add(mi);
                return true;
            }, IntPtr.Zero);
            return list;
        }

        private static MONITORINFO? GetMonitorAtPoint(List<MONITORINFO> monitors, int x, int y)
        {
            foreach (var m in monitors)
                if (m.rcMonitor.Contains(x, y))
                    return m;
            return monitors.Count > 0 ? monitors[0] : null;
        }

        private static void MoveWindowToMonitor(IntPtr hwnd, MONITORINFO targetMon, List<MONITORINFO> monitors)
        {
            if (!GetWindowRect(hwnd, out RECT winRect)) return;
            var currentMon = GetMonitorAtPoint(monitors,
                winRect.Left + winRect.Width / 2,
                winRect.Top + winRect.Height / 2);

            if (currentMon != null &&
                currentMon.Value.rcWork.Left == targetMon.rcWork.Left &&
                currentMon.Value.rcWork.Top == targetMon.rcWork.Top)
                return;

            PerformMove(hwnd, winRect, targetMon, currentMon);
        }

        private static void PerformMove(IntPtr hwnd, RECT winRect, MONITORINFO targetMon, MONITORINFO? currentMon)
        {
            var wb = targetMon.rcWork;
            var srcWb = currentMon?.rcWork ?? wb;

            int relX = winRect.Left - srcWb.Left;
            int relY = winRect.Top - srcWb.Top;

            int newX = Math.Max(wb.Left, Math.Min(wb.Left + relX, wb.Right - winRect.Width));
            int newY = Math.Max(wb.Top, Math.Min(wb.Top + relY, wb.Bottom - winRect.Height));

            SetWindowPos(hwnd, IntPtr.Zero, newX, newY, 0, 0,
                SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }
}
