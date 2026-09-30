using System.IO;
using DIBA_School_Portal.Server.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DIBA_School_Portal.Server.Services
{
    public interface IPdfReportService
    {
        byte[] GenerateReportCardPdf(StudentTermReportDto report);
    }

    public class PdfReportService : IPdfReportService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PdfReportService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateReportCardPdf(StudentTermReportDto report)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // --- HEADER WITH LOGO & BIG SCHOOL NAME ---
                    page.Header().ShowOnce().Column(headerCol =>
                    {
                        headerCol.Spacing(10);

                        headerCol.Item().Row(row =>
                        {
                            // Left Column: Logo + Big School Name Side-by-Side
                            row.RelativeItem().Row(logoNameRow =>
                            {
                                // Check if school logo file exists in wwwroot/images/school-logo.png
                                string logoPath = Path.Combine(_webHostEnvironment.WebRootPath ?? Directory.GetCurrentDirectory(), "images", "school-logo.png");

                                if (File.Exists(logoPath))
                                {
                                    logoNameRow.ConstantItem(50).Height(50).Image(logoPath);
                                    logoNameRow.ConstantItem(10); // Spacing between logo and text
                                }
                                else
                                {
                                    // Fallback visual placeholder badge if logo file is missing
                                    logoNameRow.ConstantItem(50).Height(50).Background(Colors.Blue.Darken2).AlignCenter().AlignMiddle()
                                        .Text("LOGO").Bold().FontColor(Colors.White).FontSize(9);
                                    logoNameRow.ConstantItem(10);
                                }

                                // School Name in Big Bold Font & Subtitle
                                logoNameRow.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("DIBA SCHOOL PORTAL")
                                        .Bold()
                                        .FontSize(20)
                                        .FontColor(Colors.Blue.Darken3);

                                    col.Item().Text("OFFICIAL STUDENT TERM REPORT CARD")
                                        .FontSize(10)
                                        .LetterSpacing(0.05f)
                                        .FontColor(Colors.Grey.Darken2);
                                });
                            });

                            // Right Column: Session & Term Info
                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Session: {report.SessionName}")
                                    .SemiBold()
                                    .FontSize(10)
                                    .AlignRight();

                                col.Item().Text($"Term: {report.TermName}")
                                    .SemiBold()
                                    .FontSize(10)
                                    .AlignRight();
                            });
                        });

                        // Decorative Divider Line
                        headerCol.Item().LineHorizontal(1.5f).LineColor(Colors.Blue.Medium);
                    });

                    // --- CONTENT ---
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(12);

                        // Student Information Box
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Student Name:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{report.StudentName}").Bold().FontSize(12);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Class:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{report.ClassName}").Bold().FontSize(12);
                            });
                        });

                        col.Item().Text("Subject Performance Summary").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);

                        // Subjects Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Subject Name
                                columns.RelativeColumn(1); // Test 1
                                columns.RelativeColumn(1); // Test 2
                                columns.RelativeColumn(1); // Exam
                                columns.RelativeColumn(1); // Total
                                columns.RelativeColumn(1); // Grade
                                columns.RelativeColumn(1); // Position
                                columns.RelativeColumn(2); // Remark
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Subject").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Test1").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Test2").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Exam").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Total").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Grade").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Pos").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(6).Text("Remark").Bold().FontColor(Colors.White);
                            });

                            foreach (var sub in report.SubjectResults ?? new List<TermSubjectResultDto>())
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.SubjectName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.Test1Score.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.Test2Score.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.ExamScore.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.TotalScore.ToString()).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.Grade).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.SubjectPosition.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(sub.Remark);
                            }
                        });

                        // Summary Metrics Box
                        col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Average Score").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{report.AverageScore:N2}%").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Class Position").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{report.ClassPosition}").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Overall Grade").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{report.OverallGrade}").Bold().FontSize(13).FontColor(Colors.Blue.Darken3);
                            });
                        });
                    });

                    // --- FOOTER ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}