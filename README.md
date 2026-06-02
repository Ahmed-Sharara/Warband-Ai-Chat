# Calradia Unified C# AI Bridge (v2.0.0)

## Description
This project connects Mount & Blade: Warband to AI language models. Instead of reading the exact same dialogue lines over and over, you can actually type to NPCs and get dynamic responses based on their character, faction, and role. 

This isn't just text, either. The mod hooks into the game's actual mechanics. For example, if you threaten a Village Elder, the AI detects the threat and triggers the game's village raid screen. If you order your companion to go to a town, purchase an item (like swords, shields, or specific foods like smoked fish or chicken), and return, the AI companion dynamically schedules and executes this multi-step mission!

We have completely retired the old, error-prone Python bridge system. The project now runs on a modern, ultra-reliable **C# (.NET Core) Windows/Console App Bridge** with custom built-in GUI configuration.

---

## Key Features & Solved Problems in v2.0.0

- **Dynamic GUI Configuration Form**: No more editing text files to paste API keys or change folder paths. On startup, the C# Bridge opens a polished settings window to configure your paths and model keys.
- **Improved Item Mapping & Questing**: Fixed an issue where companions would travel to towns but fail to buy/give requested items (such as chickens, smoked fish, etc.). The C# bridge now dynamically parses the mod's `module_items.py` at runtime to match internal item indices. It also uses a robust, built-in fallback mapping for 25+ classic items:
  - **Weapons & Gear**: Swords, Bows, Shields, Arrows, Spears, Axes, Maces, Armor, Horses.
  - **Sustenance & Trade**: Smoked Fish, Chickens, Beef, Bread, Pork, Butter, Cheese, Honey, Ale, Wine, Grain, Grapes, Olives, Cabbages.
- **Unified Location Coordinates Engine**: Translates town, village, and castle names directly to their in-game destination ID indices, ensuring your companion actually travels to the correct target settlement.
- **Intelligent Multi-Step Quests**: Companions can handle complex, multi-segmented commands (e.g. *"Ride to Praven, bring me a sword, and meet me back here"*). The bridge compiles these commands into unified AI Action sequences: `[TASKS: Move|TownName, Fetch|ItemName, Return]` along with a programmatic `[MOVE_TownName]` tag.
- **Clean Dialogue Generation**: Prevents the AI from repeating internal prompts, uttering meta-instructions, or generating run-together words.
- **Zero Python Hassles**: Removed the need for installing Python, Pip extensions, or debugging Windows environment paths. Runs as a standalone compiled application.

---

## Step-by-Step Installation & Setup

### 1. Requirements
- Mount & Blade: Warband (WSE2 is highly recommended for optimal stability).
- .NET Desktop Runtime 6.0+ (installed by default on most modern Windows PCs).

### 2. Run the C# AI Bridge
1. Simply run the pre-compiled executable (`Ai_bridge_system-4.7.exe`) provided in your release folder. There is no need to compile or set up any code!
2. Upon launch, a **Settings GUI Window** will greet you. Configure the following properties safely within the user-friendly editor:
   - **WatchDir**: Provide the directory path to your active Mod folder (the folder containing your WSE2 structure where `To AI Chat.json` and `From AI Chat.json` are generated). By default, this points to your system documents folder.
   - **DefaultMode**: Choose one of your preferred AI backends:
     - `cloud`: Standard cloud mode using fast, robust OpenRouter models.
     - `local`: Pure offline local mode (such as LM Studio or Ollama).
     - `player2_api` / `player2_app`: Advanced multi-player/companion interactive platforms.
   - **OpenRouterApiKey**: Enter your OpenRouter key (e.g. `sk-or-v1-...`) for cloud-based play.
   - **CloudModelId**: Enter your preferred model (e.g., `google/gemini-2.0-flash-exp:free` or `openai/gpt-4o-mini`).
   - **LocalApiUrl**: If running locally on LM Studio, point this to `http://localhost:1234/v1/chat/completions`.
   - **LocalModelId**: Match the model name loaded inside your local LM Studio instance.
3. Click the **Save Settings & Start Server** button. 
4. The application window will close and launch a real-time console. The File Watcher is now active and actively monitoring the Calradian world!

### 3. Compiling the Mod Files
If you are integrating this mod system into your Mount & Blade directory:
1. Copy the files inside the `module files mod` directory to your Warband Module System repository.
2. Run your local `build_module.bat` to compile the script edits into `.txt` resources readable by the game engine.
3. Launch Mount & Blade: Warband, locate an NPC (such as your Companion or a Village Elder), and select **"Chat with AI"** to start roleplaying!

---

## How It Works Behind the Scenes

```
 [Player types message in-game] 
              │
              ▼
[Warband writes "To AI Chat.json" to WatchDir]
              │
              ▼
[C# Bridge file-watcher captures event] ───► [Parses context: location, relations, roles, items]
                                                        │
                                                        ▼
                                       [Translates inputs to specific game action tags]
                                                        │
                                                        ▼
[Warband reads "From AI Chat.json" response] ◄─── [Writes response with action schemas]
              │
              ▼
 [Action executes in-game (Raid or companion movement)]
```

---

## Shout Outs
Special thanks to the TaleWorlds team for Mount & Blade, the creators of Warband Script Enhancer 2, and the open-source community powering modular gaming integrations!
