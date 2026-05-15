from header_common import *
from header_operations import *
from header_parties import *
from header_items import *
from header_skills import *
from header_triggers import *
from header_troops import *
from header_music import *

from module_constants import *

simple_triggers = [
# --- AI TIMEOUT & FILE LISTENER ---
(1.0, [
    (eq, "$g_ai_is_waiting", 1),
    (val_add, "$g_ai_timer", 1), 
    
    (dict_create, "$ai_check"),
    (dict_load_file_json, "$ai_check", "@From AI Chat", 0),
    (assign, ":size", 0), (dict_get_size, ":size", "$ai_check"),
    (try_begin),
        (gt, ":size", 0),
        # Success
        (dict_save_json, "$ai_check", "@AI_Memory"),
        (dict_clear, "$ai_check"), (dict_save_json, "$ai_check", "@From AI Chat"),
        (assign, "$g_ai_response_ready", 1),
        (assign, "$g_ai_is_waiting", 0),
        (display_message, "@[AI System] Response received!", 0x00FF00),
    (else_try),
        # TIMEOUT
        (ge, "$g_ai_timer", 30),
        (assign, "$g_ai_is_waiting", 0),
        (display_message, "@[AI System] ERROR: AI request timed out (30s).", 0xFF3333),
    (try_end),
    (dict_free, "$ai_check"),
  ]),
]
