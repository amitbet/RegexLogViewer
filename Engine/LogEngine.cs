using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LogViewer.GUI;

namespace LogViewer
{
    public class LogEngine
    {
        Regex m_regVSParsingReg = new Regex(@"(?:(?<exinfo>(?<file>[\w\d\s\.]*)\((?<line>\d{1,5}),(?<column>\d{1,5})\)):\s(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])|(?:(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Dictionary<string, long> _colWatchedFiles = new Dictionary<string, long>();
        private Dictionary<string, WildCards> _colLineFilter = new Dictionary<string, WildCards>();
        private Dictionary<string, LogBehavior> _behaviorsForFiles = new Dictionary<string, LogBehavior>();
        private List<LogEntry> _dvMainView = new List<LogEntry>();
        Encoding _objEncoding = Encoding.ASCII;
        List<LogBehavior> _colBehaviors = new List<LogBehavior>();
        LogBehavior _objChosenBehavior = LogBehavior.AutoDetectBehaviour;
        private List<LogEntry> _dtlogEntries;
        private string _strBehaviorConfigFileName = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "BehaviorConfig.xml");

        public LogEngine()
        {
            GlobalLineFilter = null;
        }

        internal Encoding CurrentEncoding
        {
            get
            {
                if (_objEncoding != Encoding.ASCII)
                    return _objEncoding;

                string encName = ConfigurationManager.AppSettings["Encoding"];
                try
                {
                    if (!string.IsNullOrEmpty(encName))
                    {
                        _objEncoding = Encoding.GetEncoding(encName.Trim());
                        return _objEncoding;
                    }
                }
                catch { }

                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                int codePage = cultureInfo.TextInfo.ANSICodePage;
                _objEncoding = codePage.Equals(0) ?
                                        Encoding.UTF8 :
                                        Encoding.GetEncoding(codePage);
                return _objEncoding;
            }
        }

        /// <summary>
        /// parse all logs according to the given behavior
        /// </summary>
        /// <param name="b"></param>
        internal void ReparseAllLogs(LogBehavior b)
        {
            _dtlogEntries.Clear();
            _objChosenBehavior = b;

            LogBehavior behavior = _objChosenBehavior;

            foreach (string file in WatchedFiles)
            {
                if (IsAutoDetectMode)
                {
                    behavior = FindCorrectBehaviorForFile(file);
                }

                ParseLogFileRegExp(file, 0, behavior);
            }

        }

        /// <summary>
        /// filters the main view according to the given filter
        /// </summary>
        /// <param name="filter"></param>
        internal void RefreshFilter(LogFilter filter)
        {
            _dvMainView.Clear();
            _dvMainView.AddRange(ApplyFilterToEntryList(_dtlogEntries, filter));
        }

        /// <summary>
        /// log viewer engine intialization routine
        /// </summary>
        public void InitEngine()
        {
            if (File.Exists(BehaviorConfigFileName))
            {
                LoadBehaviorConfig();
            }
            else
            {
                CreateDefaultBehaviorCollection();
            }
            _colBehaviors.Add(LogBehavior.AutoDetectBehaviour);

            _dtlogEntries = new List<LogEntry>();
            _dvMainView.AddRange(_dtlogEntries);
        }

        /// <summary>
        /// creates a default behavior configuration (used if no behavior file was found)
        /// </summary>
        private void CreateDefaultBehaviorCollection()
        {
            _colBehaviors.Add(new DefaultLogBehavior());

            _colBehaviors.Add(new LogBehavior()
                {
                    BehaviorName = "VisualStudio",
                    ParserRegex = m_regVSParsingReg,
                    DateFormat = "dd/MM HH:mm:ss,fff",
                    CreateGridCols = LogBehavior.CreateGridColumnActionFromColDefenitionList(
                        new List<LogGridColDefinition>()
                            {
                                new LogGridColDefinition
                                    {
                                        Header = "No.",
                                        Name = "EntryNumber",
                                        LogViewerDataMemberName = LogViwerDataFieldName.Key
                                    },
                                new LogGridColDefinition
                                    {
                                        Header = "Level",
                                        Name = "Level",
                                        LogViewerDataMemberName = LogViwerDataFieldName.LogLevel
                                    },
                                new LogGridColDefinition
                                    {
                                        Header = "Info",
                                        Name = "Info",
                                        LogViewerDataMemberName = LogViwerDataFieldName.Info
                                    },
                                new LogGridColDefinition
                                    {
                                        Header = "ExInfo",
                                        Name = "ExInfo",
                                        LogViewerDataMemberName = LogViwerDataFieldName.ErrorInfo
                                    },
                                new LogGridColDefinition
                                    {
                                        Header = "SourceLogFile",
                                        Name = "SourceLogFile",
                                        LogViewerDataMemberName = LogViwerDataFieldName.SourceLogFile
                                    }
                            })
                });
        }

