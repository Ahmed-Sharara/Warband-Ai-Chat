using System.Collections.Generic;
using System.Linq;
using System;

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

        static void UpdateMemoryResult( string key, string userText, string aiText ) {
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
