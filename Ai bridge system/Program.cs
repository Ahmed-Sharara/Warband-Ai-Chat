using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System;

namespace CalradiaAiBridge {
    partial class Program {
        // --- CONFIGURATION HUB ---
        static string DefaultMode = "";
        static string OpenRouterApiKey = "";
        static string CloudModelId = "";
        static string LocalApiUrl = "";
        static string LocalModelId = "";
        static string Player2ApiKey = "";

        // API endpoints
        static string P2ApiBase = "https://api.player2.game/v1";
        static string P2ChatUrl = "https://api.player2.game/v1/chat/completions";
        static string P2HealthUrl = "https://api.player2.game/v1/health";
        static string P2AppLogin = "http://localhost:4315/v1/login/web/019e3c62-2a9e-7de3-a7ea-9222669593f4";
        static string P2DeviceNew = "https://api.player2.game/v1/login/device/new";
        static string P2DeviceToken = "https://api.player2.game/v1/login/device/token";
        static string GameClientId = "019e3c62-2a9e-7de3-a7ea-9222669593f4";
        static string KeyFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".p2key");
        static string ConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        static string _p2Key = "";

        static string WatchDir = "";
        static string InputFile = "";
        static string OutputFile = "";

        static double Cooldown = 0.5;
        static HttpClient _http = new HttpClient();
        static JavaScriptSerializer _json = new JavaScriptSerializer();
        static string _lastMsgHash = "";
        static DateTime _lastProcessedTime = DateTime.MinValue;
        static Dictionary<string, List<Dictionary<string, string>>> _memoryDb = new Dictionary<string, List<Dictionary<string, string>>>();
        static string _currentBridgeMode;

