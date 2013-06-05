using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Linq;
using LogViewer.GUI;

namespace LogViewer
{

    public partial class MainForm : Form
    {
        public static string DATE_TIME_FORMAT = "dd/MM/yyyy HH:mm:ss,fff";
        private TimeBuffer _refreshFilterTimeBuff = null;
        private EntryCard _frmCard = new EntryCard();
        PreFilterCard _frmBatchCollector = new PreFilterCard();
        public const int NUM_LATEST_FILES_TO_COLLECT = 1;
        private int _intUserSelectionKey = -1;
        private LogEngine _engine = new LogEngine();
        private bool _dateFilterActive = false;
        SortableBindingList<LogEntry> _gridModel = null;

        /// <summary>
        /// main form constructor
        /// </summary>
        public MainForm()
        {
            FindPositionRow = 0;
            _refreshFilterTimeBuff = new TimeBuffer(RefreshFilter, TimeSpan.FromMilliseconds(500));
            string strDateTimeFormat = ConfigurationManager.AppSettings["GridDateTimeFormat"];
            if (strDateTimeFormat != null)
                DATE_TIME_FORMAT = strDateTimeFormat;

            _engine.InitEngine();

            try
            {
                InitializeComponent();

                foreach (LogBehavior b in _engine.Behaviors)
                {
                    cmbBehaviors.Items.Add(b);
                }

                _gridModel = new SortableBindingList<LogEntry>(_engine.MainView);
                dataGridView1.DataSource = _gridModel;

                lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
                dataGridView1.AutoGenerateColumns = false;
                cmbBehaviors.SelectedItem = LogBehavior.AutoDetectBehaviour;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "Error");
            }
        }

