using System.Text.RegularExpressions;

namespace GenAI.Server.Jinja
{
    public static class Utils
    {
        public static bool IsWord(char c)
        {
            return Regex.IsMatch(c.ToString(), @"\w", RegexOptions.Compiled);
        }

        public static bool IsInteger(char c)
        {
            return Regex.IsMatch(c.ToString(), @"\d", RegexOptions.Compiled);
        }
    }
}
