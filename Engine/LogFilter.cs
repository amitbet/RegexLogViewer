using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogViewer.GUI
{
    public class LogFilter
    {
        public string TextFilter { get; set; }
        public string Thread { get; set; }
        public string Level { get; set; }
        public string User { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
