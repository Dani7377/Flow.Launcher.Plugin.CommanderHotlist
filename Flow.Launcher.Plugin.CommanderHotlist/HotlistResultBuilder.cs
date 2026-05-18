using Flow.Launcher.Plugin.SharedModels;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class HotlistResultBuilder
    {
        /// <summary>
        /// Creates a <see cref="Result"/> for the given <paramref name="entry"/>.
        /// If <paramref name="searchTerm"/> is provided, the entry is fuzzy-matched against both name and path
        /// </summary>
        public static Result? Build(HotlistEntry entry, string searchTerm, PluginInitContext context, Settings settings, Dictionary<ToolType, ImageSource> cachedIcons)
        {
            // Prefix the title (name) with the name of the parent submenu(s) if it's enabled in settings
            string entryNameParentsPrefix = "";
            if(settings.ShowSubmenuNames)
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
            string displayedEntryPath = entryPathWithoutPrefix;

            Result result = new Result
            {
                Title = displayedEntryName,
                SubTitle = displayedEntryPath,
                Icon = () => cachedIcons[entry.ToolType],
                ContextData = (ToolType)entry.ToolType,
                Action = _ =>
                {
                    ToolConfig currentTool = settings.GetTools().First(t => t.ToolType == entry.ToolType);
                    ActionResult launchToolActionResult = CommanderLauncher.Launch(entryPathWithoutPrefix, currentTool, context);
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