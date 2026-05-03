using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class CommanderLauncher
    {
        /// <summary>
        /// Launches the configured file manager with the given target directory with optional arguments.
        /// </summary>
        public static bool Launch(string targetDirectory, ToolConfig tool, PluginInitContext context)
        {
            if (string.IsNullOrWhiteSpace(tool.ExecutablePath))
            {
                context.API.ShowMsg(
                    $"{tool.DisplayName} not configured",
                    $"Please configure it in the settings.",
                    "Images\\app.png");
                return true;
            }

            if (!File.Exists(tool.ExecutablePath))
            {
                context.API.ShowMsg(
                    $"{tool.DisplayName} executable not found",
                    $"Pleae check the configuration in the settings.",
                    "Images\\app.png");
                return true;
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
            }
            catch (Exception ex)
            {
                context.API.ShowMsg(
                    "Error Launching Application",
                    ex.Message,
                    "Images\\app.png");
            }

            return true;
        }
    }
}