        static Dictionary<string, int> _townsMap = new Dictionary<string, int>()
        {
            {"sargoth", 21}, {"tihr", 22}, {"veluca", 23}, {"suno", 24}, {"jelkala", 25},
            {"praven", 26}, {"praven tavern", 26}, {"praven's tavern", 26}, {"tavern in praven", 26}, {"tavern of praven", 26}, {"uxkhal", 27}, {"reyvadin", 28}, {"khudan", 29}, {"tulga", 30},
            {"curaw", 31}, {"wercheg", 32}, {"rivacheg", 33}, {"halmar", 34}, {"yalen", 35},
            {"dhirim", 36}, {"ichamur", 37}, {"narra", 38}, {"shariz", 39}, {"durquba", 40},
            {"ahmerrad", 41}, {"bariyye", 42}, {"culmarr castle", 43}, {"culmarr", 43},
            {"malayurg castle", 44}, {"malayurg", 44}, {"bulugha castle", 45}, {"bulugha", 45},
            {"radoghir castle", 46}, {"radoghir", 46}, {"tehlrog castle", 47}, {"tehlrog", 47},
            {"tilbaut castle", 48}, {"tilbaut", 48}, {"sungetche castle", 49}, {"sungetche", 49},
            {"jeirbe castle", 50}, {"jeirbe", 50}, {"jamiche castle", 51}, {"jamiche", 180},
            {"alburq castle", 52}, {"alburq", 52}, {"curin castle", 53}, {"curin", 53},
            {"chalbek castle", 54}, {"chalbek", 54}, {"kelredan castle", 55}, {"kelredan", 55},
            {"maras castle", 56}, {"maras", 56}, {"ergellon castle", 57}, {"ergellon", 57},
            {"almerra castle", 58}, {"almerra", 58}, {"distar castle", 59}, {"distar", 59},
            {"ismirala castle", 60}, {"ismirala", 175}, {"yruma castle", 61}, {"yruma", 61},
            {"derchios castle", 62}, {"derchios", 62}, {"ibdeles castle", 63}, {"ibdeles", 158},
            {"unuzdaq castle", 64}, {"unuzdaq", 64}, {"tevarin castle", 65}, {"tevarin", 65},
            {"reindi castle", 66}, {"reindi", 66}, {"ryibelet castle", 67}, {"ryibelet", 105},
            {"senuzgda castle", 68}, {"senuzgda", 68}, {"rindyar castle", 69}, {"rindyar", 69},
            {"grunwalder castle", 70}, {"grunwalder", 70}, {"nelag castle", 71}, {"nelag", 71},
            {"asugan castle", 72}, {"asugan", 72}, {"vyincourd castle", 73}, {"vyincourd", 73},
            {"knudarr castle", 74}, {"knudarr", 74}, {"etrosq castle", 75}, {"etrosq", 75},
            {"hrus castle", 76}, {"hrus", 76}, {"haringoth castle", 77}, {"haringoth", 77},
            {"jelbegi castle", 78}, {"jelbegi", 141}, {"dramug castle", 79}, {"dramug", 79},
            {"tulbuk castle", 80}, {"tulbuk", 178}, {"slezkh castle", 81}, {"slezkh", 176},
            {"uhhun castle", 82}, {"uhhun", 179}, {"jameyyed castle", 83}, {"jameyyed", 83},
            {"teramma castle", 84}, {"teramma", 84}, {"sharwa castle", 85}, {"sharwa", 85},
            {"durrin castle", 86}, {"durrin", 86}, {"caraf castle", 87}, {"caraf", 87},
            {"weyyah castle", 88}, {"weyyah", 88}, {"samarra castle", 89}, {"samarra", 89},
            {"bardaq castle", 90}, {"bardaq", 90}, {"yaragar", 91}, {"burglen", 92},
            {"azgad", 93}, {"nomar", 94}, {"kulum", 95}, {"emirin", 96}, {"amere", 97},
            {"haen", 98}, {"buvran", 99}, {"mechin", 100}, {"dusturil", 101}, {"emer", 102},
            {"nemeja", 103}, {"sumbuja", 104}, {"shapeshte", 106}, {"mazen", 107},
            {"ulburban", 108}, {"hanun", 109}, {"uslum", 110}, {"bazeck", 111}, {"shulus", 112},
            {"ilvia", 113}, {"ruldi", 114}, {"dashbigha", 115}, {"pagundur", 116},
            {"glunmar", 117}, {"tash kulun", 118}, {"buillin", 119}, {"ruvar", 120},
            {"ambean", 121}, {"tosdhar", 122}, {"ruluns", 123}, {"ehlerdah", 124},
            {"fearichen", 125}, {"jayek", 126}, {"ada kulun", 127}, {"ibiran", 128},
            {"reveran", 129}, {"saren", 130}, {"dugan", 131}, {"dirigh aban", 132},
            {"zagush", 133}, {"peshmi", 134}, {"bulugur", 135}, {"fedner", 136},
            {"epeshe", 137}, {"veidar", 138}, {"tismirr", 139}, {"karindi", 140},
            {"amashke", 142}, {"balanli", 143}, {"chide", 144}, {"tadsamesh", 145},
            {"fenada", 146}, {"ushkuru", 147}, {"vezin", 148}, {"dumar", 149},
            {"tahlberl", 150}, {"aldelen", 151}, {"rebache", 152}, {"rduna", 153},
            {"serindiar", 154}, {"iyindah", 155}, {"fisdnar", 156}, {"tebandra", 157},
            {"kwynn", 159}, {"dirigsene", 160}, {"tshibtin", 161}, {"elberl", 162},
            {"chaeza", 163}, {"ayyike", 164}, {"bhulaban", 165}, {"kedelke", 166},
            {"rizi", 167}, {"sarimish", 168}, {"istiniar", 169}, {"vayejeg", 170},
            {"odasan", 171}, {"yalibe", 172}, {"gisim", 173}, {"chelez", 174},
            {"udiniad", 177}, {"ayn assuadi", 181}, {"dhibbain", 182}, {"qalyut", 183},
            {"mazigh", 184}, {"tamnuh", 185}, {"habba", 186}, {"sekhtem", 187},
            {"mawiti", 188}, {"fishara", 189}, {"iqbayl", 190}, {"uzgha", 191},
            {"shibal zumr", 192}, {"mijayet", 193}, {"tazjunat", 194}, {"aab", 195},
            {"hawaha", 196}, {"unriya", 197}, {"mit nun", 198}, {"tilimsal", 199},
            {"rushdigh", 200}
        };


