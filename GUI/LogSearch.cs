namespace LogViewer
{
    public class LogSearch
    {
        public string Query { get; set; }
        public SearchDirection Direction { get; set; }

        public enum SearchDirection
        {
            Down,
            Up
        }
    }
}