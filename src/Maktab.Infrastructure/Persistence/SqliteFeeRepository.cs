using System.Globalization;
using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteFeeRepository(IConnectionStringProvider connectionStringProvider) : IFeeRepository
{
    private const string FeeColumns = "FeeID, StudentID, Title, AmountDue, DueDate, AcademicYear, CreatedDate";
    private const string PaymentColumns = "PaymentID, FeeID, AmountPaid, PaymentDate, ReceiptNumber";

    public async Task<IReadOnlyList<FeeRecord>> GetFeeRecordsAsync(CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {FeeColumns}
FROM tbl_FeeRecords
ORDER BY FeeID DESC;";

        return await QueryFeesAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeRecord>> GetFeeRecordsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {FeeColumns}
FROM tbl_FeeRecords
WHERE StudentID = $studentId
ORDER BY FeeID DESC;";

        return await QueryFeesAsync(sql, ("$studentId", studentId), cancellationToken);
    }

    public async Task<FeeRecord?> GetFeeRecordByIdAsync(int feeId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {FeeColumns}
FROM tbl_FeeRecords
WHERE FeeID = $feeId;";

        var fees = await QueryFeesAsync(sql, ("$feeId", feeId), cancellationToken);
        return fees.Count > 0 ? fees[0] : null;
    }

    public async Task<int> CreateFeeRecordAsync(FeeRecord feeRecord, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_FeeRecords (StudentID, Title, AmountDue, DueDate, AcademicYear)
VALUES ($studentId, $title, $amountDue, $dueDate, $academicYear);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$studentId", feeRecord.StudentId);
            command.Parameters.AddWithValue("$title", feeRecord.Title);
            command.Parameters.AddWithValue("$amountDue", (double)feeRecord.AmountDue);
            command.Parameters.AddWithValue("$dueDate", feeRecord.DueDate is null ? DBNull.Value : FormatDate(feeRecord.DueDate.Value));
            command.Parameters.AddWithValue("$academicYear", (object?)feeRecord.AcademicYear ?? DBNull.Value);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteFeeRecordAsync(int feeId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_FeeRecords WHERE FeeID = $feeId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$feeId", feeId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Fee record not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<FeePayment>> GetPaymentsByFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {PaymentColumns}
FROM tbl_FeePayments
WHERE FeeID = $feeId
ORDER BY PaymentID;";

        return await QueryPaymentsAsync(sql, ("$feeId", feeId), cancellationToken);
    }

    public async Task<IReadOnlyList<FeePayment>> GetPaymentsByStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT p.PaymentID, p.FeeID, p.AmountPaid, p.PaymentDate, p.ReceiptNumber
FROM tbl_FeePayments p
INNER JOIN tbl_FeeRecords f ON f.FeeID = p.FeeID
WHERE f.StudentID = $studentId
ORDER BY p.PaymentID DESC;";

        return await QueryPaymentsAsync(sql, ("$studentId", studentId), cancellationToken);
    }

    public async Task<decimal> GetTotalPaidForFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT COALESCE(SUM(AmountPaid), 0)
FROM tbl_FeePayments
WHERE FeeID = $feeId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$feeId", feeId);

        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<int> CreatePaymentAsync(FeePayment payment, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = @"
INSERT INTO tbl_FeePayments (FeeID, AmountPaid, PaymentDate, ReceiptNumber)
VALUES ($feeId, $amountPaid, $paymentDate, $pendingReceipt);
SELECT last_insert_rowid();";
                insert.Parameters.AddWithValue("$feeId", payment.FeeId);
                insert.Parameters.AddWithValue("$amountPaid", (double)payment.AmountPaid);
                insert.Parameters.AddWithValue("$paymentDate", FormatDate(payment.PaymentDate));
                insert.Parameters.AddWithValue("$pendingReceipt", $"PENDING-{Guid.NewGuid():N}");

                payment.PaymentId = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken));
            }

            var receiptNumber = $"RCP-{payment.PaymentDate:yyyyMMdd}-{payment.PaymentId:D5}";

            await using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = "UPDATE tbl_FeePayments SET ReceiptNumber = $receipt WHERE PaymentID = $paymentId;";
                update.Parameters.AddWithValue("$receipt", receiptNumber);
                update.Parameters.AddWithValue("$paymentId", payment.PaymentId);

                var affected = await update.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0) throw new InvalidOperationException("Payment not found after insert.");
            }

            payment.ReceiptNumber = receiptNumber;
            await transaction.CommitAsync(cancellationToken);
            return payment.PaymentId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FeePayment?> GetPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var sql = $@"
SELECT {PaymentColumns}
FROM tbl_FeePayments
WHERE PaymentID = $paymentId;";

        var payments = await QueryPaymentsAsync(sql, ("$paymentId", paymentId), cancellationToken);
        return payments.Count > 0 ? payments[0] : null;
    }

    private async Task<IReadOnlyList<FeeRecord>> QueryFeesAsync(
        string sql,
        (string Name, int Value)? parameter,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter is not null)
        {
            command.Parameters.AddWithValue(parameter.Value.Name, parameter.Value.Value);
        }

        var result = new List<FeeRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FeeRecord
            {
                FeeId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                Title = reader.GetString(2),
                AmountDue = Convert.ToDecimal(reader.GetDouble(3)),
                DueDate = reader.IsDBNull(4) ? null : ParseDate(reader.GetString(4)),
                AcademicYear = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedDate = reader.IsDBNull(6) ? default : DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<FeePayment>> QueryPaymentsAsync(
        string sql,
        (string Name, int Value)? parameter,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter is not null)
        {
            command.Parameters.AddWithValue(parameter.Value.Name, parameter.Value.Value);
        }

        var result = new List<FeePayment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FeePayment
            {
                PaymentId = reader.GetInt32(0),
                FeeId = reader.GetInt32(1),
                AmountPaid = Convert.ToDecimal(reader.GetDouble(2)),
                PaymentDate = ParseDate(reader.GetString(3)),
                ReceiptNumber = reader.GetString(4)
            });
        }

        return result;
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd");
}
