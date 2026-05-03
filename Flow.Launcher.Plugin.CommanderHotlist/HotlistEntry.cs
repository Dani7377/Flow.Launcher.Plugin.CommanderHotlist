namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Represents a single directory hotlist entry from a file manager tool.
    /// </summary>
    internal sealed record HotlistEntry(string Name, string Path, ToolType ToolType);
}