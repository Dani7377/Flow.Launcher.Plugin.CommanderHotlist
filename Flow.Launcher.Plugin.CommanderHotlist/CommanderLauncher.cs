using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class CommanderLauncher
    {
        /// <summary>
        /// Launches the configured file manager with the given target directory with optional arguments.
        /// </summary>
        public static ActionResult Launch(string targetDirectory, ToolConfig tool, PluginInitContext context)
        {
            if (string.IsNullOrWhiteSpace(tool.ExecutablePath))
            {
                return ActionResult.Fail($"{tool.DisplayName} not configured", null);
            }

            if (!File.Exists(tool.ExecutablePath))
            {
                return ActionResult.Fail($"{tool.DisplayName} executable not found", null);
            }

            if(!Directory.Exists(targetDirectory))
            {
                return ActionResult.Fail("The selected location does not exist", null);
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
                return ActionResult.Fail($"Error launching {tool.DisplayName}", ex);
            }
        }
    }
}