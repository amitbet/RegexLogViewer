using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogViewer
{
    public partial class FrmProgressBar : Form
    {
        public System.Windows.Forms.ProgressBar ProgressBarControl
        {
            get { return progressBar1; }
        }

        public FrmProgressBar()
        {
            InitializeComponent();
        }

        public void SetLableText (string text)
        {
            label1.Text = text;
        }

        internal void SetTotalProgressSteps(int intProgressSteps)
        {
            progressBar1.Maximum = intProgressSteps;
        }
    }
}
