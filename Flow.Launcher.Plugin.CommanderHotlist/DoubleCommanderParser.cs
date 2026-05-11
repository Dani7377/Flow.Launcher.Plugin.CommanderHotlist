using Flow.Launcher.Plugin.CommanderHotlist;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

internal class DoubleCommanderParser : IHotlistParser
{
    public IEnumerable<HotlistEntry> Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var menuStack = new Stack<string>();

        foreach (var hotDir in doc.Descendants("HotDir"))
        {
            var name = hotDir.Attribute("Name")?.Value ?? string.Empty;
            var path = hotDir.Attribute("Path")?.Value ?? string.Empty;

            // Handle submenu exit
            if (name == "--")
            {
                if (menuStack.Count > 0) menuStack.Pop();
                continue;
            }

            // Handle submenu entry
            // We only push to the stack if it starts with a single "-" and contains actual text
            if (name.StartsWith("-") && !name.StartsWith("--") && string.IsNullOrEmpty(path))
            {
                var cleanName = name.TrimStart('-').Trim();

                // If it's just a separator "-" ignore
                if (!string.IsNullOrEmpty(cleanName))
                {
                    menuStack.Push(cleanName);
                }
                continue;
            }

            // Handle directory entries
            // We ignore items with no path (e.g. separators)
            if (!string.IsNullOrEmpty(path))
            {
                var parents = menuStack.Reverse().ToList();

                yield return new HotlistEntry(
                    StringUtilities.RemoveMnemonicsFromEntryName(name),
                    path,
                    ToolType.DoubleCommander,
                    parents);
            }
        }
    }
}