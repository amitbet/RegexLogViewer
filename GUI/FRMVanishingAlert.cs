using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LogViewer.Properties;

namespace LogViewer
{
    public partial class FRMVanishingAlert : Form, IDisposable
    {

        string m_linkRef = "";
        System.Windows.Forms.Timer m_Timer = new System.Windows.Forms.Timer();

        private void FRMVanishingAlert_Load(object sender, EventArgs e)
        {
        }

        private FRMVanishingAlert()
        {
            InitializeComponent();
            
        }
        /// <summary>
        /// shows the form after initializing it's look and the time it will stay around..
        /// </summary>
        /// <param name="p_intSeconds"></param>
        /// <param name="p_strMessage"></param>
        public static void ShowForm(int p_intSeconds, string p_strHeaderMessage, string p_strMessage, string p_strLinkTitle, string p_strLinkRef, int intPosX, int intPosY,bool blnShowIcon,FormStartPosition p_enmStartPostion,bool blnInformationIcon )
        {
            
            FRMVanishingAlert frmAlert = new FRMVanishingAlert();
            frmAlert.lblHeader.Text = p_strHeaderMessage;
            frmAlert.lblMessage.Text = p_strMessage;
            frmAlert.lnkHelp.Text = p_strLinkTitle;
            frmAlert.m_linkRef = p_strLinkRef;
            frmAlert.Bounds = new Rectangle(intPosX, intPosY, 290, 140);
            frmAlert.pictureBox1.Visible = blnShowIcon;
            frmAlert.StartPosition = p_enmStartPostion;

            if (blnInformationIcon)
            {
                //frmAlert.pictureBox1.Image = Resources.THInformation.ToBitmap();
            }


            new Thread((ThreadStart)delegate { 
                
                frmAlert.m_Timer.Interval = p_intSeconds * 1000;
                frmAlert.m_Timer.Tick += new EventHandler(frmAlert.t_Tick);
                frmAlert.m_Timer.Start();
                Application.Run(frmAlert);
            }).Start();
        }

        void t_Tick(object sender, EventArgs e)
        {
            m_Timer.Stop();
            fadeOut();
        }


        private void lnkHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void lblHeader_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void lblMessage_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            fadeOut();
        }

        private void FRMVanishingAlert_FormClosing(object sender, FormClosingEventArgs e)
        {
            fadeOut();
        }

        private void FRMVanishingAlert_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible == false)
            {
                fadeOut();
            }
        }
        private void fadeOut()
        {
            while (Opacity > 0.07)
            {
                this.Invoke((ThreadStart)delegate
                {
                    this.Opacity = this.Opacity - 0.05;
                    Thread.Sleep(90);
                });
            }
            Application.ExitThread();
            //this.Hide();
        }

        private void FRMVanishingAlert_Deactivate(object sender, EventArgs e)
        {
            //fadeOut();
        }
    }
}