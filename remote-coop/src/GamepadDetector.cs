using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace DragonCrownRemoteCoop
{
    public enum PadBackend { Missing, ViGEmBus, VirtualHid }

    public sealed class BackendStatus
    {
        public PadBackend Kind = PadBackend.Missing;
        public string Version = "";
        public string Detail = "";
        public bool Ready => Kind != PadBackend.Missing;
        public string Label => Kind switch
        {
            PadBackend.VirtualHid => "Virtual HID" + (Version.Length > 0 ? " " + Version : ""),
            PadBackend.ViGEmBus => "ViGEmBus " + (Version.Length > 0 ? Version : "(1.17+ 필요)"),
            _ => "Missing",
        };
    }

    public sealed class PadTestResult
    {
        public bool InputReceived;
        public int DeviceCount;
        public string Buttons = "";
        public string Sticks = "";
        public List<string> Lines = new List<string>();
    }

    /// <summary>
    /// XInput virtual/physical pad detection + gamepad backend (Virtual HID / ViGEmBus) detection.
    /// Sunshine creates the virtual pad only while a guest is streaming, so "no pad yet" is a
    /// normal WAITING state, not an error.
    /// </summary>
    public static class GamepadDetector
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState14(uint index, ref XINPUT_STATE state);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState910(uint index, ref XINPUT_STATE state);

        private static bool? _use910;

        private static uint GetState(uint index, ref XINPUT_STATE state)
        {
            if (_use910 == true) return XInputGetState910(index, ref state);
            try
            {
                uint r = XInputGetState14(index, ref state);
                _use910 = false;
                return r;
            }
            catch (DllNotFoundException)
            {
                _use910 = true;
                return XInputGetState910(index, ref state);
            }
        }

        /// <summary>Connected XInput device count (0..4).</summary>
        public static int Count()
        {
            int n = 0;
            for (uint i = 0; i < 4; i++)
            {
                var st = new XINPUT_STATE();
                try { if (GetState(i, ref st) == 0) n++; } catch { }
            }
            return n;
        }

        private static bool TryGetState(int index, out XINPUT_GAMEPAD pad)
        {
            pad = default;
            var st = new XINPUT_STATE();
            try
            {
                if (GetState((uint)index, ref st) != 0) return false;
                pad = st.Gamepad;
                return true;
            }
            catch { return false; }
        }

        public static string[] Names()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -Command \"Get-CimInstance Win32_PnPEntity | Where-Object { $_.Name -match 'Xbox|DualSense|DualShock|Gamepad|XINPUT|8BitDo|Pro Controller|Joy-Con' -and $_.Name -notmatch 'GPIO|NVM|Storage|Ethernet|Family|I2C|Audio' } | Select-Object -ExpandProperty Name -Unique\"")
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                return o.Split('\n').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToArray();
            }
            catch { return Array.Empty<string>(); }
        }

        /// <summary>True when an XInput pad or any controller-like HID device is present.</summary>
        public static bool AnyControllerDetected() => Count() > 0 || Names().Length > 0;

        // ------------------------------------------------------------------ backends
        private static string RegUninstallVersion(params string[] namePatterns)
        {
            foreach (var root in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(root);
                    if (key == null) continue;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using var sk = key.OpenSubKey(sub);
                        string name = sk?.GetValue("DisplayName") as string;
                        if (string.IsNullOrEmpty(name)) continue;
                        if (!namePatterns.Any(p => Regex.IsMatch(name, p, RegexOptions.IgnoreCase))) continue;
                        string v = (sk.GetValue("DisplayVersion") as string) ?? "";
                        if (v.Length > 0) return v;
                    }
                }
                catch { }
            }
            return "";
        }

        public static bool ViGEmBusInstalled(out string version, out string detail)
        {
            version = ""; detail = "";
            bool service = ServiceExists("ViGEmBus");
            bool driver = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "ViGEmBus.sys"));
            string reg = RegUninstallVersion("^ViGEmBus");
            if (reg.Length > 0) version = reg;
            if (!version.Contains('.') && service)
            {
                try
                {
                    var f = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "ViGEmBus.sys");
                    if (File.Exists(f)) version = FileVersionInfo.GetVersionInfo(f).FileVersion ?? "";
                }
                catch { }
            }
            detail = $"service={(service ? "yes" : "no")} driver={(driver ? "yes" : "no")} reg={(reg.Length > 0 ? reg : "-")}";
            return service || driver || reg.Length > 0;
        }

        public static bool VirtualHidInstalled(out string version, out string detail)
        {
            version = ""; detail = "";
            string reg = RegUninstallVersion("Virtual HID");
            bool dir = Directory.Exists(@"C:\Program Files\LizardByte") ||
                       Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LizardByte"));
            if (reg.Length > 0) version = reg;
            detail = $"reg={(reg.Length > 0 ? reg : "-")} lizardbyte_dir={(dir ? "yes" : "no")}";
            return reg.Length > 0;
        }

        /// <summary>Virtual HID (preferred when installed/licensed) -> ViGEmBus fallback -> Missing.</summary>
        public static BackendStatus Backend()
        {
            var s = new BackendStatus();
            if (VirtualHidInstalled(out string vh, out string vhd))
            {
                s.Kind = PadBackend.VirtualHid;
                s.Version = vh;
                s.Detail = "Virtual HID Driver detected (" + vhd + "). License status is shown in the Sunshine Web UI.";
                return s;
            }
            if (ViGEmBusInstalled(out string vg, out string vgd))
            {
                s.Kind = PadBackend.ViGEmBus;
                s.Version = vg;
                s.Detail = "ViGEmBus detected (" + vgd + "). Free legacy fallback (Xbox 360 / DS4 only).";
                return s;
            }
            s.Detail = "No gamepad backend. Install Virtual HID Driver (paid, Sunshine Web UI) or the free ViGEmBus fallback.";
            return s;
        }

        public static bool ServiceExists(string name)
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "query " + name)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var p = Process.Start(psi);
                string o = p.StandardOutput.ReadToEnd();
                return o.Contains("SERVICE_NAME") || o.Contains("STATE");
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------ pad test
        /// <summary>Polls XInput for N seconds and reports whether any remote input arrived.</summary>
        public static PadTestResult Test(int seconds, Action<string> progress = null)
        {
            var r = new PadTestResult();
            r.Lines.Add($"XInput device count : {Count()}");
            var names = Names();
            r.Lines.Add("Devices             : " + (names.Length > 0 ? string.Join(" | ", names) : "(none)"));
            r.Lines.Add($"Listening for input for {seconds}s (move sticks / press buttons on the guest pad) ...");

            var buttonsSeen = new HashSet<string>();
            var sticksSeen = new HashSet<string>();
            int baseline = Count();
            r.DeviceCount = baseline;

            for (int t = 0; t < seconds * 10; t++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!TryGetState(i, out var pad)) continue;
                    r.DeviceCount = Math.Max(r.DeviceCount, Count());
                    if (pad.wButtons != 0)
                    {
                        foreach (var b in DecodeButtons(pad.wButtons)) buttonsSeen.Add(b);
                    }
                    if (Math.Abs(pad.sThumbLX) > 8000) sticksSeen.Add("LX");
                    if (Math.Abs(pad.sThumbLY) > 8000) sticksSeen.Add("LY");
                    if (Math.Abs(pad.sThumbRX) > 8000) sticksSeen.Add("RX");
                    if (Math.Abs(pad.sThumbRY) > 8000) sticksSeen.Add("RY");
                    if (pad.bLeftTrigger > 32) sticksSeen.Add("LT");
                    if (pad.bRightTrigger > 32) sticksSeen.Add("RT");
                    if (buttonsSeen.Count > 0 || sticksSeen.Count > 0)
                    {
                        r.InputReceived = true;
                        progress?.Invoke($"input detected: {string.Join(",", buttonsSeen)} {string.Join(",", sticksSeen)}");
                        goto done;
                    }
                }
                Thread.Sleep(100);
            }
            done:
            r.Buttons = string.Join(",", buttonsSeen);
            r.Sticks = string.Join(",", sticksSeen);
            r.Lines.Add("Buttons seen        : " + (r.Buttons.Length > 0 ? r.Buttons : "(none)"));
            r.Lines.Add("Sticks/triggers     : " + (r.Sticks.Length > 0 ? r.Sticks : "(none)"));
            r.Lines.Add(r.InputReceived ? "REMOTE CONTROLLER READY" : "NO INPUT DETECTED (guest connected? pad forwarded?)");
            return r;
        }

        private static IEnumerable<string> DecodeButtons(ushort b)
        {
            if ((b & 0x1000) != 0) yield return "A";
            if ((b & 0x2000) != 0) yield return "B";
            if ((b & 0x4000) != 0) yield return "X";
            if ((b & 0x8000) != 0) yield return "Y";
            if ((b & 0x0001) != 0) yield return "Up";
            if ((b & 0x0002) != 0) yield return "Down";
            if ((b & 0x0004) != 0) yield return "Left";
            if ((b & 0x0008) != 0) yield return "Right";
            if ((b & 0x0010) != 0) yield return "Start";
            if ((b & 0x0020) != 0) yield return "Back";
            if ((b & 0x0040) != 0) yield return "LS";
            if ((b & 0x0080) != 0) yield return "RS";
            if ((b & 0x0100) != 0) yield return "LB";
            if ((b & 0x0200) != 0) yield return "RB";
        }
    }
}
