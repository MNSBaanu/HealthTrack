using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HealthTrack.Services
{
    public class ExportService : IExportService
    {
        public byte[] ToCsv(IEnumerable<ReportSection> sections)
        {
            var csv = new StringBuilder();
            foreach (var section in sections)
            {
                csv.AppendLine(Escape(section.Title));
                csv.AppendLine(string.Join(",", section.Headers.Select(Escape)));
                foreach (var row in section.Rows)
                {
                    csv.AppendLine(string.Join(",", row.Select(cell => Escape(Format(cell)))));
                }
                csv.AppendLine();
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        public byte[] ToPdf(string title, IEnumerable<ReportSection> sections)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header().Text(title).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        foreach (var section in sections)
                        {
                            var rows = section.Rows.ToList();
                            column.Item().PaddingTop(15).PaddingBottom(5).Text(section.Title).FontSize(13).Bold();

                            if (rows.Count == 0)
                            {
                                column.Item().Text("No records.").Italic();
                                continue;
                            }

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var _ in section.Headers)
                                    {
                                        columns.RelativeColumn();
                                    }
                                });

                                table.Header(header =>
                                {
                                    foreach (var heading in section.Headers)
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(heading).Bold();
                                    }
                                });

                                foreach (var row in rows)
                                {
                                    foreach (var cell in row)
                                    {
                                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(Format(cell));
                                    }
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Generated {DateTime.Now:g} | Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static string Format(object? value) => value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            double number => number.ToString("0.##", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };

        private static string Escape(string value)
        {
            if (value.Length > 0 && "=+-@".Contains(value[0]))
            {
                value = "'" + value;
            }
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
