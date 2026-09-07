using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class StudentService(
    IStudentRepository repository,
    IStudentAcademicEnrollmentRepository enrollmentRepository) : IStudentService
{
    public Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetStudentsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        return repository.GetStudentsByClassAsync(classId, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetStudentsByClassAndAcademicYearAsync(
        int classId,
        int academicYearId,
        CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));
        if (academicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(academicYearId));

        var enrollments = await enrollmentRepository.GetByClassAndAcademicYearAsync(
            classId,
            academicYearId,
            cancellationToken);

        if (enrollments.Count == 0)
        {
            return Array.Empty<Student>();
        }

        var students = new List<Student>(enrollments.Count);
        foreach (var enrollment in enrollments)
        {
            var student = await repository.GetStudentByIdAsync(enrollment.StudentId, cancellationToken);
            if (student is not null)
            {
                students.Add(student);
            }
        }

        return students;
    }

    public async Task<IReadOnlyList<StudentAcademicEnrollment>> GetStudentAcademicHistoryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        return await enrollmentRepository.GetByStudentAsync(studentId, cancellationToken);
    }

    public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        return repository.GetStudentByIdAsync(studentId, cancellationToken);
    }

    public async Task<int> RegisterStudentAsync(
        string firstName,
        string lastName,
        string fatherName,
        int classId,
        string rollNumber,
        CancellationToken cancellationToken = default)
    {
        ValidateStudentInfo(firstName, lastName, fatherName, classId, rollNumber);

        if (await repository.ExistsByRollNumberAsync(classId, rollNumber.Trim(), cancellationToken))
        {
            throw new InvalidOperationException($"Roll number '{rollNumber}' is already taken in this class.");
        }

        var student = new Student
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            FatherName = fatherName.Trim(),
            ClassId = classId,
            RollNumber = rollNumber.Trim(),
            RegistrationDate = DateTime.Now
        };

        return await repository.CreateStudentAsync(student, cancellationToken);
    }

    public async Task UpdateStudentAsync(
        int studentId,
        string firstName,
        string lastName,
        string fatherName,
        int classId,
        string rollNumber,
        CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        ValidateStudentInfo(firstName, lastName, fatherName, classId, rollNumber);

        var existing = await repository.GetStudentByIdAsync(studentId, cancellationToken);
        if (existing == null) throw new InvalidOperationException("Student not found.");

        if (existing.ClassId != classId || existing.RollNumber != rollNumber.Trim())
        {
            if (await repository.ExistsByRollNumberAsync(classId, rollNumber.Trim(), cancellationToken))
            {
                throw new InvalidOperationException($"Roll number '{rollNumber}' is already taken in this class.");
            }
        }

        var student = new Student
        {
            StudentId = studentId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            FatherName = fatherName.Trim(),
            ClassId = classId,
            RollNumber = rollNumber.Trim(),
            RegistrationDate = existing.RegistrationDate,
            AdmissionNumber = existing.AdmissionNumber
        };

        await repository.UpdateStudentAsync(student, cancellationToken);
    }

    public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        return repository.DeleteStudentAsync(studentId, cancellationToken);
    }

    public async Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default)
    {
        if (classId <= 0) throw new ArgumentOutOfRangeException(nameof(classId));

        var students = await repository.GetStudentsByClassAsync(classId, cancellationToken);

        int max = 0;
        foreach (var student in students)
        {
            if (int.TryParse(student.RollNumber, out var num) && num > max)
            {
                max = num;
            }
        }

        return max + 1;
    }

    private static void ValidateStudentInfo(
        string firstName,
        string lastName,
        string fatherName,
        int classId,
        string rollNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(fatherName))
            throw new ArgumentException("Father name is required.", nameof(fatherName));
        if (string.IsNullOrWhiteSpace(rollNumber))
            throw new ArgumentException("Roll number is required.", nameof(rollNumber));
        if (classId <= 0)
            throw new ArgumentOutOfRangeException(nameof(classId), "Valid class must be selected.");
    }
}
