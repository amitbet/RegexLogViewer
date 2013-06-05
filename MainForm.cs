using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Globalization;
using System.Configuration;
using LogViewer.GUI;

namespace LogViewer
{


    public partial class MainForm : Form
    {
        Regex m_regVSParsingReg = new Regex(@"(?:(?<exinfo>(?<file>[\w\d\s\.]*)\((?<line>\d{1,5}),(?<column>\d{1,5})\)):\s(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])|(?:(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss,fff";
        private TimeBuffer _refreshFilterTimeBuff = null;
        private EntryCard _frmCard = new EntryCard();
        PreFilterCard _frmBatchCollector = new PreFilterCard();
        public const int NUM_LATEST_FILES_TO_COLLECT = 1;
        private int _intUserSelectionKey = -1;
        private List<LogEntry> _dvMainView = new List<LogEntry>();

        #region engine
        private Dictionary<string, long> _colWatchedFiles = new Dictionary<string, long>();
        private WildCards _objGlobalLineFilter = null;
        private Dictionary<string, WildCards> _colLineFilter = new Dictionary<string, WildCards>();
        private Dictionary<string, LogBehavior> _behaviorsForFiles = new Dictionary<string, LogBehavior>();
        Encoding _objEncoding = Encoding.ASCII;
        Encoding CurrentEncoding
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
        List<LogBehavior> _colBehaviors = new List<LogBehavior>();
        LogBehavior _objChosenBehavior = null;
        private List<LogEntry> _dtlogEntries;
        private string _strBehaviorConfigFileName = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "BehaviorConfig.xml");

        //move to engine
        private void RepaseAllLogs(LogBehavior b)
        {
            _dtlogEntries.Clear();
            _objChosenBehavior = b;

            //set the grid columns
            dataGridView1.Columns.Clear();
            _objChosenBehavior.CreateGridCols(dataGridView1);

            LogBehavior behavior = _objChosenBehavior;

            foreach (string file in _colWatchedFiles.Keys.ToList())
            {
                if (IsAutoDetectMode)
                {
                    behavior = FindCorrectBehaviorForFile(file);
                }

                ParseLogFileRegExp(file, 0, behavior);
            }
        }

        //move to engine
        public void InitEngine()
        {
            if (File.Exists(_strBehaviorConfigFileName))
            {
                LoadBehaviorConfig();
            }
            else
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
            _colBehaviors.Add(LogBehavior.AutoDetectBehaviour);
        }

        //move to engine
        public void SaveBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenWrite(_strBehaviorConfigFileName);
            ser.Serialize(s, _colBehaviors);
            s.Close();
        }

