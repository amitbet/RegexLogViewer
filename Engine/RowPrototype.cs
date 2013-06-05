using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogViewer
{
    class RowPrototype
    {
        public LogEntry MyDataRow = null;
        public int MyCount = 1;
        string m_strOrigString = null;
        string m_strIdString = null;
        Dictionary<string, int> m_colTrigrams = null;
        public string MyIdString
        {
            get { return m_strIdString; }
            set
            {
                m_strIdString = ReviseString(value);
            }
        }

        public RowPrototype(LogEntry p_row)
        {
            MyIdString = (p_row.Info + p_row.ErrorInfo);
            m_strOrigString = (p_row.Info + p_row.ErrorInfo);
            MyDataRow = p_row;
            m_colTrigrams = GetStringTrigrams(m_strIdString);
        }

        HashSet<char> m_colAllowedChars = new HashSet<char>("פםןוטארקףךלחיעכגדשץתצמנהבסזqwertyuiop[]\\asdfghjklzxcvbnmABCDEFGHIJKLMNOPQRSTUVWXYZ/".ToCharArray());
        private string ReviseString(string p_str)
        {
            string str = p_str.ToLower();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; ++i)
            {
                char ch = str[i];
                if (m_colAllowedChars.Contains(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }
        public static Dictionary<string, int> GetStringTrigrams(string str)
        {
            Dictionary<string, int> trigrams = new Dictionary<string, int>();
            for (int i = 0; i < str.Length - 3; ++i)
            {
                string trigram = str.Substring(i, 3);
                if (trigrams.ContainsKey(trigram))
                    ++trigrams[trigram];
                else
                    trigrams.Add(trigram, 1);
            }
            
            //cutoff the best 100
            List<KeyValuePair<string, int>> order = new List<KeyValuePair<string, int>>();
            foreach (string trig in trigrams.Keys)
            {
                order.Add(new KeyValuePair<string, int>(trig, trigrams[trig]));
            }
            order.Sort((Comparison<KeyValuePair<string, int>> )delegate (KeyValuePair<string, int>a , KeyValuePair<string, int>b ){
                return a.Value.CompareTo(b.Value);
            });

            if (trigrams.Count > 100)
                for (int i = 0; i < order.Count; ++i)
                {
                    trigrams.Remove(order[i].Key);
                    if (trigrams.Count <= 100)
                        break;
                }
            return trigrams;
        }

        public bool CheckMatchByTrigrams(RowPrototype objOther)
        {
            int intCountSame = 0;
            Dictionary<string, int> colOtherTrigrams = objOther.m_colTrigrams;
            foreach (string tri in m_colTrigrams.Keys)
            {
                if (colOtherTrigrams.ContainsKey(tri) && colOtherTrigrams[tri] == m_colTrigrams[tri])
                {
                    ++intCountSame;
                }
            }
            if (((double)intCountSame / (double)colOtherTrigrams.Count) > 0.8f)
                return true;
            else
                return false;
        }

        public bool CheckMatchByStringCompare(RowPrototype objOther)
        {
            int intCountSame = 0;
            string strOtherId = objOther.m_strIdString;
            for (int i = 0; i < Math.Min(strOtherId.Length, MyIdString.Length); ++i)
            {
                if (strOtherId[i] == m_strIdString[i])
                {
                    ++intCountSame;
                }
            }
            if (((double)intCountSame / (double)MyIdString.Length) > 0.8f)
                return true;
            else
                return false;
        }
    }
}
