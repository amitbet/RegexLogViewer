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

namespace LogViewer
{


    public partial class MainForm : Form
    {
        //Regex m_regParsingReg = new Regex(@"^(?<date>\d{2}/\d{2} \d{2}:\d{2}:\d{2},\d{3})\s*\[(?<thread>[\w\d]*)\]\s*(?<level>\w*)\s*\|\|\s*(?<user>\w*)\s*\|\|\s*(?<computer>.*?)\|\|(?<info>.*?)\|\|(?<exinfo>.*?)(?=\d{2}/\d{2}|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        //Regex m_regParsingReg = new Regex(@"(?:(?<exinfo>(?<file>.*)\((?<line>\d{1,5}),(?<column>\d{1,5})\)):\s(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])|(?:(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        List<LogBehavior> m_colBehaviors = new List<LogBehavior>();
        Regex m_regParsingReg1 = new Regex(@"(?:(?<exinfo>(?<file>[\w\d\s\.]*)\((?<line>\d{1,5}),(?<column>\d{1,5})\)):\s(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])|(?:(?<level>info|warning|error)\s.*?:\s(?<info>.*?)[\n\r])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex m_regParsingReg2 = new Regex(@"^(?<date>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2},\d{3})\s[\w.\s]*\[\s*(?<thread>[\w\d]*)\]\s*(?<level>\w*)\s*[-\s]*(?<info>.*?)\n(?<exinfo>.*?)(?=\d{4}-\d{2}|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        LogBehavior m_objChosenBehavior = null;
        public static string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss,fff";

        //private DSLogData m_dsTables = new DSLogData();
        private DSLogData.LogEntriesDataTable m_dtlogEntries;
        private EntryCard m_frmCard = new EntryCard();
        private DataViewEx m_dvMainView = null;
        private DSLogData.LogEntriesRow m_drPrevRow;
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
                //CreateGridCols = CreateGridColumnActionFromColDefenitionList(new List<LogGridColDefinition>(){

                //m_colBehaviors.Add(new DefaultLogBehavior("Default", m_regParsingReg1, "dd/MM HH:mm:ss,fff")
                //{
                //    CreateGridCols = LogBehavior.CreateGridColumnActionFromColDefenitionList(
                //         new List<LogGridColDefinition>() { 
                //                new LogGridColDefinition{ LogViewerDataMemberName = LogViwerDataFieldName.Info, Name= "Info", Header="Info"},
                //                new LogGridColDefinition{ LogViewerDataMemberName = LogViwerDataFieldName.Key, Name= "Key", Header="Key"},
                //                new LogGridColDefinition{ LogViewerDataMemberName = LogViwerDataFieldName.SourceLogFile, Name= "SourceLogFile", Header="SourceLogFile"}
                //        }
                //        )}
                //    );

                m_colBehaviors.Add(new LogBehavior()
                {
                    BehaviorName = "VisualStudio",
                    ParserRegex = m_regParsingReg1,
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




                m_colBehaviors.Add(new LogBehavior()
                     {
                         BehaviorName = "STDesigner",
                         ParserRegex = m_regParsingReg2,
                         DateFormat = "yyyy-MM-dd HH:mm:ss,fff",
                         CreateGridCols = LogBehavior.CreateGridColumnActionFromColDefenitionList(
                         new List<LogGridColDefinition>() { 
                        new LogGridColDefinition { Header = "EntryTime", Name = "EntryTime", LogViewerDataMemberName= LogViwerDataFieldName.EntryTime },
                        new LogGridColDefinition { Header = "ThreadName", Name = "ThreadName", LogViewerDataMemberName= LogViwerDataFieldName.ThreadName },
                        new LogGridColDefinition { Header = "Level", Name = "Level", LogViewerDataMemberName= LogViwerDataFieldName.LogLevel },
                        new LogGridColDefinition { Header = "Info", Name = "Info", LogViewerDataMemberName= LogViwerDataFieldName.Info },
                        new LogGridColDefinition { Header = "ExInfo", Name = "ExInfo", LogViewerDataMemberName= LogViwerDataFieldName.ErrorInfo },
                        new LogGridColDefinition { Header = "No.", Name = "EntryNumber", LogViewerDataMemberName= LogViwerDataFieldName.Key },
                        new LogGridColDefinition { Header = "SourceLogFile", Name = "SourceLogFile", LogViewerDataMemberName= LogViwerDataFieldName.SourceLogFile },
                    })
                     });

            }
            //m_colBehaviors.Add(new LogBehavior
            //{
            //    BehaviorName = "VisualStudio",
            //    ParserRegex = m_regParsingReg1,
            //    DateFormat = "dd/MM HH:mm:ss,fff",
            //    CreateGridCols = (dataGridView) =>
            //    {
            //        dataGridView.Columns.Clear();
            //        System.Windows.Forms.DataGridViewTextBoxColumn EntryNumber = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn Level = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn Info = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn ExInfo = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn SourceLogFile = new DataGridViewTextBoxColumn();


            //        // 
            //        // EntryNumber
            //        // 
            //        EntryNumber.DataPropertyName = "Key";
            //        EntryNumber.HeaderText = "No.";
            //        EntryNumber.Name = "EntryNumber";
            //        EntryNumber.ReadOnly = true;
            //        // 
            //        // Level
            //        // 
            //        Level.DataPropertyName = "LogLevel";
            //        Level.HeaderText = "Level";
            //        Level.Name = "Level";
            //        Level.ReadOnly = true;
            //        // 
            //        // Info
            //        // 
            //        Info.DataPropertyName = "Info";
            //        Info.HeaderText = "Info";
            //        Info.Name = "Info";
            //        Info.ReadOnly = true;
            //        // 
            //        // ExInfo
            //        // 
            //        ExInfo.DataPropertyName = "ErrorInfo";
            //        ExInfo.HeaderText = "ExInfo";
            //        ExInfo.Name = "ExInfo";
            //        ExInfo.ReadOnly = true;

            //        // 
            //        // SourceLogFile
            //        // 
            //        SourceLogFile.DataPropertyName = "SourceLogFile";
            //        SourceLogFile.HeaderText = "SourceLogFile";
            //        SourceLogFile.Name = "SourceLogFile";
            //        SourceLogFile.ReadOnly = true;

            //        dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            //                                                            EntryNumber,
            //                                                            Level,
            //                                                            Info,
            //                                                            ExInfo,
            //                                                            SourceLogFile});
            //    }
            //});

            //m_colBehaviors.Add(new LogBehavior
            //{
            //    BehaviorName = "STDesigner",
            //    ParserRegex = m_regParsingReg2,
            //    DateFormat = "yyyy-MM-dd HH:mm:ss,fff",
            //    CreateGridCols = (dataGridView) =>
            //    {

            //        System.Windows.Forms.DataGridViewTextBoxColumn EntryTime = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn ThreadName = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn LogLevel = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn Info = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn ErrorInfo = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn Key = new DataGridViewTextBoxColumn();
            //        System.Windows.Forms.DataGridViewTextBoxColumn SourceLogFile = new DataGridViewTextBoxColumn();

            //        dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            //                            EntryTime,
            //                            ThreadName,
            //                            LogLevel,
            //                            Info,
            //                            ErrorInfo,
            //                            Key,
            //                            SourceLogFile});

            //        // 
            //        // EntryTime
            //        // 
            //        EntryTime.DataPropertyName = "EntryTime";
            //        EntryTime.HeaderText = "EntryTime";
            //        EntryTime.Name = "EntryTime";
            //        EntryTime.ReadOnly = true;
            //        // 
            //        // ThreadName
            //        // 
            //        ThreadName.DataPropertyName = "ThreadName";
            //        ThreadName.HeaderText = "ThreadName";
            //        ThreadName.Name = "ThreadName";
            //        ThreadName.ReadOnly = true;
            //        // 
            //        // LogLevel
            //        // 
            //        LogLevel.DataPropertyName = "LogLevel";
            //        LogLevel.HeaderText = "LogLevel";
            //        LogLevel.Name = "LogLevel";
            //        LogLevel.ReadOnly = true;
            //        // 
            //        // Info
            //        // 
            //        Info.DataPropertyName = "Info";
            //        Info.HeaderText = "Info";
            //        Info.Name = "Info";
            //        Info.ReadOnly = true;
            //        // 
            //        // ErrorInfo
            //        // 
            //        ErrorInfo.DataPropertyName = "ErrorInfo";
            //        ErrorInfo.HeaderText = "ErrorInfo";
            //        ErrorInfo.Name = "ErrorInfo";
            //        ErrorInfo.ReadOnly = true;
            //        // 
            //        // Key
            //        // 
            //        Key.DataPropertyName = "Key";
            //        Key.HeaderText = "Key";
            //        Key.Name = "Key";
            //        Key.ReadOnly = true;
            //        // 
            //        // SourceLogFile
            //        // 
            //        SourceLogFile.DataPropertyName = "SourceLogFile";
            //        SourceLogFile.HeaderText = "SourceLogFile";
            //        SourceLogFile.Name = "SourceLogFile";
            //        SourceLogFile.ReadOnly = true;
            //    }
            //});



            try
            {
                //Console.Out.WriteLine(MaskStringToNumbers("dfgdfg dfg 34234ms"));
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

        /// <summary>
        /// parses a single log line assuming format is according to Tira Standards
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p_drPrevRow"></param>
        /// <returns>the row created, else retruns null to indicate this row was added to prev row since it had no row header</returns>
        private DSLogData.LogEntriesRow ParseLogLine(string line, ref DSLogData.LogEntriesRow p_drPrevRow)
        {


            //--------prepare and check data--------
            if (line == null || line.Trim() == "")
                return null;
            DSLogData.LogEntriesRow row = m_objDummyTable.NewLogEntriesRow();
            string[] arrStrings = line.Split(new string[1] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            string strHeaderData = arrStrings[0];
            string[] arrHeaderSplit = strHeaderData.Split("[] \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            //if parse went badly - it's not a log line (no header)
            if (arrHeaderSplit.Length != 4)
            {
                //add the line to previous error log entry
                p_drPrevRow.ErrorInfo += "\r\n" + line.Replace("\a", "");
                return null;
            }

            //get time as DateTime
            arrHeaderSplit[1] = arrHeaderSplit[1].Replace(',', '.');
            DateTime time = new DateTime();
            bool blnParseOk = DateTime.TryParse(arrHeaderSplit[0] + "/" + DateTime.Now.Year + " " + arrHeaderSplit[1], out time);

            //if parse went badly - it's not a log line (no date)
            if (!blnParseOk)
            {
                //add the line to previous error log entry
                p_drPrevRow.ErrorInfo += "\n\r" + line;
                return null;
            }

            //----start filling the row after performing all these checks----

            //save prev row
            //m_drPrevRow = row;
            row = m_objDummyTable.NewLogEntriesRow();

            row.ThreadName = arrHeaderSplit[2];
            row.LogLevel = arrHeaderSplit[3];

            row.EntryTime = time;
            int intStartInfoPos = 3;
            if (arrStrings.Length >= 3)
            {

                //second in large array is the user
                row.UserName = arrStrings[1];

                //third in large array is the system name
                row.ComputerName = arrStrings[2];
            }
            else
            {
                intStartInfoPos = 0;
            }

            //others are info
            int intEndInfoPos = arrStrings.Length;
            bool isErrorRow = (row.LogLevel == "ERROR");
            if (isErrorRow)
                intEndInfoPos = arrStrings.Length - 1;
            row.Info = "";
            row.ErrorInfo = "";
            StringBuilder sbErrorInfo = new StringBuilder();
            for (int i = intStartInfoPos; i < intEndInfoPos; ++i)
            {
                if (row.Info == "")
                {
                    if (m_colNumMaskedColumns.Contains("info"))
                        row["InfoNumbers"] = MaskStringToNumber(arrStrings[i]);
                    //else
                    row.Info = arrStrings[i];
                }
                else
                {
                    string str = arrStrings[i];
                    if (m_colNumMaskedColumns.Contains("errorinfo"))
                        row["ErrorinfoNumbers"] = MaskStringToNumber(str);

                    //if (m_colNumMaskedColumns.Contains("errorinfo"))
                    //  str = MaskStringToNumbers(str);

                    if (sbErrorInfo.Length == 0)
                        sbErrorInfo.Append(str);
                    //row.ErrorInfo += arrStrings[i];
                    else
                        sbErrorInfo.Append(" || " + str);
                    //row.ErrorInfo += " || " + arrStrings[i];
                }

            }
            row.ErrorInfo = sbErrorInfo.ToString();

            if (isErrorRow && String.IsNullOrEmpty(row.ErrorInfo))
            {
                row.ErrorInfo += arrStrings[arrStrings.Length - 1];
            }
            row.Key = m_intLineCount;

            return row;
        }

        //public enum GET_FILEEX_INFO_LEVELS
        //{
        //    GetFileExInfoStandard,
        //    GetFileExMaxInfoLevel
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //public struct WIN32_FILE_ATTRIBUTE_DATA
        //{
        //    public FileAttributes dwFileAttributes;
        //    public FILETIME ftCreationTime;
        //    public FILETIME ftLastAccessTime;
        //    public FILETIME ftLastWriteTime;
        //    public uint nFileSizeHigh;
        //    public uint nFileSizeLow;
        //}


        //[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool GetFileAttributesEx(string lpFileName,
        //  GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

        //private DateTime ConvertToDateTime(FILETIME ft)
        //{
        //    long hFT2 = (((long)ft.dwHighDateTime & (long)0xffffffffL) << 32) + ((long)(ft.dwLowDateTime & (long)0xffffffffL));
        //    DateTime dte = DateTime.FromFileTime((long)hFT2);
        //    return dte;
        //}


        //DateTime GetFileLastModifiedTime(string fullpath )
        //{
        //    string name = Path.GetFileName(fullpath);
        //    WIN32_FILE_ATTRIBUTE_DATA info;
        //    GetFileAttributesEx(fullpath, GET_FILEEX_INFO_LEVELS.GetFileExMaxInfoLevel, out info);
        //    //DateTime creationTime = ConvertToDateTime(info.ftCreationTime);
        //    //DateTime lastAccessTime = ConvertToDateTime(info.ftLastAccessTime);
        //    DateTime lastWriteTime = ConvertToDateTime(info.ftLastWriteTime);
        //    //long lngFileSize = ((info.nFileSizeHigh << 0x20) | (info.nFileSizeLow & ((long)0xffffffffL)));
        //    //FileAttributes fileAttributes = info.dwFileAttributes;
        //    return lastWriteTime;
        //}
        bool IsLineInFilter(string strLogFile, DSLogData.LogEntriesRow row)
        {
            string a = row.IsInfoNull() ? null : row.Info;
            string b = row.IsErrorInfoNull() ? null : row.ErrorInfo;
            string c = row.IsUserNameNull() ? null : row.UserName;
            string d = row.IsLogLevelNull() ? null : row.LogLevel;
            string line = a + " " + b + " " + c + " " + d;
            //line.Split(new string[1] { "||" }, StringSplitOptions.RemoveEmptyEntries);

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
            //DSLogData.LogEntriesRow newRow = m_dtlogEntries.NewLogEntriesRow();
            //newRow.ItemArray = m_drPrevRow.ItemArray;
            //m_dtlogEntries.AddLogEntriesRow(newRow);
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
        /// parses log file from the given position and on
        /// </summary>
        /// <param name="p_strLogFileName"></param>
        /// <param name="p_intStartPos"></param>
        /// <returns></returns>
        public long ParseLogFile(string p_strLogFileName, long p_intStartPos)
        {
            m_dtlogEntries.BeginLoadData();
            long lngEndReadPos = 0;
            try
            {
                using (FileStream objFStream = new FileStream(p_strLogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    objFStream.Position = p_intStartPos;
                    using (StreamReader objReader = new StreamReader(objFStream, Encoding.GetEncoding("windows-1255")))
                    {
                        string strLine = objReader.ReadLine();

                        DSLogData.LogEntriesRow drRow = m_objDummyTable.NewLogEntriesRow();
                        m_drPrevRow = drRow;
                        //if (strLine != null && strLine.Contains("SPC"))
                        //{ }

                        while (strLine != null)
                        {

                            //m_drPrevRow = drRow;
                            //if (IsLineInFilter(p_strLogFileName, strLine))
                            drRow = ParseLogLine(strLine, ref m_drPrevRow);
                            if (drRow != null)
                            {
                                if (IsLineInFilter(p_strLogFileName, m_drPrevRow) && !m_drPrevRow.IsKeyNull())
                                {
                                    //DSLogData.LogEntriesRow newRow = m_dtlogEntries.NewLogEntriesRow();
                                    //newRow.ItemArray = m_drPrevRow.ItemArray;
                                    //m_dtlogEntries.AddLogEntriesRow(newRow);
                                    ImportRowToTable(m_drPrevRow, m_dtlogEntries);
                                    //m_drPrevRow.Delete();
                                }
                                m_drPrevRow = drRow;
                            }
                            //else
                            //drRow = null;
                            while (drRow == null)
                            {
                                strLine = objReader.ReadLine();
                                //if (strLine != null && strLine.Contains("SPC"))
                                //{ }


                                if (strLine == null)
                                {

                                    return (objFStream.Position);
                                }

                                drRow = ParseLogLine(strLine, ref m_drPrevRow);
                                if (drRow != null)
                                {
                                    if (IsLineInFilter(p_strLogFileName, m_drPrevRow) && !m_drPrevRow.IsKeyNull())
                                    {
                                        //m_drPrevRow.Delete();
                                        ImportRowToTable(m_drPrevRow, m_dtlogEntries);
                                    }
                                    m_drPrevRow = drRow;
                                }
                                //else
                                //continue;
                            }

                            if (drRow.ThreadName != null && drRow.ThreadName != "")
                            {
                                ++m_intLineCount;
                                drRow.SourceLogFile = Path.GetFileName(p_strLogFileName);
                                if (p_strLogFileName.StartsWith("\\\\"))
                                {
                                    drRow.ServerName = p_strLogFileName.Substring(2, p_strLogFileName.IndexOf('\\', 3) - 2);
                                }
                                else
                                    drRow.ServerName = "localhost";

                                //if (IsLineInFilter(p_strLogFileName, drRow.Info + " " + drRow.RowError))
                                //m_dtlogEntries.AddLogEntriesRow(drRow);
                            }
                            //read next line
                            strLine = objReader.ReadLine();
                            //if (strLine != null && strLine.Contains("SPC"))
                            //{ }

                        }

                        //check the last line
                        if (drRow != null)
                        {
                            if (IsLineInFilter(p_strLogFileName, drRow) && !m_drPrevRow.IsKeyNull())
                            {
                                ImportRowToTable(drRow, m_dtlogEntries);
                                //m_dtlogEntries.ImportRow(drRow);
                                //drRow.Delete();
                            }
                        }

                        lngEndReadPos = objFStream.Position;
                    }
                }
            }
            catch
            {
                //only open a file in notepad if it's a new file causing the problem...
                if (p_intStartPos == 0)
                {
                    FRMVanishingAlert.ShowForm(2, "Wrong Log Format", "Not a Tira log,\r\n\rOpening Notepad", "", "", 0, 0, true, FormStartPosition.Manual, false);

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

            return lngEndReadPos;
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
            long lngEndReadPos = 0;
            try
            {
                using (FileStream objFStream = new FileStream(p_strLogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    objFStream.Position = p_intStartPos;
                    using (StreamReader objReader = new StreamReader(objFStream, Encoding.GetEncoding("windows-1255")))
                    {

                        string strAllText = objReader.ReadToEnd();

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

                            if (strLevel.ToLower() == "error")
                                strLevel = "ERROR";
                            if (strLevel.ToLower() == "warning")
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
                        }

                        lngEndReadPos = objFStream.Position;
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

            return lngEndReadPos;
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
                        for (int i = 0; i < 150; i++)
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
            foreach (string file in files)
            {
                AddFile(file);
            }
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

        string m_strServerScriptFile = null;
        private void loadServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if (res == DialogResult.Cancel)
                return;

            string file = openFileDialog1.FileName;
            if (File.Exists(file))
            {
                m_strServerScriptFile = file;
                LoadServerListFile(file);
            }

            //perform a memory collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

        }

        private void LoadServerListFile(string file)
        {
            List<string> colExcludeList = new List<string>();
            List<string> colIncludeList = new List<string>();
            List<string> colLogDirectories = new List<string>();
            m_colNumMaskedColumns = new List<string>();
            WildCards cardsLineFilter = null;
            int intHistory = NUM_LATEST_FILES_TO_COLLECT;

            if (File.Exists(file))
            {
                m_strServerScriptFile = file;
                //each line in file should hold a log directory in a server e.g.: "\\inttiradev1\c$\log\" 
                string[] lines = File.ReadAllLines(file);
                foreach (string line in lines)
                {
                    if (String.IsNullOrEmpty(line.Trim()))
                        continue;

                    //get all exclude lines and construct exclude list
                    if (line.Trim().ToLower().StartsWith("exclude:"))
                    {
                        string[] excludes = line.Trim().ToLower().Substring(9).Split(",;".ToCharArray());
                        colExcludeList.AddRange(excludes);
                        continue;
                    }

                    //let the user decide how many files back he wants
                    if (line.Trim().ToLower().StartsWith("history:"))
                    {
                        string strHistory = line.Trim().ToLower().Substring(9).Trim();
                        intHistory = NUM_LATEST_FILES_TO_COLLECT;
                        bool ok = int.TryParse(strHistory, out intHistory);
                        if (!ok)
                            intHistory = NUM_LATEST_FILES_TO_COLLECT;

                        continue;
                    }

                    //get all exclude lines and construct exclude list
                    if (line.Trim().ToLower().StartsWith("include:"))
                    {
                        string[] includes = line.Trim().ToLower().Substring(9).Split(",;".ToCharArray());
                        colIncludeList.AddRange(includes);
                        continue;
                    }

                    //get wildcards for line filtering
                    if (line.Trim().ToLower().StartsWith("linefilter:"))
                    {
                        string includes = line.Trim().Substring(11);
                        cardsLineFilter = new WildCards("*" + includes.Trim() + "*");
                        continue;
                    }

                    //get wildcards for line filtering
                    if (line.Trim().ToLower().StartsWith("numbermaskedcolumns:"))
                    {
                        string[] columns = line.Substring(20).ToLower().Trim().Split(",;".ToCharArray());
                        m_colNumMaskedColumns.AddRange(columns);
                        foreach (string col in m_colNumMaskedColumns)
                        {
                            string strColName = col.Substring(0, 1).ToUpper() + col.Substring(1) + "Numbers";
                            if (!m_dtlogEntries.Columns.Contains(strColName))
                                m_dtlogEntries.Columns.Add(strColName, typeof(string));
                        }
                        foreach (string col in m_colNumMaskedColumns)
                        {
                            string strColName = col.Substring(0, 1).ToUpper() + col.Substring(1) + "Numbers";
                            if (!m_objDummyTable.Columns.Contains(strColName))
                                m_objDummyTable.Columns.Add(strColName, typeof(string));
                        }
                        continue;
                    }

                    // a log directory line is the default line type
                    colLogDirectories.Add(line);
                }

                //if the file doesn't contain server lines, use it as a global filter description file
                if (colLogDirectories.Count == 0)
                {
                    m_objGlobalLineFilter = cardsLineFilter;
                }

                foreach (string directory in colLogDirectories)
                    ProcessLogDirectory(colExcludeList, colIncludeList, cardsLineFilter, intHistory, directory);
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


                for (int i = 0; i < intHistory; ++i)
                {
                    if (colLogFiles.Count > i)
                    {
                        string logFile = colLogFiles[i];
                        if (m_colLineFilter.ContainsKey(logFile))
                            m_colLineFilter[logFile] = cardsLineFilter;
                        else
                            m_colLineFilter.Add(logFile, cardsLineFilter);
                        AddFile(logFile);
                    }
                }
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
            LoadServerListFile(m_strServerScriptFile);
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_dtlogEntries.Clear();
            m_colWatchedFiles.Clear();
            lstFiles.Items.Clear();
            m_objChosenBehavior = null;
        }

        private void exportToSsvFileToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void ExportToCsvFile(string csvFileName)
        {
            try
            {
                //export
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), Encoding.GetEncoding("windows-1255")))
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
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), Encoding.GetEncoding("windows-1255")))
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
    }
}