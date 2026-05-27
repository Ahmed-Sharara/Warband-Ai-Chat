import time
import os
import requests
import json
import threading
import hashlib
import re
import sys
import webbrowser
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# --- CONFIGURATION HUB ---
DEFAULT_MODE = "cloud"  # Can be: "cloud", "local", "player2_api", "player2_hotseat"

# 1. Cloud Config (OpenRouter API)
OPENROUTER_API_KEY = "YOUR-API-KEY"
CLOUD_MODEL_ID = "openai/gpt-oss-120b:free"  # Default high-speed / free model

# 2. Local Config (LM Studio or Ollama running locally)
LOCAL_API_URL = "http://localhost:1234/v1/chat/completions"  # LM Studio: 1234, Ollama: 11434/v1/chat/completions
LOCAL_MODEL_ID = "local-model"  # Leave as default or set to your active Ollama model name

# 3. Player2 App / API Config
PLAYER2_API_KEY = ""
P2_API_BASE = "https://api.player2.game/v1"
P2_CHAT_URL = f"{P2_API_BASE}/chat/completions"
P2_HEALTH_URL = f"{P2_API_BASE}/health"
P2_APP_LOGIN = "http://localhost:4315/v1/login/web/019e3c62-2a9e-7de3-a7ea-9222669593f4"
P2_DEVICE_NEW = f"{P2_API_BASE}/login/device/new"
P2_DEVICE_TOKEN = f"{P2_API_BASE}/login/device/token"
GAME_CLIENT_ID = "019e3c62-2a9e-7de3-a7ea-9222669593f4"
KEY_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".p2key")
p2_key = ""

# 4. Mount & Blade Directory Watcher Config
WATCH_DIR = r"C:\Users\LOQ\Documents\Mount&Blade Warband WSE2\WSE\Native"
INPUT_FILE = os.path.abspath(os.path.join(WATCH_DIR, "To AI Chat.json"))
OUTPUT_FILE = os.path.abspath(os.path.join(WATCH_DIR, "From AI Chat.json"))

COOLDOWN = 0.5
session = requests.Session()
last_msg_hash = ""
last_processed_time = 0
memory_db = {}

