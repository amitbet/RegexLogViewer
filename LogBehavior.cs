using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Globalization;
using System.Xml.Serialization;
using System.Xml;

namespace LogViewer
{
    [XmlInclude(typeof(DefaultLogBehavior))]
    public class LogBehavior
    {
        Dictionary<string, LogGridColDefinition> m_colDefaultColumns = new Dictionary<string, LogGridColDefinition>();

        //public static string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss,fff";

        public LogBehavior()
        {
            m_colDefaultColumns.Add("key", new LogGridColDefinition { Header = "No.", Name = "EntryNumber", LogViewerDataMemberName = LogViwerDataFieldName.Key });
            m_colDefaultColumns.Add("date", new LogGridColDefinition { Header = "EntryTime", Name = "EntryTime", LogViewerDataMemberName = LogViwerDataFieldName.EntryTime });
            m_colDefaultColumns.Add("level", new LogGridColDefinition { Header = "Level", Name = "Level", LogViewerDataMemberName = LogViwerDataFieldName.LogLevel });
            m_colDefaultColumns.Add("info", new LogGridColDefinition { Header = "Info", Name = "Info", LogViewerDataMemberName = LogViwerDataFieldName.Info });
            m_colDefaultColumns.Add("exinfo", new LogGridColDefinition { Header = "ExInfo", Name = "ExInfo", LogViewerDataMemberName = LogViwerDataFieldName.ErrorInfo });
            m_colDefaultColumns.Add("thread", new LogGridColDefinition { Header = "ThreadName", Name = "ThreadName", LogViewerDataMemberName = LogViwerDataFieldName.ThreadName });
            m_colDefaultColumns.Add("user", new LogGridColDefinition { Header = "User", Name = "User", LogViewerDataMemberName = LogViwerDataFieldName.UserName });
            m_colDefaultColumns.Add("machine", new LogGridColDefinition { Header = "Machine", Name = "Machine", LogViewerDataMemberName = LogViwerDataFieldName.ComputerName });
            m_colDefaultColumns.Add("sourcefile", new LogGridColDefinition { Header = "SourceLogFile", Name = "SourceLogFile", LogViewerDataMemberName = LogViwerDataFieldName.SourceLogFile });
        }

        int m_intGrade = 0;
        //indicates how good this parser is after testing it against the first log file in.
        public int Grade
        {
            get { return m_intGrade; }
            set { m_intGrade = value; }
        }

        HashSet<string> m_colGridCols = new HashSet<string>();
        [XmlIgnore]
        public HashSet<string> GridCols
        {
            get
            {
                return m_colGridCols;
            }
            set
            {
                m_colGridCols = value;
            }
        }


        string m_strDateFormat = DateTimeFormatInfo.CurrentInfo.FullDateTimePattern;

        //the dateTime format for this behavior
        public string DateFormat
        {
            get { return m_strDateFormat; }
            set { m_strDateFormat = value; }
        }

        private Regex m_regParser = null;
        [XmlIgnore]
        public Regex ParserRegex
        {
            get { return m_regParser; }
            set
            {
                m_regParserPattern = value.ToString();
                m_enmParserOptions = value.Options;
                //FillRegexFlagsString(value.Options);
                FillColumnsFromRegexPattern(m_regParserPattern);
                m_regParser = new Regex(m_regParserPattern, m_enmParserOptions);
            }
        }

        string m_regParserPattern = null;
        [XmlIgnore]
        public string ParserRegexPattern
        {
            get
            {
                return m_regParserPattern;
            }
            set
            {
                m_regParserPattern = value;
                m_regParser = new Regex(value, m_enmParserOptions);
                FillColumnsFromRegexPattern(value);
            }
        }

        Regex m_regGroupFinder = new Regex(@"\(\?\<(?<groupname>\w*)\>", RegexOptions.Compiled);
        private void FillColumnsFromRegexPattern(string strPattern)
        {
            HashSet<string> groups = new HashSet<string>();

            //get all named groups in the reg
            foreach (Match match in m_regGroupFinder.Matches(strPattern))
            {
                string strGroup = match.Groups["groupname"].Value.ToLower();
                if (m_colDefaultColumns.ContainsKey(strGroup))
                    groups.Add(strGroup);
            }

            GridCols = groups;
        }


