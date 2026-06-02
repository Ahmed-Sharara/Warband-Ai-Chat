using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace CalradiaAiBridge {
    partial class Program {
        static async Task<string> SendWebRequest( string url, object reqBody, string bearer = null ) {
            var content = new StringContent(_json.Serialize(reqBody), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            if (bearer != null) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }

            var res = await _http.SendAsync(request);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync();
        }

        static async Task<string> GetCloudResponse( string text, Dictionary<string, object> data ) {
            if (string.IsNullOrEmpty(OpenRouterApiKey)) return "I have no voice... (API Key Missing)";

            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Someone";
            string kingdom = data != null && data.ContainsKey("kingdom") && data["kingdom"] != null ? data["kingdom"].ToString() : "Calradia";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            string location = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString() : "Unknown Location";
            string king = data != null && data.ContainsKey("king") && data["king"] != null ? data["king"].ToString() : "None";
            string relation = data != null && data.ContainsKey("relation") && data["relation"] != null ? data["relation"].ToString() : "0";

            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            string roleContext = string.Format(" You are currently at {0}.", location);
            if (role.Contains("elder")) roleContext = string.Format(" You are the elder of {0}. You report to the lords of {1}.", location, kingdom);
            else if (role.Contains("king")) roleContext = string.Format(" You are the ruler of {0}! You demand absolute respect. You are currently at {1}.", kingdom, location);
            else if (role.Contains("lord")) roleContext = string.Format(" You are a proud noble of {0}. You are a vassal of {1}. You are currently at {2}.", kingdom, king, location);

            string relationStr = "You are neutral to the player.";
            int relInt = 0;
            if (int.TryParse(relation, out relInt)) {
                if (relInt < -10) relationStr = "You HATE the player.";
                else if (relInt < 0) relationStr = "You dislike the player.";
                else if (relInt > 20) relationStr = "You are good friends with the player.";
                else if (relInt > 5) relationStr = "You like the player.";
            }

            string systemPrompt = string.Format("You are {0}, a {1} in the medieval world of Mount & Blade: Warband. Talk naturally with correct spacing after punctuation - NEVER run words together (like 'doorsteptrade' instead of 'doorstep. Trade'). Limit your response to 1-3 short sentences.", name, role);
            if (isCompanion) {
                systemPrompt += " You are the player's loyal companion. When ordered to travel, buy, or fetch items, be fully cooperative and accept. If given multiple tasks (e.g. go to Praven, buy a sword, and come back), summarize them nicely and append a sequence block at the end in the format: '[TASKS: Move|TownName, Fetch|ItemName, Return]'. Always include '[MOVE_FirstTownName]' (e.g. '[MOVE_Praven]') at the end as well.";
            }

            var msgs = UpdateMemory(string.Format("{0}_{1}", name, role), systemPrompt, text);

            var reqBody = new Dictionary<string, object>
            {
                {"model", CloudModelId},
                {"messages", msgs},
                {"max_tokens", 80},
                {"temperature", 0.72}
            };

            try {
                string resStr = await SendWebRequest("https://openrouter.ai/api/v1/chat/completions", reqBody, OpenRouterApiKey);
                var json = _json.Deserialize<Dictionary<string, object>>(resStr);

                string aiReply = "...";
                if (json != null && json.ContainsKey("choices")) {
                    var choices = json["choices"] as System.Collections.ArrayList;
                    if (choices != null && choices.Count > 0) {
                        var firstChoice = choices[0] as Dictionary<string, object>;
                        if (firstChoice != null && firstChoice.ContainsKey("message")) {
                            var msgObj = firstChoice["message"] as Dictionary<string, object>;
                            if (msgObj != null && msgObj.ContainsKey("content")) {
                                aiReply = msgObj["content"] != null ? msgObj["content"].ToString().Trim() : "...";
                            }
                        }
                    }
                }

                UpdateMemoryResult(string.Format("{0}_{1}", name, role), aiReply);
                return aiReply;
            } catch (Exception ex) { Console.WriteLine("[ERROR] Cloud API Call Failed: " + ex.Message); }

            return "The wind howls... I cannot speak right now.";
        }

        static async Task<string> GetLocalResponse( string text, Dictionary<string, object> data ) {
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Someone";
            string kingdom = data != null && data.ContainsKey("kingdom") && data["kingdom"] != null ? data["kingdom"].ToString() : "Calradia";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            string location = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString() : "Unknown Location";
            string king = data != null && data.ContainsKey("king") && data["king"] != null ? data["king"].ToString() : "None";
            string relation = data != null && data.ContainsKey("relation") && data["relation"] != null ? data["relation"].ToString() : "0";

            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            string roleContext = string.Format(" You are currently at {0}.", location);
            if (role.Contains("elder")) roleContext = string.Format(" You are the elder of {0}. You report to the lords of {1}.", location, kingdom);
            else if (role.Contains("king")) roleContext = string.Format(" You are the ruler of {0}! You demand absolute respect. You are currently at {1}.", kingdom, location);
            else if (role.Contains("lord")) roleContext = string.Format(" You are a proud noble of {0}. You are a vassal of {1}. You are currently at {2}.", kingdom, king, location);

            string relationStr = "You are neutral to the player.";
            int relInt = 0;
            if (int.TryParse(relation, out relInt)) {
                if (relInt < -10) relationStr = "You HATE the player.";
                else if (relInt < 0) relationStr = "You dislike the player.";
                else if (relInt > 20) relationStr = "You are good friends with the player.";
                else if (relInt > 5) relationStr = "You like the player.";
            }

            string systemPrompt = string.Format("You are {0}, a {1} in the medieval world of Mount & Blade: Warband. Talk naturally with correct spacing after punctuation - NEVER run words together (like 'doorsteptrade' instead of 'doorstep. Trade'). Limit your response to 1-3 short sentences.", name, role);
            if (isCompanion) {
                systemPrompt += " You are the player's loyal companion. When ordered to travel, buy, or fetch items, be fully cooperative and accept. If given multiple tasks (e.g. go to Praven, buy a sword, and come back), summarize them nicely and append a sequence block at the end in the format: '[TASKS: Move|TownName, Fetch|ItemName, Return]'. Always include '[MOVE_FirstTownName]' (e.g. '[MOVE_Praven]') at the end as well.";
            }

            var msgs = UpdateMemory(string.Format("{0}_{1}", name, role), systemPrompt, text);

            var reqBody = new Dictionary<string, object>
            {
                {"model", LocalModelId},
                {"messages", msgs},
                {"max_tokens", 80},
                {"temperature", 0.72}
            };

            try {
                string resStr = await SendWebRequest(LocalApiUrl, reqBody);
                var json = _json.Deserialize<Dictionary<string, object>>(resStr);

                string aiReply = "...";
                if (json != null && json.ContainsKey("choices")) {
                    var choices = json["choices"] as System.Collections.ArrayList;
                    if (choices != null && choices.Count > 0) {
                        var firstChoice = choices[0] as Dictionary<string, object>;
                        if (firstChoice != null && firstChoice.ContainsKey("message")) {
                            var msgObj = firstChoice["message"] as Dictionary<string, object>;
                            if (msgObj != null && msgObj.ContainsKey("content")) {
                                aiReply = msgObj["content"] != null ? msgObj["content"].ToString().Trim() : "...";
                            }
                        }
                    }
                }

                UpdateMemoryResult(string.Format("{0}_{1}", name, role), aiReply);
                return aiReply;
            } catch (Exception ex) { Console.WriteLine("[ERROR] Local Model API Call Failed: " + ex.Message); }

            return "The local gears grind... I cannot find my voice.";
        }

        static async Task<string> GetPlayer2ApiResponse( string text, Dictionary<string, object> data ) {
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Lord";
            string kingdom = data != null && data.ContainsKey("kingdom") && data["kingdom"] != null ? data["kingdom"].ToString() : "Calradia";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            string location = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString() : "Unknown Location";
            string king = data != null && data.ContainsKey("king") && data["king"] != null ? data["king"].ToString() : "None";
            string relation = data != null && data.ContainsKey("relation") && data["relation"] != null ? data["relation"].ToString() : "0";

            string roleContext = string.Format(" You are currently at {0}.", location);
            if (role.Contains("elder")) roleContext = string.Format(" You are the elder of {0}. You report to the lords of {1}.", location, kingdom);
            else if (role.Contains("king")) roleContext = string.Format(" You are the ruler of {0}! You demand absolute respect. You are currently at {1}.", kingdom, location);
            else if (role.Contains("lord")) roleContext = string.Format(" You are a proud noble of {0}. You are a vassal of {1}. You are currently at {2}.", kingdom, king, location);

            string relationStr = "You are neutral to the player.";
            int relInt = 0;
            if (int.TryParse(relation, out relInt)) {
                if (relInt < -10) relationStr = "You HATE the player.";
                else if (relInt < 0) relationStr = "You dislike the player.";
                else if (relInt > 20) relationStr = "You are good friends with the player.";
                else if (relInt > 5) relationStr = "You like the player.";
            }

            string systemPrompt = string.Format("Roleplay as {0}, a {1} of the {2} in the world of Calradia.{3} {4} Respond strictly in character with a gritty medieval tone. Your response MUST be ONLY the spoken dialogue. Limit your response to 1-3 short sentences.", name, role, kingdom, roleContext, relationStr);
            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");
            if (isCompanion) {
                if (location.ToLower().Contains("world map") || location.ToLower().Contains("camp"))
                    systemPrompt += " You are the player's loyal companion. If asked to go to a town, castle, or village (especially Praven), or if asked to fetch/buy an item from a town, respond naturally, agree to go to that town, specify you will return, and include the exact tag [MOVE_PlaceName] at the end.";
                else
                    systemPrompt += " You are the player's loyal companion. If asked to travel, fetch an item, or go somewhere, REFUSE naturally by stating that you cannot depart while indoors or inside a settlement.";
            }

            string memoryKey = string.Format("{0}_{1}_p2", name, role);
            var msgs = UpdateMemory(memoryKey, systemPrompt, text);

            var reqBody = new Dictionary<string, object>
            {
                {"messages", msgs},
                {"temperature", 0.7},
                {"max_tokens", 150}
            };

            try {
                var content = new StringContent(_json.Serialize(reqBody), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, P2ChatUrl);
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _p2Key);

                var res = await _http.SendAsync(request);

                if (res.IsSuccessStatusCode) {
                    string resStr = await res.Content.ReadAsStringAsync();
                    var json = _json.Deserialize<Dictionary<string, object>>(resStr);
                    string textResponse = "...";

                    if (json != null && json.ContainsKey("choices")) {
                        var choices = json["choices"] as System.Collections.ArrayList;
                        if (choices != null && choices.Count > 0) {
                            var firstChoice = choices[0] as Dictionary<string, object>;
                            if (firstChoice != null) {
                                if (firstChoice.ContainsKey("message")) {
                                    var msgObj = firstChoice["message"] as Dictionary<string, object>;
                                    if (msgObj != null && msgObj.ContainsKey("content") && msgObj["content"] != null) {
                                        textResponse = msgObj["content"].ToString();
                                    }
                                } else if (firstChoice.ContainsKey("text") && firstChoice["text"] != null) {
                                    textResponse = firstChoice["text"].ToString();
                                }
                            }
                        }
                    } else if (json != null && json.ContainsKey("response") && json["response"] != null) {
                        textResponse = json["response"].ToString();
                    }

                    if (string.IsNullOrWhiteSpace(textResponse) || textResponse == "None")
                        textResponse = "...";

                    UpdateMemoryResult(memoryKey, textResponse);
                    return textResponse;
                } else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
                    ClearSavedKey();
                    return "My tongue is bound by dark magic... (Auth Error)";
                } else if ((int)res.StatusCode == 429)
                    return "My mind is clouded with exhaustion... (Rate Limit)";
                else
                    return string.Format("I have no words for you. ({0})", (int)res.StatusCode);
            } catch (Exception ex) {
                Console.WriteLine("[ERROR] API Request Failed: " + ex.Message);
                return "The winds are too loud for us to speak.";
            }
        }

        static string GetPlayer2HotseatResponse( string text, Dictionary<string, object> data ) {
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString() : "commoner";
            string npcName = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Someone";
            string kingdom = data != null && data.ContainsKey("kingdom") && data["kingdom"] != null ? data["kingdom"].ToString() : "None";
            string relation = data != null && data.ContainsKey("relation") && data["relation"] != null ? data["relation"].ToString() : "0";
            string location = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString() : "Unknown";
            string king = data != null && data.ContainsKey("king") && data["king"] != null ? data["king"].ToString() : "None";

            Console.Beep();
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("               ** PLAYER 2 CONTROL PANEL **");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine(string.Format(" NPC Name :  {0}  Role: {1}", npcName, role));
            Console.WriteLine(string.Format(" Kingdom  :  {0} (King: {1})", kingdom, king));
            Console.WriteLine(string.Format(" Relation :  {0}    | Location: {1}", relation, location));
            Console.WriteLine(new string('-', 60));
            Console.WriteLine(" [PLAYER SAYS]:");
            Console.WriteLine(string.Format("  - \"{0}\"", text));
            Console.WriteLine(new string('-', 60));
            Console.WriteLine(string.Format(" Roleplay as {0}. State your spoken dialogue.", npcName));
            Console.WriteLine(" No quotation marks or AI codes needed unless triggering actions.");

            Console.Write("\n > Enter spoken response: ");
            string npcSpeech = Console.ReadLine();
            if (npcSpeech != null) npcSpeech = npcSpeech.Trim();
            else npcSpeech = "";

            if (string.IsNullOrEmpty(npcSpeech)) npcSpeech = "The lord gazes at you in heavy silence...";

            Console.WriteLine("\n Trigger gameplay action?");
            Console.WriteLine("  [0] None (Default chat conversation)");
            Console.WriteLine("  [1] Attack / Combat transition (Village Elder only)");
            Console.WriteLine("  [2] Dispatched Movement (Companions only - set destination)");
            Console.WriteLine("  [3] Search & Recruit Companion quest action");

            Console.Write(" Make decision [0-3] (Default: 0): ");
            string actionChoice = Console.ReadLine();
            if (actionChoice != null) actionChoice = actionChoice.Trim();

            if (actionChoice == "1") npcSpeech += " [ACTION_HOSTILE]";
            else if (actionChoice == "2") {
                Console.Write(" Enter destination town/castle (e.g. Sargoth, Sungetche): ");
                string dest = Console.ReadLine();
                if (dest != null) dest = dest.Trim().ToLower();
                else dest = "";

                if (_townsMap.ContainsKey(dest)) {
                    npcSpeech += string.Format(" [MOVE_{0}]", dest.ToUpper().Replace(" ", "_"));
                    Console.WriteLine(string.Format(" > Appending move target tag [MOVE_{0}]", dest.ToUpper()));
                } else {
                    string found = _townsMap.Keys.FirstOrDefault(k => k.Contains(dest));
                    if (found != null) {
                        npcSpeech += string.Format(" [MOVE_{0}]", found.ToUpper().Replace(" ", "_"));
                        Console.WriteLine(string.Format(" > Match found! Sending to {0}", found.ToUpper()));
                    } else Console.WriteLine(" [ERROR] Unrecognized town name. Destination ignored.");
                }
            } else if (actionChoice == "3") Console.WriteLine(" > Companion searching action triggered.");

            Console.WriteLine(new string('=', 60) + "\n Processing response and writing back to game...");
            return npcSpeech;
        }
    }
}