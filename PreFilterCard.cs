using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace LogViewer
{
    public partial class PreFilterCard : Form
    {
        public PreFilterCard()
        {
            InitializeComponent();
        }

        string m_selectedPreset = null;

        public string SelectedPreset
        {
            get { return m_selectedPreset; }
            set { m_selectedPreset = value; }
        }

        private void PreFilterCard_Load(object sender, EventArgs e)
        {
            PopulatePresetsCombo();
        }

        private void PopulatePresetsCombo()
        {
            cmbExistingPresets.Items.Clear();
            
            if (Directory.Exists(m_strPresetsDir))
            {
                Directory.GetFiles(m_strPresetsDir, "*.lgs").ToList().ForEach(p => cmbExistingPresets.Items.Add(Path.GetFileName(p).Substring(0, Path.GetFileName(p).Length - 4)));

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            for (int i = lstDirectories.Items.Count - 1; i >= 0; --i)
            {
                object item = lstDirectories.Items[i];
                if (lstDirectories.SelectedItems.Contains(item))
                {
                    lstDirectories.Items.Remove(item);
                    m_colLogDirectories.Remove((string)item);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            DialogResult res = dialog.ShowDialog(this);
            if (res == DialogResult.Cancel)
            {
                return;
            }
            lstDirectories.Items.Add(dialog.Folder);
            m_colLogDirectories.Add(dialog.Folder);
        }


        private void btnLoadPreset_Click(object sender, EventArgs e)
        {
            m_selectedPreset = (string)cmbExistingPresets.SelectedItem;
            LoadPreset(m_selectedPreset);
        }

        int m_intHistory = NUM_LATEST_FILES_TO_COLLECT;

        public int History
        {
            get { return m_intHistory; }
            set { m_intHistory = value; }
        }

        public string BehaviorName { get; set; }

        string m_strPresetsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Presets");

        public const int NUM_LATEST_FILES_TO_COLLECT = 1;


        List<string> m_colExcludeList = new List<string>();

        public List<string> ExcludeList
        {
            get { return m_colExcludeList; }
            set { m_colExcludeList = value; }
        }
        List<string> m_colIncludeList = new List<string>();

        public List<string> IncludeList
        {
            get { return m_colIncludeList; }
            set { m_colIncludeList = value; }
        }
        List<string> m_colLogDirectories = new List<string>();

        public List<string> LogDirectories
        {
            get { return m_colLogDirectories; }
            set { m_colLogDirectories = value; }
        }
        WildCards m_cardsLineFilter = new WildCards();

        public WildCards CardsLineFilter
        {
            get { return m_cardsLineFilter; }
            set { m_cardsLineFilter = value; }
        }

        public void LoadPreset(string selectedPreset)
        {

            string presetsFile = Path.Combine(m_strPresetsDir, selectedPreset + ".lgs");
            if (File.Exists(presetsFile))
            {

                txtNumHistory.TextChanged -= txtNumHistory_TextChanged;
                txtFileContains.TextChanged -= txtFileContains_TextChanged;
                txtLineContains.TextChanged -= txtLineContains_TextChanged;
                txtFileExclude.TextChanged -= txtFileExclude_TextChanged;
                m_colLogDirectories.Clear();
                txtLineContains.Text = "";
                txtFileExclude.Text = "";
                txtFileContains.Text = "";
                lstDirectories.Items.Clear();
                //m_strServerScriptFile = presetsFile;
                //each line in file should hold a log directory in a server e.g.: "\\inttiradev1\c$\log\" 
                string[] lines = File.ReadAllLines(presetsFile);
                foreach (string line in lines)
                {
                    if (String.IsNullOrEmpty(line.Trim()))
                        continue;

                    //get all exclude lines and construct exclude list
                    if (line.Trim().ToLower().StartsWith("exclude:"))
                    {
                        string[] excludes = line.Trim().ToLower().Substring(8).Trim().Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (excludes.Length > 0)
                        {
                            m_colExcludeList.AddRange(excludes);
                            excludes.ToList().ForEach(p => txtFileExclude.Text += p + ",");
                            txtFileExclude.Text = txtFileExclude.Text.TrimEnd(",".ToCharArray());
                        }
                        continue;
                    }

                    //let the user decide how many files back he wants
                    if (line.Trim().ToLower().StartsWith("history:"))
                    {
                        string strHistory = line.Trim().ToLower().Substring(8).Trim();
                        m_intHistory = NUM_LATEST_FILES_TO_COLLECT;
                        bool ok = int.TryParse(strHistory, out m_intHistory);
                        if (!ok)
                            m_intHistory = NUM_LATEST_FILES_TO_COLLECT;

                        txtNumHistory.Text = m_intHistory.ToString();
                        continue;
                    }

                    //get all exclude lines and construct exclude list
                    if (line.Trim().ToLower().StartsWith("include:"))
                    {
                        string[] includes = line.Trim().ToLower().Substring(8).Trim().Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        if (includes.Length > 0)
                        {
                            m_colIncludeList.AddRange(includes);

                            includes.ToList().ForEach(p => txtFileContains.Text += p + ",");
                            txtFileContains.Text = txtFileContains.Text.TrimEnd(",".ToCharArray());
                        }
                        continue;
                    }

                    if (line.Trim().ToLower().StartsWith("behavior:", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string bName = line.Trim().Substring(9).Trim();
                        BehaviorName = bName;
                        continue;
                    }

                    //get wildcards for line filtering
                    if (line.Trim().ToLower().StartsWith("linefilter:"))
                    {
                        string includes = line.Trim().Substring(11).Trim();
                        if (!string.IsNullOrEmpty(includes))
                        {
                            txtLineContains.Text = includes;
                            //m_cardsLineFilter = new WildCards("*" + includes.Trim() + "*");
                            m_cardsLineFilter.AddWildCard("*" + includes.Trim() + "*");
                        }
                        continue;
                    }

                    // a log directory line is the default line type
                    m_colLogDirectories.Add(line);
                }

                m_colLogDirectories.ForEach(p => lstDirectories.Items.Add(p));
                txtNumHistory.Text = m_intHistory.ToString();

                txtNumHistory.TextChanged += txtNumHistory_TextChanged;
                txtFileContains.TextChanged += txtFileContains_TextChanged;
                txtLineContains.TextChanged += txtLineContains_TextChanged;
                txtFileExclude.TextChanged += txtFileExclude_TextChanged;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Hide();
        }

        private void txtFileContains_TextChanged(object sender, EventArgs e)
        {
            txtFileContains.Text.Split(";,".ToCharArray()).ToList().ForEach(p => m_colIncludeList.Add(p));
        }

        private void txtFileExclude_TextChanged(object sender, EventArgs e)
        {
            txtFileExclude.Text.Split(";,".ToCharArray()).ToList().ForEach(p => m_colExcludeList.Add(p));
        }

        private void txtLineContains_TextChanged(object sender, EventArgs e)
        {
            m_cardsLineFilter = new WildCards("*" + txtLineContains.Text.Trim() + "*");
        }

        private void txtNumHistory_TextChanged(object sender, EventArgs e)
        {
            int intHistory = -1;
            if (int.TryParse(txtNumHistory.Text.Trim(), out intHistory))
                m_intHistory = intHistory;
        }

        private void btnSavePreset_Click(object sender, EventArgs e)
        {
            string presetFileName = Path.Combine(m_strPresetsDir, txtPresetName.Text);
            if (!presetFileName.EndsWith(".lgs"))
                presetFileName += ".lgs";

            if (!Directory.Exists(m_strPresetsDir))
                Directory.CreateDirectory(m_strPresetsDir);

            if (File.Exists(presetFileName))
                if (MessageBox.Show("Preset exists, Overwrite?", "overwrite?", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;

            using (StreamWriter wr = new StreamWriter(presetFileName))
            {
                wr.WriteLine("exclude: " + txtFileExclude.Text);
                wr.WriteLine("include: " + txtFileContains.Text);
                wr.WriteLine("Linefilter: " + txtLineContains.Text);
                wr.WriteLine("history: " + txtNumHistory.Text);
                foreach (string item in lstDirectories.Items)
                    wr.WriteLine(item);
            }
        }
    }
}


