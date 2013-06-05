using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace LogViewer
{

    public class DefaultLogBehavior : LogBehavior
    {
        public DefaultLogBehavior()
            : this("Default",
                new Regex("(?<info>.*)\n", RegexOptions.Multiline | RegexOptions.Compiled),
                "yyyy-MM-dd HH:mm:ss,fff")
        {
        }

        public DefaultLogBehavior(string p_strName, Regex p_regParser, string p_strDateFormat)
        {
            BehaviorName = p_strName;
            ParserRegex = p_regParser;
            DateFormat = p_strDateFormat;
        }


    }//class
}//namespace
