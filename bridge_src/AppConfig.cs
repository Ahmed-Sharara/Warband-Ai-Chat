using System.ComponentModel;
using System;

namespace CalradiaAiBridge
{
    public enum BridgeMode
    {
        cloud,
        local,
        player2_api,
        player2_app,
        player2_hotseat
    }

    public class AppConfig
    {
        [Category("1. General")]
        [Description("Default bridge mode: cloud, local, player2_api, player2_app, player2_hotseat")]
        public BridgeMode DefaultMode { get; set; }

        [Category("1. General")]
        [Description("Path to your Mount & Blade Native directory")]
        public string WatchDir { get; set; }

        [Category("2. Cloud Settings")]
        [Description("URL for your chosen API endpoint (SHOULD END WITH /v1/chat/completions).")]
        public string CloudAPIEndpoint { get; set; }

        [Category("2. Cloud Settings")]
        [Description("Model ID for your chosen endpoint (e.g. google/gemma-4-31b-it:free, google/gemma-4-26b-a4b-it:free, qwen/qwen3.8-27b:free).")]
        public string CloudModelId { get; set; }

        [Category("2. Cloud Settings")]
        [Description("API key for your chosen endpoint.")]
        public string OpenRouterApiKey { get; set; }

        [Category("3. Local Settings")]
        public string LocalApiUrl { get; set; }
        [Category("3. Local Settings")]
        public string LocalModelId { get; set; }

        [Category("4. Player2 Settings")]
        public string Player2ApiKey { get; set; }

        public AppConfig()
        {
            DefaultMode = BridgeMode.cloud;
            WatchDir = @"C:\Users\LOQ\Documents\Mount&Blade Warband WSE2\WSE\Native";
            CloudAPIEndpoint = "https://openrouter.ai/api/v1/chat/completions";
            CloudModelId = "google/gemma-4-31b-it:free";
            OpenRouterApiKey = "";
            LocalApiUrl = "http://localhost:1234/v1/chat/completions";
            LocalModelId = "local-model";
            Player2ApiKey = "";
        }
    }
}
