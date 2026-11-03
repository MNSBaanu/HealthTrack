using System.Globalization;
using System.Text;
using ClosedXML.Excel;
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

                    page.Header().ShowOnce().Background(Ink).CornerRadius(8).PaddingVertical(14).PaddingHorizontal(18).Row(row =>
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

        public byte[] ToXlsx(string title, IEnumerable<ReportSection> sections)
        {
            var sectionList = sections.ToList();
            var lastColumn = Math.Max(2, sectionList.Select(s => s.Headers.Length).DefaultIfEmpty(2).Max());

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Report");
            sheet.ShowGridLines = false;
            sheet.Style.Font.FontName = "Calibri";
            sheet.Style.Font.FontSize = 10;
            sheet.Style.Font.FontColor = XLColor.FromHtml(TextColor);

            var banner = sheet.Range(1, 1, 2, lastColumn);
            banner.Style.Fill.BackgroundColor = XLColor.FromHtml(Ink);
            sheet.Row(1).Height = 30;
            sheet.Row(2).Height = 20;

            var wordmark = sheet.Cell(1, 1).GetRichText();
            wordmark.AddText("Health").SetBold().SetFontSize(16).SetFontColor(XLColor.FromHtml(OnInk));
            wordmark.AddText("Track").SetBold().SetItalic().SetFontSize(16).SetFontColor(XLColor.FromHtml(Saffron));
            sheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var subtitle = sheet.Cell(2, 1);
            subtitle.Value = $"{title}  |  Generated {DateTime.Now:g}";
            subtitle.Style.Font.FontColor = XLColor.FromHtml(OnInkMuted);
            subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            var rowIndex = 4;
            foreach (var section in sectionList)
            {
                var heading = sheet.Cell(rowIndex, 1);
                heading.Value = section.Title;
                heading.Style.Font.Bold = true;
                heading.Style.Font.FontSize = 13;
                heading.Style.Font.FontColor = XLColor.FromHtml(Ink);
                heading.Style.Border.LeftBorder = XLBorderStyleValues.Thick;
                heading.Style.Border.LeftBorderColor = XLColor.FromHtml(Saffron);
                rowIndex++;

                var rows = section.Rows.ToList();
                if (rows.Count == 0)
                {
                    var empty = sheet.Cell(rowIndex, 1);
                    empty.Value = "No records.";
                    empty.Style.Font.Italic = true;
                    empty.Style.Font.FontColor = XLColor.FromHtml(Muted);
                    rowIndex += 2;
                    continue;
                }

                for (var column = 0; column < section.Headers.Length; column++)
                {
                    var header = sheet.Cell(rowIndex, column + 1);
                    header.Value = section.Headers[column].ToUpperInvariant();
                    header.Style.Font.Bold = true;
                    header.Style.Font.FontSize = 8;
                    header.Style.Font.FontColor = XLColor.FromHtml(Muted);
                    header.Style.Fill.BackgroundColor = XLColor.FromHtml(Paper);
                    header.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                    header.Style.Border.BottomBorderColor = XLColor.FromHtml(LineStrong);
                }
                rowIndex++;

                foreach (var row in rows)
                {
                    for (var column = 0; column < row.Length; column++)
                    {
                        var cell = sheet.Cell(rowIndex, column + 1);
                        SetCellValue(cell, row[column]);
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.BottomBorderColor = XLColor.FromHtml(Line);
                    }
                    rowIndex++;
                }
                rowIndex++;
            }

            for (var column = 1; column <= lastColumn; column++)
            {
                sheet.Column(column).Width = column == 1 ? 26 : 16;
            }
            sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 24);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetCellValue(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    break;
                case DateTime date:
                    cell.Value = date;
                    cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                    break;
                case bool flag:
                    cell.Value = flag;
                    break;
                case byte or short or int or long or float or double or decimal:
                    cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    cell.Style.NumberFormat.Format = "0.##";
                    break;
                default:
                    cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    break;
            }
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
