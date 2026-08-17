using Maktab.Application.Abstractions;
using Maktab.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Maktab.Infrastructure.Reports;

public sealed class QuestPdfReportCardGenerator : IPdfReportCardGenerator
{
    static QuestPdfReportCardGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task GeneratePdfReportAsync(
        StudentReportCardDto reportCard,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, reportCard));
                page.Content().Element(c => ComposeContent(c, reportCard));
                page.Footer().Element(c => ComposeFooter(c, reportCard));
            });
        });

        doc.GeneratePdf(outputFilePath);
        return Task.CompletedTask;
    }

    private static void ComposeHeader(IContainer container, StudentReportCardDto reportCard)
    {
        container.Column(col =>
        {
            col.Item().BorderBottom(2).BorderColor(Colors.Blue.Darken3).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("جمهوری اسلامی افغانستان").FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text("وزارت معارف — اداره تعلیمات عمومی").FontSize(10).FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"سال تعلیمی: {reportCard.AcademicYear}").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().AlignCenter().Text("اطلاع‌نامه نمرات سالانه شاگرد").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().AlignCenter().Text("Afghan School Student Report Card").FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"تاریخ صدور: {reportCard.IssueDate}").FontSize(10);
                    c.Item().Text($"شماره اساس: {reportCard.RollNumber}").FontSize(11).Bold();
                    c.Item().Text($"کد شاگرد: {reportCard.StudentId:D4}").FontSize(10);
                });
            });

            col.Item().PaddingTop(10).PaddingBottom(8).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("نام شاگرد: ").Bold();
                    t.Span($"{reportCard.FirstName} {reportCard.LastName}");
                });

                row.RelativeItem().Text(t =>
                {
                    t.Span("نام پدر: ").Bold();
                    t.Span(reportCard.FatherName);
                });

                row.RelativeItem().Text(t =>
                {
                    t.Span("صنف: ").Bold();
                    t.Span(reportCard.ClassName);
                });

                row.RelativeItem().Text(t =>
                {
                    t.Span("شماره اساس: ").Bold();
                    t.Span(reportCard.RollNumber);
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, StudentReportCardDto reportCard)
    {
        container.PaddingTop(10).Column(col =>
        {
            // Marks Table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // No
                    columns.RelativeColumn(3);   // Subject
                    columns.RelativeColumn(2);   // Midterm (40)
                    columns.RelativeColumn(2);   // Final (60)
                    columns.RelativeColumn(2);   // Total (100)
                    columns.RelativeColumn(2);   // Pass/Fail
                });

                // Header Row
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("شماره").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).Text("مضمون").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("چهارونیم‌ماهه (۴۰)").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("سالانه (۶۰)").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("مجموع (۱۰۰)").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken3).Padding(4).AlignCenter().Text("نتیجه").FontColor(Colors.White).Bold();
                });

                int idx = 1;
                foreach (var mark in reportCard.SubjectMarks)
                {
                    var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(idx.ToString());
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(mark.SubjectName);
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(mark.MidtermScore.ToString("0.##"));
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(mark.FinalScore.ToString("0.##"));
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(mark.TotalScore.ToString("0.##")).Bold();

                    var resultColor = mark.IsPass ? Colors.Green.Darken2 : Colors.Red.Darken2;
                    var resultText = mark.IsPass ? "کامیاب" : "ناکام";
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(resultText).FontColor(resultColor).Bold();

                    idx++;
                }

                // Summary Row
                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(6).Text("مجموع کل و اوسط فیصدی").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("-");
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("-");
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text($"{reportCard.TotalObtainedScore:0.##} / {reportCard.TotalMaxScore:0.##}").Bold();
                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text($"اوسط: {reportCard.AveragePercentage:0.##}% | رتبه: {ToDariGrade(reportCard.OverallGrade)} ({reportCard.OverallGrade})").Bold();
            });

            col.Item().PaddingTop(12).Row(row =>
            {
                // Summary Statistics Box
                row.RelativeItem(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                {
                    c.Item().Text("خلاصه نتایج امتحانات").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().PaddingTop(4).Text($"• تعداد مضامین کامیاب: {reportCard.PassedSubjectsCount} مضمون");
                    c.Item().Text($"• تعداد مضامین ناکام: {reportCard.FailedSubjectsCount} مضمون");
                    c.Item().Text($"• مجموع ایام غیرحاضری: {reportCard.AbsenceDays} روز");
                });

                row.ConstantItem(12);

                // Promotion Decision Box
                var promoBorder = reportCard.PromotionOutcome switch
                {
                    PromotionOutcome.Promoted => Colors.Green.Darken2,
                    PromotionOutcome.Conditional => Colors.Yellow.Darken3,
                    _ => Colors.Red.Darken2 // Repeat
                };
                var promoBg = reportCard.PromotionOutcome switch
                {
                    PromotionOutcome.Promoted => Colors.Green.Lighten5,
                    PromotionOutcome.Conditional => Colors.Yellow.Lighten5,
                    _ => Colors.Red.Lighten5 // Repeat
                };
                var promoTitle = reportCard.PromotionOutcome switch
                {
                    PromotionOutcome.Promoted => "نتیجه: ارتقاء نموده است ✓",
                    PromotionOutcome.Conditional => "نتیجه: مشروط (نیاز به بازنگری) 🟡",
                    _ => "نتیجه: تکرار صنف (ناکام) ✗"
                };

                row.RelativeItem(4).Border(1.5f).BorderColor(promoBorder).Background(promoBg).Padding(8).Column(c =>
                {
                    c.Item().Text(promoTitle).FontSize(13).Bold().FontColor(promoBorder);
                    c.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("وضعیت نهایی: ").Bold();
                        t.Span(reportCard.PromotionStatusText);
                    });

                    if (!string.IsNullOrWhiteSpace(reportCard.FailureReason))
                    {
                        c.Item().PaddingTop(2).Text($"علت عدم ارتقاء: {reportCard.FailureReason}").FontColor(Colors.Red.Darken3);
                    }
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container, StudentReportCardDto reportCard)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(6).AlignCenter().Column(c =>
                {
                    c.Item().Text("امضای استاد نگران صنف").Bold();
                    c.Item().PaddingTop(20).Text("....................................");
                });

                row.ConstantItem(30);

                row.RelativeItem().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(6).AlignCenter().Column(c =>
                {
                    c.Item().Text("مهر و امضای سرمعلم / مدیر مکتب").Bold();
                    c.Item().PaddingTop(20).Text("....................................");
                });
            });

            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Text("سیستم مدیریت مکاتب افغانستان — نسخه ۱.۰.۱ آفلاین").FontSize(8).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text("صفحه ۱ از ۱").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static string ToDariGrade(LetterGrade grade) => grade switch
    {
        LetterGrade.A => "الف",
        LetterGrade.B => "ب",
        LetterGrade.C => "ج",
        LetterGrade.D => "د",
        _ => "ه" // LetterGrade.F
    };
}
