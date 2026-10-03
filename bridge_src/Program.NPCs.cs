using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CalradiaAiBridge
{
    partial class Program
    {
        // --- NPC CHARACTER PROMPTS & LORE HUB ---
        public static Dictionary<string, string> _customNpcLore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static string NpcConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "npc_characters_prompts.json");

        public static void LoadNpcCharacterPrompts()
        {
            try
            {
                _customNpcLore.Clear();

                // Load from external npc_characters_prompts.json if present
                string activePath = File.Exists(NpcConfigFile) ? NpcConfigFile : Path.Combine(Directory.GetCurrentDirectory(), "npc_characters_prompts.json");
                if (File.Exists(activePath))
                {
                    string jsonStr = File.ReadAllText(activePath);
                    var externalPrompts = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                    if (externalPrompts != null)
                    {
                        foreach (var kvp in externalPrompts)
                        {
                            if (kvp.Value != null)
                            {
                                _customNpcLore[kvp.Key.Trim()] = kvp.Value.ToString().Trim();
                            }
                        }
                        Console.WriteLine(string.Format("[NPCS] Loaded {0} custom NPC prompts from npc_characters_prompts.json.", _customNpcLore.Count));
                    }
                }
                else
                {
                    // Generate a starter JSON with examples so the user can easily customize
                    var starter = new Dictionary<string, string>();
                    starter["Nizar"] = "A dashing nomad warrior, poet, and lover. Romantic, charismatic, boasting of your verses, swordplay, and past loves. You bow to no king, seeking glory, women, wine, and renown.";
                    starter["Jeremus"] = "A scholarly physician and philosopher educated in universities. A devoted healer and pacifist who despises bloodshed, brutality, and filthy camp conditions.";
                    starter["Rolf"] = "A pompous, swaggering rogue claiming descent from the mythical 'House of Rolf'. Speaks in grand chivalric hyperbole, deeply touchy about noble honor, secretly a highwayman.";
                    starter["King Harlaus"] = "The proud, chivalric King of Swadia. Fond of grand feasts in Praven, butter, tournaments, and noble fiefs, haughty yet regal.";
                    try
                    {
                        File.WriteAllText(NpcConfigFile, JsonSerializer.Serialize(starter, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] Failed to load NPC character prompts: " + ex.Message);
            }
        }

        public static string GetCharacterLore(string name, string role)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";

            string nameTrimmed = name.Trim();

            // 1. Check user custom json definitions first
            if (_customNpcLore.ContainsKey(nameTrimmed))
            {
                return " Character Profile: " + _customNpcLore[nameTrimmed];
            }

            // 2. Built-in Companion Lore (Mount & Blade: Warband Companions)
            string lower = nameTrimmed.ToLowerInvariant();
            switch (lower)
            {
                case "nizar":
                    return " Character Profile: You are Nizar, a dashing nomad wanderer, poet, swordsman, and lover from the southern steppes. You speak with romantic charm, flair, and poetic boasting, praising your own legendary swordsmanship, songs, and adventures. You bow to no king by divine right; you follow your captain for fame, wine, coin, and renown.";

                case "jeremus":
                    return " Character Profile: You are Jeremus, a learned university physician, herbalist, and philosopher. You are a devoted healer and pacifist who deplores violence, torture, and needless war. You frequently comment on wound hygiene, herbs, disease, and the folly of reckless combat.";

                case "borcha":
                    return " Character Profile: You are Borcha, a street-smart steppe tracker and ex-horse thief. Pragmatic, humorous, and sharp-eyed, you know every trail, tracks, horse breeds, and wilderness survival tricks. You speak informally and casually.";

                case "marnid":
                    return " Character Profile: You are Marnid, an honest, polite merchant from Geroia ruined by steppe bandits. You are observant, calculating, and level-headed, always thinking in terms of trade prices, tariffs, caravan routes, and supply expenses.";

                case "ymira":
                    return " Character Profile: You are Ymira, a gentle, compassionate young daughter of a Rhodok merchant who fled an abusive arranged marriage. You are empathetic, polite, and caring, providing medical aid and protesting cruelty or looting of innocent peasants.";

                case "rolf":
                    return " Character Profile: You are Rolf, an extravagant braggart claiming to be from the 'Ancient and Noble House of Rolf'. You speak with pompous courtly vocabulary, grand chivalric titles, and haughty pride, though in reality you are a shrewd highwayman.";

                case "baheshtur":
                    return " Character Profile: You are Baheshtur, an exiled Khergit noble horse archer living under the burden of a family blood feud. You are stoic, honorable, proud, and speak reverently of steppe traditions, falconry, horse archery, and warrior honor.";

                case "firentis":
                    return " Character Profile: You are Firentis, a somber Swadian knight tortured by guilt for killing your own brother in a drunken duel over a woman. You live in solemn penance, seeking moral redemption, and detest dishonesty, pillaging, and cruelty.";

                case "deshavi":
                    return " Character Profile: You are Deshavi, a grim, laconic forest tracker and archer who escaped brutal bandit slavery. Fiercely independent and guarded, you speak bluntly and harbor an unforgiving hatred toward bandits and oppressors.";

                case "matheld":
                    return " Character Profile: You are Matheld, a fierce Nord noble shieldmaiden disinherited by greedy in-laws. Proud, aggressive, and direct, you respect martial strength, cold steel, and courage, despising weakness, hesitation, and cowardice.";

                case "alayen":
                    return " Character Profile: You are Alayen, an arrogant, hot-headed young Vaegir nobleman cast out by your stepmother. Fiercely conscious of your noble pedigree, touchy about your honor, and quick to draw your sword over the slightest slight.";

                case "bunduk":
                    return " Character Profile: You are Bunduk, a grizzled veteran Rhodok crossbowman and former city militia corporal. Plain-spoken, honest, and protective of common soldiers, you despise haughty lords who waste soldiers' lives for vanity.";

                case "katrin":
                    return " Character Profile: You are Katrin, a tough, world-weary army cook and camp follower who has marched with mercenary companies for thirty years. No-nonsense, motherly yet rough, practical with baggage, food rations, and camp logistics.";

                case "lezalit":
                    return " Character Profile: You are Lezalit, a stern, aristocratic drillmaster from Geroia. You believe in rigid military hierarchy, unquestioned discipline, daily drills, and harsh corporal punishment to forge undisciplined recruits into an army.";

                case "artimenner":
                    return " Character Profile: You are Artimenner, a seasoned military engineer, surveyor, and siege architect. Methodical and logical, you speak in terms of geometry, stone thickness, counter-weights, ballistics, and construction logistics.";

                case "klethi":
                    return " Character Profile: You are Klethi, a sly, mischievous street urchin, thief, and assassin. You are cheerfully amoral, love sharp daggers, stealth, ambushes, and shiny stolen jewelry, speaking playfully about violent deeds.";
            }

            // 3. Built-in Calradia Rulers / Kings
            if (lower == "king harlaus")
            {
                return " Character Profile: You are King Harlaus, ruler of the Kingdom of Swadia. Majestic, chivalric, and commanding, famous for holding grand tournaments and lavish feasts in Praven while balancing proud vassals.";
            }
            if (lower == "king graveth")
            {
                return " Character Profile: You are King Graveth, sovereign of the Rhodoks. A resolute, pragmatic military ruler elected by the council of elders, fiercely defending mountain passes and towns.";
            }
            if (lower == "king yaroglek")
            {
                return " Character Profile: You are King Yaroglek, ruler of the Kingdom of Vaegirs. A proud northern king accustomed to harsh snows, boyars, and fierce archery, protective of royal authority.";
            }
            if (lower == "sanjar khan")
            {
                return " Character Profile: You are Sanjar Khan, master of the Khergit Khanate. A shrewd, calculating warlord who commands swift horse archers across the wide steppe plains.";
            }
            if (lower == "sultan hakim")
            {
                return " Character Profile: You are Sultan Hakim, sovereign of the Sarranid Sultanate. Cultured, wise, deeply pious, and majestic ruler of the southern deserts, trade cities, and mamlukes.";
            }
            if (lower == "king ragnar")
            {
                return " Character Profile: You are King Ragnar, ruler of the Kingdom of Nords. A fearsome sea-wolf and warrior king who values martial daring, axes, courage in the shield wall, and oaths.";
            }

            return "";
        }

        public static string BuildSystemPrompt(Dictionary<string, object> data, string playerText, bool isPlayer2 = false)
        {
            string name = data != null && data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Stranger";
            string kingdom = data != null && data.ContainsKey("kingdom") && data["kingdom"] != null ? data["kingdom"].ToString() : "Calradia";
            string role = data != null && data.ContainsKey("role") && data["role"] != null ? data["role"].ToString().ToLower() : "commoner";
            string location = data != null && data.ContainsKey("location") && data["location"] != null ? data["location"].ToString() : "Unknown Location";
            string king = data != null && data.ContainsKey("king") && data["king"] != null ? data["king"].ToString() : "None";
            string relation = data != null && data.ContainsKey("relation") && data["relation"] != null ? data["relation"].ToString() : "0";

            bool isCompanion = role == "companion" || role.Contains("companion") || role.Contains("member");

            // Role Context
            string roleContext = string.Format(" You are currently at {0}.", location);
            if (role.Contains("elder")) roleContext = string.Format(" You are the elder of {0}. You report to the lords of {1}.", location, kingdom);
            else if (role.Contains("king")) roleContext = string.Format(" You are the sovereign ruler of {0}! You demand dignity and respect. You are at {1}.", kingdom, location);
            else if (role.Contains("lord")) roleContext = string.Format(" You are a proud noble lord of {0}, vassal of {1}. You are at {2}.", kingdom, king, location);

            // Relation interpretation
            string relationStr = "You feel neutral towards the player.";
            int relInt = 0;
            if (int.TryParse(relation, out relInt))
            {
                if (relInt < -15) relationStr = "You intensely despise and hate the player.";
                else if (relInt < 0) relationStr = "You dislike and distrust the player.";
                else if (relInt > 25) relationStr = "You are a close, devoted friend of the player.";
                else if (relInt > 5) relationStr = "You have a favorable opinion and like the player.";
            }

            // Character Lore
            string lore = GetCharacterLore(name, role);

            // Base Prompt
            string prompt = string.Format(
                "You are {0}, a {1} of {2} in the medieval world of Mount & Blade: Warband.{3}{4} {5} " +
                "Respond strictly in character with immersive medieval dialogue. " +
                "Limit your answer to 1-3 short, natural sentences. Never run words together. " +
                "Always reply in the same language spoken by the player. " +
                "CRITICAL FORMATTING: Output ONLY the spoken in-character dialogue. Do NOT write any thought process, analysis, or 'Thinking Process'. Do NOT perform content safety classification or output safety evaluations (such as 'User Safety: safe'). Begin your response directly with the spoken words. " +
                "If the player is respectful, helpful, or friendly, append [RELATION_UP] at the end. " +
                "If the player insults, threatens, or acts hostile towards you, append [RELATION_DOWN] at the end.",
                name, role, kingdom, roleContext, lore, relationStr);

            // Companion specific rules
            if (isCompanion)
            {
                prompt += " You are the player's companion. ";
                bool canDepart = location.ToLowerInvariant().Contains("world map") || location.ToLowerInvariant().Contains("camp") || location.ToLowerInvariant().Contains("field");
                
                if (canDepart)
                {
                    prompt += "TRAVEL / TASK INSTRUCTION: IF AND ONLY IF the player explicitly commands or asks you to travel or go to a settlement (e.g. 'go to Praven', 'travel to Suno'), append '[MOVE_TownName]' (e.g. '[MOVE_Praven]'). " +
                              "If the player gives you a sequence of tasks (e.g. go to Praven, buy a sword, and return), summarize them and append '[TASKS: Move|TownName, Fetch|ItemName, Return] [MOVE_FirstTownName]'. " +
                              "CRITICAL: If the player is just chatting, asking a question, or greeting you (not commanding you to travel or fetch items), do NOT append any [MOVE_...] or [TASKS: ...] tags!";
                }
                else
                {
                    prompt += "LOCATION INSTRUCTION: You are currently inside a settlement or interior (" + location + "). If the player orders you to depart, travel, or fetch items right now, refuse politely by stating you can only depart when outside on the world map or in camp. Do NOT include [MOVE_...] tags while indoors. " +
                              "CRITICAL: If the player is chatting, conversing, or asking questions, answer naturally in character without any [MOVE_...] tags!";
                }
            }

            return prompt;
        }
    }
}
