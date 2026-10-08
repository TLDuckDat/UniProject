namespace UniProject.API.Modules.Academic
{
    public record FacultyDto(int Id, string Code, string Name, string? Email, string? PhoneNumber, bool IsActive);
    public record CreateFacultyRequest(string Code, string Name, string? Email, string? PhoneNumber);
    public record UpdateFacultyRequest(string Name, string? Email, string? PhoneNumber);

    public record DepartmentDto(int Id, string Code, string Name, int FacultyId, string FacultyName, bool IsActive);
    public record CreateDepartmentRequest(string Code, string Name, int FacultyId);
    public record UpdateDepartmentRequest(string Name, int FacultyId);

    public record MajorDto(int Id, string Code, string Name, int DepartmentId, string DepartmentName, bool IsActive);
    public record CreateMajorRequest(string Code, string Name, int DepartmentId);
    public record UpdateMajorRequest(string Name, int DepartmentId);

    public record ClassDto(int Id, string Code, string Name, int AdmissionYear, int MajorId, string MajorName, bool IsActive);
    public record CreateClassRequest(string Code, string Name, int AdmissionYear, int MajorId);
    public record UpdateClassRequest(string Name, int AdmissionYear, int MajorId);

    public record AcademicYearDto(int Id, string Name, DateTime StartDate, DateTime EndDate, bool IsActive);
    public record CreateAcademicYearRequest(string Name, DateTime StartDate, DateTime EndDate);
    public record UpdateAcademicYearRequest(string Name, DateTime StartDate, DateTime EndDate);

    public record SemesterDto(int Id, string Name, int SemesterNumber, int AcademicYearId, string AcademicYearName, DateTime StartDate, DateTime EndDate, bool IsActive);
    public record CreateSemesterRequest(string Name, int SemesterNumber, int AcademicYearId, DateTime StartDate, DateTime EndDate);
    public record UpdateSemesterRequest(string Name, int SemesterNumber, int AcademicYearId, DateTime StartDate, DateTime EndDate);

    public record DegreeDto(int Id, string Name);
    public record AcademicRankDto(int Id, string Name);

    public record LecturerDto(
        int Id,
        string Code,
        string FullName,
        string Email,
        string? PhoneNumber,
        int DepartmentId,
        string DepartmentName,
        int DegreeId,
        string DegreeName,
        int? AcademicRankId,
        string? AcademicRankName,
        int AccountId,
        bool IsActive);

    public record CreateLecturerRequest(
        string Code,
        string FullName,
        string Email,
        string? PhoneNumber,
        int DepartmentId,
        int DegreeId,
        int? AcademicRankId,
        int AccountId);

    public record UpdateLecturerRequest(
        string FullName,
        string Email,
        string? PhoneNumber,
        int DepartmentId,
        int DegreeId,
        int? AcademicRankId);

    public record StudentDto(
        int Id,
        string Code,
        string FullName,
        DateTime? DateOfBirth,
        string? Gender,
        string Email,
        string? PhoneNumber,
        int ClassId,
        string ClassName,
        int AccountId,
        bool IsActive);

    public record CreateStudentRequest(
        string Code,
        string FullName,
        DateTime? DateOfBirth,
        string? Gender,
        string Email,
        string? PhoneNumber,
        int ClassId,
        int AccountId);

    public record UpdateStudentRequest(
        string FullName,
        DateTime? DateOfBirth,
        string? Gender,
        string Email,
        string? PhoneNumber,
        int ClassId);
}