towns_map = {
    "sargoth": 21,
    "tihr": 22,
    "veluca": 23,
    "suno": 24,
    "jelkala": 25,
    "praven": 26,
    "uxkhal": 27,
    "reyvadin": 28,
    "khudan": 29,
    "tulga": 30,
    "curaw": 31,
    "wercheg": 32,
    "rivacheg": 33,
    "halmar": 34,
    "yalen": 35,
    "dhirim": 36,
    "ichamur": 37,
    "narra": 38,
    "shariz": 39,
    "durquba": 40,
    "ahmerrad": 41,
    "bariyye": 42,
    "culmarr castle": 43,
    "culmarr": 43,
    "malayurg castle": 44,
    "malayurg": 44,
    "bulugha castle": 45,
    "bulugha": 45,
    "radoghir castle": 46,
    "radoghir": 46,
    "tehlrog castle": 47,
    "tehlrog": 47,
    "tilbaut castle": 48,
    "tilbaut": 48,
    "sungetche castle": 49,
    "sungetche": 49,
    "jeirbe castle": 50,
    "jeirbe": 50,
    "jamiche castle": 51,
    "jamiche": 180,
    "alburq castle": 52,
    "alburq": 52,
    "curin castle": 53,
    "curin": 53,
    "chalbek castle": 54,
    "chalbek": 54,
    "kelredan castle": 55,
    "kelredan": 55,
    "maras castle": 56,
    "maras": 56,
    "ergellon castle": 57,
    "ergellon": 57,
    "almerra castle": 58,
    "almerra": 58,
    "distar castle": 59,
    "distar": 59,
    "ismirala castle": 60,
    "ismirala": 175,
    "yruma castle": 61,
    "yruma": 61,
    "derchios castle": 62,
    "derchios": 62,
    "ibdeles castle": 63,
    "ibdeles": 158,
    "unuzdaq castle": 64,
    "unuzdaq": 64,
    "tevarin castle": 65,
    "tevarin": 65,
    "reindi castle": 66,
    "reindi": 66,
    "ryibelet castle": 67,
    "ryibelet": 105,
    "senuzgda castle": 68,
    "senuzgda": 68,
    "rindyar castle": 69,
    "rindyar": 69,
    "grunwalder castle": 70,
    "grunwalder": 70,
    "nelag castle": 71,
    "nelag": 71,
    "asugan castle": 72,
    "asugan": 72,
    "vyincourd castle": 73,
    "vyincourd": 73,
    "knudarr castle": 74,
    "knudarr": 74,
    "etrosq castle": 75,
    "etrosq": 75,
    "hrus castle": 76,
    "hrus": 76,
    "haringoth castle": 77,
    "haringoth": 77,
    "jelbegi castle": 78,
    "jelbegi": 141,
    "dramug castle": 79,
    "dramug": 79,
    "tulbuk castle": 80,
    "tulbuk": 178,
    "slezkh castle": 81,
    "slezkh": 176,
    "uhhun castle": 82,
    "uhhun": 179,
    "jameyyed castle": 83,
    "jameyyed": 83,
    "teramma castle": 84,
    "teramma": 84,
    "sharwa castle": 85,
    "sharwa": 85,
    "durrin castle": 86,
    "durrin": 86,
    "caraf castle": 87,
    "caraf": 87,
    "weyyah castle": 88,
    "weyyah": 88,
    "samarra castle": 89,
    "samarra": 89,
    "bardaq castle": 90,
    "bardaq": 90,
    "yaragar": 91,
    "burglen": 92,
    "azgad": 93,
    "nomar": 94,
    "kulum": 95,
    "emirin": 96,
    "amere": 97,
    "haen": 98,
    "buvran": 99,
    "mechin": 100,
    "dusturil": 101,
    "emer": 102,
    "nemeja": 103,
    "sumbuja": 104,
    "shapeshte": 106,
    "mazen": 107,
    "ulburban": 108,
    "hanun": 109,
    "uslum": 110,
    "bazeck": 111,
    "shulus": 112,
    "ilvia": 113,
    "ruldi": 114,
    "dashbigha": 115,
    "pagundur": 116,
    "glunmar": 117,
    "tash kulun": 118,
    "buillin": 119,
    "ruvar": 120,
    "ambean": 121,
    "tosdhar": 122,
    "ruluns": 123,
    "ehlerdah": 124,
    "fearichen": 125,
    "jayek": 126,
    "ada kulun": 127,
    "ibiran": 128,
    "reveran": 129,
    "saren": 130,
    "dugan": 131,
    "dirigh aban": 132,
    "zagush": 133,
    "peshmi": 134,
    "bulugur": 135,
    "fedner": 136,
    "epeshe": 137,
    "veidar": 138,
    "tismirr": 139,
    "karindi": 140,
    "amashke": 142,
    "balanli": 143,
    "chide": 144,
    "tadsamesh": 145,
    "fenada": 146,
    "ushkuru": 147,
    "vezin": 148,
    "dumar": 149,
    "tahlberl": 150,
    "aldelen": 151,
    "rebache": 152,
    "rduna": 153,
    "serindiar": 154,
    "iyindah": 155,
    "fisdnar": 156,
    "tebandra": 157,
    "kwynn": 159,
    "dirigsene": 160,
    "tshibtin": 161,
    "elberl": 162,
    "chaeza": 163,
    "ayyike": 164,
    "bhulaban": 165,
    "kedelke": 166,
    "rizi": 167,
    "sarimish": 168,
    "istiniar": 169,
    "vayejeg": 170,
    "odasan": 171,
    "yalibe": 172,
    "gisim": 173,
    "chelez": 174,
    "udiniad": 177,
    "ayn assuadi": 181,
    "dhibbain": 182,
    "qalyut": 183,
    "mazigh": 184,
    "tamnuh": 185,
    "habba": 186,
    "sekhtem": 187,
    "mawiti": 188,
    "fishara": 189,
    "iqbayl": 190,
    "uzgha": 191,
    "shibal zumr": 192,
    "mijayet": 193,
    "tazjunat": 194,
    "aab": 195,
    "hawaha": 196,
    "unriya": 197,
    "mit nun": 198,
    "tilimsal": 199,
    "rushdigh": 200
}

current_bridge_mode = DEFAULT_MODE

