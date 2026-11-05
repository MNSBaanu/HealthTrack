namespace HealthTrack.Services
{
    // One titled table in a report export.
    public class ReportSection
    {
        public ReportSection(string title, string[] headers, IEnumerable<object?[]> rows)
        {
            Title = title;
            Headers = headers;
            Rows = rows;
        }

        public string Title { get; }
        public string[] Headers { get; }
        public IEnumerable<object?[]> Rows { get; }
    }
}
