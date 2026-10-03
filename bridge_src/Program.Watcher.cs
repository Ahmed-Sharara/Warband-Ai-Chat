using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace CalradiaAiBridge {
    partial class Program {
        static async void OnFileChanged( object sender, FileSystemEventArgs e ) {
            if (string.Equals(e.FullPath, InputFile, StringComparison.OrdinalIgnoreCase)) {
                await ProcessRequestAsync();
            }
        }

        static async Task ProcessRequestAsync() {
            try {
                if ((DateTime.UtcNow - _lastProcessedTime).TotalSeconds < Cooldown) return;
                await Task.Delay(50); // Small debounce

                if (!File.Exists(InputFile) || new FileInfo(InputFile).Length < 5) return;

                string jsonStr = "";
                // Read with retry for file locks
                for (int i = 0; i < 5; i++) {
                    try {
                        using (var fs = new FileStream(InputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs, Encoding.UTF8)) {
                            jsonStr = await sr.ReadToEndAsync();
                        }
                        break;
                    } catch (IOException) { await Task.Delay(10); }
                }

                if (string.IsNullOrWhiteSpace(jsonStr)) return;

                Dictionary<string, object> data = null;
                try { data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr); } catch { return; }

                string msg = "";
                if (data != null && data.ContainsKey("message") && data["message"] != null) {
                    msg = data["message"].ToString().Trim();
                }

                if (string.IsNullOrEmpty(msg)) return;

                string currentHash = CreateMD5(msg);
                if (currentHash == _lastMsgHash) return;
                _lastMsgHash = currentHash;
                _lastProcessedTime = DateTime.UtcNow;

                // Clear input file
                for (int i = 0; i < 5; i++) {
                    try {
                        File.WriteAllText(InputFile, "{}");
                        break;
                    } catch (IOException) { await Task.Delay(10); }
                }

                string responseText;
                if (_currentBridgeMode == "cloud") responseText = await GetCloudResponse(msg, data);
                else if (_currentBridgeMode == "local") responseText = await GetLocalResponse(msg, data);
                else if (_currentBridgeMode == "player2_hotseat") responseText = GetPlayer2HotseatResponse(msg, data);
                else if (_currentBridgeMode == "player2_api" || _currentBridgeMode == "player2_app") responseText = await GetPlayer2ApiResponse(msg, data);
                else responseText = "Who goes there? [SYSTEM: Bridge mode is incorrectly configured.]";

                string cleanResponse = responseText.Replace("\n", " ").Replace("\"", "'").Trim();

                int relDec = 0;
                int relInc = 0;
                if (cleanResponse.Contains("[RELATION_DOWN]")) relDec = 1;
                if (cleanResponse.Contains("[RELATION_UP]")) relInc = 1;

                var outData = new Dictionary<string, object>
                {
                    {"response", cleanResponse},
                    {"actionPresent", 0},
                    {"action", 0},
                    {"moveTarget", -1},
                    {"relationDecrease", relDec},
                    {"relationIncrease", relInc}
                };

                string msgLower = msg.ToLower();
                string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
                string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString().ToLower() : "";
                string locationStr = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString().ToLower() : "";

                bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");
                bool isElder = name.Contains("elder") || role.Contains("elder");
                bool isWorldMap = locationStr.Contains("world map") || locationStr.Contains("camp");

                string[] threats = { "burn", "killing", "raid", "destroy", "attack", "to arms" };
                bool isThreat = threats.Any(w => msgLower.Contains(w));

                if (isCompanion) {
                    string[] hateWords = { "hate you", "despise you", "dislike you", "hate", "scum" };
                    if (hateWords.Any(w => msgLower.Contains(w))) outData["relationDecrease"] = 1;
                }

                string[] moveWords = { "go", "ride", "travel", "move", "head", "lead", "scout", "march", "run", "depart", "patrol", "sent to", "send to", "heading", "riding", "traveling", "moving" };
                bool hasMoveIntent = moveWords.Any(w => msgLower.Contains(w));

                string[] fetchWords = { "give me", "fetch", "bring me", "buy", "get me", "find me", "can you get", "get", "procure", "purchase", "obtain", "chicken", "fish", "food", "bread", "beef" };
                bool hasFetchIntent = fetchWords.Any(w => msgLower.Contains(w));

                var match = Regex.Match(cleanResponse, @"\[MOVE_([A-Za-z_]+)\]", RegexOptions.IgnoreCase);
                string detectedTownName = null;

                if (match.Success) {
                    detectedTownName = match.Groups[1].Value.ToLower().Replace("_", " ");
                    hasMoveIntent = true;
                } else {
                    string searchText = string.Format("{0} {1}", msgLower, cleanResponse.ToLower());
                    foreach (var tName in _townsMap.Keys) {
                        if (searchText.Contains(tName)) {
                            detectedTownName = tName;
                            break;
                        }
                    }
                }

                int detectedItemId = -1;
                if (hasFetchIntent) {
                    if (_itemsMap.Count > 0) {
                        foreach (var itemKey in _itemsMap.Keys.OrderByDescending(k => k.Length)) {
                            if (msgLower.Contains(itemKey) && itemKey.Length >= 3) // ensure we don't match on "a" or "an"
                            {
                                detectedItemId = _itemsMap[itemKey];
                                break;
                            }
                        }
                    } else {
                        // Fallback mapping if python didn't load
                        var itemsMap = new Dictionary<string, int> {
                            {"best sword", 13}, {"sword", 13}, {"food", 111}, {"bread", 122},
                            {"armor", 130}, {"horse", 9}, {"shield", 10}, {"bow", 6},
                            {"arrows", 4}, {"axe", 14}, {"spear", 1}, {"mace", 2},
                            {"ale", 110}, {"wine", 109},
                            {"smoked fish", 111}, {"smoked_fish", 111}, {"fish", 111},
                            {"cheese", 112}, {"honey", 113}, {"sausages", 114},
                            {"cabbages", 115}, {"cabbage", 115}, {"dried meat", 116},
                            {"dried_meat", 116}, {"meat", 116}, {"apples", 117},
                            {"fruit", 117}, {"grapes", 118}, {"olives", 119},
                            {"grain", 120}, {"beef", 121}, {"chicken", 123},
                            {"chickens", 123}, {"pork", 124}, {"butter", 125}
                        };

                        foreach (var itemKey in itemsMap.Keys.OrderByDescending(k => k.Length)) {
                            if (msgLower.Contains(itemKey)) {
                                detectedItemId = itemsMap[itemKey];
                                break;
                            }
                        }
                    }
                }

                bool hasHostileTag = cleanResponse.Contains("[ACTION_HOSTILE]");

                var tasks = new List<Tuple<int, int>>();
                var tasksMatch = Regex.Match(cleanResponse, @"\[TASKS:\s*([^\]]+)\]", RegexOptions.IgnoreCase);
                if (tasksMatch.Success) {
                    string tasksStr = tasksMatch.Groups[1].Value.Trim();
                    string[] parts = tasksStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string partRaw in parts) {
                        string part = partRaw.Trim().ToLower();
                        if (part.Contains("move|") || part.Contains("go|") || part.Contains("travel|") || part.Contains("ride|")) {
                            string[] sep = part.Split('|');
                            if (sep.Length > 1) {
                                string tName = sep[1].Trim();
                                if (_townsMap.ContainsKey(tName)) {
                                    tasks.Add(Tuple.Create(1, _townsMap[tName]));
                                }
                            }
                        } else if (part.Contains("fetch|") || part.Contains("buy|") || part.Contains("get|")) {
                            string[] sep = part.Split('|');
                            if (sep.Length > 1) {
                                string iName = sep[1].Trim();
                                int itemId = -1;
                                foreach (var itemKey in _itemsMap.Keys.OrderByDescending(k => k.Length)) {
                                    if (iName.Contains(itemKey) && itemKey.Length >= 3) {
                                        itemId = _itemsMap[itemKey];
                                        break;
                                    }
                                }
                                if (itemId == -1) {
                                    var commonItems = new Dictionary<string, int> {
                                        {"best sword", 13}, {"sword", 13}, {"food", 111}, {"bread", 122},
                                        {"armor", 130}, {"horse", 9}, {"shield", 10}, {"bow", 6},
                                        {"arrows", 4}, {"axe", 14}, {"spear", 1}, {"mace", 2},
                                        {"ale", 110}, {"wine", 109},
                                        {"smoked fish", 111}, {"smoked_fish", 111}, {"fish", 111},
                                        {"cheese", 112}, {"honey", 113}, {"sausages", 114},
                                        {"cabbages", 115}, {"cabbage", 115}, {"dried meat", 116},
                                        {"dried_meat", 116}, {"meat", 116}, {"apples", 117},
                                        {"fruit", 117}, {"grapes", 118}, {"olives", 119},
                                        {"grain", 120}, {"beef", 121}, {"chicken", 123},
                                        {"chickens", 123}, {"pork", 124}, {"butter", 125}
                                    };
                                    foreach (var key in commonItems.Keys) {
                                        if (iName.Contains(key)) {
                                            itemId = commonItems[key];
                                            break;
                                        }
                                    }
                                }
                                if (itemId != -1) {
                                    tasks.Add(Tuple.Create(2, itemId));
                                }
                            }
                        } else if (part.Contains("return") || part.Contains("back")) {
                            tasks.Add(Tuple.Create(3, 0));
                        }
                    }
                }

                if (tasks.Count == 0 && isCompanion && hasMoveIntent && detectedTownName != null) {
                    int townId = _townsMap.ContainsKey(detectedTownName) ? _townsMap[detectedTownName] : -1;
                    if (townId != -1) {
                        tasks.Add(Tuple.Create(1, townId));
                        if (detectedItemId != -1) {
                            tasks.Add(Tuple.Create(2, detectedItemId));
                            tasks.Add(Tuple.Create(3, 0));
                        }
                    }
                }

                if (isCompanion && tasks.Count > 0) {
                    outData["action"] = 2;
                    outData["actionPresent"] = 1;
                    outData["moveTarget"] = tasks[0].Item2;
                    outData["task_count"] = tasks.Count;
                    for (int i = 0; i < tasks.Count; i++) {
                        outData[$"task_{i + 1}_type"] = tasks[i].Item1;
                        outData[$"task_{i + 1}_val"] = tasks[i].Item2;
                    }

                    var buyTask = tasks.FirstOrDefault(t => t.Item1 == 2);
                    if (buyTask != null) {
                        outData["fetchItem"] = buyTask.Item2;
                    }
                } else if (isElder && isThreat) {
                    outData["action"] = 1;
                    outData["actionPresent"] = 1;
                } else if (hasHostileTag && (isElder || isThreat || _currentBridgeMode == "player2_hotseat" || _currentBridgeMode == "player2_api")) {
                    outData["action"] = 1;
                    outData["actionPresent"] = 1;
                } else if (isCompanion && new[] { "rescue", "find", "lost", "where" }.Any(w => msgLower.Contains(w))) {
                    outData["action"] = 3;
                    outData["actionPresent"] = 1;
                }

                string cleanSansTags = Regex.Replace(cleanResponse, @"\[[^\]]+\]", " ").Trim();
                cleanSansTags = Regex.Replace(cleanSansTags, @"\s+", " ").Trim();
                outData["response"] = cleanSansTags;

                for (int i = 0; i < 5; i++) {
                    try {
                        File.WriteAllText(OutputFile, JsonSerializer.Serialize(outData));
                        break;
                    } catch (IOException) { await Task.Delay(10); }
                }

                if (_currentBridgeMode != "player2_hotseat")
                    Console.WriteLine(string.Format("[SUCCESS] AI Answered: \"{0}\" | Actions: {1}", cleanSansTags, JsonSerializer.Serialize(outData)));
                else
                    Console.WriteLine(string.Format("[SUCCESS] Player 2 Answer sent to Calradia! Response: \"{0}\"", cleanSansTags));

            } catch (Exception ex) {
                Console.WriteLine("!!! Processing Error: " + ex.Message);
            }
        }
    }
}
