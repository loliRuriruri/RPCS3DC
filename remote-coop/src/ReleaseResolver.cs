using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace DragonCrownRemoteCoop
{
    public sealed class ReleaseAsset
    {
        public string Owner, Repo, Name, Url, Digest, ReleaseTag;
        public long Size;
        public DateTimeOffset PublishedAt;
        public override string ToString() => $"{ReleaseTag}/{Name} ({Size:N0} bytes)";
    }

    /// <summary>
    /// Resolves the newest *stable* GitHub release asset for a project.
    /// Drafts and pre-releases are excluded and no version strings are hardcoded.
    /// </summary>
    public static class ReleaseResolver
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd("DragonCrownRemoteCoopSetup/1.0 (+https://github.com/loliRuriruri/RPCS3DC)");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return c;
        }

        public static (string owner, string repo, string regex, string label) Target(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "sunshine": return ("LizardByte", "Sunshine", @"^Sunshine-Windows-AMD64(-installer)?\.msi$", "Sunshine");
                case "moonlight": return ("moonlight-stream", "moonlight-qt", @"^MoonlightSetup-?[\d\.]*\.exe$", "Moonlight");
                case "vigem": return ("nefarius", "ViGEmBus", @"^ViGEmBus_.*\.exe$", "ViGEmBus (legacy)");
                default: throw new ArgumentException("unknown target: " + key);
            }
        }

        /// <summary>Asset allow-list regex for a known repository (owner/repo).</summary>
        public static string RegexForRepo(string owner, string repo)
        {
            if (string.Equals(repo, "Sunshine", StringComparison.OrdinalIgnoreCase)) return @"^Sunshine-Windows-AMD64(-installer)?\.msi$";
            if (string.Equals(repo, "moonlight-qt", StringComparison.OrdinalIgnoreCase)) return @"^MoonlightSetup-?[\d\.]*\.exe$";
            if (string.Equals(repo, "ViGEmBus", StringComparison.OrdinalIgnoreCase)) return @"^ViGEmBus_.*\.exe$";
            return null;
        }

        /// <summary>Returns the newest stable release asset matching the allow-list, or null.</summary>
        public static ReleaseAsset ResolveLatestStable(string owner, string repo, string assetRegex)
        {
            try
            {
                string url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=30";
                string json = Http.GetStringAsync(url).GetAwaiter().GetResult();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

                var candidates = new List<(DateTimeOffset published, JsonElement release)>();
                foreach (var rel in doc.RootElement.EnumerateArray())
                {
                    if (rel.TryGetProperty("draft", out var d) && d.GetBoolean()) continue;
                    if (rel.TryGetProperty("prerelease", out var p) && p.GetBoolean()) continue;
                    DateTimeOffset pub = DateTimeOffset.MinValue;
                    if (rel.TryGetProperty("published_at", out var pa) && pa.ValueKind == JsonValueKind.String)
                        DateTimeOffset.TryParse(pa.GetString(), out pub);
                    candidates.Add((pub, rel));
                }

                foreach (var (pub, rel) in candidates.OrderByDescending(c => c.published))
                {
                    string tag = rel.TryGetProperty("tag_name", out var t) ? t.GetString() : "";
                    if (!rel.TryGetProperty("assets", out var assets)) continue;
                    foreach (var a in assets.EnumerateArray())
                    {
                        string name = a.TryGetProperty("name", out var n) ? n.GetString() : "";
                        if (!System.Text.RegularExpressions.Regex.IsMatch(name ?? "", assetRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            continue;
                        return new ReleaseAsset
                        {
                            Owner = owner,
                            Repo = repo,
                            Name = name,
                            Url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null,
                            Size = a.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt64() : 0,
                            Digest = a.TryGetProperty("digest", out var dg) && dg.ValueKind == JsonValueKind.String ? dg.GetString() : null,
                            ReleaseTag = tag,
                            PublishedAt = pub,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                AppEnv.Log($"release resolve failed for {owner}/{repo}: {ex.Message}");
            }
            return null;
        }
    }
}