        /// <summary>
        /// serializes behaviors to xml file
        /// </summary>
        public void SaveBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenWrite(BehaviorConfigFileName);
            ser.Serialize(s, _colBehaviors);
            s.Close();
        }

        /// <summary>
        /// load behaviors from xml serialization
        /// </summary>
        public void LoadBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenRead(BehaviorConfigFileName);
            _colBehaviors = (List<LogBehavior>)ser.Deserialize(s);
            s.Close();
        }

        /// <summary>
        /// checks if line matches the line filter (used in batch collection)
        /// </summary>
        /// <param name="strLogFile"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        internal bool IsLineInFilter(string strLogFile, string line)
        {
            //if we have a line filter, use it.
            if (_colLineFilter.ContainsKey(strLogFile))
            {
                //check line with filter, and return null if it doesn't match
                WildCards wc = _colLineFilter[strLogFile];
                if (wc != null && wc.Count > 0 && !wc.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

                //if we have a global line filter use it..
            else if (GlobalLineFilter != null)
            {
                //check line with filter, and return null if it doesn't match
                if (GlobalLineFilter != null &&
                    !GlobalLineFilter.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// discovers the correct parser to use for this log file
        /// </summary>
        /// <param name="p_strLogFileHead"></param>
        /// <returns></returns>
        LogBehavior FindCorrectBehaviorForFileByHeader(string p_strLogFileHead)
        {
            List<LogBehavior> colGoodBehaviors = new List<LogBehavior>();
            LogBehavior defBhavior = null;
            //parse with each parser in turn - find parser with most matches
            foreach (LogBehavior lb in _colBehaviors)
            {
                if (lb == LogBehavior.AutoDetectBehaviour)
                    continue;

                //don't check the default, cause it'll win every time!
                if (lb.BehaviorName == "Default")
                {
                    defBhavior = lb;
                    continue;
                }
                int count = lb.ParserRegex.Matches(p_strLogFileHead).Count;
                if (count > 0)
                {
                    //set grade
                    lb.Grade = count;
                    //add to collection
                    colGoodBehaviors.Add(lb);
                }
            }

            if (colGoodBehaviors.Count == 0)
                return defBhavior; //no specific parser worked
            else
            {
                //return the best parser (produced most matches with log head)
                int intMaxGrade = 0;
                LogBehavior chosen = null;
                foreach (LogBehavior lb in colGoodBehaviors)
                {
                    if (intMaxGrade < lb.Grade)
                    {
                        chosen = lb;
                        intMaxGrade = lb.Grade;
                    }
                }
                return chosen;
            }
        }

        /// <summary>
        /// parses log file from the given position and on
        /// </summary>
        /// <param name="p_strLogFileName"></param>
        /// <param name="p_intStartPos"></param>
        /// <returns></returns>
        public List<LogEntry> ParseLogFileRegExp(string p_strLogFileName, long p_intStartPos, LogBehavior behaviorForCurrentFile)
        {
            //if (IsAutoDetectMode)
            //    behaviorForCurrentFile = _behaviorsForFiles[p_strLogFileName];
            List<LogEntry> newLines = new List<LogEntry>();
            //m_dtlogEntries.BeginLoadData();
            long lngFileTotalBytes = 0;
            long lngFileTotalBytesRead = 0;
            long progressbytes = 0;

            try
            {
                using (FileStream objFStream = new FileStream(p_strLogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    objFStream.Position = p_intStartPos;
                    using (StreamReader objReader = new StreamReader(objFStream, CurrentEncoding))
                    {

                        string strAllText = objReader.ReadToEnd();
                        lngFileTotalBytesRead = strAllText.Length;
                        lngFileTotalBytes = objFStream.Position;
                        //m_drPrevRow = drRow;
                        MatchCollection colMatches = behaviorForCurrentFile.ParserRegex.Matches(strAllText);



                        foreach (Match match in colMatches)
                        {
                            int increment = (int)((double)lngFileTotalBytesRead / (double)colMatches.Count);
                            progressbytes += increment;
                            ProgressBarManager.IncrementProgress(increment);

                            if (!IsLineInFilter(p_strLogFileName, match.Value))
                            {
                                continue;
                            }

                            LogEntry drRow = new LogEntry();
                            string strDate = match.Groups["date"].Value;
                            string strThread = match.Groups["thread"].Value;
                            string strLevel = match.Groups["level"].Value;
                            string strComputer = match.Groups["computer"].Value;
                            string strUser = match.Groups["user"].Value;
                            string strInfo = match.Groups["info"].Value;
                            string strInfoEx = match.Groups["exinfo"].Value.TrimEnd();
                            string strMachine = match.Groups["machine"].Value;

                            LogEntry row = new LogEntry();
                            drRow.ErrorInfo = strInfoEx;
                            //"14/11 16:39:03,236"
                            DateTime dtmTemp = DateTime.Now;
                            bool ok = System.DateTime.TryParseExact(strDate, behaviorForCurrentFile.DateFormat, Application.CurrentCulture, System.Globalization.DateTimeStyles.None, out dtmTemp);

                            if (!ok)
                                ok = System.DateTime.TryParseExact(strDate + "0", behaviorForCurrentFile.DateFormat, Application.CurrentCulture, System.Globalization.DateTimeStyles.None, out dtmTemp);

                            if (!ok)
                                ok = System.DateTime.TryParseExact(strDate + "00", behaviorForCurrentFile.DateFormat, Application.CurrentCulture, System.Globalization.DateTimeStyles.None, out dtmTemp);

                            if (!ok)
                                drRow.EntryTime = DateTime.MinValue;
                            else
                                drRow.EntryTime = dtmTemp;
                            if (strLevel.ToLower().StartsWith("trac"))
                                strLevel = "TRACE";
                            else if (strLevel.ToLower().StartsWith("inf"))
                                strLevel = "INFO";
                            else if (strLevel.ToLower().StartsWith("deb"))
                                strLevel = "DEBUG";
                            else if (strLevel.ToLower().StartsWith("err"))
                                strLevel = "ERROR";
                            else if (strLevel.ToLower().StartsWith("fat"))
                                strLevel = "FATAL";
                            else if (strLevel.ToLower().StartsWith("warn"))
                                strLevel = "WARN";

                            //drRow.EntryTime = DateTime.Parse(strDate);
                            drRow.ComputerName = strComputer;
                            drRow.UserName = strUser;
                            drRow.ThreadName = strThread;
                            drRow.Info = strInfo;
                            drRow.LogLevel = strLevel;
                            drRow.SourceLogFile = Path.GetFileName(p_strLogFileName);
                            drRow.UserName = strUser;
                            drRow.ComputerName = strMachine;
                            if (p_strLogFileName.StartsWith("\\\\"))
                            {
                                drRow.ServerName = p_strLogFileName.Substring(2, p_strLogFileName.IndexOf('\\', 3) - 2);
                            }
                            else
                                drRow.ServerName = "localhost";

                            //if (IsLineInFilter(p_strLogFileName, drRow.Info + " " + drRow.RowError))
                            //m_dtlogEntries.AddLogEntriesRow(drRow);


                            //if (IsLineInFilter(p_strLogFileName, match.Value))
                            //{

                            _dtlogEntries.Add(drRow);
                            newLines.Add(drRow);
                            drRow.Key = _dtlogEntries.Count;
                            //}

                        }

                        ProgressBarManager.IncrementProgress(lngFileTotalBytesRead - progressbytes);
                    }
                }
            }
            catch
            {
                //only open a file in notepad if it's a new file causing the problem...
                if (p_intStartPos == 0)
                {
                    FRMVanishingAlert.ShowForm(2, "Wrong Log Format", "Not a known log,\r\n\rOpening Notepad", "", "", 0, 0, true, FormStartPosition.Manual, false);

                    string strWinDir = Environment.GetEnvironmentVariable("SystemRoot");
                    Process.Start(strWinDir + "\\notepad.exe", p_strLogFileName);
                    //this.Visible = false;
                }
                return new List<LogEntry>();
            }
            _colWatchedFiles[p_strLogFileName] = lngFileTotalBytes;

            return newLines;
        }

        /// <summary>
        /// indicates if we are using autodetect mode (each added log file will be tested to find the best parser)
        /// </summary>
        internal bool IsAutoDetectMode
        {
            get { return (_objChosenBehavior == null || _objChosenBehavior == LogBehavior.AutoDetectBehaviour); }
        }

        /// <summary>
        /// this view is the list which should be displayed on grid (after filtering)
        /// </summary>
        public List<LogEntry> MainView
        {
            get { return _dvMainView; }
            set { _dvMainView = value; }
        }

        /// <summary>
        /// the chosen behavior which will be used for all logs, if we are in AutoDetect mode, this will be set to AutoDetect behavior
        /// </summary>
        public LogBehavior ChosenBehavior
        {
            set
            {
                _objChosenBehavior = value;
            }
            get { return _objChosenBehavior; }
        }

        /// <summary>
        /// the file name where the behavior configuration will be written
        /// </summary>
        public string BehaviorConfigFileName
        {
            get { return _strBehaviorConfigFileName; }
            set { _strBehaviorConfigFileName = value; }
        }

        /// <summary>
        /// a collection of wildcards which filter incoming lines (used in batch mode)
        /// </summary>
        public WildCards GlobalLineFilter { get; set; }

        /// <summary>
        /// discovers the correct parser to use for this log file by reading the first X lines from the file and trying all parsers on them
        /// the parser selected will be the one which matches the most lines 
        /// the number of lines can be configured by changing "NumLogLinesForAutoDetect" in the configuration file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal LogBehavior FindCorrectBehaviorForFile(string file)
        {
            if (_behaviorsForFiles.ContainsKey(file))
            {
                return _behaviorsForFiles[file];
            }

            LogBehavior behaviorForCurrentFile = null;

            //if this is the firs file - we need to select a behavior for parsing the logs and formatting gridCols
            int intNumLines = 50;
            string linesForDetection = ConfigurationManager.AppSettings["NumLogLinesForAutoDetect"];
            if (linesForDetection == null || !int.TryParse(linesForDetection, out intNumLines))
                intNumLines = 50;

            if (_objChosenBehavior == null || IsAutoDetectMode)
            {
                StringBuilder sbHeader = new StringBuilder();
                FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (StreamReader sr = new StreamReader(fs))
                {
                    //get the first lines of the file
                    for (int i = 0; i < intNumLines; i++)
                    {
                        string line = sr.ReadLine();
                        if (line == null)
                            break;

                        sbHeader.AppendLine(line);
                    }
                }
                fs.Close();
                fs.Dispose();
                //check the first lines of the file to find out which type of log it is
                behaviorForCurrentFile = FindCorrectBehaviorForFileByHeader(sbHeader.ToString());

                _behaviorsForFiles[file] = behaviorForCurrentFile;
            }
            return behaviorForCurrentFile;
        }

        /// <summary>
        /// removes a log file, along with all it's lines
        /// </summary>
        /// <param name="file"></param>
        internal void RemoveLogFile(string file)
        {
            RemoveFileEntries(file);
            _colWatchedFiles.Remove(file);
        }

        /// <summary>
        /// adds a log file to the viewer (parsing will be done here)
        /// </summary>
        /// <param name="file"></param>
        internal void AddLogFile(string file)
        {
            LogBehavior behaviorForCurrentFile = null;
            if (IsAutoDetectMode)
                behaviorForCurrentFile = FindCorrectBehaviorForFile(file);
            else
                behaviorForCurrentFile = ChosenBehavior;

            ParseLogFileRegExp(file, 0, behaviorForCurrentFile);
            //_colWatchedFiles.Add(file, new FileInfo(file).Length);
        }

        /// <summary>
        /// process a directory in batch mode, taking the first X files which match the filename filters and collecting lines which pass the line filters
        /// </summary>
        /// <param name="colExcludeList">gs which must not be in a collected filename</param>
        /// <param name="colIncludeList">strings which must be in a collected filename</param>
        /// <param name="cardsLineFilter">wild cards which are used to filter the lines which are collected</param>
        /// <param name="intHistory">the number of files to collect from dir (the newest matching files will be collected) </param>
        /// <param name="directory">the directory to process</param>
        internal void ProcessLogDirectory(List<string> colExcludeList, List<string> colIncludeList, WildCards cardsLineFilter, int intHistory, string directory)
        {
            Dictionary<string, DateTime> colFileTimes = new Dictionary<string, DateTime>();

            //get all files from server dir
            string dir = directory.Trim();
            if (!dir.EndsWith("\\"))
                dir += "\\";

            if (!Directory.Exists(dir))
                return;
            try
            {
                string[] files = Directory.GetFiles(dir);
                List<string> colLogFiles = new List<string>();

                //get all included files into a list
                bool badFile = false;
                foreach (string file1 in files)
                {
                    badFile = false;
                    //include files
                    foreach (string inc in colIncludeList)
                    {
                        if (!file1.ToLower().Contains(inc.ToLower()))
                        {
                            badFile = true;
                            break;
                        }
                    }

                    //excluded files
                    if (!badFile)
                    {
                        foreach (string exc in colExcludeList)
                        {
                            if (file1.ToLower().Contains(exc.ToLower()))
                            {
                                badFile = true;
                                break;
                            }
                        }
                    }

                    //get all good files into a list
                    if (!badFile)
                    {
                        colLogFiles.Add(file1);
                        colFileTimes.Add(file1, new FileInfo(file1).LastWriteTime);
                    }
                }

                //sort the good files by last modified date
                colLogFiles.Sort((Comparison<string>)delegate(string a, string b)
                {
                    DateTime dt1 = colFileTimes[a];//GetFileLastModifiedTime(a);
                    DateTime dt2 = colFileTimes[b];//GetFileLastModifiedTime(b);
                    if (dt1 > dt2)
                        return -1;
                    if (dt1 == dt2)
                        return 0;
                    else return 1;
                });

                List<string> colFilesForCollection = new List<string>();
                long intTotalDirLogBytes = 0;
                for (int i = 0; i < intHistory; ++i)
                {
                    if (colLogFiles.Count > i)
                    {
                        string logFile = colLogFiles[i];
                        if (_colLineFilter.ContainsKey(logFile))
                            _colLineFilter[logFile] = cardsLineFilter;
                        else
                            _colLineFilter.Add(logFile, cardsLineFilter);
                        colFilesForCollection.Add(logFile);
                        intTotalDirLogBytes += (new FileInfo(logFile)).Length;
                    }
                }

                ProgressBarManager.FullProgressBarValue = intTotalDirLogBytes;
                ProgressBarManager.SetLableText("loading: " + dir);
                colFilesForCollection.ForEach(f => AddLogFile(f));
                Thread.Sleep(200);
                //Thread t = new Thread((ThreadStart)delegate
                //{
                //    Thread.Sleep(700);
                //    ProgressBarManager.ClearProgress();
                //});
                //t.Start();
                ProgressBarManager.ClearProgress();
                //ProgressBarManager.CloseProgress();
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// removes all log entries for the given file
        /// </summary>
        /// <param name="logFileName"></param>
        internal void RemoveFileEntries(string logFileName)
        {
            List<LogEntry> colRowsToRemove = new List<LogEntry>();
            foreach (LogEntry row in _dtlogEntries)
            {
                if (Path.GetFileName(logFileName) == row.SourceLogFile)
                    colRowsToRemove.Add(row);
            }
            foreach (LogEntry row in colRowsToRemove)
            {
                _dtlogEntries.Remove(row);
            }
        }

        /// <summary>
        /// exports the given list into csv file
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <param name="p_colItems"></param>
        private void ExportToCsvFile(string csvFileName, List<RowPrototype> p_colItems)
        {
            //sanity check
            if (p_colItems.Count == 0)
                return;

            var row = p_colItems[0].MyDataRow;
            //DataTable table = row.Table;
            try
            {
                //export
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), CurrentEncoding))
                {
                    var colNames = typeof(LogEntry).GetProperties().Select(p => p.Name);
                    //export headings
                    foreach (string colName in colNames)
                    {
                        //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                        wr.Write("\"" + colName.Replace("\"", "\"\"") + "\",");
                    }
                    wr.Write("\"Count\"");
                    wr.WriteLine();

                    //export data
                    foreach (RowPrototype rowP in p_colItems)
                    {
                        var drRow = rowP.MyDataRow;
                        foreach (string col in colNames)
                        {
                            string val = drRow.GetType().GetProperty(col).GetValue(drRow, null).ToString();
                            //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                            wr.Write("\"" + val.Replace("\"", "\"\"") + "\",");
                        }
                        wr.Write("\"" + rowP.MyCount.ToString() + "\",");
                        wr.WriteLine();
                    }
                }
                MessageBox.Show("export done successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("a little problem sir.." + ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// creats a report based on counting repetitions of the same lines and saves it to a cvs file
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <param name="method"></param>
        internal void GenerateReport(string csvFileName, ReportGenMethod method)
        {
            List<RowPrototype> colProts = new List<RowPrototype>();

            foreach (var drRow in MainView)
            {
                string strRowStr = drRow.Info + drRow.ErrorInfo;
                RowPrototype objFoundPrototype = null;

                //test each row on the prototypes we have seen until now
                RowPrototype currRow = new RowPrototype(drRow);
                foreach (RowPrototype prototype in colProts)
                {
                    bool isMatch = false;
                    if (method == ReportGenMethod.ByTrigram)
                        isMatch = prototype.CheckMatchByTrigrams(currRow);
                    else if (method == ReportGenMethod.ByStringCompare)
                        isMatch = prototype.CheckMatchByStringCompare(currRow);

                    if (isMatch)
                    {
                        ++prototype.MyCount;
                        objFoundPrototype = prototype;
                        break;
                    }
                }
                //new prototype found
                if (objFoundPrototype == null)
                {
                    RowPrototype prot1 = new RowPrototype(drRow);
                    colProts.Add(prot1);
                }
            }

            colProts.Sort((Comparison<RowPrototype>)delegate(RowPrototype a, RowPrototype b)
            {
                return -a.MyCount.CompareTo(b.MyCount);
            });

            ExportToCsvFile(csvFileName, colProts);
        }

        //move to engine


        //move to engine
        internal List<LogEntry> ApplyFilterToEntryList(List<LogEntry> entries, LogFilter filter)
        {
            IEnumerable<LogEntry> dvMainView = entries;
            string strFilter = "";
            if (!string.IsNullOrEmpty(filter.Level) && filter.Level != "ALL")
                dvMainView = dvMainView.Where(r => r.LogLevel.Equals(filter.Level, StringComparison.InvariantCultureIgnoreCase));

            if (!string.IsNullOrEmpty(filter.TextFilter))
                dvMainView = dvMainView.Where(r => r.ErrorInfo.Contains(filter.TextFilter) || r.Info.Contains(filter.TextFilter));

            if (!string.IsNullOrEmpty(filter.User))
                dvMainView = dvMainView.Where(r => r.UserName.Contains(filter.User));

            if (!string.IsNullOrEmpty(filter.Thread))
                dvMainView = dvMainView.Where(r => r.ThreadName.Contains(filter.Thread));

            if (filter.From != null && filter.To != null)
                dvMainView = dvMainView.Where(r => r.EntryTime >= filter.From && r.EntryTime <= filter.To);

            return dvMainView.ToList();
        }

        /// <summary>
        /// returns a LogBehavior object for the behavior which name is provided
        /// </summary>
        /// <param name="behaviorName"></param>
        /// <returns></returns>
        internal LogBehavior GetBehaviorByName(string behaviorName)
        {
            return _colBehaviors.FirstOrDefault(b => b.BehaviorName.Equals(behaviorName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// the log behaviors
        /// </summary>
        internal IEnumerable<LogBehavior> Behaviors
        {
            get { return _colBehaviors; }
        }

        /// <summary>
        /// list of watched FileNames
        /// </summary>
        internal List<string> WatchedFiles
        {
            get { return _colWatchedFiles.Keys.ToList(); }
        }

        /// <summary>
        /// clear all the content from the engine
        /// </summary>
        internal void Clear()
        {
            _dtlogEntries.Clear();
            _colWatchedFiles.Clear();
        }

        /// <summary>
        /// clears only the entries, leaving the log files, this wipes existing lines as live listening continues to add more
        /// </summary>
        internal void ClearAllEntries()
        {
            _dtlogEntries.Clear();
        }

        /// <summary>
        /// returns the new lines added to the file since last read, filters lines by the given filter
        /// </summary>
        /// <param name="file"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        internal List<LogEntry> GetNewLinesForFile(string file, LogFilter filter)
        {
            List<LogEntry> filteredNewLines = new List<LogEntry>();
            long lngPrevLength = _colWatchedFiles[file];

            if (File.Exists(file))
            {
                long lngFileLength = (long)new FileInfo(file).Length;

                //file was swapped, and a new file was created => smaller filesize
                if (lngPrevLength > lngFileLength)
                {
                    //we will adjust our counters to keep track with the file.
                    //(the following code will take care of the rest as ususal)
                    if (_colWatchedFiles.ContainsKey(file))
                        _colWatchedFiles[file] = 0;

                    lngPrevLength = 0;
                }

                //file changed (more entries were added)
                if (lngPrevLength < lngFileLength)
                {
                    LogBehavior behavior = ChosenBehavior;
                    if (IsAutoDetectMode)
                        behavior = FindCorrectBehaviorForFile(file);

                    var newLines = ParseLogFileRegExp(file, lngPrevLength, behavior);

                    filteredNewLines = ApplyFilterToEntryList(newLines, filter);
                    MainView.AddRange(filteredNewLines);
                }


            }
            return filteredNewLines;
        }
    }
}