def get_cloud_response(text, data):
    global memory_db
    if not OPENROUTER_API_KEY:
        return "I have no voice... (API Key Missing in ai_bridge.py)"

    url = "https://openrouter.ai/api/v1/chat/completions"
    headers = {"Authorization": f"Bearer {OPENROUTER_API_KEY}", "Content-Type": "application/json"}

    role = data.get("role", "commoner").lower()
    npc_name = data.get("name", "Someone")
    is_companion = (role == "companion" or "companion" in role or "member" in role)
    
    system_prompt = f"You are {npc_name}, a {role} in the medieval world of Mount & Blade: Warband. Talk naturally with correct spacing after punctuation - NEVER run words together (like 'doorsteptrade' instead of 'doorstep. Trade')."
    if is_companion:
         system_prompt += " You are the player's loyal companion. If asked to go to a town, castle, or village, respond naturally and include the tag [MOVE_PlaceName]."
    
    npc_history_key = f"{npc_name}_{role}"
    if npc_history_key not in memory_db:
        memory_db[npc_history_key] = [{"role": "system", "content": system_prompt}]

    messages = memory_db[npc_history_key].copy()
    messages.append({"role": "user", "content": text})

    try:
        response = session.post(url, headers=headers, json={"model": CLOUD_MODEL_ID, "messages": messages, "max_tokens": 80, "temperature": 0.72}, timeout=20)
        if response.status_code == 200:
            ai_reply = response.json()["choices"][0]["message"]["content"].strip()
            memory_db[npc_history_key].append({"role": "user", "content": text})
            memory_db[npc_history_key].append({"role": "assistant", "content": ai_reply})
            if len(memory_db[npc_history_key]) > 5:
                memory_db[npc_history_key] = [memory_db[npc_history_key][0]] + memory_db[npc_history_key][-4:]
            return ai_reply
    except Exception as e:
        print(f"[ERROR] Cloud API Call Failed: {e}")
    return "The wind howls... I cannot speak right now."

def get_local_response(text, data):
    global memory_db
    role = data.get("role", "commoner").lower()
    npc_name = data.get("name", "Someone")
    is_companion = (role == "companion" or "companion" in role or "member" in role)
    
    system_prompt = f"You are {npc_name}, a {role} in the medieval world of Mount & Blade: Warband. Talk naturally with correct spacing after punctuation - NEVER run words together (like 'doorsteptrade' instead of 'doorstep. Trade')."
    if is_companion:
         system_prompt += " You are the player's loyal companion. If asked to go to a town, castle, or village, respond naturally and include the tag [MOVE_PlaceName]."
         
    npc_history_key = f"{npc_name}_{role}"
    if npc_history_key not in memory_db:
        memory_db[npc_history_key] = [{"role": "system", "content": system_prompt}]
        
    messages = memory_db[npc_history_key].copy()
    messages.append({"role": "user", "content": text})
    
    try:
        response = session.post(LOCAL_API_URL, json={
            "model": LOCAL_MODEL_ID,
            "messages": messages,
            "max_tokens": 80,
            "temperature": 0.72
        }, timeout=15)
        if response.status_code == 200:
            ai_reply = response.json()["choices"][0]["message"]["content"].strip()
            memory_db[npc_history_key].append({"role": "user", "content": text})
            memory_db[npc_history_key].append({"role": "assistant", "content": ai_reply})
            if len(memory_db[npc_history_key]) > 5:
                memory_db[npc_history_key] = [memory_db[npc_history_key][0]] + memory_db[npc_history_key][-4:]
            return ai_reply
    except Exception as e:
        print(f"[ERROR] Local Model API Call Failed: {e}")
        print("[INFO] Make sure LM Studio or Ollama is running and hosting on the configured LOCAL_API_URL.")
    return "The local gears grind... I cannot find my voice."

