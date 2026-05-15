from header_common import *
from header_presentations import *
from header_operations import *
from module_constants import *


####################################################################################################################
#  Each presentation record contains the following fields:
#  1) Presentation id: used for referencing presentations in other files.
#  2) Presentation flags.
#  3) Presentation background mesh.
#  4) Presentation triggers.
####################################################################################################################

presentations = [

  # Presentation for black editbox
("ai_chat_input", 2, 0, [ # Flag 2 = Locks camera and pauses World Map time
    (ti_on_presentation_load, [
      (set_fixed_point_multiplier, 1000),
      
      # 1. THE LABEL
      (create_text_overlay, "$g_ai_label", "@TYPE MESSAGE AND PRESS ENTER:", 0),
      (position_set_x, pos1, 280), (position_set_y, pos1, 600), (overlay_set_position, "$g_ai_label", pos1),

      # 2. THE BLACK EDIT BOX 
      (create_simple_text_box_overlay, "$g_ai_input_box"),
      (position_set_x, pos1, 400), (position_set_y, pos1, 550), (overlay_set_position, "$g_ai_input_box", pos1),
      
      (assign, "$g_ai_is_thinking", 0),
      (assign, "$g_ai_poll_timer", 0), 

      # Clear any ghost responses remaining
      (dict_create, "$ai_check"),
      (dict_clear, "$ai_check"),
      (dict_save_json, "$ai_check", "@From AI Chat"),
      (dict_free, "$ai_check"),

      (overlay_obtain_focus, "$g_ai_input_box"), 
      (presentation_set_duration, 999999),
    ]),

    (ti_on_presentation_run, [
      (try_begin), (key_clicked, 0x01), (presentation_set_duration, 0), (try_end), # ESC

      # --- ENTER KEY LOGIC (SEND MESSAGE) ---
      (try_begin),
        (eq, "$g_ai_is_thinking", 0),
        (key_clicked, 0x1C), # Enter
        
        (str_store_overlay_text, s65, "$g_ai_input_box"), # Capture to safe register s65
          
        (dict_create, "$ai_out"),
        (str_store_string, s1, "@message"), (dict_set_str, "$ai_out", s1, s65),
        (str_store_string, s1, "@name"), (str_store_troop_name, s2, "$g_ai_talk_troop"), (dict_set_str, "$ai_out", s1, s2),
        (str_store_string, s1, "@kingdom"), (store_troop_faction, ":f", "$g_ai_talk_troop"), (str_store_faction_name, s2, ":f"), (dict_set_str, "$ai_out", s1, s2),
        
        (dict_save_json, "$ai_out", "@To AI Chat"), (dict_free, "$ai_out"),

        # LOCK STATE: Hide input, show thinking label
        (assign, "$g_ai_is_thinking", 1),
        (overlay_set_display, "$g_ai_input_box", 0), 
        (overlay_set_text, "$g_ai_label", "@THE LORD IS THINKING... PLEASE WAIT."),
      (try_end),

      # --- POLLING LOGIC (POLLS EVERY 1 SECOND) ---
      (try_begin),
        (eq, "$g_ai_is_thinking", 1),
        (val_add, "$g_ai_poll_timer", 1),
        (try_begin),
            (gt, "$g_ai_poll_timer", 60), # Approx 1 second
            (assign, "$g_ai_poll_timer", 0), 
            
            (dict_create, "$ai_check"),
            (dict_load_file_json, "$ai_check", "@From AI Chat", 0),
            (assign, ":size", 0), (dict_get_size, ":size", "$ai_check"),
            (try_begin),
                (gt, ":size", 0),
                
                # Success! Save to permanent memory
                (dict_save_json, "$ai_check", "@AI_Memory"),
                (dict_clear, "$ai_check"), (dict_save_json, "$ai_check", "@From AI Chat"),
                
                (assign, "$g_ai_response_ready", 1),
                (display_message, "@[AI System] Response received! Talk to the NPC.", 0x00FF00),
                
                # AUTOMATIC UNLOCK (Close presentation)
                (presentation_set_duration, 0), 
            (try_end),
            (dict_free, "$ai_check"),
        (try_end),
      (try_end),
    ]),
  ]),  

]
