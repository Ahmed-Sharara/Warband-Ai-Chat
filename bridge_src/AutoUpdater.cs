using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;

namespace CalradiaAiBridge {
    public static class AutoUpdater {
        public const string CurrentVersion = "2.0.1";

        private const string GitHubRepo = "Ahmed-Sharara/Warband-Ai-Chat";

        private static string LatestReleaseApiUrl => $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
        private static string DefaultDownloadUrl => $"https://github.com/{GitHubRepo}/releases/latest/download/Ai_bridge_system.exe";

        public static async Task CheckForUpdatesAsync() {
            try {
                // If repository is set to default placeholder, skip update check quietly
                if (string.IsNullOrWhiteSpace(GitHubRepo) ||
                    GitHubRepo.Contains("YOUR_USERNAME") ||
                    GitHubRepo.Contains("YOUR_REPO")) {
                    Console.WriteLine("[Updater] Auto-updater disabled (GitHub repository not configured).");
                    return;
                }

                using (var client = new HttpClient()) {
                    client.DefaultRequestHeaders.Add("User-Agent", "CalradiaAiBridge-Updater");

                    var response = await client.GetAsync(LatestReleaseApiUrl);
                    if (!response.IsSuccessStatusCode) {
                        Console.WriteLine($"[Updater] Could not fetch releases (HTTP {(int)response.StatusCode}). Running current version v{CurrentVersion}.");
                        return;
                    }

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    string tagName = ExtractTagName(jsonResponse);

                    if (string.IsNullOrWhiteSpace(tagName)) {
                        Console.WriteLine("[Updater] Could not parse release tag from server. Running current version.");
                        return;
                    }

                    string cleanLatestTag = tagName.TrimStart('v', 'V').Trim();
                    string cleanCurrentVer = CurrentVersion.TrimStart('v', 'V').Trim();

                    bool isNewer = IsVersionNewer(cleanCurrentVer, cleanLatestTag);

                    if (!isNewer) {
                        Console.WriteLine($"[Updater] You are running the latest version tag: {tagName} (v{CurrentVersion})");
                        return;
                    }

                    string downloadUrl = ExtractExeDownloadUrl(jsonResponse) ?? DefaultDownloadUrl;

                    Console.WriteLine($"[Updater] New release tag found: {tagName}! (Current: v{CurrentVersion}). Downloading update from {downloadUrl}...");

                    // 2. Download the new executable
                    string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                    string newExe = currentExe + ".new";

                    byte[] exeBytes = await client.GetByteArrayAsync(downloadUrl);
                    File.WriteAllBytes(newExe, exeBytes);

                    Console.WriteLine("[Updater] Download complete. Restarting to apply update...");

                    // 3. Create a batch script to replace the old executable and restart
                    string batPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");
                    string exeName = Path.GetFileName(currentExe);

                    string batContent = $@"
@echo off
timeout /t 2 /nobreak > nul
del ""{exeName}""
ren ""{Path.GetFileName(newExe)}"" ""{exeName}""
start """" ""{exeName}""
del ""%~f0""
";
                    File.WriteAllText(batPath, batContent);

                    // 4. Run the batch script and exit
                    ProcessStartInfo psi = new ProcessStartInfo {
                        FileName = batPath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi);

                    Environment.Exit(0);
                }
            } catch (Exception ex) {
                Console.WriteLine($"[Updater] Update check skipped: {ex.Message}");
            }
        }

        private static string ExtractTagName( string jsonResponse ) {
            try {
                var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
                if (jsonDict != null && jsonDict.ContainsKey("tag_name") && jsonDict["tag_name"] != null) {
                    return jsonDict["tag_name"].ToString();
                }
            } catch { }

            // Regex fallback if JSON deserialization fails
            var match = Regex.Match(jsonResponse, @"[""']tag_name[""']\s*:\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
            if (match.Success) {
                return match.Groups[1].Value;
            }

            return null;
        }

        private static string ExtractExeDownloadUrl( string jsonResponse ) {
            try {
                var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);
                if (jsonDict != null && jsonDict.ContainsKey("assets")) {
                    var assetsList = jsonDict["assets"] as System.Collections.ArrayList;
                    if (assetsList != null) {
                        foreach (var item in assetsList) {
                            var asset = item as Dictionary<string, object>;
                            if (asset != null && asset.ContainsKey("browser_download_url") && asset["browser_download_url"] != null) {
                                string url = asset["browser_download_url"].ToString();
                                if (url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
                                    return url;
                                }
                            }
                        }
                    }
                }
            } catch { }

            var match = Regex.Match(jsonResponse, @"[""']browser_download_url[""']\s*:\s*[""']([^""']+\.exe)[""']", RegexOptions.IgnoreCase);
            if (match.Success) {
                return match.Groups[1].Value;
            }

            return null;
        }

        private static bool IsVersionNewer( string currentVerStr, string latestVerStr ) {
            if (Version.TryParse(currentVerStr, out Version currentVer) &&
                Version.TryParse(latestVerStr, out Version latestVer)) {
                return latestVer > currentVer;
            }

            // Fallback string comparison if version string is non-standard
            return !string.Equals(currentVerStr, latestVerStr, StringComparison.OrdinalIgnoreCase);
        }
    }
}


