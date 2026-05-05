using Flow.Launcher.Plugin.SharedModels;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class HotlistResultBuilder
    {
        /// <summary>
        /// Creates a <see cref="Result"/> for the given <paramref name="entry"/>.
        /// If <paramref name="searchTerm"/> is provided, the entry is fuzzy-matched against both name and path
        /// </summary>
        public static Result? Build(HotlistEntry entry, string searchTerm, PluginInitContext context,
            IReadOnlyDictionary<ToolType, string> subtitleTagLookup, 
            bool showSourceTag, IReadOnlyDictionary<ToolType, ToolConfig> toolLookup)
        {
            string title = showSourceTag ? subtitleTagLookup[entry.ToolType] + " " + entry.Name : entry.Name;

            var tool = toolLookup[entry.ToolType];

            Result result = new Result
            {
                Title = title,
                SubTitle = entry.Path,
                IcoPath = "Images\\app.png",
                ContextData = (ToolType)tool.ToolType,
                Action = _ => CommanderLauncher.Launch(entry.Path, tool, context)
            };

            // No search term: assign a default score and return
            if (string.IsNullOrEmpty(searchTerm))
            {
                result.Score = 0;
                return result;
            }

            // Fuzzy-match against both name and path, pick the best score
            MatchResult nameMatch = context.API.FuzzySearch(searchTerm, entry.Name);
            MatchResult pathMatch = context.API.FuzzySearch(searchTerm, entry.Path);

            // Determine which match is better, but only if at least one meets the precision score
            MatchResult? bestMatch = null;
            if (nameMatch.IsSearchPrecisionScoreMet() && pathMatch.IsSearchPrecisionScoreMet())
            {
                bestMatch = nameMatch.Score >= pathMatch.Score ? nameMatch : pathMatch;
            }
            else if (nameMatch.IsSearchPrecisionScoreMet())
            {
                bestMatch = nameMatch;
            }
            else if (pathMatch.IsSearchPrecisionScoreMet())
            {
                bestMatch = pathMatch;
            }

            if (bestMatch is null)
            {
                return null;
            }

            // Use the fuzzy score as the base
            result.Score = bestMatch.Score;

            // Boost entries where the search term matches at the very start of the name
            // e.g. searching "pr" will match "Projects" higher than "VS Projects"
            if (entry.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                result.Score += 100;
            }

            return result;
        }
    }
}