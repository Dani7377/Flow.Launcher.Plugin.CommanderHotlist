namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal interface IHotlistParser
    {
        /// <summary>
        /// Parses the given settings file and yields all directory hotlist entries found.
        /// </summary>
        IEnumerable<HotlistEntry> Parse(string filePath);
    }
}