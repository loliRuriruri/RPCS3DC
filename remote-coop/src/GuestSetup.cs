using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DragonCrownRemoteCoop
{
    public sealed class GuestStatus
    {
        public bool MoonlightInstalled;
        public string MoonlightVersion = "";
        public bool MoonlightRunning;
        public int ControllerCount;
        public string[] ControllerNames = Array.Empty<string>();
        public bool ControllerDetected => ControllerCount > 0 || ControllerNames.Length > 0;
        public string HostAddress = "";
        public bool HostConfigured => !string.IsNullOrEmpty(HostAddress);
        public bool StreamingCapable => MoonlightInstalled && ControllerDetected;
        public string PairingStatus = "Not verified (check Moonlight GUI)";
        public string ControllerState => ControllerCount > 0
            ? $"Detected (XInput x{ControllerCount})"
            : (ControllerNames.Length > 0 ? "Detected (non-XInput)" : "Missing");
    }

    /// <summary>
    /// GUEST orchestration (friend PC). RPCS3 / PS3 firmware / the game are NOT required here:
    /// Moonlight + a controller + network access are enough.
    /// </summary>
    public static class GuestSetup
    {
        public static GuestStatus GetStatus()
        {
            var s = new GuestStatus();
            s.MoonlightInstalled = MoonlightManager.IsInstalled;
            s.MoonlightVersion = MoonlightManager.Version();
            s.MoonlightRunning = MoonlightManager.IsProcessRunning();
            s.ControllerCount = GamepadDetector.Count();
            s.ControllerNames = GamepadDetector.Names();
            var settings = AppEnv.LoadSettings();
            s.HostAddress = settings.TryGetValue("GuestHostAddress", out string h) ? h : "";
            return s;
        }

        public static List<string> StatusLines()
        {
            var s = GetStatus();
            return new List<string>
            {
                "GUEST_MOONLIGHT_INSTALLED=" + (s.MoonlightInstalled ? "YES" : "NO"),
                "GUEST_MOONLIGHT_VERSION=" + s.MoonlightVersion,
                "GUEST_CONTROLLER=" + (s.ControllerCount > 0 ? "DETECTED_XINPUT(" + s.ControllerCount + ")" : (s.ControllerNames.Length > 0 ? "DETECTED_NON_XINPUT" : "MISSING")),
                "GUEST_CONTROLLER_NAMES=" + (s.ControllerNames.Length > 0 ? string.Join("|", s.ControllerNames) : ""),
                "GUEST_HOST=" + (s.HostConfigured ? s.HostAddress : "NOT_PAIRED"),
                "GUEST_PAIRING=" + s.PairingStatus,
                "GUEST_STREAM_READY=" + (s.StreamingCapable ? "YES" : "NO"),
            };
        }

        public static (bool ok, string message) InstallMoonlight(Action<string> progress)
        {
            progress?.Invoke("Resolving latest stable Moonlight release ...");
            var asset = ReleaseResolver.ResolveLatestStable("moonlight-stream", "moonlight-qt", ReleaseResolver.Target("moonlight").regex);
            if (asset == null) return (false, "MOONLIGHT_NOT_INSTALLED: stable Moonlight release not found (network/GitHub API?)");

            progress?.Invoke($"Downloading {asset.Name} ({asset.ReleaseTag}, {asset.Size:N0} bytes) ...");
            var dl = Downloader.Download(asset, p => progress?.Invoke($"Downloading ... {p:0}%"));
            if (!dl.Ok) return (false, "Download verification failed: " + dl.Error);
            progress?.Invoke($"Verified: SHA256={dl.Sha256} authenticode={dl.Authenticode}");

            progress?.Invoke("Starting the Moonlight installer (interactive) ...");
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(dl.Path) { UseShellExecute = true };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { return (false, "installer launch failed: " + ex.Message); }

            for (int i = 0; i < 20 && !MoonlightManager.IsInstalled; i++) Thread.Sleep(500);
            if (!MoonlightManager.IsInstalled)
                return (false, "MOONLIGHT_NOT_INSTALLED: installer finished but Moonlight.exe was not found.");
            return (true, "Moonlight installed: " + MoonlightManager.Version());
        }

        public static PadTestResult TestController(int seconds, Action<string> progress = null)
        {
            progress?.Invoke("Testing local controller (XInput) ...");
            var r = GamepadDetector.Test(seconds, progress);
            AppEnv.Log($"guest controller test: devices={r.DeviceCount} input={r.InputReceived}");
            return r;
        }

        public static (bool ok, string message) SetHostAddress(string address)
        {
            if (!MoonlightManager.IsValidHost(address, out string error)) return (false, error);
            AppEnv.SaveSetting("GuestHostAddress", address.Trim());
            AppEnv.Log("guest host address saved: " + address.Trim());
            return (true, "Host address saved: " + address.Trim());
        }

        /// <summary>[1/3]..[3/3] steps for the progress UI.</summary>
        public static List<(string step, bool ok, string detail)> Steps()
        {
            var s = GetStatus();
            return new List<(string, bool, string)>
            {
                ("[1/3] Moonlight", s.MoonlightInstalled, s.MoonlightInstalled ? s.MoonlightVersion : "not installed"),
                ("[2/3] Controller", s.ControllerDetected, s.ControllerState),
                ("[3/3] Pair host", s.HostConfigured, s.HostConfigured ? s.HostAddress : "no host address yet"),
            };
        }
    }
}
