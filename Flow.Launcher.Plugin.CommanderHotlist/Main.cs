using System.IO;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public class Main : IPlugin, ISettingProvider
    {
        private PluginInitContext _context = null!;
        private Settings _settings = null!;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings>();
        }

        public List<Result> Query(Query query)
        {
            var searchTerm = query.Search.Trim();

            // Get all supported tools (file managers) independent whether they are disabled/enabled in settings
            var allTools = _settings.GetTools().ToList();

            // Determine which tools are enabled and have a valid settings file
            var activeTools = allTools
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

        public Control CreateSettingPanel()
        {
            return new SettingsControl(_context, _settings);
        }
    }
}