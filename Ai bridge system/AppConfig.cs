using System.ComponentModel;

namespace CalradiaAiBridge {
    public enum BridgeMode {
        cloud,
        local,
        player2_api,
        player2_app,
        player2_hotseat
    }

    public class AppConfig {
        [Category("1. General")]
        [Description("Default bridge mode: cloud, local, player2_api, player2_app, player2_hotseat")]
        public BridgeMode DefaultMode { get; set; } = BridgeMode.cloud;

        [Category("1. General")]
        [Description("Path to your Mount & Blade Native directory")]
        public string WatchDir { get; set; } = @"C:\Users\LOQ\Documents\Mount&Blade Warband WSE2\WSE\Native";

        [Category("2. Cloud Settings")]
        public string OpenRouterApiKey { get; set; } = "your_api_key";
        [Category("2. Cloud Settings")]
        public string CloudModelId { get; set; } = "openai/gpt-oss-120b:free";

        [Category("3. Local Settings")]
        public string LocalApiUrl { get; set; } = "http://localhost:1234/v1/chat/completions";
        [Category("3. Local Settings")]
        public string LocalModelId { get; set; } = "local-model";

        [Category("4. Player2 Settings")]
        public string Player2ApiKey { get; set; } = "your_api_key";
    }

}
