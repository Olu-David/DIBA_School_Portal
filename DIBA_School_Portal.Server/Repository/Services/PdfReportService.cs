using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace DIBA_School_Portal.Server.Services
{
    public class PdfReportService : IPdfReportService
    {
        #region Constants
        private const string AvatarBucket = "avatars";

        private const string BgPage = "#F8FAFC";
        private const string BgCard = "#FFFFFF";
        private const string BgSubtle = "#F1F5F9";
        private const string Border = "#E2E8F0";
        private const string TextPrimary = "#0F172A";
        private const string TextSecondary = "#64748B";
        private const string TextMuted = "#94A3B8";
        private const string Accent = "#059669";
        private const string AccentLight = "#ECFDF5";
        private const string AccentBlue = "#2563EB";
        private const string AccentBlueLight = "#EFF6FF";
        private const string AccentAmber = "#D97706";
        private const string AccentAmberLight = "#FFFBEB";
        private const string AccentRose = "#E11D48";
        private const string AccentRoseLight = "#FFF1F2";
        #endregion

        #region Fields + Constructor
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISupaBaseFileService _files;
        private readonly ILogger<PdfReportService> _logger;

        public PdfReportService(
            IWebHostEnvironment webHostEnvironment,
            ISupaBaseFileService files,
            ILogger<PdfReportService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _files = files;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }
        #endregion

        // ==========================================================
        // REPORT CARD
        // ==========================================================
        #region GenerateReportCardPdf
        public byte[] GenerateReportCardPdf(StudentTermReportDto report)
        {
            var profileImage = LoadProfileImage(report.ProfilePictureUrl);
            var gradeColor = GradeColor(report.OverallGrade);
            var gradeBg = GradeBg(report.OverallGrade);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.4f, Unit.Centimetre);
                    page.PageColor(BgPage);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica").FontColor(TextPrimary));

                    // ============================================
                    // HEADER
                    // ============================================
                    page.Header().ShowOnce().Element(header =>
                    {
                        header.Background(BgCard).Padding(18).CornerRadius(12).Border(1).BorderColor(Border).Row(row =>
                        {
                            row.RelativeItem().Row(logoRow =>
                            {
                                string logoPath = Path.Combine(
                                    _webHostEnvironment.WebRootPath ?? Directory.GetCurrentDirectory(),
                                    "images", "school-logo.png");

                                if (File.Exists(logoPath))
                                {
                                    logoRow.ConstantItem(52).Height(52).Image(logoPath).FitArea();
                                }
                                else
                                {
                                    logoRow.ConstantItem(52).Height(52)
                                        .Background(Accent).CornerRadius(10)
                                        .AlignCenter().AlignMiddle()
                                        .Text("DS").Bold().FontColor(Colors.White).FontSize(16);
                                }

                                logoRow.ConstantItem(14);

                                logoRow.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("DIBA SCHOOL").Bold().FontSize(17).FontColor(TextPrimary);
                                    col.Item().Text("Official Student Report Card")
                                        .FontSize(9).FontColor(TextSecondary);
                                });
                            });

                            row.ConstantItem(220).AlignRight().Row(chips =>
                            {
                                chips.RelativeItem().Background(AccentLight).CornerRadius(8).Padding(9).Column(c =>
                                {
                                    c.Item().Text("SESSION").FontSize(7).Bold().FontColor(Accent).LetterSpacing(0.05f);
                                    c.Item().Text(report.SessionName ?? "—").FontSize(10).Bold().FontColor(TextPrimary);
                                });

                                chips.ConstantItem(8);

                                chips.RelativeItem().Background(AccentBlueLight).CornerRadius(8).Padding(9).Column(c =>
                                {
                                    c.Item().Text("TERM").FontSize(7).Bold().FontColor(AccentBlue).LetterSpacing(0.05f);
                                    c.Item().Text(report.TermName ?? "—").FontSize(10).Bold().FontColor(TextPrimary);
                                });
                            });
                        });
                    });

                    // ============================================
                    // CONTENT
                    // ============================================
                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(12);

                        // ---------- Student hero card ----------
                        col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(18).Row(row =>
                        {
                            if (profileImage != null)
                            {
                                row.ConstantItem(78).Height(78)
                                    .Background(BgSubtle)
                                    .CornerRadius(39)
                                    .Image(profileImage)
                                    .FitArea();
                            }
                            else
                            {
                                row.ConstantItem(78).Height(78)
                                    .Background(Accent).CornerRadius(39)
                                    .AlignCenter().AlignMiddle()
                                    .Text(GetInitials(report.StudentName))
                                    .Bold().FontColor(Colors.White).FontSize(22);
                            }

                            row.ConstantItem(16);

                            row.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().Text(report.StudentName ?? "—").Bold().FontSize(19).FontColor(TextPrimary);
                                c.Item().Text($"Class: {report.ClassName ?? "—"}")
                                    .FontSize(11).FontColor(TextSecondary);

                                if (!string.IsNullOrWhiteSpace(report.AdmissionNumber))
                                {
                                    c.Item().Text($"Admission No: {report.AdmissionNumber}")
                                        .FontSize(10).FontColor(TextMuted);
                                }
                            });

                            row.ConstantItem(90).AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().AlignRight().Background(gradeBg).CornerRadius(10).Padding(10).Column(badge =>
                                {
                                    badge.Item().AlignCenter().Text("GRADE").FontSize(7).Bold()
                                        .FontColor(gradeColor).LetterSpacing(0.05f);
                                    badge.Item().AlignCenter().Text(report.OverallGrade ?? "-")
                                        .Bold().FontSize(20).FontColor(gradeColor);
                                });
                            });
                        });

                        // ---------- Summary stat tiles ----------
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e => StatTile(e, "Average Score", $"{report.AverageScore:N2}%"));
                            row.ConstantItem(10);
                            row.RelativeItem().Element(e => StatTile(e, "Class Position", $"{report.ClassPosition}"));
                            row.ConstantItem(10);
                            row.RelativeItem().Element(e => StatTile(e, "Subjects Taken",
                                (report.SubjectResults?.Count ?? 0).ToString()));
                        });

                        // ---------- Subject table ----------
                        col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(16).Column(tableSection =>
                        {
                            tableSection.Spacing(10);

                            tableSection.Item().Text("Subject Performance")
                                .Bold().FontSize(12).FontColor(TextPrimary);

                            tableSection.Item().Table(table =>
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

                                // ---------- HEADER ROW (inline, no helper) ----------
                                table.Header(header =>
                                {
                                    header.Cell().Background(BgSubtle).Padding(9)
                                        .Text("Subject").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Test 1").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Test 2").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Exam").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Total").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Grade").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9).AlignRight()
                                        .Text("Pos").FontSize(9).Bold().FontColor(TextSecondary);

                                    header.Cell().Background(BgSubtle).Padding(9)
                                        .Text("Remark").FontSize(9).Bold().FontColor(TextSecondary);
                                });

                                // ---------- BODY ROWS ----------
                                var subjects = report.SubjectResults ?? new List<TermSubjectResultDto>();
                                var isAlt = false;

                                foreach (var sub in subjects)
                                {
                                    var rowBg = isAlt ? BgSubtle : BgCard;

                                    table.Cell().Background(rowBg).Padding(9)
                                        .Text(sub.SubjectName ?? "—").FontSize(10).Bold().FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.Test1Score.ToString("0.##")).FontSize(10).FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.Test2Score.ToString("0.##")).FontSize(10).FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.ExamScore.ToString("0.##")).FontSize(10).FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.TotalScore.ToString("0.##")).FontSize(10).Bold().FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.Grade ?? "—").FontSize(10).Bold().FontColor(GradeColor(sub.Grade));

                                    table.Cell().Background(rowBg).Padding(9).AlignRight()
                                        .Text(sub.SubjectPosition.ToString()).FontSize(10).FontColor(TextPrimary);

                                    table.Cell().Background(rowBg).Padding(9)
                                        .Text(sub.Remark ?? "—").FontSize(10).FontColor(TextSecondary);

                                    isAlt = !isAlt;
                                }
                            });
                        });

                        // ---------- Remarks ----------
                        if (!string.IsNullOrWhiteSpace(report.TeacherRemark) ||
                            !string.IsNullOrWhiteSpace(report.PrincipalRemark))
                        {
                            col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(16).Column(rem =>
                            {
                                rem.Spacing(10);
                                rem.Item().Text("Remarks").Bold().FontSize(12).FontColor(TextPrimary);

                                // ---- Class Teacher Remark ----
                                if (!string.IsNullOrWhiteSpace(report.TeacherRemark))
                                {
                                    rem.Item().Background(AccentBlueLight).CornerRadius(8).Padding(10).Column(c =>
                                    {
                                        c.Spacing(3);
                                        c.Item().Text("CLASS TEACHER").FontSize(8).Bold()
                                            .FontColor(AccentBlue).LetterSpacing(0.05f);
                                        c.Item().Text(report.TeacherRemark)
                                            .FontSize(10).FontColor(TextPrimary);
                                    });
                                }

                                // ---- Principal Remark ----
                                if (!string.IsNullOrWhiteSpace(report.PrincipalRemark))
                                {
                                    rem.Item().Background(AccentLight).CornerRadius(8).Padding(10).Column(c =>
                                    {
                                        c.Spacing(3);
                                        c.Item().Text("PRINCIPAL").FontSize(8).Bold()
                                            .FontColor(Accent).LetterSpacing(0.05f);
                                        c.Item().Text(report.PrincipalRemark)
                                            .FontSize(10).FontColor(TextPrimary);
                                    });
                                }
                            });
                        }

                        // ---------- Signature strip ----------
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingTop(24).LineHorizontal(1).LineColor(Border);
                                c.Item().PaddingTop(4).Text("Class Teacher").FontSize(8).FontColor(TextMuted);
                            });

                            row.ConstantItem(60);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingTop(24).LineHorizontal(1).LineColor(Border);
                                c.Item().PaddingTop(4).Text("Principal").FontSize(8).FontColor(TextMuted);
                            });
                        });
                    });

                    // ============================================
                    // FOOTER
                    // ============================================
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted));
                        text.Span("DIBA School Portal  •  Computer-generated report card  •  Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
        #endregion

        // ==========================================================
        // PAYMENT RECEIPT
        // ==========================================================
        #region GeneratePaymentReceiptPdf
        public byte[] GeneratePaymentReceiptPdf(StudentPaymentReceiptDto receipt)
        {
            var profileImage = LoadProfileImage(receipt.ProfilePictureUrl);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(1.0f, Unit.Centimetre);
                    page.PageColor(BgPage);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica").FontColor(TextPrimary));

                    // ---------- HEADER ----------
                    page.Header().ShowOnce().Element(header =>
                    {
                        header.Background(Accent).CornerRadius(12).Padding(16).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().Text("DIBA SCHOOL").Bold().FontSize(15).FontColor(Colors.White);
                                c.Item().Text("Official Payment Receipt")
                                    .FontSize(9).FontColor(Colors.White);
                            });

                            // PAID badge — background comes BEFORE .Text()
                            row.ConstantItem(70).AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().Background("#047857").CornerRadius(6).Padding(7)
                                    .AlignCenter().AlignMiddle()
                                    .Text("PAID").Bold().FontSize(10).FontColor(Colors.White);
                            });
                        });
                    });

                    // ---------- CONTENT ----------
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);

                        // ---------- Student card ----------
                        col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(14).Row(row =>
                        {
                            if (profileImage != null)
                            {
                                row.ConstantItem(56).Height(56)
                                    .Background(BgSubtle).CornerRadius(28)
                                    .Image(profileImage).FitArea();
                            }
                            else
                            {
                                row.ConstantItem(56).Height(56)
                                    .Background(Accent).CornerRadius(28)
                                    .AlignCenter().AlignMiddle()
                                    .Text(GetInitials(receipt.StudentName))
                                    .Bold().FontColor(Colors.White).FontSize(16);
                            }

                            row.ConstantItem(12);

                            row.RelativeItem().Column(c =>
                            {
                                c.Spacing(2);
                                c.Item().Text(receipt.StudentName ?? "—").Bold().FontSize(13);
                                c.Item().Text($"Class: {receipt.ClassName ?? "—"}")
                                    .FontSize(10).FontColor(TextSecondary);
                                c.Item().Text($"Session / Term: {receipt.SessionName} - {receipt.TermName}")
                                    .FontSize(9).FontColor(TextMuted);
                            });
                        });

                        // ---------- Receipt meta ----------
                        col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("RECEIPT NO").FontSize(7).Bold()
                                    .FontColor(TextMuted).LetterSpacing(0.05f);
                                c.Item().Text(receipt.ReferenceNumber ?? "—")
                                    .FontSize(10).Bold().FontColor(TextPrimary);
                            });

                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("DATE").FontSize(7).Bold()
                                    .FontColor(TextMuted).LetterSpacing(0.05f);
                                c.Item().AlignRight().Text(receipt.PaymentDate.ToString("dd MMM, yyyy"))
                                    .FontSize(10).Bold().FontColor(TextPrimary);
                            });
                        });

                        // ---------- Fee items table ----------
                        col.Item().Background(BgCard).CornerRadius(12).Border(1).BorderColor(Border).Padding(12).Column(section =>
                        {
                            section.Spacing(8);

                            section.Item().Text("Fee Breakdown")
                                .Bold().FontSize(11).FontColor(TextPrimary);

                            section.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(BgSubtle).Padding(8)
                                        .Text("Description").FontSize(9).Bold().FontColor(TextSecondary);
                                    header.Cell().Background(BgSubtle).Padding(8).AlignRight()
                                        .Text("Amount").FontSize(9).Bold().FontColor(TextSecondary);
                                });

                                foreach (var item in receipt.FeeItems ?? new List<FeeItemDto>())
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Border).Padding(8)
                                        .Text(item.Name ?? "—").FontSize(10).FontColor(TextPrimary);
                                    table.Cell().BorderBottom(1).BorderColor(Border).Padding(8).AlignRight()
                                        .Text($"₦{item.Amount:N2}").FontSize(10).FontColor(TextPrimary);
                                }
                            });
                        });

                        // ---------- Total ----------
                        col.Item().Background(AccentLight).CornerRadius(12).Border(1).BorderColor("#A7F3D0").Padding(14).Row(row =>
                        {
                            row.RelativeItem().AlignMiddle().Text("TOTAL PAID")
                                .Bold().FontSize(11).FontColor(Accent);

                            row.RelativeItem().AlignRight().AlignMiddle().Text($"₦{receipt.TotalAmountPaid:N2}")
                                .Bold().FontSize(18).FontColor(Accent);
                        });

                        col.Item().PaddingTop(6).AlignCenter().Text("Thank you for your payment.")
                            .Italic().FontSize(9).FontColor(TextSecondary);
                    });

                    // ---------- FOOTER ----------
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(7).FontColor(TextMuted));
                        text.Span("Computer-generated receipt from DIBA School Portal  •  No signature required");
                    });
                });
            }).GeneratePdf();
        }
        #endregion

        // ==========================================================
        // HELPERS
        // ==========================================================
        #region Helpers

        private byte[]? LoadProfileImage(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    var bytes = _files
                        .DownloadFileAsync(url, AvatarBucket)
                        .GetAwaiter()
                        .GetResult();

                    if (bytes != null && bytes.Length > 0)
                        return bytes;

                    _logger.LogWarning("Avatar not found in Supabase: {Url}", url);
                    return null;
                }

                if (File.Exists(url))
                    return File.ReadAllBytes(url);

                var localPath = Path.Combine(
                    _webHostEnvironment.WebRootPath ?? Directory.GetCurrentDirectory(),
                    url.TrimStart('/'));

                if (File.Exists(localPath))
                    return File.ReadAllBytes(localPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile image: {Url}", url);
            }

            return null;
        }

        private static string GetInitials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }

        private static void StatTile(IContainer container, string label, string value)
        {
            container
                .Background(BgCard)
                .CornerRadius(12)
                .Border(1)
                .BorderColor(Border)
                .Padding(14)
                .Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Text(label.ToUpperInvariant())
                        .FontSize(8).Bold().FontColor(TextMuted).LetterSpacing(0.05f);
                    col.Item().Text(value).Bold().FontSize(16).FontColor(TextPrimary);
                });
        }

        private static string GradeColor(string? grade) => grade switch
        {
            "A" => Accent,
            "B" => AccentBlue,
            "C" => AccentAmber,
            "D" or "E" => "#EA580C",
            "F" => AccentRose,
            _ => TextPrimary
        };

        private static string GradeBg(string? grade) => grade switch
        {
            "A" => AccentLight,
            "B" => AccentBlueLight,
            "C" => AccentAmberLight,
            "D" or "E" => "#FFF7ED",
            "F" => AccentRoseLight,
            _ => BgSubtle
        };

        #endregion
    }
}