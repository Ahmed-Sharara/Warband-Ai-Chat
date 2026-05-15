import time
import os
import requests
import json
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# --- CONFIGURATION ---
WATCH_DIR = r"C:\Users\LOQ\Documents\Mount&Blade Warband WSE2\WSE\Native" 
INPUT_FILE = os.path.abspath(os.path.join(WATCH_DIR, "To AI Chat.json"))
OUTPUT_FILE = os.path.abspath(os.path.join(WATCH_DIR, "From AI Chat.json"))

# LM Studio Local API URL
LM_STUDIO_URL = "http://localhost:1234/v1/chat/completions"

def get_lm_studio_response(text, name, kingdom):
    """Talks to LM Studio local server."""
    
    # Simple prompt for Qwen 0.5
    prompt = f"Roleplay as {name}, a medieval lord of the {kingdom} in the world of Calradia. Give a very short response (15 words max) to: '{text}'. IMPORTANT: If the player threatens to kill you, attack you, or burn your property/village, you MUST append the exact text [ACTION_HOSTILE] to the end of your response."

    payload = {
        "model": "qwen", # LM Studio usually ignores this and uses the loaded model
        "messages": [
            {"role": "system", "content": f"You are {name} from Mount & Blade. Gritty medieval tone. Short answers only. If the player threatens you or your property, append [ACTION_HOSTILE] to the end of your reply."},
            {"role": "user", "content": text}
        ],
        "temperature": 0.7,
        "max_tokens": 50
    }
    
    try:
        # We use a timeout of 10s because Qwen 0.5 on your PC should be instant
        r = requests.post(LM_STUDIO_URL, json=payload, timeout=10)
        r.raise_for_status()
        return r.json()['choices'][0]['message']['content']
    except Exception as e:
        print(f"!!! LM Studio Error: {e}")
        return "I am lost in my own thoughts, traveler."

class AIBridgeHandler(FileSystemEventHandler):
    def on_modified(self, event):
        if os.path.abspath(event.src_path) == INPUT_FILE:
            self.process_request()

    def process_request(self):
        try:
            if not os.path.exists(INPUT_FILE) or os.stat(INPUT_FILE).st_size < 5:
                return

            time.sleep(0.1) # Small wait for file lock

            with open(INPUT_FILE, "r", encoding="utf-8") as f:
                data = json.load(f)
            
            # Immediately clear to prevent ghost triggers re-reading old data
            with open(INPUT_FILE, "w", encoding="utf-8") as f:
                json.dump({}, f)

            msg = data.get("message")
            name = data.get("name", "Lord")
            kind = data.get("kingdom", "Calradia")

            if msg:
                print(f"\n[LOCAL AI] {name} is thinking...")
                response_text = get_lm_studio_response(msg, name, kind)
                
                # Clean text for Warband
                clean_response = response_text.replace("\n", " ").replace('"', "'").replace("{", "(").replace("}", ")").strip()
                
                out_data = {"response": clean_response}
                if "[ACTION_HOSTILE]" in clean_response:
                    clean_response = clean_response.replace("[ACTION_HOSTILE]", "").strip()
                    out_data["response"] = clean_response
                    out_data["action"] = "hostile"

                # Save response
                with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
                    json.dump(out_data, f, ensure_ascii=False)
                
                print(f"[SUCCESS] Answered: {clean_response}")
        except Exception as e:
            print(f"!!! processing Error: {e}")

if __name__ == "__main__":
    print("--- CALRADIA LOCAL AI BRIDGE: LM STUDIO MODE ---")
    print(f"Monitoring: {INPUT_FILE}")
    print("Ensure LM Studio Local Server is RUNNING on port 1234")

    # Clear old files
    if os.path.exists(INPUT_FILE): os.remove(INPUT_FILE)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump({}, f)

    event_handler = AIBridgeHandler()
    observer = Observer()
    observer.schedule(event_handler, WATCH_DIR, recursive=False)
    observer.start()

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()