        //move to engine
        public void LoadBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenRead(_strBehaviorConfigFileName);
            _colBehaviors = (List<LogBehavior>)ser.Deserialize(s);
            s.Close();
        }
        //move to engine
        private bool IsLineInFilter(string strLogFile, string line)
        {
            //if we have a line filter, use it.
            if (_colLineFilter.ContainsKey(strLogFile))
            {
                //check line with filter, and return null if it doesn't match
                WildCards wc = _colLineFilter[strLogFile];
                if (wc != null && !wc.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

                //if we have a global line filter use it..
            else if (_objGlobalLineFilter != null)
            {
                //check line with filter, and return null if it doesn't match
                if (_objGlobalLineFilter != null &&
                    !_objGlobalLineFilter.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

            return true;
        }

        //move to engine
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

        //move to engine
        /// <summary>
        /// parses log file from the given position and on
        /// </summary>
        /// <param name="p_strLogFileName"></param>
        /// <param name="p_intStartPos"></param>
        /// <returns></returns>
        public long ParseLogFileRegExp(string p_strLogFileName, long p_intStartPos, LogBehavior behaviorForCurrentFile)
        {
            //m_dtlogEntries.BeginLoadData();
            long lngFileTotalBytes = 0;
            long progressbytes = 0;

            try
            {
                using (FileStream objFStream = new FileStream(p_strLogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    objFStream.Position = p_intStartPos;
                    using (StreamReader objReader = new StreamReader(objFStream, CurrentEncoding))
                    {

                        string strAllText = objReader.ReadToEnd();
                        lngFileTotalBytes = strAllText.Length;
                        //m_drPrevRow = drRow;
                        MatchCollection colMatches = behaviorForCurrentFile.ParserRegex.Matches(strAllText);



                        foreach (Match match in colMatches)
                        {
                            int increment = (int)((double)lngFileTotalBytes / (double)colMatches.Count);
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
                            drRow.Key = _dtlogEntries.Count;
                            //}

                        }

                        ProgressBarManager.IncrementProgress(lngFileTotalBytes - progressbytes);
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
                return long.MinValue;
            }

            return lngFileTotalBytes;
        }
        //move to engine
        private bool IsAutoDetectMode
        {
            get { return (_objChosenBehavior == LogBehavior.AutoDetectBehaviour); }
        }

        //move to engine
        public bool AddFile(string file)
        {
            if (file.Trim() == "")
                return true;
            LogBehavior behaviorForCurrentFile = _objChosenBehavior;

            bool blnLiveListen = false;
            string strLiveListen = ConfigurationManager.AppSettings["LiveListeningOnByDefault"];
            if (strLiveListen != null && strLiveListen.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                blnLiveListen = true;

            if (timer1.Enabled == false && blnLiveListen)
            {
                timer1.Enabled = true;
                timer1.Start();
            }

            //dataGridView1.DataSource = new BindingList<DSLogData.LogEntriesRow>(m_dvMainView.ToList());
            //if (dataGridView1.Columns.Contains("EntryTime"))
            //{
            //    dataGridView1.Columns["EntryTime"].DefaultCellStyle.Format = DATE_TIME_FORMAT;
            //    dataGridView1.Columns["EntryTime"].Width = 134;
            //}
            //string dir = Path.GetDirectoryName(file);
            //FileAttributes att = File.GetAttributes(file);

            if (!_colWatchedFiles.ContainsKey(file))
            {

                behaviorForCurrentFile = FindCorrectBehaviorForFile(file);

                if (_objChosenBehavior != null && !IsAutoDetectMode)
                {
                    _objChosenBehavior = behaviorForCurrentFile;
                    cmbBehaviors.SelectedItem = _objChosenBehavior;

                    if (_objChosenBehavior != null)
                    {
                        //set the grid columns
                        dataGridView1.Columns.Clear();
                        _objChosenBehavior.CreateGridCols(dataGridView1);

                        //set the default error level filter to initialize display.
                        string strDefaultLevel = ConfigurationManager.AppSettings["DefaultLogLevel"];
                        if (!string.IsNullOrEmpty(strDefaultLevel))
                        {
                            for (int i = 0; i < cmbLevel.Items.Count; ++i)
                            {
                                string item = (string)cmbLevel.Items[i];
                                if (item.Equals(strDefaultLevel, StringComparison.InvariantCultureIgnoreCase))
                                    cmbLevel.SelectedIndex = i;
                            }
                            cmbBehaviors_SelectedIndexChanged(null, null);
                        }

                    }
                }
                //if there was no chosen behavior, return false
                if (behaviorForCurrentFile == null)
                {
                    OpenNotepad(file);
                    return false;
                }
                int intCountBefore = dataGridView1.Rows.Count;
                //parse the log file (using the chosen behaviour's regexp)
                long readBytes = ParseLogFileRegExp(file, 0, behaviorForCurrentFile);

                ////if no bytes were read, or an error occured, return false
                //if (readBytes == long.MinValue || readBytes == 0)
                //{
                //    OpenNotepad(file);
                //    return false;
                //}

                ////if there were no lines read from this file
                //if (intCountBefore == dataGridView1.Rows.Count)
                //{
                //    OpenNotepad(file);
                //    return false;
                //}

                _colWatchedFiles.Add(file, new FileInfo(file).Length);
                lstFiles.Items.Add(file);
            }

            lblCount.Text = "Total Count: " + _dvMainView.Count();
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
            RefreshFilter();
            //success
            return true;
        }

        //move to engine
        private LogBehavior FindCorrectBehaviorForFile(string file)
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
        //move to engine
        private void AddLogFiles(string[] files)
        {
            long totalSize = 0;
            foreach (string file in files)
            {
                if (File.Exists(file) && !_colWatchedFiles.ContainsKey(file))
                    totalSize += (new FileInfo(file)).Length;
            }

            ProgressBarManager.ClearProgress();
            ProgressBarManager.SetLableText("Adding Files:");
            ProgressBarManager.ShowProgressBar(totalSize);
            foreach (string file in files)
            {
                AddFile(file);
            }

            ProgressBarManager.CloseProgress();
            if (_colWatchedFiles.Count == 0) Application.Exit();
        }

        //move to engine
        private void ProcessLogDirectory(List<string> colExcludeList, List<string> colIncludeList, WildCards cardsLineFilter, int intHistory, string line)
        {
            Dictionary<string, DateTime> colFileTimes = new Dictionary<string, DateTime>();

            //get all files from server dir
            string dir = line.Trim();
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
                        if (!file1.ToLower().Contains(inc))
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
                            if (file1.ToLower().Contains(exc))
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
                colFilesForCollection.ForEach(f => AddFile(f));
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

        //move to engine
        private void RemoveFileEntriesFromDataSet(string logFileName)
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

        //move to engine
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
        //move to engine
        /// <summary>
        /// creats a report based on counting repetitions of the same lines
        /// saves the report to a Cvs file
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <param name="method"></param>
        private void GenerateReport(string csvFileName, ReportGenMethod method)
        {
            List<RowPrototype> colProts = new List<RowPrototype>();

            foreach (var drRow in _dvMainView)
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
        private void generateCvsReport3GramToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ask user for a filename
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = "csv";
            DialogResult res = saveFileDialog1.ShowDialog();

            //check that user chose a file
            if (res == DialogResult.Cancel)
                return;
            string csvFileName = saveFileDialog1.FileName;
            GenerateReport(csvFileName, ReportGenMethod.ByTrigram);
        }

        #endregion engine

        //used to decide on which method to base text comparison in report creation
        private enum ReportGenMethod
        {
            ByTrigram,
            ByStringCompare
        }

        public MainForm()
        {
            _refreshFilterTimeBuff = new TimeBuffer(RefreshFilter, TimeSpan.FromMilliseconds(500));
            string strDateTimeFormat = ConfigurationManager.AppSettings["GridDateTimeFormat"];
            if (strDateTimeFormat != null)
                DATE_TIME_FORMAT = strDateTimeFormat;

            InitEngine();

            try
            {
                InitializeComponent();

                foreach (LogBehavior b in _colBehaviors)
                {
                    cmbBehaviors.Items.Add(b);
                }

                //CreateDummyTable();
                _dtlogEntries = new List<LogEntry>();
                _dvMainView = _dtlogEntries;
                lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
                dataGridView1.AutoGenerateColumns = false;
                cmbBehaviors.SelectedItem = LogBehavior.AutoDetectBehaviour;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "Error");
            }
        }


        //gui
        private Regex regMaskStringToNumber = new Regex("[0-9]+", RegexOptions.Compiled);
        //gui
        private int MaskStringToNumber(string p_strInput)
        {
            MatchCollection col = regMaskStringToNumber.Matches(p_strInput, 0);
            if (col.Count > 0)
            {

                return int.Parse(col[0].ToString());
            }
            else
                return 0;
        }

        //gui
        bool IsLineInFilter(string strLogFile, LogEntry row)
        {
            string a = row.Info;
            string b = row.ErrorInfo;
            string c = row.UserName;
            string d = row.LogLevel;
            string line = a + " " + b + " " + c + " " + d;

            return IsLineInFilter(strLogFile, line);
        }


        //gui
        private void ShowLineCard(int intRowIndex)
        {
            if (intRowIndex == -1)
                return;

            List<LogEntry> rowList = new List<LogEntry>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                LogEntry drRow = (LogEntry)row.DataBoundItem;
                rowList.Add(drRow);
            }

            _frmCard.Init(rowList, intRowIndex);

            DialogResult res = _frmCard.ShowDialog();
            this.TopMost = true;
            this.TopMost = false;
        }

        private void RefreshFilter()
        {
            _dvMainView = ApplyFilterToEntryList(_dtlogEntries);

            dataGridView1.DataSource = new SortableBindingList<LogEntry>(_dvMainView);

            lblCount.Text = "Total Count: " + _dvMainView.Count();
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }

        //move to engine
        private List<LogEntry> ApplyFilterToEntryList(List<LogEntry> entries, LogFilter filter)
        {
            IEnumerable<LogEntry> dvMainView = entries;
            string strFilter = "";
            if (!string.IsNullOrEmpty(cmbLevel.Text) && cmbLevel.Text != "ALL")
                dvMainView = dvMainView.Where(r => r.LogLevel.Equals(cmbLevel.Text, StringComparison.InvariantCultureIgnoreCase));

            if (!string.IsNullOrEmpty(txtFilter.Text))
                dvMainView = dvMainView.Where(r => r.ErrorInfo.Contains(txtFilter.Text) || r.Info.Contains(txtFilter.Text));

            if (!string.IsNullOrEmpty(txtUser.Text))
                dvMainView = dvMainView.Where(r => r.UserName.Contains(txtUser.Text));

            if (!string.IsNullOrEmpty(txtThread.Text))
                dvMainView = dvMainView.Where(r => r.ThreadName.Contains(txtThread.Text));

            if (!string.IsNullOrEmpty(txtThread.Text))
                dvMainView = dvMainView.Where(r => r.EntryTime >= dtpFrom.Value && r.EntryTime <= dtpTo.Value);

            _dvMainView = dvMainView.ToList();

            foreach (DataGridViewRow dgRow in dataGridView1.Rows)
            {
                LogEntry drRow = (LogEntry)dgRow.DataBoundItem;
                if (drRow.Key == _intUserSelectionKey)
                {
                    dgRow.Selected = true;
                    if (!dgRow.Displayed && chkPinTrack.Checked)
                        dataGridView1.FirstDisplayedCell = dgRow.Cells[0];
                    break;
                }
            }
            return dvMainView.ToList();
        }


        //gui
        private void OpenNotepad(string p_strLogFileName)
        {
            //only open a file in notepad if it's a new file causing the problem...

            FRMVanishingAlert.ShowForm(2, "Wrong Log Format", "Not a known log format,\r\n\rOpening Notepad", "", "", 0, 0, true, FormStartPosition.Manual, false);

            string strWinDir = Environment.GetEnvironmentVariable("SystemRoot");
            Process.Start(strWinDir + "\\notepad.exe", p_strLogFileName);
            //this.Visible = false;
        }

        //gui
        private void SetLogFileAssociaction()
        {
            string strDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            string strFilePath = strDirectory + "\\file.reg";

            string fileContent =

            "Windows Registry Editor Version 5.00\r\n" +

            "[HKEY_CLASSES_ROOT\\.log]\r\n" +
            "@=\"logfile\"\r\n" +
            "[HKEY_CLASSES_ROOT\\.log\\PersistentHandler]\r\n" +

            "@=\"{5e941d80-bf96-11cd-b579-08002b30bfeb}\"\r\n" +
            "[HKEY_CLASSES_ROOT\\logfile]\r\n" +
            "[HKEY_CLASSES_ROOT\\logfile\\shell]\r\n" +
            "@=\"open\"\r\n" +
            "[HKEY_CLASSES_ROOT\\logfile\\shell\\open]\r\n" +
            "[HKEY_CLASSES_ROOT\\logfile\\shell\\open\\command]\r\n" +

            "@=\"" + Application.ExecutablePath.Replace("\\", "\\\\") + " \\\"%1\\\"\"\r\n" +
            "[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\.log]\r\n" +
            "\"Progid\"=\"logfile\"\r\n";

            FileStream file = new FileStream(strFilePath, FileMode.Create);

            byte[] bytes = Encoding.UTF8.GetBytes(fileContent);
            file.Write(bytes, 0, bytes.Length);
            file.Flush();
            file.Close();

            Process.Start(FindRegeditPath(), "/s \"" + strFilePath + "\"").WaitForExit();
        }

        //gui
        private string FindRegeditPath()
        {
            string strSystemDir = Environment.GetEnvironmentVariable("SystemRoot");
            string strFilePath = strSystemDir + @"\system32\regedt32.exe";
            if (File.Exists(strFilePath))
                return strFilePath;

            strFilePath = strSystemDir + @"\system32\regedit32.exe";
            if (File.Exists(strFilePath))
                return strFilePath;

            strFilePath = strSystemDir + @"\system32\regedit.exe";
            if (File.Exists(strFilePath))
                return strFilePath;

            return null;
        }

        //gui
        private void FilesDropped(DragEventArgs e)
        {
            e.Data.GetDataPresent("FileDrop", false);
            string[] files = (string[])e.Data.GetData("FileDrop", false);

            AddLogFiles(files);
        }


        //gui
        private void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop", false)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        //gui
        private void ExportToCsvFile(string csvFileName)
        {

            try
            {
                //export
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), CurrentEncoding))
                {
                    //export headings
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                        wr.Write("\"" + col.Name.Replace("\"", "\"\"") + "\",");
                    }
                    wr.WriteLine();

                    //export data
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        foreach (DataGridViewColumn col in dataGridView1.Columns)
                        {
                            object val = row.Cells[col.Name].Value;
                            string strVal = val.ToString();
                            if (val is DateTime)
                            {
                                strVal = ((DateTime)val).ToString(DATE_TIME_FORMAT);
                            }

                            //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                            wr.Write("\"" + strVal.Trim("\n\r\t ".ToCharArray()).Replace("\"", "\"\"") + "\",");
                        }
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

        #region eventHandlers

        private void loadServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_frmBatchCollector.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(_frmBatchCollector.BehaviorName))
                {
                    var beh = _colBehaviors.Where(b => b.BehaviorName.Equals(_frmBatchCollector.BehaviorName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                    if (beh != null)
                        _objChosenBehavior = beh;
                }

                if (_frmBatchCollector.LogDirectories.Count == 0)
                {
                    _objGlobalLineFilter = _frmBatchCollector.CardsLineFilter;
                }

                ProgressBarManager.ShowProgressBar(100);
                foreach (string directory in _frmBatchCollector.LogDirectories)
                    ProcessLogDirectory(_frmBatchCollector.ExcludeList, _frmBatchCollector.IncludeList, _frmBatchCollector.CardsLineFilter, _frmBatchCollector.History, directory);
                ProgressBarManager.CloseProgress();

                //perform a memory collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private void generateCsvReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ask user for a filename
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = "csv";
            DialogResult res = saveFileDialog1.ShowDialog();

            //check that user chose a file
            if (res == DialogResult.Cancel)
                return;
            string csvFileName = saveFileDialog1.FileName;
            GenerateReport(csvFileName, ReportGenMethod.ByStringCompare);
        }

        private void cmbBehaviors_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogBehavior b = (LogBehavior)cmbBehaviors.SelectedItem;
            RepaseAllLogs(b);
        }



        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!File.Exists(_strBehaviorConfigFileName))
                SaveBehaviorConfig();
        }

        private void addLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "log files|*.log";
            DialogResult res = openFileDialog1.ShowDialog();
            if (res == DialogResult.Cancel)
                return;

            string file = openFileDialog1.FileName;
            if (File.Exists(file))
            {
                AddLogFiles(new string[] { file });
            }
        }

        private void dataGridView1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)32 || e.KeyChar == '\r')
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    int intSelectionIndex = dataGridView1.SelectedRows[0].Index;
                    ShowLineCard(intSelectionIndex);
                }
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        private void lstFiles_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        private void clearAllFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _colWatchedFiles.Clear();
            lstFiles.Items.Clear();
        }

        private void clearEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _dtlogEntries.Clear();
            RefreshFilter();
        }

        private void stopLiveListeningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        private void startLiveListeningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //save selected row's key
            LogEntry drRow = null;
            if (dataGridView1.SelectedRows.Count > 0)
            {
                drRow = (LogEntry)dataGridView1.SelectedRows[0].DataBoundItem;
                _intUserSelectionKey = drRow.Key;
            }
        }

        private void associateWithlogFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLogFileAssociaction();
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            FilesDropped(e);
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            FilesDropped(e);
        }

        private void lstFiles_DragDrop(object sender, DragEventArgs e)
        {
            FilesDropped(e);
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            _refreshFilterTimeBuff.Restart();
        }

        private void dtpFrom_ValueChanged(object sender, EventArgs e)
        {
            RefreshFilter();
        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {
            RefreshFilter();

        }

        private void txtUser_TextChanged(object sender, EventArgs e)
        {
            _refreshFilterTimeBuff.Restart();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshFilter();
        }

        private void removeFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveFileEntriesFromDataSet((string)lstFiles.SelectedItem);
            _colWatchedFiles.Remove((string)lstFiles.SelectedItem);
            lstFiles.Items.Remove(lstFiles.SelectedItem);
            RefreshFilter();
        }

        private void lstFiles_MouseDown(object sender, MouseEventArgs e)
        {
            int index = lstFiles.IndexFromPoint(e.Location);
            lstFiles.SelectedIndex = index;
        }

        private void reloadLastServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _dtlogEntries.Clear();
            _colWatchedFiles.Clear();
            lstFiles.Items.Clear();
            ProgressBarManager.ShowProgressBar(100);
            foreach (string directory in _frmBatchCollector.LogDirectories)
                ProcessLogDirectory(_frmBatchCollector.ExcludeList, _frmBatchCollector.IncludeList, _frmBatchCollector.CardsLineFilter, _frmBatchCollector.History, directory);
            ProgressBarManager.CloseProgress();
            //perform a memory collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _dtlogEntries.Clear();
            _colWatchedFiles.Clear();
            lstFiles.Items.Clear();
            _objChosenBehavior = null;
            RefreshFilter();
        }

        private void exportToCsvFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ask user for a filename
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.DefaultExt = "csv";
            DialogResult res = saveFileDialog1.ShowDialog();

            //check that user chose a file
            if (res == DialogResult.Cancel)
                return;
            string csvFileName = saveFileDialog1.FileName;
            ExportToCsvFile(csvFileName);

        }

        /// <summary>
        /// timer controls log files refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            list.AddRange(_colWatchedFiles.Keys);
            foreach (string file in list)
            {
                long lngPrevLength = _colWatchedFiles[file];

                if (File.Exists(file))
                {
                    long lngFileLength = (long)new FileInfo(file).Length;

                    //file was swapped, and a new file was created => smaller filesize
                    if (lngPrevLength > lngFileLength)
                    {
                        //we will adjust our counters to keep track with the file.
                        //(the following code will take care of the rest as ususal)
                        _colWatchedFiles[file] = 0;
                        lngPrevLength = 0;
                    }

                    //file changed (more entries were added)
                    if (lngPrevLength < lngFileLength)
                    {
                        LogBehavior behavior = _objChosenBehavior;
                        if (IsAutoDetectMode)
                            behavior = _behaviorsForFiles[file];

                        long lngNewLength = ParseLogFileRegExp(file, lngPrevLength, behavior);
                        _colWatchedFiles[file] = lngNewLength;

                        if (!chkPinTrack.Checked && dataGridView1.Rows.Count > 0)
                            dataGridView1.FirstDisplayedCell = dataGridView1.Rows[0].Cells[0];
                    }
                }
            }
            lblCount.Text = "Total Count: " + _dvMainView.Count();
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //if (txtThread.Text.Trim() == "")
            //    m_strThreadFilter = "";
            //else
            //{
            //    m_strThreadFilter = "ThreadName Like '%" + txtThread.Text.Replace("'", "''") + "%'";
            //}
            _refreshFilterTimeBuff.Restart();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int intRowIndex = e.RowIndex;
            //don't distrupt header operations
            ShowLineCard(intRowIndex);

        }
        #endregion



    }
}