namespace LogViewer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.EntryNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Level = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Info = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ExInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.logEntriesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dSLogDataBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dSLogData = new LogViewer.DSLogData();
            this.logEntriesBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.dSLogDataBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.cmbLevel = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUser = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.cmsFiles = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtThread = new System.Windows.Forms.TextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.asdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addLogFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadServerListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadLastServerListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToSsvFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.generateCsvReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.generateCvsReport3GramToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.associateWithlogFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearAllFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearEntriesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopLiveListeningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startLiveListeningToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmbBehaviors = new System.Windows.Forms.ToolStripComboBox();
            this.chkPinTrack = new System.Windows.Forms.CheckBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.lblCount = new System.Windows.Forms.Label();
            this.lblMemory = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logEntriesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogDataBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.logEntriesBindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogDataBindingSource1)).BeginInit();
            this.cmsFiles.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowDrop = true;
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EntryNumber,
            this.Level,
            this.Info,
            this.ExInfo});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Location = new System.Drawing.Point(17, 113);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dataGridView1.RowTemplate.Height = 19;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(876, 406);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
            this.dataGridView1.DragDrop += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragDrop);
            this.dataGridView1.DragEnter += new System.Windows.Forms.DragEventHandler(this.dataGridView1_DragEnter);
            this.dataGridView1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.dataGridView1_KeyPress);
            // 
            // EntryNumber
            // 
            this.EntryNumber.DataPropertyName = "Key";
            this.EntryNumber.HeaderText = "No.";
            this.EntryNumber.Name = "EntryNumber";
            this.EntryNumber.ReadOnly = true;
            // 
            // Level
            // 
            this.Level.DataPropertyName = "LogLevel";
            this.Level.HeaderText = "Level";
            this.Level.Name = "Level";
            this.Level.ReadOnly = true;
            // 
            // Info
            // 
            this.Info.DataPropertyName = "Info";
            this.Info.HeaderText = "Info";
            this.Info.Name = "Info";
            this.Info.ReadOnly = true;
            // 
            // ExInfo
            // 
            this.ExInfo.DataPropertyName = "ErrorInfo";
            this.ExInfo.HeaderText = "ExInfo";
            this.ExInfo.Name = "ExInfo";
            this.ExInfo.ReadOnly = true;
            // 
            // logEntriesBindingSource
            // 
            this.logEntriesBindingSource.DataMember = "LogEntries";
            this.logEntriesBindingSource.DataSource = this.dSLogDataBindingSource;
            // 
            // dSLogDataBindingSource
            // 
            this.dSLogDataBindingSource.DataSource = this.dSLogData;
            this.dSLogDataBindingSource.Position = 0;
            // 
            // dSLogData
            // 
            this.dSLogData.DataSetName = "DSLogData";
            this.dSLogData.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // logEntriesBindingSource1
            // 
            this.logEntriesBindingSource1.DataMember = "LogEntries";
            this.logEntriesBindingSource1.DataSource = this.dSLogDataBindingSource1;
            // 
            // dSLogDataBindingSource1
            // 
            this.dSLogDataBindingSource1.DataSource = this.dSLogData;
            this.dSLogDataBindingSource1.Position = 0;
            // 
            // cmbLevel
            // 
            this.cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLevel.FormattingEnabled = true;
            this.cmbLevel.Items.AddRange(new object[] {
            "ALL",
            "FATAL",
            "ERROR",
            "WARN",
            "INFO",
            "DEBUG"});
            this.cmbLevel.Location = new System.Drawing.Point(69, 37);
            this.cmbLevel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbLevel.Name = "cmbLevel";
            this.cmbLevel.Size = new System.Drawing.Size(160, 24);
            this.cmbLevel.TabIndex = 5;
            this.cmbLevel.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 41);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 17);
            this.label2.TabIndex = 6;
            this.label2.Text = "Level:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(19, 75);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "From:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(272, 75);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 17);
            this.label4.TabIndex = 9;
            this.label4.Text = "To:";
            // 
            // txtFilter
            // 
            this.txtFilter.Location = new System.Drawing.Point(343, 37);
            this.txtFilter.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(143, 22);
            this.txtFilter.TabIndex = 10;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(239, 41);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 17);
            this.label5.TabIndex = 11;
            this.label5.Text = "Including Text:";
            // 
            // dtpFrom
            // 
            this.dtpFrom.CustomFormat = "dd/MM/yyyy HH:mm:ss";
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFrom.Location = new System.Drawing.Point(71, 70);
            this.dtpFrom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(176, 22);
            this.dtpFrom.TabIndex = 14;
            this.dtpFrom.ValueChanged += new System.EventHandler(this.dtpFrom_ValueChanged);
            // 
            // dtpTo
            // 
            this.dtpTo.CustomFormat = "dd/MM/yyyy HH:mm:ss";
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTo.Location = new System.Drawing.Point(311, 70);
            this.dtpTo.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(175, 22);
            this.dtpTo.TabIndex = 15;
            this.dtpTo.ValueChanged += new System.EventHandler(this.dtpTo_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(491, 75);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 17);
            this.label6.TabIndex = 17;
            this.label6.Text = "User:";
            // 
            // txtUser
            // 
            this.txtUser.Location = new System.Drawing.Point(549, 71);
            this.txtUser.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtUser.Name = "txtUser";
            this.txtUser.Size = new System.Drawing.Size(121, 22);
            this.txtUser.TabIndex = 16;
            this.txtUser.TextChanged += new System.EventHandler(this.txtUser_TextChanged);
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // lstFiles
            // 
            this.lstFiles.AllowDrop = true;
            this.lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstFiles.ContextMenuStrip = this.cmsFiles;
            this.lstFiles.FormattingEnabled = true;
            this.lstFiles.ItemHeight = 16;
            this.lstFiles.Location = new System.Drawing.Point(721, 37);
            this.lstFiles.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(171, 68);
            this.lstFiles.TabIndex = 19;
            this.lstFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.lstFiles_DragDrop);
            this.lstFiles.DragEnter += new System.Windows.Forms.DragEventHandler(this.lstFiles_DragEnter);
            this.lstFiles.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lstFiles_MouseDown);
            // 
            // cmsFiles
            // 
            this.cmsFiles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeFileToolStripMenuItem});
            this.cmsFiles.Name = "cmsFiles";
            this.cmsFiles.Size = new System.Drawing.Size(136, 26);
            // 
            // removeFileToolStripMenuItem
            // 
            this.removeFileToolStripMenuItem.Name = "removeFileToolStripMenuItem";
            this.removeFileToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.removeFileToolStripMenuItem.Text = "RemoveFile";
            this.removeFileToolStripMenuItem.Click += new System.EventHandler(this.removeFileToolStripMenuItem_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(680, 37);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 17);
            this.label7.TabIndex = 20;
            this.label7.Text = "Files:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(491, 41);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(58, 17);
            this.label8.TabIndex = 22;
            this.label8.Text = "Thread:";
            // 
            // txtThread
            // 
            this.txtThread.Location = new System.Drawing.Point(549, 37);
            this.txtThread.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtThread.Name = "txtThread";
            this.txtThread.Size = new System.Drawing.Size(121, 22);
            this.txtThread.TabIndex = 21;
            this.txtThread.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.asdToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.cmbBehaviors});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(909, 32);
            this.menuStrip1.TabIndex = 23;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // asdToolStripMenuItem
            // 
            this.asdToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addLogFileToolStripMenuItem,
            this.closeAllToolStripMenuItem,
            this.loadServerListToolStripMenuItem,
            this.reloadLastServerListToolStripMenuItem,
            this.exportToSsvFileToolStripMenuItem,
            this.generateCsvReportToolStripMenuItem,
            this.generateCvsReport3GramToolStripMenuItem});
            this.asdToolStripMenuItem.Name = "asdToolStripMenuItem";
            this.asdToolStripMenuItem.Size = new System.Drawing.Size(44, 28);
            this.asdToolStripMenuItem.Text = "File";
            // 
            // addLogFileToolStripMenuItem
            // 
            this.addLogFileToolStripMenuItem.Name = "addLogFileToolStripMenuItem";
            this.addLogFileToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.addLogFileToolStripMenuItem.Text = "Add Log File";
            this.addLogFileToolStripMenuItem.Click += new System.EventHandler(this.addLogFileToolStripMenuItem_Click);
            // 
            // closeAllToolStripMenuItem
            // 
            this.closeAllToolStripMenuItem.Name = "closeAllToolStripMenuItem";
            this.closeAllToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.closeAllToolStripMenuItem.Text = "Clear All";
            this.closeAllToolStripMenuItem.Click += new System.EventHandler(this.closeAllToolStripMenuItem_Click);
            // 
            // loadServerListToolStripMenuItem
            // 
            this.loadServerListToolStripMenuItem.Name = "loadServerListToolStripMenuItem";
            this.loadServerListToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.loadServerListToolStripMenuItem.Text = "Batch Collection";
            this.loadServerListToolStripMenuItem.Click += new System.EventHandler(this.loadServerListToolStripMenuItem_Click);
            // 
            // reloadLastServerListToolStripMenuItem
            // 
            this.reloadLastServerListToolStripMenuItem.Name = "reloadLastServerListToolStripMenuItem";
            this.reloadLastServerListToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.reloadLastServerListToolStripMenuItem.Text = "Recollect batch";
            this.reloadLastServerListToolStripMenuItem.Click += new System.EventHandler(this.reloadLastServerListToolStripMenuItem_Click);
            // 
            // exportToSsvFileToolStripMenuItem
            // 
            this.exportToSsvFileToolStripMenuItem.Name = "exportToSsvFileToolStripMenuItem";
            this.exportToSsvFileToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.exportToSsvFileToolStripMenuItem.Text = "Export gridview to csv file";
            this.exportToSsvFileToolStripMenuItem.Click += new System.EventHandler(this.exportToCsvFileToolStripMenuItem_Click);
            // 
            // generateCsvReportToolStripMenuItem
            // 
            this.generateCsvReportToolStripMenuItem.Name = "generateCsvReportToolStripMenuItem";
            this.generateCsvReportToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.generateCsvReportToolStripMenuItem.Text = "Generate csv report";
            this.generateCsvReportToolStripMenuItem.Click += new System.EventHandler(this.generateCsvReportToolStripMenuItem_Click);
            // 
            // generateCvsReport3GramToolStripMenuItem
            // 
            this.generateCvsReport3GramToolStripMenuItem.Name = "generateCvsReport3GramToolStripMenuItem";
            this.generateCvsReport3GramToolStripMenuItem.Size = new System.Drawing.Size(265, 24);
            this.generateCvsReport3GramToolStripMenuItem.Text = "Generate cvs report (3Gram)";
            this.generateCvsReport3GramToolStripMenuItem.Click += new System.EventHandler(this.generateCvsReport3GramToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.associateWithlogFilesToolStripMenuItem,
            this.clearAllFilesToolStripMenuItem,
            this.clearEntriesToolStripMenuItem,
            this.stopLiveListeningToolStripMenuItem,
            this.startLiveListeningToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(73, 28);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // associateWithlogFilesToolStripMenuItem
            // 
            this.associateWithlogFilesToolStripMenuItem.Name = "associateWithlogFilesToolStripMenuItem";
            this.associateWithlogFilesToolStripMenuItem.Size = new System.Drawing.Size(233, 24);
            this.associateWithlogFilesToolStripMenuItem.Text = "Associate with .log files";
            this.associateWithlogFilesToolStripMenuItem.Click += new System.EventHandler(this.associateWithlogFilesToolStripMenuItem_Click);
            // 
            // clearAllFilesToolStripMenuItem
            // 
            this.clearAllFilesToolStripMenuItem.Name = "clearAllFilesToolStripMenuItem";
            this.clearAllFilesToolStripMenuItem.Size = new System.Drawing.Size(233, 24);
            this.clearAllFilesToolStripMenuItem.Text = "Remove All Files";
            this.clearAllFilesToolStripMenuItem.Click += new System.EventHandler(this.clearAllFilesToolStripMenuItem_Click);
            // 
            // clearEntriesToolStripMenuItem
            // 
            this.clearEntriesToolStripMenuItem.Name = "clearEntriesToolStripMenuItem";
            this.clearEntriesToolStripMenuItem.Size = new System.Drawing.Size(233, 24);
            this.clearEntriesToolStripMenuItem.Text = "Clear Entries";
            this.clearEntriesToolStripMenuItem.Click += new System.EventHandler(this.clearEntriesToolStripMenuItem_Click);
            // 
            // stopLiveListeningToolStripMenuItem
            // 
            this.stopLiveListeningToolStripMenuItem.Name = "stopLiveListeningToolStripMenuItem";
            this.stopLiveListeningToolStripMenuItem.Size = new System.Drawing.Size(233, 24);
            this.stopLiveListeningToolStripMenuItem.Text = "Stop Live Listening";
            this.stopLiveListeningToolStripMenuItem.Click += new System.EventHandler(this.stopLiveListeningToolStripMenuItem_Click);
            // 
            // startLiveListeningToolStripMenuItem
            // 
            this.startLiveListeningToolStripMenuItem.Name = "startLiveListeningToolStripMenuItem";
            this.startLiveListeningToolStripMenuItem.Size = new System.Drawing.Size(233, 24);
            this.startLiveListeningToolStripMenuItem.Text = "Start Live Listening";
            this.startLiveListeningToolStripMenuItem.Click += new System.EventHandler(this.startLiveListeningToolStripMenuItem_Click);
            // 
            // cmbBehaviors
            // 
            this.cmbBehaviors.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.cmbBehaviors.Margin = new System.Windows.Forms.Padding(1, 0, 18, 0);
            this.cmbBehaviors.Name = "cmbBehaviors";
            this.cmbBehaviors.Size = new System.Drawing.Size(121, 28);
            this.cmbBehaviors.SelectedIndexChanged += new System.EventHandler(this.cmbBehaviors_SelectedIndexChanged);
            // 
            // chkPinTrack
            // 
            this.chkPinTrack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkPinTrack.AutoSize = true;
            this.chkPinTrack.Location = new System.Drawing.Point(17, 527);
            this.chkPinTrack.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chkPinTrack.Name = "chkPinTrack";
            this.chkPinTrack.Size = new System.Drawing.Size(201, 21);
            this.chkPinTrack.TabIndex = 24;
            this.chkPinTrack.Text = "Keep Selected Row In View";
            this.chkPinTrack.UseVisualStyleBackColor = true;
            // 
            // lblCount
            // 
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCount.Location = new System.Drawing.Point(684, 523);
            this.lblCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(209, 25);
            this.lblCount.TabIndex = 25;
            this.lblCount.Text = "Total Count: 0";
            this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblMemory
            // 
            this.lblMemory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblMemory.Location = new System.Drawing.Point(475, 523);
            this.lblMemory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMemory.Name = "lblMemory";
            this.lblMemory.Size = new System.Drawing.Size(197, 25);
            this.lblMemory.TabIndex = 26;
            this.lblMemory.Text = "Used Ram: 11MB";
            this.lblMemory.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(909, 550);
            this.Controls.Add(this.lblMemory);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.chkPinTrack);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtThread);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.lstFiles);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtUser);
            this.Controls.Add(this.dtpTo);
            this.Controls.Add(this.dtpFrom);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtFilter);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbLevel);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "MainForm";
            this.Text = "Regex LogViewer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logEntriesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogDataBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.logEntriesBindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dSLogDataBindingSource1)).EndInit();
            this.cmsFiles.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.BindingSource dSLogDataBindingSource;
        private DSLogData dSLogData;
        private System.Windows.Forms.BindingSource logEntriesBindingSource;
        private System.Windows.Forms.BindingSource logEntriesBindingSource1;
        private System.Windows.Forms.BindingSource dSLogDataBindingSource1;
        private System.Windows.Forms.ComboBox cmbLevel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUser;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ListBox lstFiles;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtThread;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem associateWithlogFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearEntriesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopLiveListeningToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startLiveListeningToolStripMenuItem;
        private System.Windows.Forms.CheckBox chkPinTrack;
        private System.Windows.Forms.ToolStripMenuItem asdToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadServerListToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip cmsFiles;
        private System.Windows.Forms.ToolStripMenuItem removeFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadLastServerListToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToSsvFileToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Label lblMemory;
        private System.Windows.Forms.ToolStripMenuItem generateCsvReportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generateCvsReport3GramToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn EntryNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn Level;
        private System.Windows.Forms.DataGridViewTextBoxColumn Info;
        private System.Windows.Forms.DataGridViewTextBoxColumn ExInfo;
        private System.Windows.Forms.ToolStripComboBox cmbBehaviors;
        private System.Windows.Forms.ToolStripMenuItem addLogFileToolStripMenuItem;
    }
}

