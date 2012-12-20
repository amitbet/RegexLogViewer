
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace LogViewer
{
    public static class ProgressBarManager
    {
        static long m_intFullProgressBarValue = 100;
        static long m_intIntermediateValue = 0;
        static FrmProgressBar m_frm = null;
        static int m_intPrevValue = 0;
        static ManualResetEvent waitForThreadCreation = new ManualResetEvent(false);

        public static void CreateInThread()
        {
            m_frm = new FrmProgressBar();
            m_frm.SetLableText(m_labelText);
            Thread t = new Thread((ThreadStart)delegate
            {
                Application.Run(m_frm);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            Thread.Sleep(1000);
        }

        public static void ShowProgressBar(long intFullProgressBarValue)
        {
            if (m_frm ==null)
                CreateInThread();
            
            m_intFullProgressBarValue = intFullProgressBarValue;
            

            m_frm.Invoke((ThreadStart)delegate
            {
                
                m_frm.Show();
                m_frm.ProgressBarControl.Value = 0;
            });
        }
        
        static string m_labelText = "Adding Files";
        public static void SetLableText(string text)
        {
            m_labelText = text;
            if (m_frm != null)
            {
                if (m_frm.InvokeRequired)
                {
                    m_frm.Invoke((ThreadStart)delegate
                    {
                        m_frm.SetLableText(text);   
                    });
                }
                else
                {
                    m_frm.SetLableText(text);
                }
            }
        }
        public static void SetProgress(long intermediateValue)
        {
            int newValue = (int)(((double)intermediateValue / (double)m_intFullProgressBarValue) * 100);
            if (newValue > 100)
                newValue = 100;

            m_intIntermediateValue = intermediateValue;
            if (newValue > m_intPrevValue && m_frm!=null)
            {
                if (m_frm.InvokeRequired)
                {
                    m_frm.Invoke((ThreadStart)delegate
                    {
                        m_frm.ProgressBarControl.Value = newValue;
                        m_intPrevValue = m_frm.ProgressBarControl.Value;
                        m_frm.SetLableText(m_labelText);
                        m_frm.Invalidate();
                        m_frm.Refresh();
                    });
                }
                else
                {
                    m_frm.ProgressBarControl.Value = newValue;
                    m_frm.SetLableText(m_labelText);
                }
                m_intPrevValue = newValue;
                Application.DoEvents();
            }
        }

        public static void IncrementProgress(long addition)
        {
            m_intIntermediateValue += addition;
            SetProgress(m_intIntermediateValue);
        }

        public static void ClearProgress()
        {
            m_intIntermediateValue = 0;
            m_intPrevValue = 0;
            SetProgress(m_intIntermediateValue);
        }

        public static void CloseProgress()
        {
            m_frm.Invoke((ThreadStart)delegate
            {
                m_frm.Hide();
            });
            ClearProgress();
        }
    }
}
