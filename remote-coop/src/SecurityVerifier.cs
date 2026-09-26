using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace DragonCrownRemoteCoop
{
    /// <summary>
    /// Download / installer security policy.
    /// - URL must be HTTPS and point at github.com/{owner}/{repo} or GitHub release asset hosts
    /// - asset file name must match the per-project allow-list
    /// - SHA256 is always computed; GitHub's asset digest is compared when available
    /// - Authenticode is checked when possible (valid / unsigned / invalid)
    /// Nothing is executed when verification fails.
    /// </summary>
    public static class SecurityVerifier
    {
        private static readonly string[] AllowedHosts =
        {
            "github.com",
            "objects.githubusercontent.com",
            "release-assets.githubusercontent.com",
            "api.github.com",
        };

        public static bool ValidateDownloadUrl(string url, string owner, string repo, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(url)) { error = "empty url"; return false; }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) { error = "invalid url"; return false; }
            if (uri.Scheme != Uri.UriSchemeHttps) { error = "not https: " + uri.Scheme; return false; }
            if (!Array.Exists(AllowedHosts, h => h.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)))
            { error = "host not allowed: " + uri.Host; return false; }

            if (uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            {
                string prefix = $"/{owner}/{repo}/releases/download/";
                if (!uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                { error = $"unexpected repository path: {uri.AbsolutePath}"; return false; }
            }
            return true;
        }

        public static bool ValidateFinalHost(string url, out string error)
        {
            error = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) { error = "invalid final url"; return false; }
            if (uri.Scheme != Uri.UriSchemeHttps) { error = "final url not https"; return false; }
            if (!Array.Exists(AllowedHosts, h => h.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)))
            { error = "final host not allowed: " + uri.Host; return false; }
            return true;
        }

        public static string Sha256(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
        }

        public static bool ExtensionAllowed(string fileName, string allowRegex, out string error)
        {
            error = null;
            if (!Regex.IsMatch(fileName ?? "", allowRegex, RegexOptions.IgnoreCase))
            { error = "asset name not allowed: " + fileName; return false; }
            string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
            if (ext != ".exe" && ext != ".msi")
            { error = "unexpected extension: " + ext; return false; }
            return true;
        }

        // ------------------------------------------------------------------ Authenticode
        private enum TrustResult { Valid, Unsigned, Invalid }

        private static TrustResult VerifyAuthenticode(string path, out string signer, out string detail)
        {
            signer = null; detail = null;
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(path);
                signer = cert.Subject;
            }
            catch (Exception ex)
            {
                detail = "no embedded signature (" + ex.GetType().Name + ")";
                return TrustResult.Unsigned;
            }

            uint result = WinVerifyTrustFile(path);
            if (result == 0) { detail = "authenticode valid"; return TrustResult.Valid; }
            detail = "WinVerifyTrust=0x" + result.ToString("X8");
            return TrustResult.Invalid;
        }

        public static string AuthenticodeStatus(string path, out string signer, out string detail)
        {
            var r = VerifyAuthenticode(path, out signer, out detail);
            return r.ToString().ToLowerInvariant();   // valid | unsigned | invalid
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", PreserveSig = true, SetLastError = false)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid actionId, IntPtr data);

        private static uint WinVerifyTrustFile(string path)
        {
            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = path,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            };
            IntPtr pFile = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
            try
            {
                Marshal.StructureToPtr(fileInfo, pFile, false);
                var data = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                    dwUIChoice = 2,             // WTD_UI_NONE
                    fdwRevocationChecks = 0,    // WTD_REVOKE_NONE
                    dwUnionChoice = 1,          // WTD_CHOICE_FILE
                    pFile = pFile,
                    dwStateAction = 0,          // WTD_STATEACTION_IGNORE
                    dwProvFlags = 0x00000010,   // WTD_CACHE_ONLY_URL_RETRIEVAL (offline friendly)
                    dwUIContext = 0
                };
                IntPtr pData = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_DATA>());
                try
                {
                    Marshal.StructureToPtr(data, pData, false);
                    var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE"); // GENERIC_VERIFY_V2
                    return WinVerifyTrust(IntPtr.Zero, action, pData);
                }
                finally { Marshal.FreeHGlobal(pData); }
            }
            finally { Marshal.FreeHGlobal(pFile); }
        }
    }
}