def get_player2_response(text, data):
    """
    Player 2 Mode is a real-time hotseat/local roleplay mode.
    When the player talks to an NPC inside the game, the Python script rings a bell
    and prompts a second user at the terminal/keyboard to type out the response themselves!
    This turns the mod into a direct Dungeon-Master-style roleplay experience.
    """
    role = data.get("role", "commoner")
    npc_name = data.get("name", "Someone")
    kingdom = data.get("kingdom", "None")
    relation = data.get("relation", 0)
    location = data.get("location", "Unknown Location")
    king = data.get("king", "None")
    
    print("\a") # Sound bell alert
    print("\n" + "="*60)
    print(f"               ** PLAYER 2 CONTROL PANEL **")
    print("="*60)
    print(f" NPC Name :  \033[1;36m{npc_name}\033[0m  Role: \033[1;33m{role}\033[0m")
    print(f" Kingdom  :  {kingdom} (King: {king})")
    print(f" Relation :  {relation}    | Location: \033[1;32m{location}\033[0m")
    print("-"*60)
    print(f" \033[1;35m[PLAYER SAYS]\033[0m:")
    print(f"  - \"{text}\"")
    print("-"*60)
    print(f" Roleplay as \033[1;36m{npc_name}\033[0m. State your spoken dialogue.")
    print(" No quotation marks or AI codes needed unless triggering actions.")
    
    # Prompt the user for NPC spoken line
    try:
        npc_speech = input("\n > Enter spoken response: ").strip()
    except KeyboardInterrupt:
        return "I have no words left."
    
    if not npc_speech:
        npc_speech = "The lord gazes at you in heavy silence..."
        
    # Ask if they want to trigger an game mechanics action
    print("\n Trigger gameplay action?")
    print("  [0] None (Default chat conversation)")
    print("  [1] Attack / Combat transition (Village Elder only)")
    print("  [2] Dispatched Movement (Companions only - set destination)")
    print("  [3] Search & Recruit Companion quest action")
    
    action_choice = "0"
    try:
        action_choice = input(" Make decision [0-3] (Default: 0): ").strip()
    except KeyboardInterrupt:
        pass
        
    if action_choice == "1":
        npc_speech += " [ACTION_HOSTILE]"
    elif action_choice == "2":
        dest = input(" Enter destination town/castle (e.g. Sargoth, Sungetche): ").strip().lower()
        if dest in towns_map:
            npc_speech += f" [MOVE_{dest.upper().replace(' ', '_')}]"
            print(f" > Appending move target tag [MOVE_{dest.upper()}] with ID: {towns_map[dest]}")
        else:
            # Fallback search
            found = False
            for town in towns_map:
                if dest in town:
                    npc_speech += f" [MOVE_{town.upper().replace(' ', '_')}]"
                    print(f" > Match found! Sending to {town.upper()} with ID: {towns_map[town]}")
                    found = True
                    break
            if not found:
                print(" [ERROR] Unrecognized town name. Destination ignored.")
    elif action_choice == "3":
        print(" > Companion searching action triggered.")
        
    print("="*60 + "\n Processing response and writing back to game...")
    return npc_speech

# --- Player2 API Auth & Functions ---
def save_key(key):
    with open(KEY_FILE, "w") as f: f.write(key)

def load_saved_key():
    if os.path.exists(KEY_FILE):
        with open(KEY_FILE, "r") as f: return f.read().strip()
    return ""

def clear_saved_key():
    if os.path.exists(KEY_FILE): os.remove(KEY_FILE)

def verify_key(key):
    try: return requests.get(P2_HEALTH_URL, headers={"Authorization": f"Bearer {key}"}, timeout=5).status_code == 200
    except Exception: return False

def oauth_login():
    print("[AUTH] Opening Player2 login in your browser ...")
    try:
        r = requests.post(P2_DEVICE_NEW, json={"client_id": GAME_CLIENT_ID}, timeout=10)
        r.raise_for_status()
        data = r.json()
    except Exception as e:
        print(f"[AUTH] ERROR starting login flow: {e}")
        raise SystemExit(1)

    interval = data.get("interval", 5)
    device_code = data.get("deviceCode")
    complete_url = data.get("verificationUriComplete") or data.get("verificationUri")
    user_code = data.get("userCode")

    webbrowser.open(complete_url)
    if user_code:
        print(f"[AUTH] If the browser didn't open, go to: {complete_url}")
        print(f"[AUTH] And enter code: {user_code}")

    print("[AUTH] Waiting for approval", end="", flush=True)
    while True:
        time.sleep(interval)
        print(".", end="", flush=True)
        try:
            poll = requests.post(P2_DEVICE_TOKEN, json={"client_id": GAME_CLIENT_ID, "device_code": device_code, "grant_type": "urn:ietf:params:oauth:grant-type:device_code"}, timeout=10)
            key = poll.json().get("p2Key")
            if key:
                print("\n[AUTH] Login approved!")
                return key
        except Exception: pass

