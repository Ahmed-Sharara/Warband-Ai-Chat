using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace CalradiaAiBridge {
    partial class Program {
        // --- Auth & Utilities ---
        static string CreateMD5( string input ) {
            using (var md5 = MD5.Create()) {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        static void SaveKey( string key ) { File.WriteAllText(KeyFile, key); }
        static string LoadSavedKey() { return File.Exists(KeyFile) ? File.ReadAllText(KeyFile).Trim() : ""; }
        static void ClearSavedKey() { if (File.Exists(KeyFile)) File.Delete(KeyFile); }

        static async Task<bool> VerifyKey( string key ) {
            try {
                int port = GetPlayer2AppPort();
                var healthUrl = _currentBridgeMode == "player2_app" ? $"http://127.0.0.1:{port}/v1/health" : P2HealthUrl;
                var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
                request.Headers.Add("player2-game-key", GameClientId);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
                    var res = await _http.SendAsync(request, cts.Token);
                    return res.IsSuccessStatusCode;
                }
            } catch { return false; }
        }

        static async Task<string> OAuthLogin() {
            Console.WriteLine("[AUTH] Opening Player2 login in your browser ...");
            Dictionary<string, object> data = null;
            try {
                var reqBody = new Dictionary<string, object> { { "client_id", GameClientId } };
                string resStr = await SendWebRequest(P2DeviceNew, reqBody);
                data = _json.Deserialize<Dictionary<string, object>>(resStr);
            } catch (Exception ex) {
                Console.WriteLine("[AUTH] ERROR starting login flow: " + ex.Message);
                Environment.Exit(1);
            }

            int interval = 5;
            if (data != null && data.ContainsKey("interval") && data["interval"] != null) {
                int.TryParse(data["interval"].ToString(), out interval);
            }

            string deviceCode = data != null && data.ContainsKey("deviceCode") && data["deviceCode"] != null ? data["deviceCode"].ToString() : "";

            string completeUrl = "";
            if (data != null) {
                if (data.ContainsKey("verificationUriComplete") && data["verificationUriComplete"] != null) completeUrl = data["verificationUriComplete"].ToString();
                else if (data.ContainsKey("verificationUri") && data["verificationUri"] != null) completeUrl = data["verificationUri"].ToString();
            }

            string userCode = data != null && data.ContainsKey("userCode") && data["userCode"] != null ? data["userCode"].ToString() : "";

            try { Process.Start(completeUrl); } catch { }

            if (!string.IsNullOrEmpty(userCode)) {
                Console.WriteLine(string.Format("[AUTH] If the browser didn't open, go to: {0}", completeUrl));
                Console.WriteLine(string.Format("[AUTH] And enter code: {0}", userCode));
            }

            Console.Write("[AUTH] Waiting for approval");
            while (true) {
                await Task.Delay(interval * 1000);
                Console.Write(".");
                try {
                    var reqBody = new Dictionary<string, object>
                    {
                        { "client_id", GameClientId },
                        { "device_code", deviceCode },
                        { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" }
                    };

                    var content = new StringContent(_json.Serialize(reqBody), Encoding.UTF8, "application/json");
                    var request = new HttpRequestMessage(HttpMethod.Post, P2DeviceToken);
                    request.Headers.Add("player2-game-key", GameClientId);
                    request.Content = content;
                    var res = await _http.SendAsync(request);

                    if (res.IsSuccessStatusCode) {
                        var resStr = await res.Content.ReadAsStringAsync();
                        var json = _json.Deserialize<Dictionary<string, object>>(resStr);
                        if (json != null && json.ContainsKey("p2Key") && json["p2Key"] != null) {
                            string key = json["p2Key"].ToString();
                            if (!string.IsNullOrEmpty(key)) {
                                Console.WriteLine("\n[AUTH] Login approved!");
                                return key;
                            }
                        }
                    }
                } catch { }
            }
        }

        static async Task Authenticate() {
            if (_currentBridgeMode == "player2_app") {
                int port = GetPlayer2AppPort();
                Console.WriteLine($"[AUTH] Connecting to Player2 App on 127.0.0.1:{port} ...");
                try {
                    string p2AppLoginDyn = $"http://127.0.0.1:{port}/v1/login/web/{GameClientId}";
                    var request = new HttpRequestMessage(HttpMethod.Post, p2AppLoginDyn);
                    request.Headers.Add("player2-game-key", GameClientId);
                    var r = await _http.SendAsync(request);
                    r.EnsureSuccessStatusCode();

                    var resStr = await r.Content.ReadAsStringAsync();
                    var json = _json.Deserialize<Dictionary<string, object>>(resStr);

                    if (json != null && json.ContainsKey("p2Key") && json["p2Key"] != null) {
                        _p2Key = json["p2Key"].ToString();
                    }

                    if (string.IsNullOrEmpty(_p2Key)) {
                        Console.WriteLine("[AUTH] ERROR: Player2 App returned no key. Are you logged in?");
                        Environment.Exit(1);
                    }
                    Console.WriteLine("[AUTH] Got key from Player2 App.");
                } catch {
                    Console.WriteLine("[AUTH] ERROR: Could not reach Player2 App. Make sure it is running.");
                    Environment.Exit(1);
                }
            } else if (_currentBridgeMode == "player2_api") {
                if (!string.IsNullOrWhiteSpace(Player2ApiKey)) {
                    _p2Key = Player2ApiKey.Trim();
                    Console.WriteLine("[AUTH] Using API key from config.");
                    if (!await VerifyKey(_p2Key))
                        Console.WriteLine("[AUTH] WARNING: Provided API Key from config seems invalid. If it fails, leave PLAYER2_API_KEY blank to login via browser.");
                } else {
                    string saved = LoadSavedKey();
                    if (!string.IsNullOrEmpty(saved) && await VerifyKey(saved)) {
                        _p2Key = saved;
                        Console.WriteLine("[AUTH] Saved key is valid.");
                        return;
                    }
                    if (!string.IsNullOrEmpty(saved)) ClearSavedKey();
                    _p2Key = await OAuthLogin();
                    SaveKey(_p2Key);
                    Console.WriteLine("[AUTH] Key saved.");
                }
            }
        }

        static void StartHealthPing() {
            Task.Run(async () => {
                while (true) {
                    try {
                        int port = GetPlayer2AppPort();
                        var healthUrl = _currentBridgeMode == "player2_app" ? $"http://127.0.0.1:{port}/v1/health" : P2HealthUrl;
                        var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
                        request.Headers.Add("player2-game-key", GameClientId);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _p2Key);
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5))) {
                            var res = await _http.SendAsync(request, cts.Token);
                            if (res.IsSuccessStatusCode) {
                                Console.WriteLine("[AUTH] Health ping sent successfully.");
                            } else {
                                Console.WriteLine($"[AUTH] Health ping failed: {res.StatusCode}");
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"[AUTH] Health ping error: {ex.Message}");
                    }
                    await Task.Delay(60000);
                }
            });
        }
    }
}
