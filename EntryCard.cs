using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace LogViewer
{
    public partial class EntryCard : Form
    {
        public EntryCard()
        {
            InitializeComponent();
        }
        List<DSLogData.LogEntriesRow> m_ItemList = null;
        int m_position = 0;
        public void Init(List<DSLogData.LogEntriesRow> list, int position)
        {
            m_position = position;
            m_ItemList = list;
            SetRowInForm();
        }

        private void SetRowInForm()
        {
            DSLogData.LogEntriesRow row = (DSLogData.LogEntriesRow)m_ItemList[m_position];

            

            txtComp.Text = row.ComputerName;
            txtExInfo.Text = row.ErrorInfo;
            txtInfo.Text = row.Info;
            txtThread.Text = row.ThreadName;
            txtTime.Text = row.EntryTime.ToString(MainForm.DATE_TIME_FORMAT);
            txtUser.Text = row.UserName;
            txtLogLevel.Text = row.LogLevel;
        }

        private void butPrev_Click(object sender, EventArgs e)
        {
            --m_position;
            if (m_position < 0)
                m_position = 0;
            SetRowInForm();
        }

        private void butNext_Click(object sender, EventArgs e)
        {
            ++m_position;
            if (m_position > m_ItemList.Count - 1)
                m_position = m_ItemList.Count - 1;
            SetRowInForm();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Hide();
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult = DialogResult.Cancel;
                Hide();
            }
        }
    }
}