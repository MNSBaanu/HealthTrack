using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HealthTrack.Services
{
    public class ExportService : IExportService
    {
        private const string Ink = "#1a1823";
        private const string Paper = "#f4efe6";
        private const string Surface = "#fffdf9";
        private const string Line = "#e4dccf";
        private const string LineStrong = "#cfc4b2";
        private const string TextColor = "#3a3642";
        private const string Muted = "#7a7482";
        private const string OnInk = "#f4efe6";
        private const string OnInkMuted = "#a9a3b2";
        private const string Saffron = "#e9a21b";

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
                    page.PageColor(Surface);
                    page.DefaultTextStyle(style => style.FontSize(10).FontColor(TextColor));

                    page.Header().Background(Ink).CornerRadius(8).PaddingVertical(14).PaddingHorizontal(18).Row(row =>
                    {
                        row.AutoItem().AlignMiddle().Width(10).Height(10).Background(Saffron).CornerRadius(5);
                        row.AutoItem().PaddingLeft(8).AlignMiddle().Text(text =>
                        {
                            text.Span("Health").FontSize(16).Bold().FontColor(OnInk);
                            text.Span("Track").FontSize(16).Bold().Italic().FontColor(Saffron);
                        });
                        row.RelativeItem().AlignMiddle().AlignRight().Text(title).FontSize(11).FontColor(OnInkMuted);
                    });

                    page.Content().Column(column =>
                    {
                        foreach (var section in sections)
                        {
                            var rows = section.Rows.ToList();
                            column.Item().PaddingTop(18).PaddingBottom(6).BorderLeft(3).BorderColor(Saffron).PaddingLeft(8)
                                .Text(section.Title).FontSize(13).Bold().FontColor(Ink);

                            if (rows.Count == 0)
                            {
                                column.Item().Text("No records.").Italic().FontColor(Muted);
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
                                        header.Cell().Background(Paper).BorderBottom(1).BorderColor(LineStrong).Padding(5)
                                            .Text(heading.ToUpperInvariant()).FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.08f);
                                    }
                                });

                                foreach (var row in rows)
                                {
                                    foreach (var cell in row)
                                    {
                                        table.Cell().BorderBottom(0.5f).BorderColor(Line).Padding(5).Text(Format(cell));
                                    }
                                }
                            });
                        }
                    });

                    page.Footer().BorderTop(0.5f).BorderColor(Line).PaddingTop(6).AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
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
