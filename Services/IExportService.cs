namespace HealthTrack.Services
{
    // Exports report sections to downloadable files.
    public interface IExportService
    {
        byte[] ToCsv(IEnumerable<ReportSection> sections);
        byte[] ToPdf(string title, IEnumerable<ReportSection> sections);
        byte[] ToXlsx(string title, IEnumerable<ReportSection> sections);
    }
}
