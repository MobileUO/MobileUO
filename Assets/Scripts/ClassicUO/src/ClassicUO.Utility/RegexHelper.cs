using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ClassicUO.Utility
{
    public static class RegexHelper
    {
        private static ConcurrentDictionary<string, Regex> _regexes = new();

        public static Regex GetRegex(string pattern, RegexOptions options = RegexOptions.Compiled)
        {
            if ((options & RegexOptions.Compiled) == 0)
                options |= RegexOptions.Compiled;

            return _regexes.GetOrAdd(pattern, p => new Regex(p, options));
        }
    }
}