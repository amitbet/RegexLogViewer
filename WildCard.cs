using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogViewer
{
    /// <summary>

    /// Represents a wildcard running on the

    /// <see cref="System.Text.RegularExpressions"/> engine.

    /// </summary>

    public class Wildcard : Regex
    {
        /// <summary>

        /// Initializes a wildcard with the given search pattern.

        /// </summary>

        /// <param name="pattern">The wildcard pattern to match.</param>

        public Wildcard(string pattern)
            : base(WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.Compiled)
        {
        }

        /// <summary>

        /// Initializes a wildcard with the given search pattern and options.

        /// </summary>

        /// <param name="pattern">The wildcard pattern to match.</param>

        /// <param name="options">A combination of one or more

        /// <see cref="System.Text.RegexOptions"/>.</param>

        public Wildcard(string pattern, RegexOptions options)
            : base(WildcardToRegex(pattern), options)
        {
        }

        /// <summary>

        /// Converts a wildcard to a regex.

        /// </summary>

        /// <param name="pattern">The wildcard pattern to convert.</param>

        /// <returns>A regex equivalent of the given wildcard.</returns>

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
             Replace("\\*", ".*").
             Replace("\\?", ".") + "$";
        }
    }

    public class WildCards
    {
        public WildCards()
        {
        }

        public WildCards(string p_strFilters)
        {
            string strFilters = p_strFilters.Trim(" ,;".ToCharArray());
            if (strFilters == "")
                strFilters = "*";

            string[] filters = strFilters.Split(";,".ToCharArray());
            foreach (string filter in filters)
                m_colWildCards.Add(new Wildcard(filter));
        }

        public void AddWildCard(string str)
        {
            m_colWildCards.Add(new Wildcard(str));
        }

        List<Wildcard> m_colWildCards = new List<Wildcard>();

        /// <summary>
        /// returns true if there's a match with one filter at least
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="strAnyString"></param>
        /// <returns></returns>
        public bool IsMatch(string strAnyString)
        {

            foreach (Wildcard filter in m_colWildCards)
            {
                
                if (filter.IsMatch(strAnyString))
                    return true;
            }
            return false;
        }
    }
}
