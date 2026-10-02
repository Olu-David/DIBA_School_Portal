using System.IO;
using DIBA_School_Portal.Server.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace DIBA_School_Portal.Server.Services
{
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
            // ... (your existing report card code remains exactly the same)
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().ShowOnce().Column(headerCol =>
                    {
                        headerCol.Spacing(10);
                        headerCol.Item().Row(row =>
                        {
                            row.RelativeItem().Row(logoNameRow =>
                            {
                                string logoPath = Path.Combine(_webHostEnvironment.WebRootPath ?? Directory.GetCurrentDirectory(), "images", "school-logo.png");
                                if (File.Exists(logoPath))
                                {
                                    logoNameRow.ConstantItem(50).Height(50).Image(logoPath);
                                    logoNameRow.ConstantItem(10);
                                }
                                else
                                {
                                    logoNameRow.ConstantItem(50).Height(50).Background(Colors.Blue.Darken2).AlignCenter().AlignMiddle()
                                        .Text("LOGO").Bold().FontColor(Colors.White).FontSize(9);
                                    logoNameRow.ConstantItem(10);
                                }

                                logoNameRow.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("DIBA SCHOOL PORTAL").Bold().FontSize(20).FontColor(Colors.Blue.Darken3);
                                    col.Item().Text("OFFICIAL STUDENT TERM REPORT CARD").FontSize(10).LetterSpacing(0.05f).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text($"Session: {report.SessionName}").SemiBold().FontSize(10).AlignRight();
                                col.Item().Text($"Term: {report.TermName}").SemiBold().FontSize(10).AlignRight();
                            });
                        });
                        headerCol.Item().LineHorizontal(1.5f).LineColor(Colors.Blue.Medium);
                    });

                    // Content
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(12);
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

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
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

        // ==========================================
        // NEW: PAYMENT RECEIPT PDF GENERATION
        // ==========================================
        public byte[] GeneratePaymentReceiptPdf(StudentPaymentReceiptDto receipt)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.0f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().ShowOnce().Column(headerCol =>
                    {
                        headerCol.Spacing(8);

                        headerCol.Item().AlignCenter().Text(text =>
                        {
                            text.Span("DIBA SCHOOL PORTAL")
                                .Bold()
                                .FontSize(16)
                                .FontColor(Colors.Green.Darken3);
                        });

                        headerCol.Item().AlignCenter().Text(text =>
                        {
                            text.Span("OFFICIAL FEE PAYMENT RECEIPT")
                                .FontSize(9)
                                .LetterSpacing(0.05f)
                                .FontColor(Colors.Grey.Darken2);
                        });

                        headerCol.Item().LineHorizontal(1).LineColor(Colors.Green.Medium);
                    });

                    // Content
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);

                        // Receipt metadata box
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text(text =>
                                {
                                    text.Span($"Receipt No: {receipt.ReferenceNumber}").Bold().FontSize(9);
                                });
                                r.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span($"Date: {receipt.PaymentDate:dd MMM, yyyy}").FontSize(9);
                                });
                            });
                        });

                        // Student Details
                        col.Item().Column(c =>
                        {
                            c.Item().Text(text => text.Span($"Student Name: {receipt.StudentName}").SemiBold().FontSize(11));
                            c.Item().Text(text => text.Span($"Class: {receipt.ClassName}").FontSize(10).FontColor(Colors.Grey.Darken2));
                            c.Item().Text(text => text.Span($"Session / Term: {receipt.SessionName} - {receipt.TermName}").FontSize(10).FontColor(Colors.Grey.Darken2));
                        });

                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        // Fee Items Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Description
                                columns.RelativeColumn(2); // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Darken2).Padding(5).Text(text => text.Span("Fee Description").Bold().FontColor(Colors.White));
                                header.Cell().Background(Colors.Green.Darken2).Padding(5).AlignRight().Text(text => text.Span("Amount (₦)").Bold().FontColor(Colors.White));
                            });

                            foreach (var item in receipt.FeeItems ?? new List<FeeItemDto>())
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(text => text.Span(item.Name));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(text => text.Span($"{item.Amount:N2}"));
                            }
                        });

                        // Total Paid Section
                        col.Item().PaddingTop(5).AlignRight().Column(c =>
                        {
                            c.Item().Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Lighten2).Padding(8).Row(r =>
                            {
                                r.ConstantItem(100).Text(text => text.Span("Total Paid:").Bold().FontSize(12));
                                r.RelativeItem().AlignRight().Text(text => text.Span($"₦{receipt.TotalAmountPaid:N2}").Bold().FontSize(14).FontColor(Colors.Green.Darken3));
                            });
                        });

                        col.Item().PaddingTop(15).AlignCenter().Text(text =>
                        {
                            text.Span("Thank you for your payment!")
                                .Italic()
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("This is a computer-generated receipt from DIBA School Portal. No signature required.")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();
        }
    }
}