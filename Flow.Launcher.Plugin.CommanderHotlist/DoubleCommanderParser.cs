using System.Xml.Linq;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Parses Double Commander settings XML file to extract directory hotlist entries.
    /// </summary>
    internal class DoubleCommanderParser : IHotlistParser
    {
        /// <inheritdoc/>
        public IEnumerable<HotlistEntry> Parse(string filePath)
        {
            var doc = XDocument.Load(filePath);

            foreach (var hotDir in doc.Descendants("HotDir"))
            {
                var name = hotDir.Attribute("Name")?.Value ?? "Unnamed";
                var path = hotDir.Attribute("Path")?.Value ?? string.Empty;

                // Ignore elements that have an empty path (e.g. separators)
                if (!string.IsNullOrEmpty(path))
                {
                    yield return new HotlistEntry(name, path, ToolType.DoubleCommander);
                }
            }
        }
    }
}