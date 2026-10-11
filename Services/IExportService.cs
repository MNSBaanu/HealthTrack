namespace HealthTrack.Services
{
    public interface IExportService
    {
        byte[] ToCsv(IEnumerable<ReportSection> sections);
        byte[] ToPdf(string title, IEnumerable<ReportSection> sections);
    }
}
