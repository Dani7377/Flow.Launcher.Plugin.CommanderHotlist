using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal class TotalCommanderParser : IHotlistParser
    {
        private static readonly Regex MenuLineRegex = new(
            @"^menu\d+=(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CommandLineRegex = new(
            @"^cmd\d+=(?:cd\s+)?(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public IEnumerable<HotlistEntry> Parse(string filePath)
        {
            var iniLines = File.ReadAllLines(filePath);
            var inDirMenu = false;
            string? pendingName = null;

            var menuStack = new Stack<string>();

            foreach (var line in iniLines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    inDirMenu = trimmed.Equals("[DirMenu]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inDirMenu) continue;

                // Process menu line
                var menuMatch = MenuLineRegex.Match(trimmed);
                if (menuMatch.Success)
                {
                    var name = menuMatch.Groups[1].Value.Trim();

                    if (name == "--")
                    {
                        if (menuStack.Count > 0) menuStack.Pop();
                        pendingName = null;
                    }
                    else if (name.StartsWith("-") && name.Length > 1)
                    {
                        // If it's a submenu start e.g. "-Projects"
                        var menuName = name.TrimStart('-');
                        menuStack.Push(menuName);
                        pendingName = null; // Submenus items like "-Projects" don't have paths in TC
                    }
                    else if (name == "-") // it's just a separator
                    {
                        pendingName = null;
                    }
                    else // it's a standard entry name
                    {
                        pendingName = name;
                    }
                    continue;
                }

                // Process command line
                var cmdMatch = CommandLineRegex.Match(trimmed);
                if (cmdMatch.Success && pendingName != null)
                {
                    var path = cmdMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var parents = menuStack.Reverse().ToList();

                        yield return new HotlistEntry(
                            StringUtilities.RemoveMnemonicsFromEntryName(pendingName),
                            path,
                            ToolType.TotalCommander,
                            parents);
                    }
                    pendingName = null;
                }
            }
        }
    }
}