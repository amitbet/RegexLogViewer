using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogViewer.GUI
{
    public partial class FrmFind : Form
    {
        private TimeBuffer _timeBuffFind = null;
        Action<object> _find = null;
        public LogSearch.SearchDirection Direction
        {
            get
            {
                if (radDown.Checked)
                {
                    return LogSearch.SearchDirection.Down;
                }
                else
                {
                    return LogSearch.SearchDirection.Up;
                }
            }
        }

        public FrmFind()
        {
            InitializeComponent();
        }

        public void Initialize(Action<object> find)
        {
            _find = find;
            _timeBuffFind = new TimeBuffer(find);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _timeBuffFind.ContentObject = new LogSearch()
                {
                    Query = textBox1.Text,
                    Direction = this.Direction
                };
            _timeBuffFind.Restart();
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.F && e.Modifiers == Keys.Control) ||
                (e.KeyCode == Keys.Enter))
            {
                ExecuteSearch();
                e.SuppressKeyPress = true;
            }
        }

        private void ExecuteSearch()
        {
            var ContentObject = new LogSearch()
                {
                    Query = textBox1.Text,
                    Direction = this.Direction
                };
            _find.Invoke(ContentObject);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ExecuteSearch();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' || e.KeyChar==6)
            {
                //this line eliminates the 'ding' sounds when pressing enter or ctlr+f on the textbox
                e.Handled = true; 
            }
        }
    }
}