        /// <summary>
        /// this is used only for serialization
        /// </summary>
        [XmlElement("ParserRegexPatternCData")]
        public XmlCDataSection ParserRegexPatternCData
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                return doc.CreateCDataSection(m_regParserPattern);
            }
            set
            {
                m_regParserPattern = value.Value.Trim(" \n\r".ToCharArray());
                m_regParser = new Regex(m_regParserPattern, m_enmParserOptions);
                FillColumnsFromRegexPattern(m_regParserPattern);
            }
        }

        RegexOptions m_enmParserOptions = RegexOptions.None;
        [XmlIgnore]
        public RegexOptions ParserRegexOptions
        {
            get { return m_enmParserOptions; }
            set
            {

                m_enmParserOptions = value;
                if (m_regParserPattern != null)
                    m_regParser = new Regex(m_regParserPattern, m_enmParserOptions);

            }
        }

        /// <summary>
        /// used to parse xml config data, allowing a better user experiance (such as ,; upper/lower case)
        /// </summary>
        public String ParserRegexOptionsString
        {
            get { return ParserRegexOptions.ToString(); }
            set
            {
                ParserRegexOptions = RegexOptions.None;
                string[] parts = value.Split(" ,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string[] names = Enum.GetNames(typeof(RegexOptions));
                Dictionary<string, RegexOptions> namesToOptions = new Dictionary<string, RegexOptions>();
                foreach (string name in names)
                {
                    namesToOptions.Add(name.ToLower(), (RegexOptions)Enum.Parse(typeof(RegexOptions), name));
                }
                foreach (string part in parts)
                {
                    string lowpart = part.ToLower();
                    if (namesToOptions.ContainsKey(lowpart))
                    {
                        ParserRegexOptions |= namesToOptions[lowpart];
                    }
                }
            }
        }


        Action<DataGridView> m_actCreateGridCols = null;
        [XmlIgnore]
        public Action<DataGridView> CreateGridCols
        {
            get
            {
                if (m_actCreateGridCols == null)
                {
                    m_actCreateGridCols = GetCreationActionFromGridCols();
                }
                return m_actCreateGridCols;

            }
            set { m_actCreateGridCols = value; }
        }

        /// <summary>
        /// choose column creation actions from the list of known columns, by using the list of chosen columns in the configuration
        /// </summary>
        /// <returns></returns>
        private Action<DataGridView> GetCreationActionFromGridCols()
        {
            List<LogGridColDefinition> colColumnDefinitions = new List<LogGridColDefinition>();
            colColumnDefinitions.Add(m_colDefaultColumns["key"]);

            //find matches
            foreach (string group in m_colDefaultColumns.Keys)
            {
                if (GridCols.Contains(group.ToLower()))
                {
                    colColumnDefinitions.Add(m_colDefaultColumns[group.ToLower()]);
                }
            }
            //foreach (string group in GridCols)
            //{
            //    if (m_colDefaultColumns.ContainsKey(group.ToLower()))
            //    {
            //        colColumnDefinitions.Add(m_colDefaultColumns[group.ToLower()]);
            //    }
            //}

            colColumnDefinitions.Add(m_colDefaultColumns["sourcefile"]);

            return CreateGridColumnActionFromColDefenitionList(colColumnDefinitions);
        }

        public static Action<DataGridView> CreateGridColumnActionFromColDefenitionList(List<LogGridColDefinition> p_colColDefs)
        {

            Action<DataGridView> CreateGridCols = (dataGridView) =>
            {
                foreach (LogGridColDefinition def in p_colColDefs)
                {
                    System.Windows.Forms.DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                    col.DataPropertyName = def.LogViewerDataMemberName.ToString();
                    col.HeaderText = def.Header;
                    col.Name = def.Name;
                    col.ReadOnly = true;

                    //if this is the dateTime column, format it accordingly
                    if (def.LogViewerDataMemberName == LogViwerDataFieldName.EntryTime)
                        col.DefaultCellStyle.Format = MainForm.DATE_TIME_FORMAT;

                    dataGridView.Columns.Add(col);
                }
            };
            return CreateGridCols;
        }
        string m_strBehaviorName = "-NoName-";

        public string BehaviorName
        {
            get { return m_strBehaviorName; }
            set { m_strBehaviorName = value; }
        }
        public override string ToString()
        {
            return BehaviorName;
        }
    }
}
