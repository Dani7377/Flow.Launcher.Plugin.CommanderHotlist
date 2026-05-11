using System.Text;

namespace Flow.Launcher.Plugin.CommanderHotlist
{
    internal static class StringUtilities
    {
        /// <summary>
        /// Processes ampersands in the string to match how Total Commander and Double Commander display them. Examples:
        /// - "Des&ktop" is displayed as "Desktop" because "k" is an access key
        /// - "Des&&ktop" is displayed as literal "Des&ktop" and doesn't have any access key
        /// </summary>
        public static string RemoveMnemonicsFromEntryName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder(input.Length);
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];
                if (c == '&')
                {
                    if (i + 1 < input.Length && input[i + 1] == '&')
                    {
                        result.Append('&');
                        i += 2;
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    result.Append(c);
                    i += 1;
                }
            }
            return result.ToString();
        }
    }
}
