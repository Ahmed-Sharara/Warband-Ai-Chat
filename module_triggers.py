from header_common import *
from header_operations import *
from header_parties import *
from header_items import *
from header_skills import *
from header_triggers import *
from header_troops import *
from header_music import *

from module_constants import *

####################################################################################################################
# Triggers is a list of trigger records.
####################################################################################################################

triggers = [
  
(0.0, 0, 0, [(eq, "$g_open_ai_chat_flag", 1), (map_free)], [
      (assign, "$g_open_ai_chat_flag", 0),
      (start_presentation, "prsnt_ai_chat_input"),
  ]),


]
