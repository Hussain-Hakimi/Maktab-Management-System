using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class ClassSubjectService(IClassSubjectRepository repository) : IClassSubjectService
{
    public Task<IReadOnlyList<SchoolClass>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        return repository.GetClassesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Subject>> GetSubjectsByClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        if (classId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classId), "Class ID must be greater than zero.");
        }

        return repository.GetSubjectsByClassAsync(classId, cancellationToken);
    }

    public Task<int> CreateClassAsync(string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default)
    {
        ValidateGradeName(gradeName);

        if (numberOfSubjects < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfSubjects), "Number of subjects cannot be negative.");
        }

        var schoolClass = new SchoolClass
        {
            GradeName = gradeName.Trim(),
            NumberOfSubjects = numberOfSubjects
        };

        return repository.CreateClassAsync(schoolClass, cancellationToken);
    }

    public Task UpdateClassAsync(int classId, string gradeName, int numberOfSubjects, CancellationToken cancellationToken = default)
    {
        if (classId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classId));
        }

        ValidateGradeName(gradeName);

        if (numberOfSubjects < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfSubjects), "Number of subjects cannot be negative.");
        }

        var schoolClass = new SchoolClass
        {
            ClassId = classId,
            GradeName = gradeName.Trim(),
            NumberOfSubjects = numberOfSubjects
        };

        return repository.UpdateClassAsync(schoolClass, cancellationToken);
    }

    public Task DeleteClassAsync(int classId, CancellationToken cancellationToken = default)
    {
        if (classId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classId));
        }

        return repository.DeleteClassAsync(classId, cancellationToken);
    }

    public Task<int> CreateSubjectAsync(int classId, string subjectName, CancellationToken cancellationToken = default)
    {
        if (classId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classId));
        }

        ValidateSubjectName(subjectName);

        var subject = new Subject
        {
            ClassId = classId,
            SubjectName = subjectName.Trim()
        };

        return repository.CreateSubjectAsync(subject, cancellationToken);
    }

    public Task UpdateSubjectAsync(int subjectId, int classId, string subjectName, CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subjectId));
        }

        if (classId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(classId));
        }

        ValidateSubjectName(subjectName);

        var subject = new Subject
        {
            SubjectId = subjectId,
            ClassId = classId,
            SubjectName = subjectName.Trim()
        };

        return repository.UpdateSubjectAsync(subject, cancellationToken);
    }

    public Task DeleteSubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subjectId));
        }

        return repository.DeleteSubjectAsync(subjectId, cancellationToken);
    }

    private static void ValidateGradeName(string gradeName)
    {
        if (string.IsNullOrWhiteSpace(gradeName))
        {
            throw new ArgumentException("Grade name is required.", nameof(gradeName));
        }
    }

    private static void ValidateSubjectName(string subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            throw new ArgumentException("Subject name is required.", nameof(subjectName));
        }
    }
}