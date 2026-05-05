using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext _context = null!;
        private Settings _settings = null!;
        private List<ToolConfig>? activeTools;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
        }

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _settings);
        }

        public List<Result> Query(Query query)
        {
            var searchTerm = query.Search.Trim();

            // Get all supported tools (file managers) independent whether they are disabled/enabled in settings
            var allTools = _settings.GetTools().ToList();

            // Determine which tools are enabled and have a valid settings file
            activeTools = allTools
                .Where(t => t.IsEnabled && File.Exists(t.SettingsFilePath))
                .ToList();

            // Load bookmarks (hotlist entries) from all enabled tools
            var entries = new List<HotlistEntry>();
            foreach (var tool in activeTools)
            {
                TryLoad(() => tool.Parser.Parse(tool.SettingsFilePath), entries);
            }

            // Show source tags like "[TC]" or "[DC]" only if we have more than one tool active, otherwise don't display them
            var subtitleTagLookup = activeTools.ToDictionary(t => t.ToolType, t => t.SubtitleTag);
            var toolLookup = allTools.ToDictionary(t => t.ToolType, t => t);
            var showSourceTag = entries.Select(e => e.ToolType).Distinct().Count() > 1;

            // Convert entries to Flow Launcher results with fuzzy search
            var results = new List<Result>();
            foreach (var entry in entries)
            {
                var result = HotlistResultBuilder.Build(entry, searchTerm, _context, subtitleTagLookup, showSourceTag, toolLookup);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            // If search didn't match anything, we won't display any results
            if (results.Count == 0)
            {
                return new List<Result>();
            }

            // Sort results by relevance if we used a search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                results = results.OrderByDescending(r => r.Score).ToList();
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            List<Result> r = new List<Result>()
            {
                new Result
                {
                    Title = "Copy folder's name",
                    SubTitle = "Copy the name of the folder to clipboard",
                    IcoPath = "Images\\app.png",
                    Action = _ =>
                    {
                        bool success = Copy(selectedResult.SubTitle, true);
                        return success;
                    }
                },

                new Result
                {
                    Title = "Copy folder's path",
                    SubTitle = "Copy the full path of the folder to clipboard",
                    IcoPath = "Images\\app.png",
                    Action = _ =>
                    {
                        bool success = Copy(selectedResult.SubTitle, false);
                        return success;
                    }
                }
            };

            // Double Commander, only if it's enabled in settings
            ToolConfig? doubleCommanderToolConfig = activeTools.FirstOrDefault(t => t.ToolType == ToolType.DoubleCommander);
            if (doubleCommanderToolConfig != null)
            {
                r.Add(new Result
                {
                    Title = $"Open in {doubleCommanderToolConfig.DisplayName}",
                    SubTitle = $"Open the folder in {doubleCommanderToolConfig.DisplayName}",
                    IcoPath = "Images\\app.png",
                    Action = _ =>
                    {
                        if (!CommanderLauncher.Launch(selectedResult.SubTitle, doubleCommanderToolConfig, _context))
                        {
                            _context.API.ShowMsgError("Error while opening the folder", $"Error while opening the folder in {doubleCommanderToolConfig.DisplayName}");
                            return false;
                        }
                        return true;
                    }
                });
            }

            // Total Commander, only if it's enabled in settings
            ToolConfig? totalCommanderToolConfig = activeTools.FirstOrDefault(t => t.ToolType == ToolType.TotalCommander);
            if(totalCommanderToolConfig != null)
            {
                r.Add(new Result
                {
                    Title = $"Open in {totalCommanderToolConfig.DisplayName}",
                    SubTitle = $"Open the folder in {totalCommanderToolConfig.DisplayName}",
                    IcoPath = "Images\\app.png",
                    Action = _ =>
                    {
                        if (!CommanderLauncher.Launch(selectedResult.SubTitle, totalCommanderToolConfig, _context))
                        {
                            _context.API.ShowMsgError("Error while opening the folder", $"Error while opening the folder in {totalCommanderToolConfig.DisplayName}");
                            return false;
                        }
                        return true;
                    }
                });
            }

            return r;
        }

        /// <summary>
        /// Copies the path to clipboard (name or full path), used in the context menu.
        /// </summary>
        private bool Copy(string path, bool copyNameOnly)
        {
            string toCopy = copyNameOnly ? Path.GetFileName(path) : path;

            try
            {
                _context.API.CopyToClipboard(toCopy);
                return true;
            }
            catch
            {
                _context.API.ShowMsgError("Failed to copy", $"Failed to copy the folder's {(copyNameOnly ? "name" : "full path")} to clipboard");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse the bookmarks and add them to 'target' list.
        /// </summary>
        private static void TryLoad(Func<IEnumerable<HotlistEntry>> parse, List<HotlistEntry> target)
        {
            try
            {
                target.AddRange(parse());
            }
            catch
            {
                // Silently skip if parsing fails (e.g. the settings file is malformed)
            }
        }
    }
}