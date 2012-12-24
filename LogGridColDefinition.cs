using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogViewer
{
    public enum LogViwerDataFieldName
    {
        Info,
        ErrorInfo,
        EntryTime,
        ThreadName,
        UserName,
        ComputerName,
        LogLevel, 
        Key, 
        SourceLogFile
    }

    public class LogGridColDefinition
    {
        string m_strHeader = "";
        string m_strName = "";
        LogViwerDataFieldName m_enmLogViewerDataMemberName = LogViwerDataFieldName.Info;

        public string Header
        {
            get { return m_strHeader; }
            set { m_strHeader = value; }
        }
        
        public string Name
        {
            get { return m_strName; }
            set { m_strName = value; }
        }
        
        /// <summary>
        /// Info,ErrorInfo,EntryTime,ThreadName,LogLevel,Key,SourceLogFile
        /// </summary>
        public LogViwerDataFieldName LogViewerDataMemberName
        {
            get { return m_enmLogViewerDataMemberName; }
            set { m_enmLogViewerDataMemberName = value; }
        }
      

        //LogColumnType m_enmColType = LogColumnType.String;
        //public LogColumnType ColumnType
        //{
        //    get { return m_enmColType; }
        //    set { m_enmColType = value; }
        //}

        //string m_strRegexGroupName = "";
        //public string RegexGroupName
        //{
        //    get { return m_strRegexGroupName; }
        //    set { m_strRegexGroupName = value; }
        //}
    }
}
