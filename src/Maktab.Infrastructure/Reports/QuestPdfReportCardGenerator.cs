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
        ReportCardTemplateType templateType,
        CancellationToken cancellationToken = default)
    {
        // Note: The report type is passed via reportCard.ReportType
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, reportCard));
                page.Content().Element(c => ComposeContent(c, reportCard));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        doc.GeneratePdf(outputFilePath);
        return Task.CompletedTask;
    }

    private static void ComposeHeader(IContainer container, StudentReportCardDto reportCard)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(reportCard.ReportType == ReportCardType.Midterm
                    ? "اطلاع‌نامه امتحانات چهارماهه"
                    : "اطلاع‌نامه نمرات سالانه شاگرد")
                .FontSize(16).Bold();

            col.Item().AlignCenter().Text($"سال تعلیمی: {reportCard.AcademicYear}").FontSize(10).FontColor(Colors.Grey.Darken1);
            col.Item().AlignCenter().Text($"تاریخ صدور: {reportCard.IssueDate}").FontSize(10).FontColor(Colors.Grey.Darken1);

            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text($"نام: {reportCard.FirstName} {reportCard.LastName}");
                row.RelativeItem().Text($"صنف: {reportCard.ClassName}");
            });
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"شماره اساس: {reportCard.RollNumber}");
                row.RelativeItem().Text($"نام پدر: {reportCard.FatherName}");
            });
        });
    }

    private static void ComposeContent(IContainer container, StudentReportCardDto reportCard)
    {
        if (reportCard.ReportType == ReportCardType.Midterm)
        {
            ComposeMidtermTable(container, reportCard);
        }
        else
        {
            ComposeAnnualTable(container, reportCard);
        }
    }

    private static void ComposeMidtermTable(IContainer container, StudentReportCardDto reportCard)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // No
                    columns.RelativeColumn(3);    // Subject
                    columns.RelativeColumn(2);    // Midterm Score (40)
                    columns.RelativeColumn(2);    // Percentage
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("شماره").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("مضمون").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("نمره چهارماهه (۴۰)").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignCenter().Text("فیصدی").Bold();
                });

                int idx = 1;
                foreach (var mark in reportCard.SubjectMarks)
                {
                    var percent = mark.MidtermScore / 40m * 100m;

                    table.Cell().Padding(4).AlignCenter().Text(idx.ToString());
                    table.Cell().Padding(4).Text(mark.SubjectName);
                    table.Cell().Padding(4).AlignCenter().Text(mark.MidtermScore.ToString("0.##"));
                    table.Cell().Padding(4).AlignCenter().Text($"{percent:0.##}%");
                    idx++;
                }
            });

            // No promotion outcome block for midterm
        });
    }

    private static void ComposeAnnualTable(IContainer container, StudentReportCardDto reportCard)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // No
                    columns.RelativeColumn(3);    // Subject
                    columns.RelativeColumn(2);    // Midterm (40)
                    columns.RelativeColumn(2);    // Final (60)
                    columns.RelativeColumn(2);    // Total (100)
                    columns.RelativeColumn(2);    // Pass/Fail
                });

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

                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(6).Text("مجموع کل و اوسط فیصدی").Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("-");
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text("-");
                table.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text($"{reportCard.TotalObtainedScore:0.##} / {reportCard.TotalMaxScore:0.##}").Bold();
                table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(6).AlignCenter().Text($"اوسط: {reportCard.AveragePercentage:0.##}% | رتبه: {ToDariGrade(reportCard.OverallGrade)} ({reportCard.OverallGrade})").Bold();
            });

            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem(3).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                {
                    c.Item().Text("خلاصه نتایج امتحانات").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                    c.Item().PaddingTop(4).Text($"• تعداد مضامین کامیاب: {reportCard.PassedSubjectsCount} مضمون");
                    c.Item().Text($"• تعداد مضامین ناکام: {reportCard.FailedSubjectsCount} مضمون");
                    c.Item().Text($"• مجموع ایام غیرحاضری: {reportCard.AbsenceDays} روز");
                });

                row.ConstantItem(12);

                var promoBorder = reportCard.PromotionOutcome switch
                {
                    PromotionOutcome.Promoted => Colors.Green.Darken2,
                    PromotionOutcome.Conditional => Colors.Yellow.Darken3,
                    _ => Colors.Red.Darken2
                };
                var promoBg = reportCard.PromotionOutcome switch
                {
                    PromotionOutcome.Promoted => Colors.Green.Lighten5,
                    PromotionOutcome.Conditional => Colors.Yellow.Lighten5,
                    _ => Colors.Red.Lighten5
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

    private static void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(20).AlignCenter().Text("سیستم مدیریت مکاتب افغانستان — نسخه ۱.۹.۰ آفلاین")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static string ToDariGrade(LetterGrade grade) => grade switch
    {
        LetterGrade.A => "الف",
        LetterGrade.B => "ب",
        LetterGrade.C => "ج",
        LetterGrade.D => "د",
        _ => "ه"
    };
}
