namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Groups all configuration for a single file manager tool: exe path, settings path, parser, UI metadata
    /// </summary>
    internal class ToolConfig
    {
        public ToolType ToolType { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string ExecutablePath { get; set; } = string.Empty;
        public string SettingsFilePath { get; set; } = string.Empty;
        public string AdditionalArguments { get; set; } = string.Empty;
        public IHotlistParser Parser { get; init; } = null!;
        public string SettingsFileLabel { get; init; } = string.Empty;
        public string SettingsFileFilter { get; init; } = string.Empty;
        public string ExeFileFilter { get; init; } = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
        public string SubtitleTag { get; init; } = string.Empty;
    }
}