using System.IO;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Parses Total Commander INI settings files to extract directory hotlist entries.
    /// </summary>
    internal class TotalCommanderParser : IHotlistParser
    {
        // Matches "menu0=Some Name" — captures the display name
        private static readonly Regex MenuLineRegex = new(
            @"^menu\d+=(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches "cmd0=cd C:\Path" or "cmd0=C:\Path" — captures the directory path
        private static readonly Regex CommandLineRegex = new(
            @"^cmd\d+=(?:cd\s+)?(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <inheritdoc/>
        public IEnumerable<HotlistEntry> Parse(string filePath)
        {
            var iniLines = File.ReadAllLines(filePath);
            var inDirMenu = false;
            string? pendingName = null;

            foreach (var line in iniLines)
            {
                var trimmed = line.Trim();

                // Detect entry into or exit from the [DirMenu] section
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    inDirMenu = trimmed.Equals("[DirMenu]", StringComparison.OrdinalIgnoreCase);
                    pendingName = null;
                    continue;
                }

                if (!inDirMenu)
                {
                    continue;
                }

                // Try to match a "menuN=..." line — stores the display name
                var match = MenuLineRegex.Match(trimmed);
                if (match.Success)
                {
                    pendingName = match.Groups[1].Value;
                    continue;
                }

                // Try to match a "cmdN=..." line — produces an entry using the pending name
                match = CommandLineRegex.Match(trimmed);
                if (match.Success)
                {
                    var path = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var name = pendingName ?? "Unnamed";
                        yield return new HotlistEntry(name, path, ToolType.TotalCommander);
                    }
                    pendingName = null;
                }
            }
        }
    }
}