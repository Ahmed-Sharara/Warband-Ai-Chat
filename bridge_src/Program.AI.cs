using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;

namespace CalradiaAiBridge
{
    partial class Program
    {
        static async Task<string> SendWebRequest(string url, object reqBody, string bearer = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");
            
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (url.Contains("player2"))
            {
                request.Headers.Add("player2-game-key", GameClientId);
            }
            if (url.Contains("openrouter.ai"))
            {
                request.Headers.Add("HTTP-Referer", "https://github.com/CalradiaAiBridge");
                request.Headers.Add("X-Title", "Calradia AI Bridge");
            }
            request.Content = content;
            if (bearer != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            }
            
            var res = await _http.SendAsync(request);
            if (!res.IsSuccessStatusCode)
            {
                string errContent = await res.Content.ReadAsStringAsync();
                throw new Exception(string.Format("HTTP {0}: {1}", (int)res.StatusCode, errContent));
            }
            return await res.Content.ReadAsStringAsync();
        }

        static string ExtractContentString(object contentObj)
        {
            if (contentObj == null) return null;
            if (contentObj is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.String) return elem.GetString()?.Trim();
                if (elem.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var item in elem.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String) sb.Append(item.GetString());
                        else if (item.ValueKind == JsonValueKind.Object)
                        {
                            if (item.TryGetProperty("text", out var t)) sb.Append(t.ToString());
                            else if (item.TryGetProperty("content", out var c)) sb.Append(c.ToString());
                        }
                    }
                    return sb.ToString().Trim();
                }
                return elem.ToString()?.Trim();
            }
            if (contentObj is string str) return str.Trim();
            if (contentObj is System.Collections.IList list)
            {
                var sb = new StringBuilder();
                foreach (var item in list)
                {
                    if (item is string s) sb.Append(s);
                    else if (item is Dictionary<string, object> d)
                    {
                        if (d.ContainsKey("text") && d["text"] != null) sb.Append(d["text"].ToString());
                        else if (d.ContainsKey("content") && d["content"] != null) sb.Append(d["content"].ToString());
                    }
                }
                return sb.ToString().Trim();
            }
            return contentObj.ToString().Trim();
        }

        static string RecoverDialogueFromReasoning(string reasoning)
        {
            if (string.IsNullOrWhiteSpace(reasoning)) return null;

            var m = System.Text.RegularExpressions.Regex.Match(reasoning, @"###\s*Answer\s*:\s*([\s\S]+?)(?=""|\r?\n\r?\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                string ans = m.Groups[1].Value.Trim();
                if (IsValidAiReply(ans)) return ans;
            }

            var mAgent = System.Text.RegularExpressions.Regex.Match(reasoning, @"(?:agent|assistant)\s*:\s*([\s\S]+?)(?=""|\r?\n\r?\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (mAgent.Success && !string.IsNullOrWhiteSpace(mAgent.Groups[1].Value))
            {
                string ans = mAgent.Groups[1].Value.Trim();
                if (IsValidAiReply(ans)) return ans;
            }

            return null;
        }

        static string ExtractResponseText(Dictionary<string, object> json)
        {
            if (json == null) return null;

            if (json.ContainsKey("error") && json["error"] != null)
            {
                Console.WriteLine("[ERROR] API Error: " + JsonSerializer.Serialize(json["error"]));
                return null;
            }

            if (json.ContainsKey("choices"))
            {
                object choicesObj = json["choices"];
                if (choicesObj is JsonElement choicesElem && choicesElem.ValueKind == JsonValueKind.Array && choicesElem.GetArrayLength() > 0)
                {
                    var firstChoice = choicesElem[0];
                    if (firstChoice.ValueKind == JsonValueKind.Object)
                    {
                        if (firstChoice.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.Object)
                        {
                            if (msgProp.TryGetProperty("content", out var c))
                            {
                                string text = c.ValueKind == JsonValueKind.String ? c.GetString() : c.ToString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (IsValidAiReply(text)) return text.Trim();
                                    if (msgProp.TryGetProperty("reasoning", out var r))
                                    {
                                        string rText = r.ValueKind == JsonValueKind.String ? r.GetString() : r.ToString();
                                        string recovered = RecoverDialogueFromReasoning(rText);
                                        if (!string.IsNullOrWhiteSpace(recovered)) return recovered;
                                    }
                                }
                            }
                            if (msgProp.TryGetProperty("text", out var t))
                            {
                                string text = t.ValueKind == JsonValueKind.String ? t.GetString() : t.ToString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (IsValidAiReply(text)) return text.Trim();
                                    if (msgProp.TryGetProperty("reasoning", out var r))
                                    {
                                        string rText = r.ValueKind == JsonValueKind.String ? r.GetString() : r.ToString();
                                        string recovered = RecoverDialogueFromReasoning(rText);
                                        if (!string.IsNullOrWhiteSpace(recovered)) return recovered;
                                    }
                                }
                            }
                        }
                        if (firstChoice.TryGetProperty("delta", out var deltaProp) && deltaProp.ValueKind == JsonValueKind.Object)
                        {
                            if (deltaProp.TryGetProperty("content", out var c))
                            {
                                string text = c.ValueKind == JsonValueKind.String ? c.GetString() : c.ToString();
                                if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text.Trim();
                            }
                        }
                        if (firstChoice.TryGetProperty("text", out var txtProp))
                        {
                            string text = txtProp.ValueKind == JsonValueKind.String ? txtProp.GetString() : txtProp.ToString();
                            if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text.Trim();
                        }
                        if (firstChoice.TryGetProperty("response", out var respProp))
                        {
                            string text = respProp.ValueKind == JsonValueKind.String ? respProp.GetString() : respProp.ToString();
                            if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text.Trim();
                        }
                    }
                }
                else if (choicesObj is System.Collections.IList list && list.Count > 0)
                {
                    if (list[0] is Dictionary<string, object> firstChoice)
                    {
                        // 1. Check message object
                        if (firstChoice.ContainsKey("message") && firstChoice["message"] is Dictionary<string, object> msgObj)
                        {
                            if (msgObj.ContainsKey("content") && msgObj["content"] != null)
                            {
                                string text = ExtractContentString(msgObj["content"]);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (IsValidAiReply(text)) return text;
                                    if (msgObj.ContainsKey("reasoning") && msgObj["reasoning"] != null)
                                    {
                                        string rText = msgObj["reasoning"].ToString();
                                        string recovered = RecoverDialogueFromReasoning(rText);
                                        if (!string.IsNullOrWhiteSpace(recovered)) return recovered;
                                    }
                                }
                            }
                            if (msgObj.ContainsKey("text") && msgObj["text"] != null)
                            {
                                string text = ExtractContentString(msgObj["text"]);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    if (IsValidAiReply(text)) return text;
                                    if (msgObj.ContainsKey("reasoning") && msgObj["reasoning"] != null)
                                    {
                                        string rText = msgObj["reasoning"].ToString();
                                        string recovered = RecoverDialogueFromReasoning(rText);
                                        if (!string.IsNullOrWhiteSpace(recovered)) return recovered;
                                    }
                                }
                            }
                        }

                        // 2. Check delta object (streaming/chunk format)
                        if (firstChoice.ContainsKey("delta") && firstChoice["delta"] is Dictionary<string, object> deltaObj)
                        {
                            if (deltaObj.ContainsKey("content") && deltaObj["content"] != null)
                            {
                                string text = ExtractContentString(deltaObj["content"]);
                                if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
                            }
                        }

                        // 3. Direct choice fields
                        if (firstChoice.ContainsKey("text") && firstChoice["text"] != null)
                        {
                            string text = ExtractContentString(firstChoice["text"]);
                            if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
                        }
                        if (firstChoice.ContainsKey("response") && firstChoice["response"] != null)
                        {
                            string text = ExtractContentString(firstChoice["response"]);
                            if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
                        }
                    }
                }
            }

            // Fallbacks for Ollama / LocalAI / custom servers
            if (json.ContainsKey("response") && json["response"] != null)
            {
                string text = ExtractContentString(json["response"]);
                if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
            }
            if (json.ContainsKey("text") && json["text"] != null)
            {
                string text = ExtractContentString(json["text"]);
                if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
            }
            if (json.ContainsKey("content") && json["content"] != null)
            {
                string text = ExtractContentString(json["content"]);
                if (!string.IsNullOrWhiteSpace(text) && IsValidAiReply(text)) return text;
            }

            return null;
        }

        static bool IsValidAiReply(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string trimmed = text.Trim();
            string lower = trimmed.ToLowerInvariant();

            // Detect and reject content safety classifier outputs (e.g. nvidia/nemotron-3.5-content-safety)
            if (lower.Contains("user safety:") || lower.Contains("response safety:") || lower.Contains("user safety") || lower.Contains("response safety"))
                return false;
            if (lower.StartsWith("safety rating") || lower.Contains("safety: safe") || lower.Contains("safety: unsafe"))
                return false;
            if (lower == "safe" || lower == "unsafe")
                return false;

            return true;
        }

        public static string SanitizeAiResponse(string raw, bool isCompanion = false)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return isCompanion ? "I hear you, captain. Speak your mind." : "I am listening. What business have you with me?";
            }

            string text = raw;

            // 1. Remove XML/HTML style thinking tags (<think>...</think>, <thought>...</thought>, etc.)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<thought>[\s\S]*?</thought>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // In case the token limit truncated before the closing </think>
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<thought>[\s\S]*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 2. Handle raw plain-text thinking / reasoning dumps
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"(?:Here'?s\s+a\s+thinking\s+process|Thinking\s+Process|Analyze\s+User\s+Input|Check\s+Instructions|Formulate\s+Response)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                // Check if there is an explicit dialogue/response section after the reasoning
                var dialogueMatch = System.Text.RegularExpressions.Regex.Match(text, @"(?:(?:Final\s+Answer|Final\s+Response|Spoken\s+Dialogue|Dialogue|Response|Output|Nizar|Companion|Lord|In\s+character)\s*:\s*)([\s\S]+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (dialogueMatch.Success && !string.IsNullOrWhiteSpace(dialogueMatch.Groups[1].Value))
                {
                    text = dialogueMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // Or look for quoted text at the end
                    var quoteMatches = System.Text.RegularExpressions.Regex.Matches(text, @"""([^""]{4,})""");
                    if (quoteMatches.Count > 0)
                    {
                        string lastQuote = quoteMatches[quoteMatches.Count - 1].Groups[1].Value.Trim();
                        if (!lastQuote.ToLower().Contains("analyze user input") && !lastQuote.ToLower().Contains("thinking process") && !lastQuote.ToLower().Contains("relation_"))
                        {
                            text = lastQuote;
                        }
                        else
                        {
                            text = "";
                        }
                    }
                    else
                    {
                        // The entire response was consumed by the thinking process (token cut off)
                        text = "";
                    }
                }
            }

            // 3. Remove surrounding quotes if the entire dialogue is enclosed
            text = text.Trim();
            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 2)
                text = text.Substring(1, text.Length - 2).Trim();
            if (text.StartsWith("`") && text.EndsWith("`") && text.Length > 2)
                text = text.Substring(1, text.Length - 2).Trim();

            if (string.IsNullOrWhiteSpace(text) || text == "..." || text == "None" || !IsValidAiReply(text))
            {
                return isCompanion ? "I hear you, captain. Speak your mind." : "I am listening. What business have you with me?";
            }

            return text;
        }

        static List<string> _cachedOpenRouterFreeModels = null;
        static DateTime _lastOpenRouterFetch = DateTime.MinValue;

        static async Task<List<string>> GetOpenRouterFreeModels()
        {
            if (_cachedOpenRouterFreeModels != null && (DateTime.UtcNow - _lastOpenRouterFetch).TotalMinutes < 60)
            {
                return _cachedOpenRouterFreeModels;
            }

            var freshList = new List<string>();
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/models");
                req.Headers.Add("User-Agent", "CalradiaAiBridge");
                if (!string.IsNullOrEmpty(OpenRouterApiKey))
                {
                    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OpenRouterApiKey);
                }
                var resp = await _http.SendAsync(req);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in dataArr.EnumerateArray())
                            {
                                if (item.TryGetProperty("id", out var idProp))
                                {
                                    string id = idProp.GetString();
                                    if (string.IsNullOrWhiteSpace(id)) continue;
                                    string lower = id.ToLowerInvariant();
                                    
                                    // Skip safety classifiers, non-text, and image/audio generators
                                    if (lower.Contains("content-safety") || lower.Contains("lyria") || lower.Contains("clip") || lower.Contains("image"))
                                        continue;
                                    
                                    bool isFree = lower.EndsWith(":free");
                                    if (!isFree && item.TryGetProperty("pricing", out var pricing))
                                    {
                                        if (pricing.TryGetProperty("prompt", out var p) && pricing.TryGetProperty("completion", out var c))
                                        {
                                            if (p.GetString() == "0" && c.GetString() == "0") isFree = true;
                                        }
                                    }

                                    if (isFree && !id.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase))
                                    {
                                        freshList.Add(id);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            var verifiedFallbacks = new string[] {
                "google/gemma-4-31b-it:free",
                "google/gemma-4-26b-a4b-it:free",
                "qwen/qwen3.8-27b:free",
                "nvidia/nemotron-3-super-120b-a12b:free",
                "nvidia/nemotron-3.5-lightning:free",
                "liquid/lfm-2.5-2.6b:free",
                "inclusionai/ling-3.1-flash"
            };

            if (freshList.Count > 0)
            {
                var result = new List<string>();
                foreach (var p in verifiedFallbacks)
                {
                    if (freshList.Contains(p, StringComparer.OrdinalIgnoreCase))
                        result.Add(p);
                }
                foreach (var item in freshList)
                {
                    if (!result.Contains(item, StringComparer.OrdinalIgnoreCase))
                        result.Add(item);
                }

                _cachedOpenRouterFreeModels = result;
                _lastOpenRouterFetch = DateTime.UtcNow;
                Console.WriteLine(string.Format("[OPENROUTER] Refreshed {0} active free models from OpenRouter API.", _cachedOpenRouterFreeModels.Count));
                return _cachedOpenRouterFreeModels;
            }

            return new List<string>(verifiedFallbacks);
        }

        static async Task<string> GetCloudResponse(string text, Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(OpenRouterApiKey)) return "I have no voice... (API Key Missing)";
            
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Someone";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            string systemPrompt = BuildSystemPrompt(data, text);
            var msgs = UpdateMemory(string.Format("{0}_{1}", name, role), systemPrompt, text);

            // Custom or default OpenAI-compatible cloud endpoint
            string endpoint = string.IsNullOrWhiteSpace(CloudAPIEndpoint) ? "https://openrouter.ai/api/v1/chat/completions" : CloudAPIEndpoint.Trim();

            // Candidate models for fallback if a free tier model gets rate-limited (HTTP 429) or fails
            var obsolete = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "meta-llama/llama-3.3-70b-instruct:free",
                "meta-llama/llama-3.1-8b-instruct:free",
                "qwen/qwen-2.5-72b-instruct:free",
                "google/gemini-2.0-flash-lite-preview-02-05:free",
                "deepseek/deepseek-r1:free",
                "mistralai/mistral-7b-instruct:free",
                "openrouter/free",
                "nvidia/nemotron-3.5-content-safety:free"
            };

            var modelsToTry = new List<string>();
            if (!string.IsNullOrWhiteSpace(CloudModelId) && !obsolete.Contains(CloudModelId.Trim()))
            {
                modelsToTry.Add(CloudModelId.Trim());
            }

            if (endpoint.Contains("openrouter.ai"))
            {
                var activeFree = await GetOpenRouterFreeModels();
                foreach (var fb in activeFree)
                {
                    if (!modelsToTry.Contains(fb, StringComparer.OrdinalIgnoreCase))
                    {
                        modelsToTry.Add(fb);
                    }
                }
            }
            if (modelsToTry.Count == 0)
            {
                modelsToTry.Add("google/gemma-4-31b-it:free");
            }

            foreach (var model in modelsToTry)
            {
                var reqBody = new Dictionary<string, object>
                {
                    {"model", model},
                    {"messages", msgs},
                    {"max_tokens", 450},
                    {"temperature", 0.72}
                };

                try
                {
                    Console.WriteLine(string.Format("[CLOUD] Calling API endpoint: {0} | Model: {1}", endpoint, model));
                    string resStr = await SendWebRequest(endpoint, reqBody, OpenRouterApiKey);
                    Console.WriteLine("[DEBUG] Cloud Raw Response: " + resStr);
                    var json = JsonSerializer.Deserialize<Dictionary<string, object>>(resStr);
                    
                    string rawReply = ExtractResponseText(json);
                    if (!IsValidAiReply(rawReply))
                    {
                        Console.WriteLine(string.Format("[WARNING] Model {0} returned non-dialogue/safety response: \"{1}\". Trying fallback...", model, rawReply));
                        continue;
                    }

                    string aiReply = SanitizeAiResponse(rawReply, isCompanion);
                    if (!IsValidAiReply(aiReply))
                    {
                        Console.WriteLine(string.Format("[WARNING] Model {0} returned invalid response after sanitizing. Trying fallback...", model));
                        continue;
                    }
                    
                    UpdateMemoryResult(string.Format("{0}_{1}", name, role), text, aiReply);
                    return aiReply;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[WARNING] Model {0} failed ({1}). Trying fallback...", model, ex.Message));
                    // If rate limited or error, loop continues to the next candidate model
                }
            }
            
            return "The wind howls... I cannot speak right now.";
        }

        static async Task<string> GetLocalResponse(string text, Dictionary<string, object> data)
        {
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Someone";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            string systemPrompt = BuildSystemPrompt(data, text);
            var msgs = UpdateMemory(string.Format("{0}_{1}", name, role), systemPrompt, text);

            var reqBody = new Dictionary<string, object>
            {
                {"model", LocalModelId},
                {"messages", msgs},
                {"max_tokens", 450},
                {"temperature", 0.72}
            };

            try
            {
                Console.WriteLine(string.Format("[LOCAL] Calling local model endpoint: {0} ({1})", LocalApiUrl, LocalModelId));
                string resStr = await SendWebRequest(LocalApiUrl, reqBody);
                Console.WriteLine("[DEBUG] Local Raw Response: " + resStr);
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(resStr);
                
                string rawReply = ExtractResponseText(json);
                string aiReply = SanitizeAiResponse(rawReply, isCompanion);
                
                UpdateMemoryResult(string.Format("{0}_{1}", name, role), text, aiReply);
                return aiReply;
            }
            catch (Exception ex) { Console.WriteLine("[ERROR] Local Model API Call Failed: " + ex.Message); }
            
            return "The local gears grind... I cannot find my voice.";
        }

        static async Task<string> GetPlayer2ApiResponse(string text, Dictionary<string, object> data)
        {
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Lord";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            string systemPrompt = BuildSystemPrompt(data, text, isPlayer2: true);
            string memoryKey = string.Format("{0}_{1}_p2", name, role);
            var msgs = UpdateMemory(memoryKey, systemPrompt, text);

            var reqBody = new Dictionary<string, object>
            {
                {"model", "gpt-oss-120b"},
                {"messages", msgs},
                {"temperature", 0.7},
                {"max_tokens", 150},
                {"stream", false},
                {"client_id", GameClientId},
                {"character", name},
                {"game", "warband"}
            };

            Console.WriteLine("[DEBUG] Sending payload: " + JsonSerializer.Serialize(reqBody));

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");
                
                string chatUrl = _currentBridgeMode == "player2_app" ? string.Format("http://127.0.0.1:{0}/v1/chat/completions", GetPlayer2AppPort()) : P2ChatUrl;
                var request = new HttpRequestMessage(HttpMethod.Post, chatUrl);
                request.Headers.Add("player2-game-key", GameClientId);
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _p2Key);
                
                var res = await _http.SendAsync(request);
                
                if (res.IsSuccessStatusCode)
                {
                    string rawResponse = await res.Content.ReadAsStringAsync();
                    Console.WriteLine("[DEBUG] P2 API Raw Response: " + rawResponse);

                    string textResponse = "";

                    if (!string.IsNullOrWhiteSpace(rawResponse))
                    {
                        // Check if payload is SSE formatted
                        if (rawResponse.Contains("data: "))
                        {
                            using (var reader = new StringReader(rawResponse))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (string.IsNullOrWhiteSpace(line)) continue;
                                    line = line.Trim();
                                    if (!line.StartsWith("data: ")) continue;
                                    string dataStr = line.Substring(6).Trim();
                                    if (dataStr == "[DONE]") break;

                                    try
                                    {
                                        var chunk = JsonSerializer.Deserialize<Dictionary<string, object>>(dataStr);
                                        if (chunk != null && chunk.ContainsKey("choices"))
                                        {
                                            Dictionary<string, object> firstChoice = null;
                                            if (chunk["choices"] is object[] arr && arr.Length > 0) firstChoice = arr[0] as Dictionary<string, object>;
                                            else if (chunk["choices"] is System.Collections.ArrayList al && al.Count > 0) firstChoice = al[0] as Dictionary<string, object>;

                                            if (firstChoice != null)
                                            {
                                                if (firstChoice.ContainsKey("delta"))
                                                {
                                                    var delta = firstChoice["delta"] as Dictionary<string, object>;
                                                    if (delta != null)
                                                    {
                                                        if (delta.ContainsKey("content") && delta["content"] != null)
                                                            textResponse += delta["content"].ToString();
                                                        else if (delta.ContainsKey("text") && delta["text"] != null)
                                                            textResponse += delta["text"].ToString();
                                                    }
                                                }
                                                else if (firstChoice.ContainsKey("text") && firstChoice["text"] != null)
                                                {
                                                    textResponse += firstChoice["text"].ToString();
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            // Standard complete JSON payload (stream: false)
                            try
                            {
                                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(rawResponse);
                                if (json != null)
                                {
                                    if (json.ContainsKey("choices"))
                                    {
                                        Dictionary<string, object> firstChoice = null;
                                        if (json["choices"] is object[] arr && arr.Length > 0) firstChoice = arr[0] as Dictionary<string, object>;
                                        else if (json["choices"] is System.Collections.ArrayList al && al.Count > 0) firstChoice = al[0] as Dictionary<string, object>;

                                        if (firstChoice != null)
                                        {
                                            if (firstChoice.ContainsKey("message"))
                                            {
                                                var msgObj = firstChoice["message"] as Dictionary<string, object>;
                                                if (msgObj != null)
                                                {
                                                    if (msgObj.ContainsKey("content") && msgObj["content"] != null)
                                                        textResponse = msgObj["content"].ToString();
                                                    else if (msgObj.ContainsKey("text") && msgObj["text"] != null)
                                                        textResponse = msgObj["text"].ToString();
                                                }
                                            }
                                            if (string.IsNullOrWhiteSpace(textResponse) && firstChoice.ContainsKey("text") && firstChoice["text"] != null)
                                            {
                                                textResponse = firstChoice["text"].ToString();
                                            }
                                            if (string.IsNullOrWhiteSpace(textResponse) && firstChoice.ContainsKey("response") && firstChoice["response"] != null)
                                            {
                                                textResponse = firstChoice["response"].ToString();
                                            }
                                        }
                                    }

                                    if (string.IsNullOrWhiteSpace(textResponse) && json.ContainsKey("text") && json["text"] != null)
                                        textResponse = json["text"].ToString();
                                    if (string.IsNullOrWhiteSpace(textResponse) && json.ContainsKey("response") && json["response"] != null)
                                        textResponse = json["response"].ToString();
                                    if (string.IsNullOrWhiteSpace(textResponse) && json.ContainsKey("content") && json["content"] != null)
                                        textResponse = json["content"].ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("[ERROR] JSON deserialization failed: " + ex.Message);
                            }
                        }
                    }

                    textResponse = SanitizeAiResponse(textResponse, isCompanion);
                    
                    Console.WriteLine("[DEBUG] P2 API Assembled Response: " + textResponse);
                    
                    UpdateMemoryResult(memoryKey, text, textResponse);
                    return textResponse;
                }
                else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ClearSavedKey();
                    return "My tongue is bound by dark magic... (Auth Error)";
                }
                else if ((int)res.StatusCode == 429)
                    return "My mind is clouded with exhaustion... (Rate Limit)";
                else
                    return string.Format("I have no words for you. ({0})", (int)res.StatusCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] API Request Failed: " + ex.Message);
                return "The winds are too loud for us to speak.";
            }
        }

        static string GetPlayer2HotseatResponse(string text, Dictionary<string, object> data)
        {
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
            else if (actionChoice == "2")
            {
                Console.Write(" Enter destination town/castle (e.g. Sargoth, Sungetche): ");
                string dest = Console.ReadLine();
                if (dest != null) dest = dest.Trim().ToLower();
                else dest = "";
                
                if (_townsMap.ContainsKey(dest))
                {
                    npcSpeech += string.Format(" [MOVE_{0}]", dest.ToUpper().Replace(" ", "_"));
                    Console.WriteLine(string.Format(" > Appending move target tag [MOVE_{0}]", dest.ToUpper()));
                }
                else
                {
                    string found = _townsMap.Keys.FirstOrDefault(k => k.Contains(dest));
                    if (found != null)
                    {
                        npcSpeech += string.Format(" [MOVE_{0}]", found.ToUpper().Replace(" ", "_"));
                        Console.WriteLine(string.Format(" > Match found! Sending to {0}", found.ToUpper()));
                    }
                    else Console.WriteLine(" [ERROR] Unrecognized town name. Destination ignored.");
                }
            }
            else if (actionChoice == "3") Console.WriteLine(" > Companion searching action triggered.");

            Console.WriteLine(new string('=', 60) + "\n Processing response and writing back to game...");
            return npcSpeech;
        }
    }
}