using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    public class Main : IPlugin, ISettingProvider, IContextMenu
    {
        private string mainClassName = nameof(Main);
        private PluginInitContext _context = null!;
        private Settings _settings = null!;
        private List<ToolConfig>? activeTools;
        private Dictionary<ToolType, (string exePath, ImageSource icon)> _cachedIcons = new Dictionary<ToolType, (string exePath, ImageSource icon)>();

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
            activeTools = allTools.Where(t => t.IsEnabled).ToList();

            // No tools configured in settings, show a message as a result
            if(activeTools.Count == 0)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "No file managers configured.",
                        SubTitle = "Please configure at least one in the settings.",
                        Action = _ =>
                        {
                            _context.API.OpenSettingDialog();
                            return true;
                        }
                    }
                };
            }

            /* Populate icon cache for each enabled tool. We use caching approach so that we avoid calling the icon extraction `IconExtractor.GetIconFromExe()` directly
             * in `Result.IconDelegate` (this would lead to calling the extraction logic every time we do a search with our action keyword) */
            PopulateIconCache();
            Dictionary<ToolType, ImageSource> resolvedIcons = _cachedIcons.ToDictionary(pair => pair.Key, pair => pair.Value.icon);

            // Load bookmarks (hotlist entries) from all enabled tools
            var entries = new List<HotlistEntry>();
            foreach (var tool in activeTools)
            {
                var toolEntries = TryLoad(() => tool.Parser.Parse(tool.SettingsFilePath));
                entries.AddRange(toolEntries);
            }

            // Convert entries to Flow Launcher results with fuzzy search
            var results = new List<Result>();
            foreach (var entry in entries)
            {
                var result = HotlistResultBuilder.Build(entry, searchTerm, _context, _settings, resolvedIcons);
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
            List<Result> r = new List<Result>();

            if (activeTools == null || activeTools.Count == 0)
            {
                return r;
            }

            // --- Context menu items for "Open in <tool>" ---

            if (activeTools.Count > 0)
            {
                /* If we have more than one tool enabled in settings (e.g. TC, DC and maybe others in future), we might have mixed bookmarks from each of these in the results
                We show the "source tool" (the one where the bookmark comes from) first in the context menu, above the other ones */

                ToolType? sourceToolType = selectedResult.ContextData is ToolType ? (ToolType)selectedResult.ContextData : null;
                ToolConfig? sourceTool = sourceToolType != null ? activeTools.FirstOrDefault(t => t.ToolType == sourceToolType) : null;

                if (sourceTool != null)
                {
                    r.Add(new Result
                    {
                        Title = $"Open in {sourceTool.DisplayName}",
                        SubTitle = $"Open the folder in {sourceTool.DisplayName}",
                        IcoPath = IconAssets.AppImage,
                        Glyph = IconAssets.GlyphOpenFolder,
                        Action = _ =>
                        {
                            ActionResult launchSourceToolActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, sourceTool, _context);
                            return ActionResult.HandleActionResult(launchSourceToolActionResult, _context);
                        }
                    });

                    // Launch presets for the source tool if we have any
                    foreach (var preset in sourceTool.LaunchPresets)
                    {
                        var capturedPreset = preset;
                        r.Add(new Result
                        {
                            Title = GetPresetName(capturedPreset, sourceTool),
                            SubTitle = GetPresetDescription(capturedPreset, sourceTool),
                            IcoPath = IconAssets.AppImage,
                            Glyph = IconAssets.GlyphOpenFolder,
                            Action = _ =>
                            {
                                ActionResult launchSourceToolPresetActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, sourceTool, capturedPreset.Arguments, _context);
                                return ActionResult.HandleActionResult(launchSourceToolPresetActionResult, _context);
                            }
                        });
                    }
                }

                // ... and then show the other enabled tools if we have any more

                foreach (var tool in activeTools.Where(t => t != sourceTool))
                {
                    r.Add(new Result
                    {
                        Title = $"Open in {tool.DisplayName}",
                        SubTitle = $"Open the folder in {tool.DisplayName}",
                        IcoPath = IconAssets.AppImage,
                        Glyph = IconAssets.GlyphOpenFolder,
                        Action = _ =>
                        {
                            ActionResult launchOtherToolActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, tool, _context);
                            return ActionResult.HandleActionResult(launchOtherToolActionResult, _context);
                        }
                    });

                    // Launch presets for this tool if we have any
                    foreach (var preset in tool.LaunchPresets)
                    {
                        var capturedPreset = preset;
                        r.Add(new Result
                        {
                            Title = GetPresetName(capturedPreset, tool),
                            SubTitle = GetPresetDescription(capturedPreset, tool),
                            IcoPath = IconAssets.AppImage,
                            Glyph = IconAssets.GlyphOpenFolder,
                            Action = _ =>
                            {
                                ActionResult launchOtherToolPresetActionResult = CommanderLauncher.Launch(selectedResult.SubTitle, tool, capturedPreset.Arguments, _context);
                                return ActionResult.HandleActionResult(launchOtherToolPresetActionResult, _context);
                            }
                        });
                    }
                }
            }

            // --- Context menu items for copy folder's name/path ---

            r.Add(new Result
            {
                Title = "Copy folder's name",
                SubTitle = "Copy the name of the folder to clipboard",
                IcoPath = IconAssets.AppImage,
                Glyph = IconAssets.GlyphCopy,
                Action = _ =>
                {
                    ActionResult copyNameActionResult = Copy(selectedResult.SubTitle, true);
                    return ActionResult.HandleActionResult(copyNameActionResult, _context);
                }
            });

            r.Add(new Result
            {
                Title = "Copy folder's path",
                SubTitle = "Copy the full path of the folder to clipboard",
                IcoPath = IconAssets.AppImage,
                Glyph = IconAssets.GlyphCopy,
                Action = _ =>
                {
                    ActionResult copyPathActionResult = Copy(selectedResult.SubTitle, false);
                    return ActionResult.HandleActionResult(copyPathActionResult, _context);
                }
            });

            // --- Context menu item for open folder in terminal ---

            r.Add(new Result
            {
                Title = "Open in terminal",
                SubTitle = "Open the folder in the default terminal",
                IcoPath = IconAssets.AppImage,
                Glyph = IconAssets.GlyphCommandPrompt,
                Action = _ =>
                {
                    ActionResult openTerminalActionResult = OpenTerminalInDirectory(selectedResult.SubTitle);
                    return ActionResult.HandleActionResult(openTerminalActionResult, _context);
                }
            });

            return r;
        }

        /// <summary>
        /// Returns the display name for a launch preset, or use default "Open in <tool.DisplayName> (<args>)" if empty
        /// </summary>
        private static string GetPresetName(LaunchPreset preset, ToolConfig tool)
        {
            if (!string.IsNullOrWhiteSpace(preset.Name))
                return preset.Name;

            string args = preset.Arguments ?? string.Empty;
            return string.IsNullOrWhiteSpace(args)
                ? $"Open in {tool.DisplayName}"
                : $"Open in {tool.DisplayName} ({args})";
        }

        /// <summary>
        /// Returns the display description for a launch preset, or use the default "<executableName> <args>" if empty
        /// </summary>
        private static string GetPresetDescription(LaunchPreset preset, ToolConfig tool)
        {
            if (!string.IsNullOrWhiteSpace(preset.Description))
                return preset.Description;

            string exeName = Path.GetFileName(tool.ExecutablePath);
            string args = preset.Arguments ?? string.Empty;
            return string.IsNullOrWhiteSpace(args) ? exeName : $"{exeName} {args}";
        }

        /// <summary>
        /// Copies the path to clipboard (name or full path), used in the context menu.
        /// </summary>
        private ActionResult Copy(string path, bool copyNameOnly)
        {
            try
            {
                string toCopy = copyNameOnly ? new DirectoryInfo(path).Name : path;
                _context.API.CopyToClipboard(toCopy);
                return ActionResult.Success();
            }
            catch(Exception ex)
            {
                return ActionResult.Fail($"Failed to copy {(copyNameOnly ? "name" : "path")} to clipboard", ex, mainClassName);
            }
        }

        /// <summary>
        /// Launches the terminal in the specified directory, fallbacks to "cmd.exe" if launching Windows Terminal fails
        /// </summary>
        private ActionResult OpenTerminalInDirectory(string workingDirectory)
        {
            /* To prevent bugs and delays when we deal with UNC paths, we won't check if the path exists or not and 
             * leave the terminal handle it.
             * Also from my testing, a `Directory.Exists` check on a network path will always return `false` no matter what */

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
                    return ActionResult.Fail("Failed to launch the terminal", ex, mainClassName);
                }
            }
        }

        /// <summary>
        /// Extracts and caches the icon for each enabled tool and re-extracts if the tool's executable was reconfigured in settings
        /// </summary>
        private void PopulateIconCache()
        {
            if (activeTools == null)
            {
                return;
            }

            foreach (ToolConfig tool in activeTools)
            {
                // Re-extract if the executable path changed (e.g. user changed it in settings)
                if (_cachedIcons.TryGetValue(tool.ToolType, out var cached) && cached.exePath == tool.ExecutablePath)
                {
                    continue;
                }

                (ImageSource? icon, ActionResult result) = IconExtractor.GetIconFromExe(tool.ExecutablePath, _context);

                _cachedIcons[tool.ToolType] = (tool.ExecutablePath, icon);
                ActionResult.HandleActionResult(result, _context);
            }
        }

        /// <summary>
        /// Attempts to parse the bookmarks and add them to 'target' list.
        /// </summary>
        private List<HotlistEntry> TryLoad(Func<IEnumerable<HotlistEntry>> parse)
        {
            List<HotlistEntry> resultEntries = new List<HotlistEntry>();
            try
            {
                resultEntries.AddRange(parse());
            }
            catch
            {
                // We don't really need to show a message to the user here; if the result loading fails somehow, we just don't show them
            }
            return resultEntries;
        }
    }

    internal static class IconAssets
    {
        private const string GlyphFont = "Segoe Fluent Icons";

        public const string AppImage = "Images\\icon.png";

        public static readonly GlyphInfo GlyphCopy = new GlyphInfo(GlyphFont, "\ue8c8");
        public static readonly GlyphInfo GlyphCommandPrompt = new GlyphInfo(GlyphFont, "\ue756");
        public static readonly GlyphInfo GlyphOpenFolder = new GlyphInfo(GlyphFont, "\ue838");
    }
}