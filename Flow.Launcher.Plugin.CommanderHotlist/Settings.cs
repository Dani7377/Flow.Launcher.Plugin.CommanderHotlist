namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public class Settings
    {
        // --- Double Commander ---
        public bool DcEnabled { get; set; } = false;
        public string DcExecutablePath { get; set; } = string.Empty;
        public string DcSettingsXmlPath { get; set; } = string.Empty;
        public string DcAdditionalArguments { get; set; } = string.Empty;

        // --- Total Commander ---
        public bool TcEnabled { get; set; } = false;
        public string TcExecutablePath { get; set; } = string.Empty;
        public string TcSettingsIniPath { get; set; } = string.Empty;
        public string TcAdditionalArguments { get; set; } = string.Empty;

        // --- Global settings (affect all tools) ---
        public bool ShowSubmenuNames { get; set; } = false;

        internal IEnumerable<ToolConfig> GetTools()
        {
            yield return new ToolConfig
            {
                ToolType = ToolType.DoubleCommander,
                DisplayName = "Double Commander",
                IsEnabled = DcEnabled,
                ExecutablePath = DcExecutablePath,
                SettingsFilePath = DcSettingsXmlPath,
                AdditionalArguments = DcAdditionalArguments,
                Parser = new DoubleCommanderParser(),
                SettingsFileLabel = "DC Settings XML Path:",
                SettingsFileFilter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                SubtitleTag = "[DC]",
                ShowSubmenuNames = ShowSubmenuNames
            };

            yield return new ToolConfig
            {
                ToolType = ToolType.TotalCommander,
                DisplayName = "Total Commander",
                IsEnabled = TcEnabled,
                ExecutablePath = TcExecutablePath,
                SettingsFilePath = TcSettingsIniPath,
                AdditionalArguments = TcAdditionalArguments,
                Parser = new TotalCommanderParser(),
                SettingsFileLabel = "TC Settings INI Path:",
                SettingsFileFilter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                SubtitleTag = "[TC]",
                ShowSubmenuNames = ShowSubmenuNames
            };
        }

        internal void SaveFromToolConfig(ToolConfig tool)
        {
            switch (tool.ToolType)
            {
                case ToolType.DoubleCommander:
                    DcEnabled = tool.IsEnabled;
                    DcExecutablePath = tool.ExecutablePath;
                    DcSettingsXmlPath = tool.SettingsFilePath;
                    DcAdditionalArguments = tool.AdditionalArguments;
                    break;
                case ToolType.TotalCommander:
                    TcEnabled = tool.IsEnabled;
                    TcExecutablePath = tool.ExecutablePath;
                    TcSettingsIniPath = tool.SettingsFilePath;
                    TcAdditionalArguments = tool.AdditionalArguments;
                    break;
            }
        }
    }
}
