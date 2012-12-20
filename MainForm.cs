using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Globalization;
using System.Configuration;

namespace LogViewer
{


    public partial class MainForm : Form
    {
        List<LogBehavior> m_colBehaviors = new List<LogBehavior>();
        Regex m_regVSParsingReg = new Regex(@"(?:(?<exinfo>(?<file>[\w\d\s\.]*)\((?<line>\d{1,5}),(?<column>\d{1,5})\)):\s(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])|(?:(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        LogBehavior m_objChosenBehavior = null;
        public static string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss,fff";

        //private DSLogData m_dsTables = new DSLogData();
        private DSLogData.LogEntriesDataTable m_dtlogEntries;
        private EntryCard m_frmCard = new EntryCard();
        private DataViewEx m_dvMainView = null;

        private int m_intLineCount = 0;
        private Regex regMaskStringToNumber = new Regex("[0-9]+", RegexOptions.Compiled);

        private Dictionary<string, long> m_colWatchedFiles = new Dictionary<string, long>();
        private string m_strLevelFilter = "";
        private string m_strTextFilter = "";
        private string m_strDateFilter = "";
        private string m_strUserFilter = "";
        private string m_strThreadFilter = "";
        public const int NUM_LATEST_FILES_TO_COLLECT = 1;
        private WildCards m_objGlobalLineFilter = null;
        private List<string> m_colNumMaskedColumns = new List<string>();
        private Dictionary<string, WildCards> m_colLineFilter = new Dictionary<string, WildCards>();
        DSLogData.LogEntriesDataTable m_objDummyTable = null;
        public MainForm()
        {
            if (File.Exists(m_strBehaviorConfigFileName))
            {
                LoadBehaviorConfig();
            }
            else
            {
                m_colBehaviors.Add(new DefaultLogBehavior());

                m_colBehaviors.Add(new LogBehavior()
                {
                    BehaviorName = "VisualStudio",
                    ParserRegex = m_regVSParsingReg,
                    DateFormat = "dd/MM HH:mm:ss,fff",
                    CreateGridCols = LogBehavior.CreateGridColumnActionFromColDefenitionList(
                         new List<LogGridColDefinition>() { 
                            new LogGridColDefinition { Header = "No.", Name = "EntryNumber", LogViewerDataMemberName= LogViwerDataFieldName.Key },
                            new LogGridColDefinition { Header = "Level", Name = "Level", LogViewerDataMemberName= LogViwerDataFieldName.LogLevel },
                            new LogGridColDefinition { Header = "Info", Name = "Info", LogViewerDataMemberName= LogViwerDataFieldName.Info },
                            new LogGridColDefinition { Header = "ExInfo", Name = "ExInfo", LogViewerDataMemberName= LogViwerDataFieldName.ErrorInfo },
                            new LogGridColDefinition { Header = "SourceLogFile", Name = "SourceLogFile", LogViewerDataMemberName= LogViwerDataFieldName.SourceLogFile }
                        })
                });
            }

            try
            {
                InitializeComponent();

                foreach (LogBehavior b in m_colBehaviors)
                {
                    cmbBehaviors.Items.Add(b);
                }

                CreateDummyTable();
                m_dtlogEntries = new DSLogData.LogEntriesDataTable();
                m_dvMainView = new DataViewEx(m_dtlogEntries);
                lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
                dataGridView1.AutoGenerateColumns = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "Error");
            }
        }

        //used to decide on which method to base text comparison in report creation
        enum ReportGenMethod
        {
            ByTrigram,
            ByStringCompare
        }

