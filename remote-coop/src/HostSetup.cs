using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace DragonCrownRemoteCoop
{
    public sealed class HostStatus
    {
        public bool SunshineInstalled;
        public string SunshineVersion = "";
        public bool ServiceExists, ServiceRunning;
        public string ServiceState = "";
        public bool WebUiReachable;
        public bool SunshineProcess;
        public BackendStatus Backend = new BackendStatus();
        public SunshineManager.ControllerPolicy Policy = new SunshineManager.ControllerPolicy();
        public bool Rpcs3Found;
        public string P1 = "", P2 = "";
        public int XInputCount;
        public bool VirtualPadReady;
        public bool GuestConnected;
        public bool X360Configured => Policy.Gamepad.Equals("x360", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Dragon's Crown ready rule:
        /// A) Virtual HID Driver working  OR  B) ViGEmBus + gamepad=x360 (free path).
        /// The free path (B) alone is sufficient for Remote 2P.
        /// </summary>
        public bool BackendReadyForDc => Backend.Kind == PadBackend.VirtualHid || (Backend.ViGEmBusInstalled && X360Configured);

        public bool Ready => SunshineInstalled && ServiceRunning && BackendReadyForDc &&
                             Policy.ControllerOnly && Rpcs3Found && Rpcs3Integration.Player2IsXInput;
        public string VirtualPadState => VirtualPadReady ? "READY" : (GuestConnected ? "WAITING FOR PAD" : "WAITING FOR GUEST");
    }

    /// <summary>HOST orchestration: status, Sunshine/ViGEmBus install flows, controller-only policy, pad test.</summary>
    public static class HostSetup
    {
        private static int _baselineXInput = -1;

        public static void CaptureBaseline()
        {
            _baselineXInput = GamepadDetector.Count();
            AppEnv.Log("host: xinput baseline = " + _baselineXInput);
        }

        public static HostStatus GetStatus()
        {
            var s = new HostStatus();
            s.SunshineInstalled = SunshineManager.IsInstalled;
            s.SunshineVersion = SunshineManager.Version();
            var svc = SunshineManager.Service();
            s.ServiceExists = svc.Exists;
            s.ServiceRunning = svc.Running;
            s.ServiceState = svc.State;
            s.WebUiReachable = SunshineManager.WebUiReachable();
            s.SunshineProcess = SunshineManager.IsProcessRunning();
            s.Backend = GamepadDetector.Backend();
            s.Policy = SunshineManager.ReadPolicy();
            s.Rpcs3Found = Rpcs3Integration.Rpcs3Found;
            s.P1 = Rpcs3Integration.ControllerHandler(1);
            s.P2 = Rpcs3Integration.ControllerHandler(2);
            s.XInputCount = GamepadDetector.Count();
            if (_baselineXInput < 0) _baselineXInput = s.XInputCount;
            s.VirtualPadReady = s.XInputCount > _baselineXInput;
            s.GuestConnected = GuestConnected();
            return s;
        }

        public static bool GuestConnected()
        {
            try
            {
                int[] ports = { 47984, 47989, 47990, 48010 };
                var props = IPGlobalProperties.GetIPGlobalProperties();
                foreach (var c in props.GetActiveTcpConnections())
                {
                    if (c.State != TcpState.Established) continue;
                    bool localHit = ports.Contains(c.LocalEndPoint.Port);
                    bool remoteHit = ports.Contains(c.RemoteEndPoint.Port);
                    if (!localHit && !remoteHit) continue;
                    var addr = c.RemoteEndPoint.Address;
                    if (System.Net.IPAddress.IsLoopback(addr)) continue;
                    return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>KEY=VALUE lines for the main launcher (Remote Co-op panel).</summary>
        public static List<string> StatusLines()
        {
            var s = GetStatus();
            return new List<string>
            {
                "HOST_SUNSHINE_INSTALLED=" + (s.SunshineInstalled ? "YES" : "NO"),
                "HOST_SUNSHINE_VERSION=" + s.SunshineVersion,
                "HOST_SUNSHINE_SERVICE=" + (s.ServiceRunning ? "RUNNING" : (s.ServiceExists ? s.ServiceState : "NOT_FOUND")),
                "HOST_SUNSHINE_WEBUI=" + (s.WebUiReachable ? "OK" : "UNAVAILABLE"),
                "HOST_BACKEND=" + s.Backend.Label,
                "HOST_BACKEND_READY=" + (s.BackendReadyForDc ? "YES" : "NO"),
                "HOST_BACKEND_FREE=" + (s.Backend.FreeReady ? "YES" : "NO"),
                "HOST_BACKEND_FREE_LABEL=" + s.Backend.FreeLabel,
                "HOST_BACKEND_PREMIUM=" + (s.Backend.VirtualHidInstalled ? "YES" : "NO_OPTIONAL"),
                "HOST_GAMEPAD_MODE=" + (s.Policy.Gamepad.Length > 0 ? s.Policy.Gamepad : "auto(default)"),
                "HOST_X360=" + (s.X360Configured ? "YES" : "NO"),
                "HOST_CONTROLLER_ONLY=" + (s.Policy.ControllerOnly ? "ON" : "OFF"),
                "HOST_POLICY=" + s.Policy.Summary,
                "HOST_RPCS3=" + (s.Rpcs3Found ? "FOUND" : "MISSING"),
                "HOST_P1=" + s.P1,
                "HOST_P2=" + s.P2,
                "HOST_P2_XINPUT=" + (Rpcs3Integration.Player2IsXInput ? "YES" : "NO"),
                "HOST_XINPUT_COUNT=" + s.XInputCount,
                "HOST_VIRTUAL_PAD=" + s.VirtualPadState,
                "HOST_GUEST=" + (s.GuestConnected ? "CONNECTED" : "WAITING"),
                "HOST_READY=" + (s.Ready ? "YES" : "NO"),
            };
        }

        // ------------------------------------------------------------------ install flows
        public static (bool ok, string message) InstallSunshine(Action<string> progress)
        {
            progress?.Invoke("Resolving latest stable Sunshine release ...");
            var asset = ReleaseResolver.ResolveLatestStable("LizardByte", "Sunshine", ReleaseResolver.Target("sunshine").regex);
            if (asset == null) return (false, "SUNSHINE_NOT_INSTALLED: stable Sunshine release not found (network/GitHub API?)");

            progress?.Invoke($"Downloading {asset.Name} ({asset.ReleaseTag}, {asset.Size:N0} bytes) ...");
            var dl = Downloader.Download(asset, p => progress?.Invoke($"Downloading ... {p:0}%"));
            if (!dl.Ok) return (false, "Download verification failed: " + dl.Error);
            progress?.Invoke($"Verified: SHA256={dl.Sha256} authenticode={dl.Authenticode}");

            if (dl.Authenticode != "valid")
            {
                progress?.Invoke("WARNING: installer is not Authenticode-signed. Installation requires explicit confirmation.");
            }
            progress?.Invoke("Starting the official MSI installer (interactive) ...");
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("msiexec.exe", "/i \"" + dl.Path + "\"")
                { UseShellExecute = true };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { return (false, "MSI launch failed: " + ex.Message); }

            progress?.Invoke("Verifying installation (exe / service / config dir) ...");
            for (int i = 0; i < 20 && !SunshineManager.IsInstalled; i++) Thread.Sleep(500);
            if (!SunshineManager.IsInstalled)
                return (false, "SUNSHINE_NOT_INSTALLED: installer finished but sunshine.exe was not found.");
            var svc = SunshineManager.Service();
            if (!svc.Exists)
                progress?.Invoke("NOTE: service not registered yet - start Sunshine once so it can install the service.");
            return (true, "Sunshine installed: " + SunshineManager.Version());
        }

        public static (bool ok, string message) InstallViGEmBus(Action<string> progress)
        {
            progress?.Invoke("Resolving latest ViGEmBus release (legacy/EOL, official nefarius repo) ...");
            var asset = ReleaseResolver.ResolveLatestStable("nefarius", "ViGEmBus", ReleaseResolver.Target("vigem").regex);
            if (asset == null) return (false, "VIGEMBUS_MISSING: release not found (network/GitHub API?)");

            progress?.Invoke($"Downloading {asset.Name} ({asset.ReleaseTag}, {asset.Size:N0} bytes) ...");
            var dl = Downloader.Download(asset, p => progress?.Invoke($"Downloading ... {p:0}%"));
            if (!dl.Ok) return (false, "Download verification failed: " + dl.Error);
            progress?.Invoke($"Verified: SHA256={dl.Sha256} authenticode={dl.Authenticode}");

            progress?.Invoke("Starting the ViGEmBus installer (interactive) ...");
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(dl.Path) { UseShellExecute = true };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { return (false, "installer launch failed: " + ex.Message); }

            for (int i = 0; i < 20 && !GamepadDetector.ViGEmBusInstalled(out _, out _); i++) Thread.Sleep(500);
            if (!GamepadDetector.ViGEmBusInstalled(out string ver, out _))
                return (false, "VIGEMBUS_MISSING: installer finished but the ViGEmBus driver was not detected.");
            return (true, "ViGEmBus installed: " + ver);
        }

        public static (bool ok, string message) StartSunshine()
        {
            if (SunshineManager.Service().Running) return (true, "Sunshine service already running.");
            if (SunshineManager.Service().Exists)
            {
                bool ok = SunshineManager.StartService(out string output);
                Thread.Sleep(1500);
                var svc = SunshineManager.Service();
                if (svc.Running) return (true, "Sunshine service started.");
                return (false, "SUNSHINE_SERVICE_STOPPED: sc start failed (" + output.Trim() + ")");
            }
            if (SunshineManager.IsInstalled)
            {
                SunshineManager.OpenExe();
                return (true, "Sunshine started (app mode; the service appears after the first run).");
            }
            return (false, "SUNSHINE_NOT_INSTALLED");
        }

        public static PadTestResult TestRemotePad(int seconds, Action<string> progress = null)
        {
            progress?.Invoke("Testing XInput (remote pad) ...");
            var r = GamepadDetector.Test(seconds, progress);
            var st = GetStatus();
            r.Lines.Add($"RPCS3 Player 2      : {st.P2} (XInput={(Rpcs3Integration.Player2IsXInput ? "yes" : "no")})");
            r.Lines.Add($"Virtual pad state   : {st.VirtualPadState} (xinput={st.XInputCount})");
            if (r.InputReceived && Rpcs3Integration.Player2IsXInput)
                r.Lines.Add("REMOTE CONTROLLER READY");
            AppEnv.Log($"pad test: input={r.InputReceived} devices={r.DeviceCount} buttons={r.Buttons} sticks={r.Sticks}");
            return r;
        }

        /// <summary>[1/6]..[6/6] step list for the progress UI.</summary>
        public static List<(string step, bool ok, string detail)> Steps()
        {
            var s = GetStatus();
            string backendDetail = s.Backend.FreeReady
                ? $"FREE: {s.Backend.FreeLabel} · gamepad={(s.Policy.Gamepad.Length > 0 ? s.Policy.Gamepad : "auto")}"
                : s.Backend.Label;
            return new List<(string, bool, string)>
            {
                ("[1/6] Sunshine", s.SunshineInstalled, s.SunshineInstalled ? s.SunshineVersion : "not installed"),
                ("[2/6] Gamepad backend", s.BackendReadyForDc, backendDetail),
                ("[3/6] Controller settings", s.Policy.ControllerOnly, s.Policy.Summary),
                ("[4/6] RPCS3 Player 2", Rpcs3Integration.Player2IsXInput, s.P2),
                ("[5/6] Pairing", s.WebUiReachable, s.WebUiReachable ? "Sunshine Web UI ready" : "Web UI unavailable"),
                ("[6/6] Remote pad test", s.VirtualPadReady, s.VirtualPadState),
            };
        }
    }
}
