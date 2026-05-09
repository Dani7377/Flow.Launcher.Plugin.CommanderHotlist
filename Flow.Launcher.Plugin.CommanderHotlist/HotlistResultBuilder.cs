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
            ToolConfig tool = toolLookup[entry.ToolType];
             
            // Prefix the title (name) with the name of the parent submenu(s) if it's enabled in settings
            string entryNameParentsPrefix = "";
            if(tool.ShowSubmenuNames)
            {
                for(int i=0;i<entry.Parents.Count;i++)
                {
                    entryNameParentsPrefix += entry.Parents[i];
                    if (i < entry.Parents.Count)
                        entryNameParentsPrefix += " > ";
                }
            }

            string entryNameWithoutPrefix = entry.Name;
            string entryPathWithoutPrefix = entry.Path;

            string displayedEntryName = entryNameParentsPrefix + entryNameWithoutPrefix;
            string displayedEntryPath = showSourceTag ? 
                subtitleTagLookup[entry.ToolType] + " " + entryPathWithoutPrefix : entryPathWithoutPrefix;

            Result result = new Result
            {
                Title = displayedEntryName,
                SubTitle = displayedEntryPath,
                IcoPath = ImagePaths.AppImage,
                ContextData = (ToolType)tool.ToolType,
                Action = _ =>
                {
                    ActionResult launchToolActionResult = CommanderLauncher.Launch(entryPathWithoutPrefix, tool, context);
                    return ActionResult.HandleActionResult(launchToolActionResult, context);
                }
            };

            // No search term: assign a default score and return
            if (string.IsNullOrEmpty(searchTerm))
            {
                result.Score = 0;
                return result;
            }

            /* Fuzzy-match against both name and path, pick the best score
             * If submenu names are displayed in title (enabled in settings), use "displayedEntryName" to include them in fuzzy search
             * If source tags are displayed in subtitle, use "entryPathWithoutPrefix", we don't need them to be included in search
             */
            MatchResult nameMatch = context.API.FuzzySearch(searchTerm, displayedEntryName);
            MatchResult pathMatch = context.API.FuzzySearch(searchTerm, entryPathWithoutPrefix);

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
            if (displayedEntryName.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                result.Score += 100;
            }

            return result;
        }
    }
}