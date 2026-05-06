using System.Diagnostics;
using System.IO;
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
            // --- Context menu items for copy folder's name/path ---

            List<Result> r = new List<Result>()
            {
                new Result
                {
                    Title = "Copy folder's name",
                    SubTitle = "Copy the name of the folder to clipboard",
                    IcoPath = ImagePaths.AppImage,
                    Action = _ =>
                    {
                        ActionResult copyNameActionResult = Copy(selectedResult.SubTitle, true);
                        return ActionResult.HandleActionResult(copyNameActionResult, _context);
                    }
                },

                new Result
                {
                    Title = "Copy folder's path",
                    SubTitle = "Copy the full path of the folder to clipboard",
                    IcoPath = ImagePaths.AppImage,
                    Action = _ =>
                    {
                        ActionResult copyPathActionResult = Copy(selectedResult.SubTitle, false);
                        return ActionResult.HandleActionResult(copyPathActionResult, _context);
                    }
                }
            };

            // --- Context menu items for "Open in <tool>" ---

            if (activeTools != null && activeTools.Count > 0)
            {
                /* If we have more than one tool enabled in settings (e.g. TC, DC and maybe others in future), we might have mixed bookmarks from each of these in the results
                We show the "source tool" (the one where the bookmark comes from) first in the context menu, above the other ones */

                ToolType? sourceToolType = selectedResult.ContextData is ToolType ? (ToolType)selectedResult.ContextData : null;
                ToolConfig? sourceTool = sourceToolType != null ? activeTools.FirstOrDefault(t => t.ToolType == sourceToolType) : null;

                if (sourceTool != null)
                    r.Add(new Result
                    {
                        Title = $"Open in {sourceTool.DisplayName}",
                        SubTitle = $"Open the folder in {sourceTool.DisplayName}",
                        IcoPath = ImagePaths.AppImage,
                        Action = _ =>
                        {
                            ActionResult launchSourceToolActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, sourceTool, _context);
                            return ActionResult.HandleActionResult(launchSourceToolActionResult, _context);
                        }
                    });

                // ... and then show the other enabled tools if we have any more

                foreach (var tool in activeTools.Where(t => t != sourceTool))
                {
                    r.Add(new Result
                    {
                        Title = $"Open in {tool.DisplayName}",
                        SubTitle = $"Open the folder in {tool.DisplayName}",
                        IcoPath = ImagePaths.AppImage,
                        Action = _ =>
                        {
                            ActionResult launchOtherToolActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, tool, _context);
                            return ActionResult.HandleActionResult(launchOtherToolActionResult, _context);
                        }
                    });
                }
            }

            // --- Open folder in terminal ---

            r.Add(new Result
            {
                Title = "Open in terminal",
                SubTitle = "Open the folder in the default terminal",
                IcoPath = ImagePaths.AppImage,
                Action = _ =>
                {
                    ActionResult openTerminalActionResult = OpenTerminalInDirectory(selectedResult.SubTitle);
                    return ActionResult.HandleActionResult(openTerminalActionResult, _context);
                }
            });

            return r;
        }

        /// <summary>
        /// Copies the path to clipboard (name or full path), used in the context menu.
        /// </summary>
        private ActionResult Copy(string path, bool copyNameOnly)
        {
            string toCopy = copyNameOnly ? Path.GetFileName(path) : path;

            try
            {
                _context.API.CopyToClipboard(toCopy);
                return ActionResult.Success();
            }
            catch(Exception ex)
            {
                return ActionResult.Fail($"Failed to copy {(copyNameOnly ? "name" : "path")} to clipboard", ex);
            }
        }

        /// <summary>
        /// Launches the terminal in the specified directory, fallbacks to "cmd.exe" if launching Windows Terminal fails
        /// </summary>
        private ActionResult OpenTerminalInDirectory(string workingDirectory)
        {
            if(!Directory.Exists(workingDirectory))
            {
                return ActionResult.Fail("The selected location no longer exists", null);
            }

            try
            {
                // Try with Windows Terminal first
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = $"-d \"{workingDirectory}\"",
                    UseShellExecute = true
                });

                return ActionResult.Success();
            }
            catch
            {
                // Fallback to cmd.exe
                try
                {
                    string shellPath = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = shellPath,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = true
                    });

                    return ActionResult.Success();
                }
                catch (Exception ex)
                {
                    return ActionResult.Fail("Failed to launch the terminal", ex);
                }
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

    internal static class ImagePaths
    {
        public const string AppImage = "Images\\app.png";
    }
}