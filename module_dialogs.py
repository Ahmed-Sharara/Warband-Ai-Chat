from header_common import *
from header_dialogs import *
from header_operations import *
from header_troops import *

dialogs = [
    
# --- the top ---
  [anyone, "start", [
      (eq, "$g_ai_response_ready", 1),
      (store_conversation_troop, ":active_troop"),
      (eq, ":active_troop", "$g_ai_talk_troop"), 
      (dict_create, "$ai_mem"),
      (dict_load_file_json, "$ai_mem", "@AI_Memory", 0),
      (dict_get_size, reg1, "$ai_mem"),
      (str_store_string, s1, "@response"), (dict_get_str, s10, "$ai_mem", s1),
      (dict_free, "$ai_mem"),
      (ge, reg1, 2), # This is the branching condition: size >= 2 implies hostile action present!
      (assign, "$g_ai_response_ready", 0),
  ], "{s10}", "ai_hostile_plyr", []],

  [anyone, "start", [
      (eq, "$g_ai_response_ready", 1),
      (store_conversation_troop, ":active_troop"),
      (eq, ":active_troop", "$g_ai_talk_troop"), 
      (dict_create, "$ai_mem"),
      (dict_load_file_json, "$ai_mem", "@AI_Memory", 0),
      (str_store_string, s1, "@response"), (dict_get_str, s10, "$ai_mem", s1),
      (dict_free, "$ai_mem"),
      (assign, "$g_ai_response_ready", 0),
  ], "{s10}", "lord_talk", []],

[anyone|plyr, "ai_hostile_plyr", [], "To arms!", "close_window", [
    # 1. Print a debug message to confirm the code block is actually running
    (display_message, "@Debug: Triggering attack script..."),

    (try_begin),
        # Check if the engine knows we are at a town/village
        (gt, "$current_town", 0),
        
        # 2. Force the engine to recognize the village as the current encounter
        (assign, "$g_encountered_party", "$current_town"),
        
        # 3. Prevent the engine from accidentally kicking you to the world map
        (assign, "$g_leave_encounter", 0), 

        # ---------------------------------------------------------
        # CRITICAL STEP:
        # If you are walking around the village in 3D when you talk 
        # to this NPC, leave this line UNCOMMENTED. 
        # If you are talking to them from a UI Text Menu, DELETE this line.
        (finish_mission), 
        # ---------------------------------------------------------
        
        # 4. Jump to the Native village attack menu
        (jump_to_menu, "mnu_village_start_attack"),
        
        (display_message, "@Debug: Menu jump executed!"),
        
    (else_try),
        # 5. If $current_town was somehow 0, this warning will print on your screen
        (display_message, "@Debug: ERROR - $current_town is 0. Falling back to map attack."),
        (encounter_attack),
    (try_end),
]],


  # --- THE BUTTONS ---
  [anyone|plyr, "lord_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "mayor_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "village_elder_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "member_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "tavernkeeper_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "merchant_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "arena_master_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "spouse_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "village_center", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "town_center", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "ramun_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "tavern_traveler_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "ransom_broker_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "castle_guard_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "prison_guard_talk", [], "Chat with AI.", "ai_listening", []],
  [anyone|plyr, "lady_talk", [], "Chat with AI.", "ai_listening", []],

  [anyone, "ai_listening", [], "I am listening...", "close_window", [
      (store_conversation_troop, "$g_ai_talk_troop"),
      (assign, "$g_ai_response_ready", 0),
      (assign, "$g_open_ai_chat_flag", 1), # Signal the openers
  ]],
]