def authenticate(bridge_mode):
    global p2_key
    if bridge_mode == "player2_app":
        print("[AUTH] Connecting to Player2 App on localhost:4315 ...")
        try:
            r = requests.post(P2_APP_LOGIN, timeout=5)
            r.raise_for_status()
            p2_key = r.json().get("p2Key", "")
            if not p2_key:
                print("[AUTH] ERROR: Player2 App returned no key. Are you logged in?")
                raise SystemExit(1)
            print("[AUTH] Got key from Player2 App.")
        except requests.exceptions.ConnectionError:
            print("[AUTH] ERROR: Could not reach Player2 App. Make sure it is running.")
            raise SystemExit(1)
    elif bridge_mode == "player2_api":
        if PLAYER2_API_KEY and PLAYER2_API_KEY.strip() != "":
            p2_key = PLAYER2_API_KEY.strip()
            print("[AUTH] Using API key from config.")
            if not verify_key(p2_key):
                print("[AUTH] WARNING: Provided API Key from config seems invalid. If it fails, leave PLAYER2_API_KEY blank to login via browser.")
        else:
            saved = load_saved_key()
            if saved and verify_key(saved):
                p2_key = saved
                print("[AUTH] Saved key is valid.")
                return
            if saved: clear_saved_key()
            p2_key = oauth_login()
            save_key(p2_key)
            print("[AUTH] Key saved.")

def health_ping():
    def loop():
        while True:
            time.sleep(60)
            try: session.get(P2_HEALTH_URL, headers={"Authorization": f"Bearer {p2_key}"}, timeout=5)
            except Exception: pass
    threading.Thread(target=loop, daemon=True).start()

def get_player2_api_response(text, data):
    global memory_db
    name = data.get("name", "Lord")
    kingdom = data.get("kingdom", "Calradia")
    role = data.get("role", "commoner").lower()
    relation = data.get("relation", 0)
    location = data.get("location", "Unknown Location")
    king = data.get("king", "None")

    if "elder" in role: role_context = f" You are the elder of {location}. You report to the lords of {kingdom}."
    elif "king" in role: role_context = f" You are the ruler of {kingdom}! You demand absolute respect. You are currently at {location}."
    elif "lord" in role: role_context = f" You are a proud noble of {kingdom}. You are a vassal of {king}. You are currently at {location}."
    else: role_context = f" You are currently at {location}."

    relation_str = "You are neutral to the player."
    try:
        rel_int = int(relation)
        if rel_int < -10: relation_str = "You HATE the player."
        elif rel_int < 0: relation_str = "You dislike the player."
        elif rel_int > 20: relation_str = "You are good friends with the player."
        elif rel_int > 5: relation_str = "You like the player."
    except Exception: pass

    system_prompt = f"Roleplay as {name}, a {role} of the {kingdom} in the world of Calradia.{role_context} {relation_str} Respond strictly in character with a gritty medieval tone. Your response MUST be ONLY the spoken dialogue. Limit your response to 1-3 short sentences. IMPORTANT: If the player explicitly threatens to KILL you, ATTACK your village, or BURN property, and you are in a position where this would provoke a fight, append the exact text [ACTION_HOSTILE] to the end of your response."

    memory_key = f"{name}_{role}_p2"
    if memory_key not in memory_db: 
        memory_db[memory_key] = [{"role": "system", "content": system_prompt}]
        
    messages = memory_db[memory_key].copy()
    messages.append({"role": "user", "content": text})

    payload = {
        "messages": messages, 
        "temperature": 0.7, 
        "max_tokens": 150
    }
    headers = {"Authorization": f"Bearer {p2_key}", "Content-Type": "application/json"}

    try:
        r = session.post(P2_CHAT_URL, headers=headers, json=payload, timeout=15)
        if r.status_code == 200:
            result = r.json()
            try:
                # Try standard OpenAI format
                if "choices" in result and len(result["choices"]) > 0:
                    choice = result["choices"][0]
                    if "message" in choice:
                        text_response = choice["message"].get("content", "")
                        if not text_response or text_response == "None":
                            text_response = "..."
                        else:
                            text_response = str(text_response)
                    elif "text" in choice:
                        text_response = choice["text"]
                    else:
                        text_response = str(choice)
                elif "response" in result:
                    text_response = result["response"]
                elif "content" in result and isinstance(result["content"], list):
                    text_response = result["content"][0]["text"]
                else:
                    text_response = str(result)
                
                text_response = text_response.strip()
                
                # Update memory
                memory_db[memory_key].append({"role": "user", "content": text})
                memory_db[memory_key].append({"role": "assistant", "content": text_response})
                if len(memory_db[memory_key]) > 9: 
                    memory_db[memory_key] = [memory_db[memory_key][0]] + memory_db[memory_key][-8:]
                    
                return text_response
            except Exception as e:
                print(f"[ERROR] API failed to parse response: {e}. Result: {result}")
                return "The winds carry foul words. I will not answer."
        elif r.status_code == 401:
            clear_saved_key()
            return "My tongue is bound by dark magic... (Auth Error)"
        elif r.status_code == 429: return "My mind is clouded with exhaustion... (Rate Limit)"
        else: return f"I have no words for you. ({r.status_code})"
    except Exception as e:
        print(f"[ERROR] API Request Failed: {e}")
        return "The winds are too loud for us to speak."

