using System;
using System.IO;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Resolves which file actually contains the [DirMenu] section based on the redirection parmeters (RedirectSection 
    /// or AlternateUserIni)
    /// </summary>
    internal static class TotalCommanderIniResolver
    {
        private const string ConfigurationSection = "Configuration";
        private const string DirMenuSection = "DirMenu";

        private const string AlternateUserIniKey = "AlternateUserIni";
        private const string RedirectSectionKey = "RedirectSection";

        private const string CommanderPathVariable = "%COMMANDER_PATH%";

        /// <summary>
        /// Returns the file that should be parsed for retrieving the bookmarks
        /// </summary>
        public static string ResolveDirMenuFile(string mainIniPath, string? tcExecutablePath)
        {
            if (string.IsNullOrWhiteSpace(mainIniPath))
                return mainIniPath;

            var alternateUserIni = IniReader.GetValue(mainIniPath, ConfigurationSection, AlternateUserIniKey);
            var redirectSection = IniReader.GetValue(mainIniPath, DirMenuSection, RedirectSectionKey);

            var target = ResolveTargetValue(mainIniPath, alternateUserIni, redirectSection);
            var resolved = ResolvePath(mainIniPath, target, tcExecutablePath);

            if (!File.Exists(resolved) && File.Exists(mainIniPath))
                return mainIniPath;

            return resolved;
        }

        /// <summary>
        /// Resolves the target based on the presence of RedirectSection/AlternateUserIni (all possible combinations)
        /// </summary>
        private static string ResolveTargetValue(string mainIniPath, string? alternateUserIni, string? redirectSection)
        {
            var hasAlternateIni = !string.IsNullOrWhiteSpace(alternateUserIni);

            if (redirectSection == null)
            {
                return hasAlternateIni ? alternateUserIni! : mainIniPath;
            }

            var value = redirectSection.Trim();

            if (value == "0")
                return mainIniPath;

            if (value == "1")
                return hasAlternateIni ? alternateUserIni! : mainIniPath;

            if (value.Length == 0)
                return mainIniPath;

            return value;
        }

        /// <summary>
        /// Expands TC or Windows env variables and resolves relative paths against the dir that contains the main INI file
        /// </summary>
        private static string ResolvePath(string mainIniPath, string target, string? tcExecutablePath)
        {
            var value = target.Trim();

            // Expand %COMMANDER_PATH% based on the TC's exe path that we have configured in the settings
            if (value.Contains(CommanderPathVariable, StringComparison.OrdinalIgnoreCase))
            {
                var commanderPath = GetCommanderPath(tcExecutablePath);
                if (!string.IsNullOrEmpty(commanderPath))
                    value = ReplaceOrdinalIgnoreCase(value, CommanderPathVariable, commanderPath);
            }

            value = Environment.ExpandEnvironmentVariables(value);

            if (!Path.IsPathRooted(value))
            {
                var baseDirectory = Path.GetDirectoryName(mainIniPath);
                if (!string.IsNullOrEmpty(baseDirectory))
                    value = Path.Combine(baseDirectory, value);
            }

            try
            {
                return Path.GetFullPath(value);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// %COMMANDER_PATH% points to the directory containing TOTALCMD.EXE or TOTALCMD64.EXE.
        /// Prefer the configured executable, then fall back to the COMMANDER_PATH environment variable.
        /// </summary>
        private static string? GetCommanderPath(string? tcExecutablePath)
        {
            if (!string.IsNullOrWhiteSpace(tcExecutablePath))
            {
                var directory = Path.GetDirectoryName(tcExecutablePath);
                if (!string.IsNullOrEmpty(directory))
                    return directory;
            }

            return Environment.GetEnvironmentVariable("COMMANDER_PATH");
        }

        /// <summary>
        /// Case-insensitive replacement of every occurrence of oldValue/>.
        /// </summary>
        private static string ReplaceOrdinalIgnoreCase(string input, string oldValue, string newValue)
        {
            var index = input.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                input = string.Concat(input.AsSpan(0, index), newValue, input.AsSpan(index + oldValue.Length));
                index = input.IndexOf(oldValue, index + newValue.Length, StringComparison.OrdinalIgnoreCase);
            }

            return input;
        }
    }
}