        /// <summary>
        /// adds a new file to the log viewer
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool AddFile(string file)
        {
            if (_engine.WatchedFiles.Contains(file))
                return true;

            if (file.Trim() == "")
                return true;
            LogBehavior behaviorForCurrentFile = _engine.ChosenBehavior;

            bool blnLiveListen = false;
            string strLiveListen = ConfigurationManager.AppSettings["LiveListeningOnByDefault"];
            if (strLiveListen != null && strLiveListen.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                blnLiveListen = true;

            if (timer1.Enabled == false && blnLiveListen)
            {
                timer1.Enabled = true;
                timer1.Start();
            }

            if (!_engine.WatchedFiles.Contains(file))
            {

                behaviorForCurrentFile = _engine.FindCorrectBehaviorForFile(file);

                if (!_engine.IsAutoDetectMode)
                {
                    //_engine.ChosenBehavior = behaviorForCurrentFile;
                    cmbBehaviors.SelectedItem = _engine.ChosenBehavior;

                    if (_engine.ChosenBehavior != null)
                    {
                        //set the grid columns
                        dataGridView1.Columns.Clear();
                        _engine.ChosenBehavior.CreateGridCols(dataGridView1);

                        //set the default error level filter to initialize display.
                        SetDefaultLogLevel();
                    }
                }
                //if there was no chosen behavior, return false
                if (behaviorForCurrentFile == null)
                {
                    OpenNotepad(file);
                    return false;
                }

                //parse the log file (using the chosen behaviour's regexp)
                _engine.AddLogFile(file);
            }

            if (!lstFiles.Items.Contains(file))
                lstFiles.Items.Add(file);

            lblCount.Text = "Total Count: " + _engine.MainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
            RefreshFilter();

            //success
            return true;
        }

        /// <summary>
        /// set the default error level filter to initialize display.
        /// </summary>
        private void SetDefaultLogLevel()
        {
            string strDefaultLevel = ConfigurationManager.AppSettings["DefaultLogLevel"];
            SetLogLevel(strDefaultLevel);
        }

        /// <summary>
        /// sets the given log level in the combobox
        /// </summary>
        /// <param name="strDefaultLevel"></param>
        private void SetLogLevel(string strDefaultLevel)
        {
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

        /// <summary>
        /// creates and opens a Log Entry card for the selected line in grid
        /// </summary>
        /// <param name="intRowIndex"></param>
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

        /// <summary>
        /// reads the filter UI components and creates a LogFilter object
        /// </summary>
        /// <returns></returns>
        private LogFilter CreateLogFilter()
        {
            LogFilter filter = new LogFilter();
            if (_dateFilterActive)
            {
                filter.From = dtpFrom.Value;
                filter.To = dtpTo.Value;
            }
            filter.Thread = txtThread.Text;
            filter.User = txtUser.Text;
            filter.TextFilter = txtFilter.Text;
            filter.Level = cmbLevel.Text;
            return filter;
        }

        private void RefreshFilter(object obj)
        {
            RefreshFilter();
        }

        /// <summary>
        /// filters the data and sets the new view into the grid
        /// </summary>
        private void RefreshFilter()
        {
            _engine.RefreshFilter(CreateLogFilter());
            _gridModel.ResetBindings();
            _gridModel.ReSort();
            MarkSelectedRowInGrid();
            //UpdateGridView();

            lblCount.Text = "Total Count: " + _engine.MainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }

        /// <summary>
        /// brings the selected row into view when "keep selected row in view" is selected
        /// </summary>
        private void MarkSelectedRowInGrid()
        {
            if (!chkPinTrack.Checked)
                return;
            //!dgRow.Displayed && 
            foreach (DataGridViewRow dgRow in dataGridView1.Rows)
            {
                LogEntry drRow = (LogEntry)dgRow.DataBoundItem;
                if (drRow.Key == _intUserSelectionKey)
                {
                    if (/*!dgRow.Displayed && */ chkPinTrack.Checked)
                        dataGridView1.FirstDisplayedCell = dgRow.Cells[0];
                    dgRow.Selected = true;
                    break;
                }
            }
        }

        /// <summary>
        /// open notepad to be used in case the log is incompatible with logviewer (no parser)
        /// </summary>
        /// <param name="p_strLogFileName"></param>
        private void OpenNotepad(string p_strLogFileName)
        {
            //only open a file in notepad if it's a new file causing the problem...

            FRMVanishingAlert.ShowForm(2, "Wrong Log Format", "Not a known log format,\r\n\rOpening Notepad", "", "", 0, 0, true, FormStartPosition.Manual, false);

            string strWinDir = Environment.GetEnvironmentVariable("SystemRoot");
            Process.Start(strWinDir + "\\notepad.exe", p_strLogFileName);
            //this.Visible = false;
        }

        /// <summary>
        /// create a reg file to register logviewer as the default program to open .log files
        /// </summary>
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

        /// <summary>
        /// find the path of regedit.exe
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// a callback function that is called when files are dragged & dropped into logviewer
        /// </summary>
        /// <param name="e"></param>
        private void FilesDropped(DragEventArgs e)
        {
            e.Data.GetDataPresent("FileDrop", false);
            string[] files = (string[])e.Data.GetData("FileDrop", false);

            ProgressBarManager.ShowProgressBar(100);
            AddLogFiles(files);
            ProgressBarManager.CloseProgress();
        }

        /// <summary>
        /// adds log files as batch (progress bar is updated accordingly)
        /// </summary>
        /// <param name="files"></param>
        internal void AddLogFiles(string[] files)
        {
            long totalSize = 0;
            foreach (string file in files)
            {
                if (File.Exists(file) && !_engine.WatchedFiles.Contains(file))
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
            if (_engine.WatchedFiles.Count == 0) Application.Exit();
        }

        /// <summary>
        ///  drag & drop plumbing
        /// </summary>
        /// <param name="e"></param>
        private void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop", false)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// exports grid content to a .csv file
        /// </summary>
        /// <param name="csvFileName"></param>
        private void ExportToCsvFile(string csvFileName)
        {

            try
            {
                //export
                using (StreamWriter wr = new StreamWriter(File.OpenWrite(csvFileName), _engine.CurrentEncoding))
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

        /// <summary>
        /// collects multiple logs from several directories
        /// </summary>
        private void PerformBatchCollection()
        {
            if (!string.IsNullOrEmpty(_frmBatchCollector.BehaviorName))
            {
                var beh = _engine.GetBehaviorByName(_frmBatchCollector.BehaviorName);
                if (beh != null)
                    _engine.ChosenBehavior = beh;
            }

            if (_frmBatchCollector.LogDirectories.Count == 0)
            {
                _engine.GlobalLineFilter = _frmBatchCollector.CardsLineFilter;
            }

            ProgressBarManager.ShowProgressBar(100);
            foreach (string directory in _frmBatchCollector.LogDirectories)
                _engine.ProcessLogDirectory(_frmBatchCollector.ExcludeList, _frmBatchCollector.IncludeList,
                                            _frmBatchCollector.CardsLineFilter, _frmBatchCollector.History, directory);
            ProgressBarManager.CloseProgress();

            foreach (var file in _engine.WatchedFiles)
                if (!lstFiles.Items.Contains(file))
                    lstFiles.Items.Add(file);

            //perform a memory collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            lblCount.Text = "Total Count: " + _engine.MainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
            RefreshFilter();
        }

        #region eventHandlers

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
            _engine.GenerateReport(csvFileName, ReportGenMethod.ByTrigram);
        }

        private void loadServerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_frmBatchCollector.ShowDialog() == DialogResult.OK)
            {
                PerformBatchCollection();
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
            _engine.GenerateReport(csvFileName, ReportGenMethod.ByStringCompare);
        }

        private void cmbBehaviors_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogBehavior b = (LogBehavior)cmbBehaviors.SelectedItem;
            dataGridView1.Columns.Clear();
            _engine.ChosenBehavior.CreateGridCols(dataGridView1);
            _engine.ReparseAllLogs(b);
            RefreshFilter();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!File.Exists(_engine.BehaviorConfigFileName))
                _engine.SaveBehaviorConfig();
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
                ProgressBarManager.ShowProgressBar(100);
                AddFile(file);
                ProgressBarManager.CloseProgress();
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

        
        public int FindPositionRow { get; set; }
        public string FindPrevSearchQuery { get; set; }

        private void FindCallback(object searchQuery)
        {
            var search = (LogSearch)searchQuery;
            bool found = false;
            DataGridViewRow dgRow = null;
            int startPos = FindPositionRow;
            int endPos = dataGridView1.Rows.Count;
            int increment = 1;

            bool searchUp = false;
            if (search.Direction == LogSearch.SearchDirection.Up)
            {
                endPos = 0;
                increment = -1;
                searchUp = true;
            }

            //if this is the same query, look for it on another row
            if (FindPrevSearchQuery == search.Query)
                startPos += increment;

            FindPrevSearchQuery = search.Query;

            for (int i = startPos; searchUp ? (i > endPos) : (i < endPos); i += increment)
            {
                dgRow = dataGridView1.Rows[i];
                LogEntry ent = (LogEntry)dgRow.DataBoundItem;
                if (ent.Info.ToLower().Contains(search.Query.ToLower())
                    || ent.ErrorInfo.ToLower().Contains(search.Query.ToLower())
                    )
                {
                    FindPositionRow = i;
                    found = true;
                    break;
                }
            }

            if (!found)
                return;

            dgRow.Selected = true;
            if (!dgRow.Displayed)
                dataGridView1.FirstDisplayedCell = dgRow.Cells[0];
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

        private void clearEntriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _engine.ClearAllEntries();

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
            _dateFilterActive = true;
            RefreshFilter();
        }

        private void dtpTo_ValueChanged(object sender, EventArgs e)
        {
            _dateFilterActive = true;
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
            _engine.RemoveLogFile((string)lstFiles.SelectedItem);
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
            _engine.Clear();
            lstFiles.Items.Clear();
            PerformBatchCollection();
        }

        private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _engine.Clear();
            lstFiles.Items.Clear();
            _engine.ChosenBehavior = null;
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
            list.AddRange(_engine.WatchedFiles);
            List<LogEntry> filteredNewLines = new List<LogEntry>();
            foreach (string file in list)
            {
                _engine.GetNewLinesForFile(file, CreateLogFilter());


                if (filteredNewLines.Count > 0 && !chkPinTrack.Checked && dataGridView1.Rows.Count > 0)
                    dataGridView1.FirstDisplayedCell = dataGridView1.Rows[0].Cells[0];
            }

            if (filteredNewLines.Count > 0)
            {
                _gridModel.ResetBindings();
                _gridModel.ReSort();
                MarkSelectedRowInGrid();
            }

            lblCount.Text = "Total Count: " + _engine.MainView.Count;
            lblMemory.Text = "Used Ram: " + ((double)Process.GetCurrentProcess().WorkingSet64 / 1000000d).ToString(".00") + " MB";
        }


        private void UpdateGridView()
        {
            _gridModel = new SortableBindingList<LogEntry>(_engine.MainView);

            dataGridView1.DataSource = _gridModel;
            _gridModel.ResetBindings();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _refreshFilterTimeBuff.Restart();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            int intRowIndex = e.RowIndex;
            //don't distrupt header operations
            ShowLineCard(intRowIndex);

        }

        private void dataGridView1_Sorted(object sender, EventArgs e)
        {
            MarkSelectedRowInGrid();
        }
        #endregion

        FrmFind _frmFinder = new FrmFind();
        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if(_frmFinder .IsDisposed)
                _frmFinder = new FrmFind();

            if (_frmFinder.Visible)
                _frmFinder.Focus();

            if (e.Control && e.KeyCode == Keys.F)
            {
                _frmFinder.Initialize(FindCallback);
                _frmFinder.Show();
            }
        }
    }
}