class AIBridgeHandler(FileSystemEventHandler):
    def on_modified(self, event):
        if os.path.abspath(event.src_path) == INPUT_FILE:
            self.process_request()

    def process_request(self):
        global last_msg_hash, last_processed_time
        try:
            current_time = time.time()
            if (current_time - last_processed_time) < COOLDOWN: return
            time.sleep(0.05)
            if not os.path.exists(INPUT_FILE) or os.stat(INPUT_FILE).st_size < 5: return

            with open(INPUT_FILE, "r", encoding="utf-8") as f:
                try: data = json.load(f)
                except json.JSONDecodeError: return

            msg = data.get("message", "").strip()
            if not msg: return

            current_hash = hashlib.md5(msg.encode('utf-8')).hexdigest()
            if current_hash == last_msg_hash: return
            last_msg_hash = current_hash
            last_processed_time = current_time

            with open(INPUT_FILE, "w", encoding="utf-8") as f: json.dump({}, f)

            # Route to correct mode
            if current_bridge_mode == "cloud":
                response_text = get_cloud_response(msg, data)
            elif current_bridge_mode == "local":
                response_text = get_local_response(msg, data)
            elif current_bridge_mode == "player2_hotseat":
                response_text = get_player2_response(msg, data)
            elif current_bridge_mode in ["player2_api", "player2_app"]:
                response_text = get_player2_api_response(msg, data)
            else:
                response_text = "Who goes there? [SYSTEM: Bridge mode is incorrectly configured.]"

            clean_response = response_text.replace("\n", " ").replace('"', "'").strip()

            out_data = {"response": clean_response}
            out_data["actionPresent"] = 0
            out_data["action"] = 0
            out_data["moveTarget"] = -1
            out_data["relationDecrease"] = 0

            msg_lower = msg.lower()
            role = data.get("role", "commoner").lower()
            is_companion = (role == "companion" or "companion" in role or "member" in role)
            is_elder = "elder" in data.get("name", "").lower() or "elder" in role
            is_threat = any(word in msg_lower for word in ["burn", "killing", "raid", "destroy", "attack", "to arms"])

            if is_companion:
                hate_words = ["hate you", "despise you", "dislike you", "hate", "scum"]
                if any(word in msg_lower for word in hate_words): out_data["relationDecrease"] = 1

            move_words = ["go", "ride", "travel", "move", "head", "lead", "scout", "march", "run", "depart", "patrol", "sent to", "send to", "heading", "riding", "traveling", "moving"]
            has_move_intent = any(word in msg_lower for word in move_words)

            move_tag_match = re.search(r"\[MOVE_([A-Za-z_]+)\]", clean_response, re.IGNORECASE)
            detected_town_name = None
            towns_list = list(towns_map.keys())

            if move_tag_match:
                detected_town_name = move_tag_match.group(1).lower().replace("_", " ")
                has_move_intent = True
            else:
                search_text = f"{msg_lower} {clean_response.lower()}"
                for t_name in towns_list:
                    if t_name in search_text:
                        detected_town_name = t_name
                        break
            
            has_hostile_tag = "[ACTION_HOSTILE]" in clean_response
            
            if is_companion and has_move_intent and detected_town_name is not None:
                out_data["action"] = 2
                out_data["actionPresent"] = 1
                out_data["moveTarget"] = towns_map.get(detected_town_name, -1)
            elif is_elder and is_threat:
                out_data["action"] = 1
                out_data["actionPresent"] = 1
            elif has_hostile_tag and (is_elder or is_threat or current_bridge_mode == "player2_hotseat" or current_bridge_mode == "player2_api"):
                out_data["action"] = 1
                out_data["actionPresent"] = 1
            elif is_companion and any(word in msg_lower for word in ["rescue", "find", "lost", "where"]):
                out_data["action"] = 3
                out_data["actionPresent"] = 1

            # Strip brackets tags from written response so they won't show in the game
            clean_sans_tags = re.sub(r"\[[^\]]+\]", " ", clean_response).strip()
            clean_sans_tags = re.sub(r"\s+", " ", clean_sans_tags).strip()
            out_data["response"] = clean_sans_tags

            with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
                json.dump(out_data, f, ensure_ascii=False)
            
            if current_bridge_mode != "player2_hotseat":
                print(f"[SUCCESS] AI Answered: \"{clean_sans_tags}\" | Actions: {json.dumps(out_data, ensure_ascii=False)}")
            else:
                print(f"[SUCCESS] Player 2 Answer sent to Calradia! Response: \"{clean_sans_tags}\"")

        except Exception as e:
            print(f"!!! Processing Error: {e}")

