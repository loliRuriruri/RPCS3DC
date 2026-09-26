using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DragonCrownRemoteCoop
{
    /// <summary>
    /// RPCS3 integration: root discovery (no hardcoded paths), Player 1/2 handler detection,
    /// and an opt-in "set Player 2 to XInput" action that always backs up the input config first.
    /// Player 1 is never touched.
    /// </summary>
    public static class Rpcs3Integration
    {
        public static string Root => AppEnv.Root;
        public static string Rpcs3Dir => Path.Combine(Root, "RPCS3");
        public static string Rpcs3Exe => Path.Combine(Rpcs3Dir, "rpcs3.exe");
        public static string LauncherExe => Path.Combine(Root, "Launcher", "DragonCrownProEnhanced.exe");
        public static string InputConfig => Path.Combine(Rpcs3Dir, "config", "input_configs", "global", "Default.yml");

        public static bool Rpcs3Found => File.Exists(Rpcs3Exe);
        public static bool LauncherFound => File.Exists(LauncherExe);

        public static bool Rpcs3Running()
        {
            try
            {
                return System.Diagnostics.Process.GetProcesses()
                    .Any(p => { try { return p.ProcessName.Equals("rpcs3", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
            }
            catch { return false; }
        }

        /// <summary>Configured pad handler for a player (1..4), or "없음".</summary>
        public static string ControllerHandler(int player)
        {
            try
            {
                if (!File.Exists(InputConfig)) return "없음";
                var text = File.ReadAllText(InputConfig);
                var m = Regex.Match(text, @"(?ms)^Player " + player + @" Input:.*?^\s*Handler:\s*""?([^""\r\n]+)""?");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return "없음";
        }

        public static string ControllerDevice(int player)
        {
            try
            {
                if (!File.Exists(InputConfig)) return "없음";
                var text = File.ReadAllText(InputConfig);
                var m = Regex.Match(text, @"(?ms)^Player " + player + @" Input:.*?^\s*Device:\s*""?([^""\r\n]+)""?");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch { }
            return "없음";
        }

        public static bool Player2IsXInput =>
            ControllerHandler(2).Equals("XInput", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Rewrites only the "Player 2 Input:" block with a standard XInput mapping.
        /// Backs up the input config first and validates the result.
        /// </summary>
        public static bool SetPlayer2ToXInput(out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(InputConfig)) { error = "input config not found: " + InputConfig; return false; }
                if (Rpcs3Running())
                {
                    error = "RPCS3 is running. Close RPCS3 first (it would overwrite the config on exit).";
                    return false;
                }

                string backup = BackupManager.BackupRpcs3Input();
                if (backup == null) { error = "backup failed"; return false; }
                AppEnv.Log("P2->XInput: input config backed up to " + backup);

                var lines = File.ReadAllLines(InputConfig).ToList();
                int start = lines.FindIndex(l => Regex.IsMatch(l, @"^Player 2 Input:\s*$"));
                if (start < 0) { error = "Player 2 Input block not found"; return false; }
                int end = lines.FindIndex(start + 1, l => Regex.IsMatch(l, @"^Player \d+ Input:\s*$"));
                if (end < 0) end = lines.Count;

                var replacement = BuildPlayer2XInputBlock().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                lines.RemoveRange(start, end - start);
                lines.InsertRange(start, replacement);
                File.WriteAllLines(InputConfig, lines, new UTF8Encoding(false));

                if (!Player2IsXInput) { error = "validation failed after write (handler != XInput)"; return false; }
                AppEnv.Log("P2->XInput: applied and validated");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppEnv.Log("P2->XInput error: " + ex.Message);
                return false;
            }
        }

        /// <summary>Standard RPCS3 XInput mapping (same structure as the verified local 2P setup).</summary>
        private static string BuildPlayer2XInputBlock() => string.Join("\r\n", new[]
        {
            "Player 2 Input:",
            "  Handler: XInput",
            "  Device: \"XInput Pad #1\"",
            "  Config:",
            "    Left Stick Left: LS X-",
            "    Left Stick Down: LS Y-",
            "    Left Stick Right: LS X+",
            "    Left Stick Up: LS Y+",
            "    Right Stick Left: RS X-",
            "    Right Stick Down: RS Y-",
            "    Right Stick Right: RS X+",
            "    Right Stick Up: RS Y+",
            "    Start: Start",
            "    Select: Back",
            "    PS Button: \"Back&Start,Guide\"",
            "    Square: X",
            "    Cross: A",
            "    Circle: B",
            "    Triangle: Y",
            "    Left: Left",
            "    Down: Down",
            "    Right: Right",
            "    Up: Up",
            "    R1: RB",
            "    R2: RT",
            "    R3: RS",
            "    L1: LB",
            "    L2: LT",
            "    L3: LS",
            "    IR Nose: \"\"",
            "    IR Tail: \"\"",
            "    IR Left: \"\"",
            "    IR Right: \"\"",
            "    Tilt Left: \"\"",
            "    Tilt Right: \"\"",
            "    Motion Sensor X:",
            "      Axis: \"\"",
            "      Mirrored: false",
            "      Shift: 0",
            "    Motion Sensor Y:",
            "      Axis: \"\"",
            "      Mirrored: false",
            "      Shift: 0",
            "    Motion Sensor Z:",
            "      Axis: \"\"",
            "      Mirrored: false",
            "      Shift: 0",
            "    Motion Sensor G:",
            "      Axis: \"\"",
            "      Mirrored: false",
            "      Shift: 0",
            "    Orientation Reset Button: \"\"",
            "    Orientation Enabled: false",
            "    Pressure Intensity Button: \"\"",
            "    Pressure Intensity Percent: 50",
            "    Pressure Intensity Toggle Mode: false",
            "    Pressure Intensity Deadzone: 0",
            "    Analog Limiter Button: \"\"",
            "    Analog Limiter Toggle Mode: false",
            "    Left Stick Multiplier: 100",
            "    Right Stick Multiplier: 100",
            "    Left Stick Deadzone: 7849",
            "    Right Stick Deadzone: 8689",
            "    Left Stick Anti-Deadzone: 4259",
            "    Right Stick Anti-Deadzone: 4259",
            "    Left Trigger Threshold: 30",
            "    Right Trigger Threshold: 30",
            "    Left Pad Squircling Factor: 4000",
            "    Right Pad Squircling Factor: 4000",
            "    Color Value R: 0",
            "    Color Value G: 0",
            "    Color Value B: 0",
            "    Blink LED when battery is below 20%: true",
            "    Use LED as a battery indicator: false",
            "    LED battery indicator brightness: 50",
            "    Player LED enabled: true",
            "    Large Vibration Motor Multiplier: 100",
            "    Small Vibration Motor Multiplier: 100",
            "    Switch Vibration Motors: false",
            "    Vibration Threshold: 63",
            "    Mouse Movement Mode: Relative",
            "    Mouse Deadzone X Axis: 60",
            "    Mouse Deadzone Y Axis: 60",
            "    Mouse Acceleration X Axis: 200",
            "    Mouse Acceleration Y Axis: 250",
            "    Left Stick Lerp Factor: 100",
            "    Right Stick Lerp Factor: 100",
            "    Analog Button Lerp Factor: 100",
            "    Trigger Lerp Factor: 100",
            "    Device Class Type: 0",
            "    Vendor ID: 1356",
            "    Product ID: 616",
            "  Buddy Device: \"Null\"",
        });

        public static void OpenRpcs3()
        {
            if (!Rpcs3Found) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Rpcs3Exe)
            { WorkingDirectory = Rpcs3Dir, UseShellExecute = true });
        }

        /// <summary>Launches the existing main launcher (Local 2P path) - the remote pad becomes P2.</summary>
        public static bool StartRemoteCoop(out string error)
        {
            error = null;
            if (!LauncherFound) { error = "main launcher not found: " + LauncherExe; return false; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(LauncherExe)
            { WorkingDirectory = Path.GetDirectoryName(LauncherExe), UseShellExecute = true });
            return true;
        }
    }
}
