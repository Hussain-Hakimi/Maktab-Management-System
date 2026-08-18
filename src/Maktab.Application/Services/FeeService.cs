using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class FeeService(
    IFeeRepository feeRepository,
    IStudentRepository studentRepository,
    IClassSubjectRepository classSubjectRepository) : IFeeService
{
    public async Task<IReadOnlyList<FeeRecordDto>> GetAllFeesAsync(CancellationToken cancellationToken = default)
    {
        var fees = await feeRepository.GetFeeRecordsAsync(cancellationToken);
        return await EnrichFeesAsync(fees, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeRecordDto>> GetFeesByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var fees = await feeRepository.GetFeeRecordsByStudentAsync(studentId, cancellationToken);
        return await EnrichFeesAsync(fees, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeRecordDto>> GetOutstandingFeesAsync(int? classId = null, CancellationToken cancellationToken = default)
    {
        var fees = await feeRepository.GetFeeRecordsAsync(cancellationToken);
        var enriched = await EnrichFeesAsync(fees, cancellationToken);

        return enriched
            .Where(f => !f.IsSettled)
            .Where(f => classId is null || f.ClassId == classId.Value)
            .OrderBy(f => f.ClassName)
            .ThenBy(f => f.StudentName)
            .ToList();
    }

    public async Task<IReadOnlyList<StudentFeeSummaryDto>> GetStudentFeeSummariesAsync(int? classId = null, CancellationToken cancellationToken = default)
    {
        var fees = await GetOutstandingFeesAsync(null, cancellationToken);
        var students = await studentRepository.GetStudentsAsync(cancellationToken);
        var classes = (await classSubjectRepository.GetClassesAsync(cancellationToken)).ToDictionary(c => c.ClassId);

        return fees
            .GroupBy(f => f.StudentId)
            .Select(g =>
            {
                var student = students.FirstOrDefault(s => s.StudentId == g.Key);
                var className = student is not null && classes.TryGetValue(student.ClassId, out var c) ? c.GradeName : string.Empty;
                return new StudentFeeSummaryDto
                {
                    StudentId = g.Key,
                    StudentName = g.First().StudentName,
                    RollNumber = g.First().RollNumber,
                    ClassName = className,
                    TotalDue = g.Sum(f => f.AmountDue),
                    TotalPaid = g.Sum(f => f.AmountPaid),
                    OpenFeeCount = g.Count()
                };
            })
            .Where(s => classId is null || (students.FirstOrDefault(st => st.StudentId == s.StudentId)?.ClassId == classId.Value))
            .OrderBy(s => s.ClassName)
            .ThenBy(s => s.StudentName)
            .ToList();
    }

    public async Task<IReadOnlyList<FeePaymentDto>> GetPaymentsByFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));

        var fee = await feeRepository.GetFeeRecordByIdAsync(feeId, cancellationToken);
        if (fee is null) throw new InvalidOperationException("رکورد فیس یافت نشد.");

        var payments = await feeRepository.GetPaymentsByFeeAsync(feeId, cancellationToken);
        var student = await studentRepository.GetStudentByIdAsync(fee.StudentId, cancellationToken);
        var studentName = student is null ? $"شاگرد {fee.StudentId}" : $"{student.FirstName} {student.LastName}";

        return payments.Select(p => new FeePaymentDto
        {
            PaymentId = p.PaymentId,
            FeeId = p.FeeId,
            StudentName = studentName,
            FeeTitle = fee.Title,
            AmountPaid = p.AmountPaid,
            PaymentDate = p.PaymentDate,
            ReceiptNumber = p.ReceiptNumber
        }).ToList();
    }

    public async Task<IReadOnlyList<FeePaymentDto>> GetPaymentsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");

        var fees = (await feeRepository.GetFeeRecordsByStudentAsync(studentId, cancellationToken)).ToDictionary(f => f.FeeId);
        var payments = await feeRepository.GetPaymentsByStudentAsync(studentId, cancellationToken);

        return payments.Select(p => new FeePaymentDto
        {
            PaymentId = p.PaymentId,
            FeeId = p.FeeId,
            StudentName = $"{student.FirstName} {student.LastName}",
            FeeTitle = fees.TryGetValue(p.FeeId, out var fee) ? fee.Title : $"فیس {p.FeeId}",
            AmountPaid = p.AmountPaid,
            PaymentDate = p.PaymentDate,
            ReceiptNumber = p.ReceiptNumber
        }).ToList();
    }

    public async Task<int> CreateFeeRecordAsync(int studentId, string title, decimal amountDue, DateOnly? dueDate, string? academicYear, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        ValidateFee(title, amountDue);

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException($"شاگرد با آیدی {studentId} یافت نشد.");

        var feeRecord = new FeeRecord
        {
            StudentId = studentId,
            Title = title.Trim(),
            AmountDue = amountDue,
            DueDate = dueDate,
            AcademicYear = string.IsNullOrWhiteSpace(academicYear) ? null : academicYear.Trim()
        };

        return await feeRepository.CreateFeeRecordAsync(feeRecord, cancellationToken);
    }

    public async Task<FeePaymentDto> RecordPaymentAsync(int feeId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "مبلغ پرداخت باید بزرگتر از صفر باشد.");

        var fee = await feeRepository.GetFeeRecordByIdAsync(feeId, cancellationToken);
        if (fee is null) throw new InvalidOperationException("رکورد فیس یافت نشد.");

        var paidSoFar = await feeRepository.GetTotalPaidForFeeAsync(feeId, cancellationToken);
        var outstanding = fee.AmountDue - paidSoFar;
        if (outstanding <= 0)
        {
            throw new InvalidOperationException("این فیس قبلاً به‌طور کامل پرداخت شده است.");
        }

        if (amount > outstanding)
        {
            throw new InvalidOperationException($"مبلغ پرداخت ({amount:N0}) از باقیمانده فیس ({outstanding:N0}) بیشتر است.");
        }

        var payment = new FeePayment
        {
            FeeId = feeId,
            AmountPaid = amount,
            PaymentDate = DateOnly.FromDateTime(DateTime.Now),
            ReceiptNumber = string.Empty // assigned by the repository in the same transaction
        };

        var paymentId = await feeRepository.CreatePaymentAsync(payment, cancellationToken);
        var saved = await feeRepository.GetPaymentByIdAsync(paymentId, cancellationToken)
            ?? throw new InvalidOperationException("ثبت پرداخت ناموفق بود.");

        var student = await studentRepository.GetStudentByIdAsync(fee.StudentId, cancellationToken);

        return new FeePaymentDto
        {
            PaymentId = saved.PaymentId,
            FeeId = saved.FeeId,
            StudentName = student is null ? $"شاگرد {fee.StudentId}" : $"{student.FirstName} {student.LastName}",
            FeeTitle = fee.Title,
            AmountPaid = saved.AmountPaid,
            PaymentDate = saved.PaymentDate,
            ReceiptNumber = saved.ReceiptNumber
        };
    }

    public async Task RemoveFeeRecordAsync(int feeId, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));

        var paid = await feeRepository.GetTotalPaidForFeeAsync(feeId, cancellationToken);
        if (paid > 0)
        {
            throw new InvalidOperationException("برای این فیس پرداخت ثبت شده و قابل حذف نیست.");
        }

        await feeRepository.DeleteFeeRecordAsync(feeId, cancellationToken);
    }

    private async Task<IReadOnlyList<FeeRecordDto>> EnrichFeesAsync(IReadOnlyList<FeeRecord> fees, CancellationToken cancellationToken)
    {
        var students = (await studentRepository.GetStudentsAsync(cancellationToken)).ToDictionary(s => s.StudentId);
        var classes = (await classSubjectRepository.GetClassesAsync(cancellationToken)).ToDictionary(c => c.ClassId);

        var result = new List<FeeRecordDto>();
        foreach (var fee in fees)
        {
            students.TryGetValue(fee.StudentId, out var student);
            var className = student is not null && classes.TryGetValue(student.ClassId, out var c) ? c.GradeName : string.Empty;
            var paid = await feeRepository.GetTotalPaidForFeeAsync(fee.FeeId, cancellationToken);

            result.Add(new FeeRecordDto
            {
                FeeId = fee.FeeId,
                StudentId = fee.StudentId,
                StudentName = student is null ? $"شاگرد {fee.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                ClassName = className,
                ClassId = student?.ClassId ?? 0,
                Title = fee.Title,
                AmountDue = fee.AmountDue,
                AmountPaid = paid,
                DueDate = fee.DueDate,
                AcademicYear = fee.AcademicYear
            });
        }

        return result;
    }

    private static void ValidateFee(string title, decimal amountDue)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("عنوان فیس ضروری است.", nameof(title));
        if (amountDue <= 0) throw new ArgumentOutOfRangeException(nameof(amountDue), "مبلغ فیس باید بزرگتر از صفر باشد.");
    }
}
