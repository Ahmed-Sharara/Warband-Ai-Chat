using System.Collections.Generic;
using System.Linq;

namespace CalradiaAiBridge {
    partial class Program {
        // --- Memory Helpers ---
        static List<Dictionary<string, string>> UpdateMemory( string key, string systemPrompt, string userText ) {
            if (!_memoryDb.ContainsKey(key)) {
                var initList = new List<Dictionary<string, string>>();
                initList.Add(new Dictionary<string, string> { { "role", "system" }, { "content", systemPrompt } });
                _memoryDb[key] = initList;
            }
            var list = new List<Dictionary<string, string>>(_memoryDb[key]);
            list.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            return list;
        }

        static void UpdateMemoryResult( string key, string aiText ) {
            var list = _memoryDb[key];
            string userText = "";
            for (int i = list.Count - 1; i >= 0; i--) {
                if (list[i]["role"] == "user") {
                    userText = list[i]["content"];
                    break;
                }
            }

            _memoryDb[key].Add(new Dictionary<string, string> { { "role", "user" }, { "content", userText } });
            _memoryDb[key].Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", aiText } });

            if (_memoryDb[key].Count > 9) {
                var sysMsg = _memoryDb[key][0];
                var recent = _memoryDb[key].Skip(_memoryDb[key].Count - 8).ToList();
                recent.Insert(0, sysMsg);
                _memoryDb[key] = recent;
            }
        }
    }
}