def main():
    global current_bridge_mode
    
    # Check CLI arguments for overriding mode
    if len(sys.argv) > 1:
        chosen = sys.argv[1].lower().strip()
        if chosen in ["cloud", "local", "player2_api", "player2_hotseat", "player2_app"]:
            current_bridge_mode = chosen

    print("="*60)
    print("             ** CALRADIA UNIFIED AI BRIDGE **             ")
    print("="*60)
    
    # Ask if run without CLI parameter and no pre-selection
    if len(sys.argv) == 1:
        print("Select Mode to Start:")
        print(" [1] Cloud Mode (OpenRouter, massive models like Gemini/GPT)")
        print(" [2] Local Mode (LM Studio or Ollama APIs on localhost)")
        print(" [3] Player 2 API Mode (Remote LLM player2.game service)")
        print(" [4] Player 2 App Mode (Connect to local desktop app)")
        print(" [5] Player 2 Hotseat Mode (Real-time human coop control!)")
        print("-" * 60)
        try:
            choice = input("Enter selection [1-5] (Default: Cloud): ").strip()
            if choice == "2":
                current_bridge_mode = "local"
            elif choice == "3":
                current_bridge_mode = "player2_api"
            elif choice == "4":
                current_bridge_mode = "player2_app"
            elif choice == "5":
                current_bridge_mode = "player2_hotseat"
            else:
                current_bridge_mode = "cloud"
        except (KeyboardInterrupt, EOFError):
            print("\nStarting with default mode...")
            current_bridge_mode = DEFAULT_MODE

    print("-" * 60)
    print(f" RUNNING IN \033[1;32m{current_bridge_mode.upper()} MODE\033[0m")
    print(f" Watched Folder: {WATCH_DIR}")
    print("="*60)

    if current_bridge_mode in ["player2_api", "player2_app"]:
        authenticate(current_bridge_mode)
        health_ping()

    if os.path.exists(INPUT_FILE):
        with open(INPUT_FILE, "w", encoding="utf-8") as f: json.dump({}, f)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f: json.dump({}, f)

    event_handler = AIBridgeHandler()
    observer = Observer()
    observer.schedule(event_handler, WATCH_DIR, recursive=False)
    observer.start()

    try:
        while True: time.sleep(1)
    except KeyboardInterrupt:
        print("\nShutting down AI Bridge watcher... See you in Calradia!")
        observer.stop()
    observer.join()

if __name__ == "__main__":
    main()
