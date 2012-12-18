using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

namespace LogViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm f1 = new MainForm();
            
            bool fileAdded = false;
            foreach (string file in args)
            {
                fileAdded = f1.AddFile(file);
            }

            if (fileAdded || args.Length == 0)
            {
                Application.Run(f1);
            }
            //else
            //{
            //    for (int i = 0; i < 15; ++i)
            //    {
            //        Thread.Sleep(500);
            //        Application.DoEvents();
            //    }
            //}
        }
    }
}