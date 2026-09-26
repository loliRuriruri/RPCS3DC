using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace DragonCrownProEnhanced
{
    /// <summary>
    /// UI Automation for RPCS3 toolbar actions (Config / Pads / RPCN).
    /// Runs a small Windows PowerShell UIA client (the managed UIA assemblies ship with
    /// Windows PowerShell) and invokes the toolbar button. No coordinate clicking is used.
    /// If the button cannot be found, the caller must fall back to "bring the existing
    /// window to the front" - never start a second RPCS3.
    /// </summary>
    public static class Rpcs3Automation
    {
        public static (bool ok, string detail) InvokeToolbar(Project p, int pid, Rpcs3SettingsKind kind)
        {
            string[] names = Names(kind);
            if (names.Length == 0) return (false, "no toolbar action for " + kind);

            string script = BuildScript(pid, names);
            try
            {
                string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var psi = new ProcessStartInfo("powershell",
                    "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded)
                { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                using var proc = Process.Start(psi);

                // UI Automation Invoke can stay blocked while the modal settings dialog is open,
                // so wait only briefly for the RESULT line and treat "still running" as delivered.
                var deadline = DateTime.UtcNow.AddSeconds(8);
                while (DateTime.UtcNow < deadline && !proc.HasExited) Thread.Sleep(200);

                if (!proc.HasExited)
                {
                    p.Log($"rpcs3 uia [{kind}]: invoke delivered (UIA client still busy with the modal dialog - treated as ok)");
                    try { proc.Kill(); } catch { }
                    return (true, "Settings dialog requested via UI Automation (" + kind + ")");
                }

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();

                string result = stdout.Split('\n').Select(s => s.Trim()).FirstOrDefault(s => s.StartsWith("RESULT=")) ?? "";
                p.Log($"rpcs3 uia [{kind}]: {(result.Length > 0 ? result : "(no result)")}" +
                      (stderr.Trim().Length > 0 ? " | stderr=" + Truncate(stderr.Trim(), 200) : ""));

                if (result.StartsWith("RESULT=ok"))
                    return (true, "Settings dialog opened via UI Automation: " + result.Substring("RESULT=ok|".Length));
                return (false, result.Length > 0 ? result : "UI Automation produced no result");
            }
            catch (Exception ex)
            {
                p.Log("rpcs3 uia error: " + ex.Message);
                return (false, "UI Automation error: " + ex.Message);
            }
        }

        private static string[] Names(Rpcs3SettingsKind kind)
        {
            switch (kind)
            {
                case Rpcs3SettingsKind.Controller: return new[] { "Pads", "패드", "Controllers", "Controller" };
                case Rpcs3SettingsKind.Rpcn: return new[] { "RPCN", "RPCN 설정" };
                case Rpcs3SettingsKind.General: return new[] { "Config", "설정", "Configuration", "Settings" };
                default: return Array.Empty<string>();
            }
        }

        private static string BuildScript(int pid, string[] names)
        {
            string nameList = string.Join(",", names.Select(n => "'" + n.Replace("'", "''") + "'"));
            return $@"
$ErrorActionPreference = 'Stop'
try {{
  Add-Type -AssemblyName UIAutomationClient | Out-Null
  Add-Type -AssemblyName UIAutomationTypes | Out-Null
}} catch {{ Write-Output 'RESULT=fail|UIA assemblies unavailable'; exit }}
try {{
  $root = [System.Windows.Automation.AutomationElement]::RootElement
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]{pid})
  $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
  if ($win -eq $null) {{ Write-Output 'RESULT=fail|main window not found'; exit }}
  $toolbar = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
      (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'gui_application.main_window.mw_toolbar')))
  $scope = if ($toolbar -ne $null) {{ $toolbar }} else {{ $win }}
  foreach ($n in @({nameList})) {{
    $el = $scope.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $n)))
    if ($el -ne $null) {{
      try {{
        $invoke = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Write-Output ('RESULT=ok|' + $n)
        exit
      }} catch {{ }}
    }}
  }}
  Write-Output 'RESULT=fail|toolbar action not found'
}} catch {{
  Write-Output ('RESULT=fail|' + $_.Exception.Message)
}}";
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