        static Dictionary<string, int> _itemsMap = new Dictionary<string, int>();

        [STAThread]
        static void Main( string[] args ) {
            try {
                // Attempt to parse items map
                LoadItemsMap();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppConfig config = new AppConfig();
                bool shouldStart = false;

                if (File.Exists(ConfigFile)) {
                    try {
                        var jsonStr = File.ReadAllText(ConfigFile);
                        var parsed = _json.Deserialize<AppConfig>(jsonStr);
                        if (parsed != null) config = parsed;
                    } catch { }
                }

                using (var form = new Form()) {
                    form.Text = "Calradia AI Bridge Settings";
                    form.Size = new Size(500, 600);
                    form.StartPosition = FormStartPosition.CenterScreen;

                    var grid = new PropertyGrid();
                    grid.SelectedObject = config;
                    grid.Dock = DockStyle.Fill;
                    grid.ToolbarVisible = false;

                    var pnl = new Panel() { Dock = DockStyle.Bottom, Height = 45 };
                    var btnStart = new Button() { Text = "Save Settings && Start Server", Dock = DockStyle.Fill, BackColor = Color.LightGreen, Font = new Font(form.Font, FontStyle.Bold), FlatStyle = FlatStyle.Flat };

                    btnStart.Click += ( s, e ) => {
                        try { File.WriteAllText(ConfigFile, _json.Serialize(config)); } catch { }
                        shouldStart = true;
                        form.Close();
                    };
                    pnl.Controls.Add(btnStart);

                    form.Controls.Add(grid);
                    form.Controls.Add(pnl);

                    Application.Run(form);
                }

                if (!shouldStart) {
                    Console.WriteLine("Setup closed. Exiting...");
                    return;
                }

                // Apply logic
                DefaultMode = config.DefaultMode.ToString();
                OpenRouterApiKey = config.OpenRouterApiKey;
                CloudModelId = config.CloudModelId;
                LocalApiUrl = config.LocalApiUrl;
                LocalModelId = config.LocalModelId;
                Player2ApiKey = config.Player2ApiKey;
                WatchDir = config.WatchDir;
                InputFile = Path.Combine(WatchDir, "To AI Chat.json");
                OutputFile = Path.Combine(WatchDir, "From AI Chat.json");

                Task.Run(() => MainAsync(args)).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Console.WriteLine("Fatal Error: " + ex.Message);
            }
        }

