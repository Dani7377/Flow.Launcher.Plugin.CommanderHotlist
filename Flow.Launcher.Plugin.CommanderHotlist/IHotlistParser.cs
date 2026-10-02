namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal interface IHotlistParser
    {
        /// <summary>
        /// Parses the tool's settings and yields all directory hotlist entries found.
        /// </summary>
        IEnumerable<HotlistEntry> Parse(HotlistSource source);
    }
}