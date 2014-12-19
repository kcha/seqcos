using System.Text.RegularExpressions;

namespace SeqcosFilterTools.Common
{
    /// <summary>
    /// Set of static tools related to regular expression processing
    /// </summary>
    public static class RegexTools
    {
        /// <summary>
        /// Determines whether a regular expression pattern is valid.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool IsValidRegexPattern(string pattern)
        {
            try
            {
                new Regex(pattern);
                return true;
            }
            catch { }
            return false;
        }
    }
}
