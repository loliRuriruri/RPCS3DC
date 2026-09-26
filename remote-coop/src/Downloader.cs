using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace DragonCrownRemoteCoop
{
    public sealed class DownloadResult
    {
        public bool Ok;
        public string Path;
        public string Sha256;
        public string Authenticode;     // valid | unsigned | invalid
        public string Signer;
        public string Detail;
        public string FinalUrl;
        public string Error;
    }

    /// <summary>
    /// Downloads a resolved release asset with the project security policy:
    /// HTTPS + owner/repo URL check + asset allow-list + SHA256 (GitHub digest compare) + Authenticode.
    /// The file is deleted and reported as failed when verification fails.
    /// </summary>
    public static class Downloader
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 };
            var c = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            c.DefaultRequestHeaders.UserAgent.ParseAdd("DragonCrownRemoteCoopSetup/1.0");
            return c;
        }

        public static bool DownloadVerified(ReleaseAsset asset, Action<double> progress, out string sha256, out string error)
        {
            var r = Download(asset, progress);
            sha256 = r.Sha256;
            error = r.Error;
            return r.Ok;
        }

        public static DownloadResult Download(ReleaseAsset asset, Action<double> progress)
        {
            var result = new DownloadResult();
            try
            {
                if (asset == null) { result.Error = "no asset"; return result; }

                if (!SecurityVerifier.ValidateDownloadUrl(asset.Url, asset.Owner, asset.Repo, out string urlError))
                { result.Error = "url rejected: " + urlError; AppEnv.Log("download rejected: " + result.Error); return result; }

                var (_, _, regex, _) = ReleaseResolver.Target(
                    asset.Repo.Equals("Sunshine", StringComparison.OrdinalIgnoreCase) ? "sunshine" :
                    asset.Repo.Equals("moonlight-qt", StringComparison.OrdinalIgnoreCase) ? "moonlight" : "vigem");
                regex = ReleaseResolver.RegexForRepo(asset.Owner, asset.Repo) ?? regex;
                if (!SecurityVerifier.ExtensionAllowed(asset.Name, regex, out string extError))
                { result.Error = "asset rejected: " + extError; AppEnv.Log("download rejected: " + result.Error); return result; }

                Directory.CreateDirectory(AppEnv.DownloadsDir);
                string target = Path.Combine(AppEnv.DownloadsDir, asset.Name);
                if (File.Exists(target)) { try { File.Delete(target); } catch { } }

                using (var resp = Http.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
                {
                    resp.EnsureSuccessStatusCode();
                    result.FinalUrl = resp.RequestMessage?.RequestUri?.ToString() ?? asset.Url;
                    if (!SecurityVerifier.ValidateFinalHost(result.FinalUrl, out string finalError))
                    { result.Error = "final url rejected: " + finalError; return result; }

                    long total = resp.Content.Headers.ContentLength ?? asset.Size;
                    using var src = resp.Content.ReadAsStream();
                    using var dst = File.Create(target);
                    var buffer = new byte[1 << 20];
                    long read = 0;
                    int n;
                    while ((n = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dst.Write(buffer, 0, n);
                        read += n;
                        if (total > 0) progress?.Invoke(Math.Min(100.0, read * 100.0 / total));
                    }
                    if (total > 0 && read != total)
                    { result.Error = $"size mismatch (expected {total}, got {read})"; TryDelete(target); return result; }
                }

                result.Path = target;
                result.Sha256 = SecurityVerifier.Sha256(target);

                if (!string.IsNullOrEmpty(asset.Digest) && asset.Digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    string expected = asset.Digest.Substring("sha256:".Length).Trim().ToLowerInvariant();
                    if (!expected.Equals(result.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Error = $"sha256 mismatch (github digest {expected}, file {result.Sha256})";
                        AppEnv.Log("download digest mismatch: " + asset.Name);
                        TryDelete(target);
                        return result;
                    }
                }

                result.Authenticode = SecurityVerifier.AuthenticodeStatus(target, out string signer, out string detail);
                result.Signer = signer;
                result.Detail = detail;
                if (result.Authenticode == "invalid")
                {
                    result.Error = "authenticode invalid: " + detail;
                    AppEnv.Log("download authenticode invalid: " + asset.Name);
                    TryDelete(target);
                    return result;
                }

                result.Ok = true;
                AppEnv.Log($"download ok: {asset.Name} tag={asset.ReleaseTag} sha256={result.Sha256} " +
                           $"authenticode={result.Authenticode} signer={(signer ?? "-")}");
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                AppEnv.Log("download error: " + ex.Message);
                if (!string.IsNullOrEmpty(result.Path)) TryDelete(result.Path);
                return result;
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