        static void LoadItemsMap() {
            try {
                // Fallback / common mappings requested by user - aligned with this module's actual item positions
                _itemsMap["sword"] = 13;
                _itemsMap["best sword"] = 13;
                _itemsMap["food"] = 111;   // smoked fish (first food item)
                _itemsMap["bread"] = 122;
                _itemsMap["armor"] = 130;  // arena_armor_white
                _itemsMap["horse"] = 9;    // tutorial_saddle_horse
                _itemsMap["shield"] = 10;   // tutorial_shield
                _itemsMap["bow"] = 6;      // tutorial_short_bow
                _itemsMap["arrows"] = 4;   // tutorial_arrows
                _itemsMap["axe"] = 14;     // tutorial_axe
                _itemsMap["spear"] = 1;    // tutorial_spear
                _itemsMap["mace"] = 2;     // tutorial_club
                _itemsMap["ale"] = 110;    // merchandise ale
                _itemsMap["wine"] = 109;   // merchandise wine
                _itemsMap["smoked_fish"] = 111;
                _itemsMap["smoked fish"] = 111;
                _itemsMap["fish"] = 111;
                _itemsMap["cheese"] = 112;
                _itemsMap["honey"] = 113;
                _itemsMap["sausages"] = 114;
                _itemsMap["cabbages"] = 115;
                _itemsMap["cabbage"] = 115;
                _itemsMap["dried_meat"] = 116;
                _itemsMap["dried meat"] = 116;
                _itemsMap["meat"] = 116;
                _itemsMap["apples"] = 117;
                _itemsMap["fruit"] = 117;
                _itemsMap["grapes"] = 118;
                _itemsMap["olives"] = 119;
                _itemsMap["grain"] = 120;
                _itemsMap["beef"] = 121;
                _itemsMap["chicken"] = 123;
                _itemsMap["chickens"] = 123;
                _itemsMap["pork"] = 124;
                _itemsMap["butter"] = 125;

                // Climb up from execution dir to find "module files mod/module_items.py"
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string pyPath = "";
                while (!string.IsNullOrEmpty(dir)) {
                    string candidate = Path.Combine(dir, "module files mod", "module_items.py");
                    if (File.Exists(candidate)) {
                        pyPath = candidate;
                        break;
                    }
                    candidate = Path.Combine(dir, "..", "module files mod", "module_items.py");
                    if (File.Exists(candidate)) {
                        pyPath = Path.GetFullPath(candidate);
                        break;
                    }
                    string parent = Path.GetDirectoryName(dir);
                    if (parent == dir) break;
                    dir = parent;
                }

                if (string.IsNullOrEmpty(pyPath) || !File.Exists(pyPath)) {
                    Console.WriteLine("[WARNING] Could not find module_items.py path. Using fallback item mappings.");
                    return;
                }

                string[] lines = File.ReadAllLines(pyPath);
                int itemIndex = 0;
                bool readingItems = false;
                foreach (var line in lines) {
                    string l = line.Trim();
                    if (l.StartsWith("items = [")) { readingItems = true; continue; }
                    if (!readingItems) continue;
                    if (l.StartsWith("]") && l.Length <= 2) break; // end of items array

                    if (l.StartsWith("[\"") || l.StartsWith("['")) {
                        var parts = l.Split(',');
                        if (parts.Length >= 2) {
                            string internalName = parts[0].Trim('[', ' ', '\'', '"').ToLower();
                            string displayName = parts[1].Trim(' ', '\'', '"').ToLower();

                            // add to dictionary
                            if (!_itemsMap.ContainsKey(internalName)) _itemsMap[internalName] = itemIndex;
                            if (!string.IsNullOrEmpty(displayName) && !_itemsMap.ContainsKey(displayName)) _itemsMap[displayName] = itemIndex;

                            itemIndex++;
                        }
                    }
                }
                Console.WriteLine($"[INFO] Loaded {_itemsMap.Count} item names from module_items.py at: {pyPath}");
            } catch (Exception ex) {
                Console.WriteLine("[WARNING] Failed to parse module_items.py: " + ex.Message);
            }
        }

        static async Task MainAsync( string[] args ) {
            _currentBridgeMode = DefaultMode;
            if (args.Length > 0) {
                var chosen = args[0].ToLower().Trim();
                if (new[] { "cloud", "local", "player2_api", "player2_hotseat", "player2_app" }.Contains(chosen))
                    _currentBridgeMode = chosen;
            }

            Console.WriteLine("============================================================");
            Console.WriteLine("             ** CALRADIA UNIFIED AI BRIDGE **             ");
            Console.WriteLine("============================================================");

            Console.WriteLine(new string('-', 60));
            Console.WriteLine(string.Format(" RUNNING IN {0} MODE", _currentBridgeMode.ToUpper()));
            Console.WriteLine(" Watched Folder: " + WatchDir);
            Console.WriteLine("============================================================");

            if (_currentBridgeMode == "player2_api" || _currentBridgeMode == "player2_app") {
                await Authenticate();
                StartHealthPing();
            }

            if (File.Exists(InputFile)) File.WriteAllText(InputFile, "{}");
            File.WriteAllText(OutputFile, "{}");

            if (!Directory.Exists(WatchDir)) {
                Console.WriteLine("[ERROR] Watch directory '{0}' does not exist! Please check the path.", WatchDir);
                return;
            }

            using (var watcher = new FileSystemWatcher(WatchDir)) {
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "To AI Chat.json";
                watcher.Changed += OnFileChanged;
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Watcher started. Press Ctrl+C to exit.");

                var tcs = new TaskCompletionSource<bool>();
                Console.CancelKeyPress += ( s, e ) => { e.Cancel = true; tcs.SetResult(true); };
                await tcs.Task;
            }

            Console.WriteLine("\nShutting down AI Bridge watcher... See you in Calradia!");
        }
    }
}
