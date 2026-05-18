using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class CommanderLauncher
    {
        private static string cmdLauncherClassName = nameof(CommanderLauncher);

        /// <summary>
        /// Launches the configured file manager with the given target directory with optional arguments.
        /// </summary>
        public static ActionResult Launch(string targetDirectory, ToolConfig tool, PluginInitContext context)
        {
            /* To prevent bugs and delays when we deal with UNC paths, we won't check if the path exists or not and 
             * leave the tool handle it with its default behavior (both TC and DC use hierarchical fallback approach). 
             * Also from my testing, a `Directory.Exists` check on a network path will always return `false` no matter what */

            if (string.IsNullOrWhiteSpace(tool.ExecutablePath))
            {
                return ActionResult.Fail($"{tool.DisplayName} not configured", null, cmdLauncherClassName);
            }

            if (!File.Exists(tool.ExecutablePath))
            {
                return ActionResult.Fail($"{tool.DisplayName} executable not found", null, cmdLauncherClassName);
            }

            try
            {
                var arguments = string.IsNullOrWhiteSpace(tool.AdditionalArguments)
                    ? $"\"{targetDirectory}\""
                    : $"{tool.AdditionalArguments} \"{targetDirectory}\"";

                var process = new ProcessStartInfo
                {
                    FileName = tool.ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                Process.Start(process);

                return ActionResult.Success();
            }
            catch(Exception ex)
            {
                return ActionResult.Fail($"Error launching {tool.DisplayName}", ex, cmdLauncherClassName);
            }
        }
    }
}