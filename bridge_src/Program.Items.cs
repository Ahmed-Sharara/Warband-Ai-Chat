using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CalradiaAiBridge
{
    partial class Program
    {
        // --- ITEM DATA HUB ---
        public static Dictionary<string, int> _itemsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        static string ItemsConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "items.json");

        public static void LoadItemsMap()
        {
            try
            {
                _itemsMap.Clear();

                // 1. Built-in base items mapped to standard Warband module indices
                PopulateDefaultItems();

                // 2. Load from external items.json if present (allows modders/users to edit/add without compiling)
                string activePath = File.Exists(ItemsConfigFile) ? ItemsConfigFile : Path.Combine(Directory.GetCurrentDirectory(), "items.json");
                if (File.Exists(activePath))
                {
                    string jsonStr = File.ReadAllText(activePath);
                    var externalItems = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                    if (externalItems != null)
                    {
                        int loadedCustom = 0;
                        foreach (var kvp in externalItems)
                        {
                            if (kvp.Value != null && int.TryParse(kvp.Value.ToString(), out int id))
                            {
                                _itemsMap[kvp.Key.Trim()] = id;
                                loadedCustom++;
                            }
                        }
                        Console.WriteLine(string.Format("[ITEMS] Loaded {0} custom item mappings from items.json.", loadedCustom));
                    }
                }
                else
                {
                    // Create a starter items.json file for the user to edit
                    try
                    {
                        File.WriteAllText(ItemsConfigFile, JsonSerializer.Serialize(_itemsMap, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch { }
                }

                Console.WriteLine(string.Format("[ITEMS] Total active item mappings: {0}", _itemsMap.Count));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] Failed to load item mappings: " + ex.Message);
            }
        }

        static void PopulateDefaultItems()
        {
            // Weapons
            _itemsMap["spear"] = 1;
            _itemsMap["tutorial_spear"] = 1;
            _itemsMap["mace"] = 2;
            _itemsMap["tutorial_club"] = 2;
            _itemsMap["club"] = 2;
            _itemsMap["arrows"] = 4;
            _itemsMap["tutorial_arrows"] = 4;
            _itemsMap["bow"] = 6;
            _itemsMap["short_bow"] = 6;
            _itemsMap["short bow"] = 6;
            _itemsMap["hunting bow"] = 7;
            _itemsMap["war bow"] = 8;
            _itemsMap["horse"] = 9;
            _itemsMap["saddle horse"] = 9;
            _itemsMap["saddle_horse"] = 9;
            _itemsMap["steppe horse"] = 10;
            _itemsMap["hunter"] = 11;
            _itemsMap["warhorse"] = 12;
            _itemsMap["shield"] = 10;
            _itemsMap["round shield"] = 10;
            _itemsMap["sword"] = 13;
            _itemsMap["best sword"] = 13;
            _itemsMap["arming sword"] = 13;
            _itemsMap["broadsword"] = 13;
            _itemsMap["bastard sword"] = 13;
            _itemsMap["great sword"] = 13;
            _itemsMap["scimitar"] = 13;
            _itemsMap["axe"] = 14;
            _itemsMap["battle axe"] = 14;
            _itemsMap["great axe"] = 14;
            _itemsMap["dagger"] = 15;
            _itemsMap["throwing daggers"] = 16;
            _itemsMap["throwing axes"] = 17;
            _itemsMap["javelins"] = 18;
            _itemsMap["crossbow"] = 19;
            _itemsMap["heavy crossbow"] = 20;
            _itemsMap["bolts"] = 21;

            // Food & Trade Goods
            _itemsMap["wine"] = 109;
            _itemsMap["ale"] = 110;
            _itemsMap["food"] = 111;
            _itemsMap["smoked_fish"] = 111;
            _itemsMap["smoked fish"] = 111;
            _itemsMap["fish"] = 111;
            _itemsMap["cheese"] = 112;
            _itemsMap["honey"] = 113;
            _itemsMap["sausages"] = 114;
            _itemsMap["sausage"] = 114;
            _itemsMap["cabbages"] = 115;
            _itemsMap["cabbage"] = 115;
            _itemsMap["dried_meat"] = 116;
            _itemsMap["dried meat"] = 116;
            _itemsMap["meat"] = 116;
            _itemsMap["apples"] = 117;
            _itemsMap["apple"] = 117;
            _itemsMap["fruit"] = 117;
            _itemsMap["grapes"] = 118;
            _itemsMap["olives"] = 119;
            _itemsMap["grain"] = 120;
            _itemsMap["wheat"] = 120;
            _itemsMap["beef"] = 121;
            _itemsMap["bread"] = 122;
            _itemsMap["chicken"] = 123;
            _itemsMap["chickens"] = 123;
            _itemsMap["pork"] = 124;
            _itemsMap["butter"] = 125;
            _itemsMap["oil"] = 126;
            _itemsMap["salt"] = 127;
            _itemsMap["pottery"] = 128;
            _itemsMap["linen"] = 129;
            _itemsMap["wool"] = 130;
            _itemsMap["velvet"] = 131;
            _itemsMap["iron"] = 132;
            _itemsMap["tools"] = 133;
            _itemsMap["spices"] = 134;
            _itemsMap["spice"] = 134;

            // Armor & Apparel
            _itemsMap["armor"] = 130;
            _itemsMap["leather armor"] = 135;
            _itemsMap["padded armor"] = 136;
            _itemsMap["mail armor"] = 137;
            _itemsMap["hauberk"] = 138;
            _itemsMap["plate armor"] = 139;
            _itemsMap["helmet"] = 140;
            _itemsMap["boots"] = 141;
            _itemsMap["gloves"] = 142;
        }
    }
}
