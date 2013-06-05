using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogViewer
{
    public class LogEntry : IPropertyRetriever
    {
        private DateTime _entryTime;
        public string ThreadName { get; set; }

        public string LogLevel { get; set; }
        public string UserName { get; set; }
        public string ComputerName { get; set; }
        public string Info { get; set; }
        public string ErrorInfo { get; set; }
        public int Key { get; set; }
        public string SourceLogFile { get; set; }
        public string ServerName { get; set; }
        public DateTime EntryTime
        {
            get { return _entryTime; }
            set { _entryTime = value; }
        }

        public object GetPropertyValue(string name)
        {
            switch (name)
            {
                case "ThreadName":
                    return ThreadName;
                case "LogLevel":
                    return LogLevel;
                case "UserName":
                    return UserName;
                case "ComputerName":
                    return ComputerName;
                case "Info":
                    return Info;
                case "ErrorInfo":
                    return ErrorInfo;
                case "Key":
                    return Key;
                case "SourceLogFile":
                    return SourceLogFile;
                case "ServerName":
                    return ServerName;
                case "EntryTime":
                    return EntryTime;
            }
            return null;
        }
    }
}