        string m_strBehaviorConfigFileName = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "BehaviorConfig.xml");
        public void SaveBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenWrite(m_strBehaviorConfigFileName);
            ser.Serialize(s, m_colBehaviors);
            s.Close();
        }

        public void LoadBehaviorConfig()
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<LogBehavior>));
            Stream s = File.OpenRead(m_strBehaviorConfigFileName);
            m_colBehaviors = (List<LogBehavior>)ser.Deserialize(s);
            s.Close();
        }

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




        bool IsLineInFilter(string strLogFile, DSLogData.LogEntriesRow row)
        {
            string a = row.IsInfoNull() ? null : row.Info;
            string b = row.IsErrorInfoNull() ? null : row.ErrorInfo;
            string c = row.IsUserNameNull() ? null : row.UserName;
            string d = row.IsLogLevelNull() ? null : row.LogLevel;
            string line = a + " " + b + " " + c + " " + d;

            //if we have a line filter, use it.
            if (m_colLineFilter.ContainsKey(strLogFile))
            {
                //check line with filter, and return null if it doesn't match
                WildCards wc = m_colLineFilter[strLogFile];
                if (wc != null && !wc.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

            //if we have a global line filter use it..
            else if (m_objGlobalLineFilter != null)
            {
                //check line with filter, and return null if it doesn't match
                if (m_objGlobalLineFilter != null && !m_objGlobalLineFilter.IsMatch(line.Replace('\n', ' ').Replace('\a', ' ').Replace('\r', ' ')))
                    return false;
            }

            return true;
        }

        public void ImportRowToTable(DataRow row, DataTable table)
        {
            DataRow newRow = table.NewRow();
            newRow.ItemArray = row.ItemArray;

            table.Rows.Add(newRow);
        }

        private void CreateDummyTable()
        {
            m_objDummyTable = new DSLogData.LogEntriesDataTable();
            if (m_colNumMaskedColumns != null)
                foreach (string col in m_colNumMaskedColumns)
                {
                    string strColName = col.Substring(0, 1).ToUpper() + col.Substring(1) + "Numbers";
                    if (!m_objDummyTable.Columns.Contains(strColName))
                        m_objDummyTable.Columns.Add(strColName, typeof(string));
                }
        }

        /// <summary>
        /// discovers the correct parser to use for this log file
        /// </summary>
        /// <param name="p_strLogFileHead"></param>
        /// <returns></returns>
        LogBehavior FindCorrectBehaviorForFile(string p_strLogFileHead)
        {
            List<LogBehavior> colGoodBehaviors = new List<LogBehavior>();
            LogBehavior defBhavior = null;
            //parse with each parser in turn - find parser with most matches
            foreach (LogBehavior lb in m_colBehaviors)
            {
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
        public long ParseLogFileRegExp(string p_strLogFileName, long p_intStartPos)
        {
            m_dtlogEntries.BeginLoadData();
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
                        MatchCollection colMatches = m_objChosenBehavior.ParserRegex.Matches(strAllText);
                        foreach (Match match in colMatches)
                        {
                            DSLogData.LogEntriesRow drRow = m_objDummyTable.NewLogEntriesRow();
                            string strDate = match.Groups["date"].Value;
                            string strThread = match.Groups["thread"].Value;
                            string strLevel = match.Groups["level"].Value;
                            string strComputer = match.Groups["computer"].Value;
                            string strUser = match.Groups["user"].Value;
                            string strInfo = match.Groups["info"].Value;
                            string strInfoEx = match.Groups["exinfo"].Value.TrimEnd();
                            DSLogData.LogEntriesRow row = m_objDummyTable.NewLogEntriesRow();
                            drRow.ErrorInfo = strInfoEx;
                            //"14/11 16:39:03,236"
                            DateTime dtmTemp = DateTime.Now;
                            bool ok = System.DateTime.TryParseExact(strDate, m_objChosenBehavior.DateFormat, Application.CurrentCulture, System.Globalization.DateTimeStyles.None, out dtmTemp);

                            if (!ok)
                                drRow.EntryTime = DateTime.Now;
                            else
                                drRow.EntryTime = dtmTemp;

                            if (strLevel.ToLower().StartsWith("inf"))
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

                            if (p_strLogFileName.StartsWith("\\\\"))
                            {
                                drRow.ServerName = p_strLogFileName.Substring(2, p_strLogFileName.IndexOf('\\', 3) - 2);
                            }
                            else
                                drRow.ServerName = "localhost";

                            //if (IsLineInFilter(p_strLogFileName, drRow.Info + " " + drRow.RowError))
                            //m_dtlogEntries.AddLogEntriesRow(drRow);


                            if (IsLineInFilter(p_strLogFileName, drRow))
                            {
                                drRow.Key = m_intLineCount;
                                ++m_intLineCount;
                                ImportRowToTable(drRow, m_dtlogEntries);
                            }
                            int increment = (int)((double)lngFileTotalBytes / (double)colMatches.Count);
                            progressbytes += increment;
                            ProgressBarManager.IncrementProgress(increment);
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
            finally
            {
                m_dtlogEntries.EndLoadData();
                CreateDummyTable();
            }

            return lngFileTotalBytes;
        }


        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int intRowIndex = e.RowIndex;
            //don't distrupt header operations
            ShowLineCard(intRowIndex);

        }

        private void ShowLineCard(int intRowIndex)
        {
            if (intRowIndex == -1)
                return;

            List<DSLogData.LogEntriesRow> rowList = new List<DSLogData.LogEntriesRow>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DSLogData.LogEntriesRow drRow = (DSLogData.LogEntriesRow)(((DataRowView)row.DataBoundItem).Row);
                rowList.Add(drRow);
            }

            m_frmCard.Init(rowList, intRowIndex);

            DialogResult res = m_frmCard.ShowDialog();
            this.TopMost = true;
            this.TopMost = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLevel.Text == "ALL")
            {
                m_strLevelFilter = "";
            }
            else
            {
                m_strLevelFilter = "LogLevel = '" + cmbLevel.Text + "'";
            }
            RefreshFilter();
        }

        int m_intUserSelectionKey = -1;
        private void RefreshFilter()
        {
            string strFilter = "";
            if (m_strLevelFilter != "")
            {
                strFilter += "(" + m_strLevelFilter + ")";
            }

            if (m_strTextFilter != "")
            {
                if (strFilter != "")
                    strFilter += " AND ";
                strFilter += "(" + m_strTextFilter + ")";
            }

            if (m_strUserFilter != "")
            {
                if (strFilter != "")
                    strFilter += " AND ";
                strFilter += "(" + m_strUserFilter + ")";
            }

            if (m_strThreadFilter != "")
            {
                if (strFilter != "")
                    strFilter += " AND ";
                strFilter += "(" + m_strThreadFilter + ")";
            }

            if (m_strDateFilter != "")
            {
                if (strFilter != "")
                    strFilter += " AND ";
                strFilter += "(" + m_strDateFilter + ")";
            }

            m_dvMainView.RowFilter = strFilter;

            foreach (DataGridViewRow dgRow in dataGridView1.Rows)
            {
                DSLogData.LogEntriesRow drRow = (DSLogData.LogEntriesRow)(((DataRowView)dgRow.DataBoundItem).Row);
                if (drRow.Key == m_intUserSelectionKey)
                {
                    dgRow.Selected = true;
                    if (!dgRow.Displayed && chkPinTrack.Checked)
                        dataGridView1.FirstDisplayedCell = dgRow.Cells[0];
                    break;
                }
            }

            lblCount.Text = "Total Count: " + m_dvMainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            if (txtFilter.Text.Trim() == "")
                m_strTextFilter = "";
            else
            {
                m_strTextFilter = "ErrorInfo Like '%" + txtFilter.Text.Replace("'", "''") + "%' OR Info Like '%" + txtFilter.Text.Replace("'", "''") + "%'";
            }
            RefreshFilter();
        }

        private void dtpFrom_ValueChanged(object sender, EventArgs e)
        {
            m_strDateFilter = "EntryTime >= '" + dtpFrom.Value.ToString("dd/MM/yyyy HH:mm:ss.00") + "'";
            if (m_strDateFilter != "")
            {
                m_strDateFilter += "AND EntryTime <= '" + dtpTo.Value.ToString("dd/MM/yyyy HH:mm:ss.00") + "'";
            }
            RefreshFilter();
        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {
            m_strDateFilter = "EntryTime >= '" + dtpFrom.Value.ToString("dd/MM/yyyy HH:mm:ss.00") + "'";
            if (m_strDateFilter != "")
            {
                m_strDateFilter += "AND EntryTime <= '" + dtpTo.Value.ToString("dd/MM/yyyy HH:mm:ss.00") + "'";
            }
            RefreshFilter();

        }

        private void txtUser_TextChanged(object sender, EventArgs e)
        {
            if (txtUser.Text.Trim() == "")
                m_strUserFilter = "";
            else
            {
                m_strUserFilter = "UserName Like '%" + txtUser.Text.Replace("'", "''") + "%'";
            }
            RefreshFilter();
        }

        public bool AddFile(string file)
        {
            if (file.Trim() == "")
                return true;

            if (timer1.Enabled == false)
            {
                timer1.Enabled = true;
                timer1.Start();
            }

            dataGridView1.DataSource = m_dvMainView;
            if (dataGridView1.Columns.Contains("EntryTime"))
            {
                dataGridView1.Columns["EntryTime"].DefaultCellStyle.Format = DATE_TIME_FORMAT;
                dataGridView1.Columns["EntryTime"].Width = 134;
            }
            //string dir = Path.GetDirectoryName(file);
            //FileAttributes att = File.GetAttributes(file);

            if (!m_colWatchedFiles.ContainsKey(file))
            {

                //if this is the firs file - we need to select a behavior for parsing the logs and formatting gridCols

                if (m_objChosenBehavior == null)
                {
                    StringBuilder sbHeader = new StringBuilder();
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        //get the first lines of the file
                        for (int i = 0; i < 50; i++)
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
                    m_objChosenBehavior = FindCorrectBehaviorForFile(sbHeader.ToString());
                    cmbBehaviors.SelectedItem = m_objChosenBehavior;

                    if (m_objChosenBehavior != null)
                    {
                        //set the grid columns
                        dataGridView1.Columns.Clear();
                        m_objChosenBehavior.CreateGridCols(dataGridView1);

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
                if (m_objChosenBehavior == null)
                {
                    OpenNotepad(file);
                    return false;
                }
                int intCountBefore = dataGridView1.Rows.Count;
                //parse the log file (using the chosen behaviour's regexp)
                long readBytes = ParseLogFileRegExp(file, 0);

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

                m_colWatchedFiles.Add(file, new FileInfo(file).Length);
                lstFiles.Items.Add(file);
            }

            lblCount.Text = "Total Count: " + m_dvMainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";

            //success
            return true;
        }

        void OpenNotepad(string p_strLogFileName)
        {
            //only open a file in notepad if it's a new file causing the problem...

            FRMVanishingAlert.ShowForm(2, "Wrong Log Format", "Not a known log format,\r\n\rOpening Notepad", "", "", 0, 0, true, FormStartPosition.Manual, false);

            string strWinDir = Environment.GetEnvironmentVariable("SystemRoot");
            Process.Start(strWinDir + "\\notepad.exe", p_strLogFileName);
            //this.Visible = false;
        }

        /// <summary>
        /// timer controls log files refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            list.AddRange(m_colWatchedFiles.Keys);
            foreach (string file in list)
            {
                long lngPrevLength = m_colWatchedFiles[file];

                if (File.Exists(file))
                {
                    long lngFileLength = (long)new FileInfo(file).Length;

                    //file was swapped, and a new file was created => smaller filesize
                    if (lngPrevLength > lngFileLength)
                    {
                        //we will adjust our counters to keep track with the file.
                        //(the following code will take care of the rest as ususal)
                        m_colWatchedFiles[file] = 0;
                        lngPrevLength = 0;
                    }

                    //file changed (more entries were added)
                    if (lngPrevLength < lngFileLength)
                    {
                        long lngNewLength = ParseLogFileRegExp(file, lngPrevLength);
                        m_colWatchedFiles[file] = lngNewLength;

                        if (!chkPinTrack.Checked && dataGridView1.Rows.Count > 0)
                            dataGridView1.FirstDisplayedCell = dataGridView1.Rows[0].Cells[0];
                    }
                }
            }
            lblCount.Text = "Total Count: " + m_dvMainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (txtThread.Text.Trim() == "")
                m_strThreadFilter = "";
            else
            {
                m_strThreadFilter = "ThreadName Like '%" + txtThread.Text.Replace("'", "''") + "%'";
            }
            RefreshFilter();
        }

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

        string FindRegeditPath()
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

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void associateWithlogFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetLogFileAssociaction();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

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

        private void FilesDropped(DragEventArgs e)
        {
            e.Data.GetDataPresent("FileDrop", false);
            string[] files = (string[])e.Data.GetData("FileDrop", false);

            AddLogFiles(files);
        }

        private void AddLogFiles(string[] files)
        {
            long totalSize = 0;
            foreach (string file in files)
            {
                if (File.Exists(file) && !m_colWatchedFiles.ContainsKey(file))
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
            if (m_colWatchedFiles.Count == 0) Application.Exit();
        }

        private void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop", false)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
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
            m_colWatchedFiles.Clear();
            lstFiles.Items.Clear();
        }

        private void clearEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_dtlogEntries.Clear();
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
            DSLogData.LogEntriesRow drRow = null;
            if (dataGridView1.SelectedRows.Count > 0)
            {
                drRow = (DSLogData.LogEntriesRow)(((DataRowView)dataGridView1.SelectedRows[0].DataBoundItem).Row);
                m_intUserSelectionKey = drRow.Key;
            }
        }

        //string m_strServerScriptFile = null;
        PreFilterCard m_frmBatchCollector = new PreFilterCard();
        private void loadServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_frmBatchCollector.ShowDialog() == DialogResult.OK)
            {

                if (m_frmBatchCollector.LogDirectories.Count == 0)
                {
                    m_objGlobalLineFilter = m_frmBatchCollector.CardsLineFilter;
                }

                foreach (string directory in m_frmBatchCollector.LogDirectories)
                    ProcessLogDirectory(m_frmBatchCollector.ExcludeList, m_frmBatchCollector.IncludeList, m_frmBatchCollector.CardsLineFilter, m_frmBatchCollector.History, directory);

                //perform a memory collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private void LoadServerListFile(string file)
        {
            //List<string> colExcludeList = new List<string>();
            //List<string> colIncludeList = new List<string>();
            //List<string> colLogDirectories = new List<string>();
            //m_colNumMaskedColumns = new List<string>();
            //WildCards cardsLineFilter = null;
            //int intHistory = NUM_LATEST_FILES_TO_COLLECT;

            if (File.Exists(file))
            {
                //m_strServerScriptFile = file;
                ////each line in file should hold a log directory in a server e.g.: "\\inttiradev1\c$\log\" 
                //string[] lines = File.ReadAllLines(file);
                //foreach (string line in lines)
                //{
                //    if (String.IsNullOrEmpty(line.Trim()))
                //        continue;

                //    //get all exclude lines and construct exclude list
                //    if (line.Trim().ToLower().StartsWith("exclude:"))
                //    {
                //        string[] excludes = line.Trim().ToLower().Substring(9).Split(",;".ToCharArray());
                //        colExcludeList.AddRange(excludes);
                //        continue;
                //    }

                //    //let the user decide how many files back he wants
                //    if (line.Trim().ToLower().StartsWith("history:"))
                //    {
                //        string strHistory = line.Trim().ToLower().Substring(9).Trim();
                //        intHistory = NUM_LATEST_FILES_TO_COLLECT;
                //        bool ok = int.TryParse(strHistory, out intHistory);
                //        if (!ok)
                //            intHistory = NUM_LATEST_FILES_TO_COLLECT;

                //        continue;
                //    }

                //    //get all exclude lines and construct exclude list
                //    if (line.Trim().ToLower().StartsWith("include:"))
                //    {
                //        string[] includes = line.Trim().ToLower().Substring(9).Split(",;".ToCharArray());
                //        colIncludeList.AddRange(includes);
                //        continue;
                //    }

                //    //get wildcards for line filtering
                //    if (line.Trim().ToLower().StartsWith("linefilter:"))
                //    {
                //        string includes = line.Trim().Substring(11);
                //        cardsLineFilter = new WildCards("*" + includes.Trim() + "*");
                //        continue;
                //    }

                //    //get wildcards for line filtering
                //    if (line.Trim().ToLower().StartsWith("numbermaskedcolumns:"))
                //    {
                //        string[] columns = line.Substring(20).ToLower().Trim().Split(",;".ToCharArray());
                //        m_colNumMaskedColumns.AddRange(columns);
                //        foreach (string col in m_colNumMaskedColumns)
                //        {
                //            string strColName = col.Substring(0, 1).ToUpper() + col.Substring(1) + "Numbers";
                //            if (!m_dtlogEntries.Columns.Contains(strColName))
                //                m_dtlogEntries.Columns.Add(strColName, typeof(string));
                //        }
                //        foreach (string col in m_colNumMaskedColumns)
                //        {
                //            string strColName = col.Substring(0, 1).ToUpper() + col.Substring(1) + "Numbers";
                //            if (!m_objDummyTable.Columns.Contains(strColName))
                //                m_objDummyTable.Columns.Add(strColName, typeof(string));
                //        }
                //        continue;
                //    }

                //    // a log directory line is the default line type
                //    colLogDirectories.Add(line);
                //}
                // PreFilterCard card = new PreFilterCard();
                m_frmBatchCollector.LoadPreset(file);
                //if the file doesn't contain server lines, use it as a global filter description file
                if (m_frmBatchCollector.LogDirectories.Count == 0)
                {
                    m_objGlobalLineFilter = m_frmBatchCollector.CardsLineFilter;
                }

                foreach (string directory in m_frmBatchCollector.LogDirectories)
                    ProcessLogDirectory(m_frmBatchCollector.ExcludeList, m_frmBatchCollector.IncludeList, m_frmBatchCollector.CardsLineFilter, m_frmBatchCollector.History, directory);
            }
        }


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
                        if (m_colLineFilter.ContainsKey(logFile))
                            m_colLineFilter[logFile] = cardsLineFilter;
                        else
                            m_colLineFilter.Add(logFile, cardsLineFilter);
                        colFilesForCollection.Add(logFile);
                        intTotalDirLogBytes += (new FileInfo(logFile)).Length;
                    }
                }
                ProgressBarManager.ShowProgressBar(intTotalDirLogBytes);
                ProgressBarManager.SetLableText("loading: " + dir);
                colFilesForCollection.ForEach(f => AddFile(f));
                ProgressBarManager.CloseProgress();
            }
            catch (Exception ex)
            {
            }
        }

        private void RemoveFileEntriesFromDataSet(string logFileName)
        {
            List<DSLogData.LogEntriesRow> colRowsToRemove = new List<DSLogData.LogEntriesRow>();
            foreach (DSLogData.LogEntriesRow row in m_dtlogEntries.Rows)
            {
                if (Path.GetFileName(logFileName) == row.SourceLogFile)
                    colRowsToRemove.Add(row);
            }
            foreach (DSLogData.LogEntriesRow row in colRowsToRemove)
            {
                row.Delete();
            }
            m_dtlogEntries.AcceptChanges();
        }

        private void removeFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RemoveFileEntriesFromDataSet((string)lstFiles.SelectedItem);
            m_colWatchedFiles.Remove((string)lstFiles.SelectedItem);
            lstFiles.Items.Remove(lstFiles.SelectedItem);
        }

        private void lstFiles_MouseDown(object sender, MouseEventArgs e)
        {
            int index = lstFiles.IndexFromPoint(e.Location);
            lstFiles.SelectedIndex = index;
        }

        private void reloadLastServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_dtlogEntries.Clear();
            m_colWatchedFiles.Clear();
            lstFiles.Items.Clear();
            foreach (string directory in m_frmBatchCollector.LogDirectories)
                ProcessLogDirectory(m_frmBatchCollector.ExcludeList, m_frmBatchCollector.IncludeList, m_frmBatchCollector.CardsLineFilter, m_frmBatchCollector.History, directory);

            //perform a memory collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_dtlogEntries.Clear();
            m_colWatchedFiles.Clear();
            lstFiles.Items.Clear();
            m_objChosenBehavior = null;
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

        Encoding m_objEncoding = Encoding.ASCII;
        Encoding CurrentEncoding
        {
            get
            {
                if (m_objEncoding != Encoding.ASCII)
                    return m_objEncoding;

                string encName = System.Configuration.ConfigurationSettings.AppSettings["Encoding"];
                try
                {
                    if (!string.IsNullOrEmpty(encName))
                    {
                        m_objEncoding = Encoding.GetEncoding(encName.Trim());
                        return m_objEncoding;
                    }
                }
                catch { }

                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                int codePage = cultureInfo.TextInfo.ANSICodePage;
                m_objEncoding = codePage.Equals(0) ?
                                        Encoding.UTF8 :
                                        Encoding.GetEncoding(codePage);
                return m_objEncoding;
            }
        }

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
                            string val = row.Cells[col.Name].Value.ToString();
                            //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                            wr.Write("\"" + val.Replace("\"", "\"\"") + "\",");
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

        private void ExportToCsvFile(string csvFileName, List<RowPrototype> p_colItems)
        {
            //sanity check
            if (p_colItems.Count == 0)
                return;

            DataRow row = p_colItems[0].MyDataRow;
            DataTable table = row.Table;
            try
            {
                //export
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), CurrentEncoding))
                {
                    //export headings
                    foreach (DataColumn col in table.Columns)
                    {
                        //use quotes to wrap all lines (escaping the spaces and \n \r chars), and replace " with ""
                        wr.Write("\"" + col.ColumnName.Replace("\"", "\"\"") + "\",");
                    }
                    wr.Write("\"Count\"");
                    wr.WriteLine();

                    //export data
                    foreach (RowPrototype rowP in p_colItems)
                    {
                        DataRow drRow = rowP.MyDataRow;
                        foreach (DataColumn col in table.Columns)
                        {
                            string val = drRow[col.ColumnName].ToString();
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
        private void gCCollectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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

        /// <summary>
        /// creats a report based on counting repetitions of the same lines
        /// saves the report to a Cvs file
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <param name="method"></param>
        private void GenerateReport(string csvFileName, ReportGenMethod method)
        {
            List<RowPrototype> colProts = new List<RowPrototype>();

            foreach (DataRowView drv in m_dvMainView)
            {
                DSLogData.LogEntriesRow drRow = (DSLogData.LogEntriesRow)drv.Row;
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

        private void cmbBehaviors_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogBehavior b = (LogBehavior)cmbBehaviors.SelectedItem;
            m_dtlogEntries.Clear();
            m_objChosenBehavior = b;

            //set the grid columns
            dataGridView1.Columns.Clear();
            m_intLineCount = 1;
            m_objChosenBehavior.CreateGridCols(dataGridView1);

            foreach (string file in m_colWatchedFiles.Keys)
            {
                ParseLogFileRegExp(file, 0);
            }

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!File.Exists(m_strBehaviorConfigFileName))
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
    }
}