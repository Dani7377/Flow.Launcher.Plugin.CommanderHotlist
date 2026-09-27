using System;
using System.IO;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    /// <summary>
    /// Readonly INI reader used to search values like TC's redirection attributes (e.g. RedirectSection)
    /// </summary>
    internal static class IniReader
    {
        /// <summary>
        /// Returns the value of a key inside a section in the INI or null if the file/section/key are invalid
        /// </summary>
        public static string? GetValue(string filePath, string section, string key)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch
            {
                return null;
            }

            var inSection = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed[0] == ';')
                    continue;

                // Section header
                if (trimmed[0] == '[' && trimmed[^1] == ']')
                {
                    inSection = trimmed[1..^1].Trim().Equals(section, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inSection)
                    continue;

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var candidateKey = trimmed[..separatorIndex].Trim();
                if (!candidateKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    continue;

                return CleanValue(trimmed[(separatorIndex + 1)..]);
            }

            return null;
        }

        /// <summary>
        /// Normalizes a raw INI value (trim whitespaces, remove quotes, etc)
        /// </summary>
        private static string CleanValue(string raw)
        {
            var value = raw.Trim();

            if (value.Length >= 2 && value[0] == '"')
            {
                var closingQuote = value.LastIndexOf('"');
                if (closingQuote > 0)
                    return value[1..closingQuote].Trim();
            }

            var commentIndex = value.IndexOf(';');
            if (commentIndex >= 0)
                value = value[..commentIndex];

            return value.Trim();
        }
    }
}
