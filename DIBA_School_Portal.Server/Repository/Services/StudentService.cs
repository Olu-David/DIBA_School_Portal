using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Controllers;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class StudentService : IStudentService
    {
        #region Fields
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLog _auditLog;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<StudentService> _logger;

        private static readonly string[] StudentEditors =
            { "SuperAdmin", "Principal", "Registrar", "SchoolAdmin" };
        #endregion

        #region Constructor
        public StudentService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLog auditLog,
            IHttpContextAccessor http,
            ILogger<StudentService> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _http = http;
            _logger = logger;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
        #endregion

        private async Task<List<Guid>?> GetAllowedClassIdsAsync()
        {
            // Fall back to "sub" — some JWT setups only emit that.
            var userId = _http.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

            // Fail CLOSED: cannot identify caller → see nothing.
            if (string.IsNullOrEmpty(userId))
                return new List<Guid>();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new List<Guid>();

            var roles = await _userManager.GetRolesAsync(user);

            var adminRoles = new[]
            {
        "SuperAdmin", "Principal", "Headmaster", "Headmistress",
        "VicePrincipal", "SchoolAdmin", "Registrar", "Accountant",
    };

            if (roles.Any(r => adminRoles.Contains(r)))
                return null;   // null = unrestricted

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null)
                return new List<Guid>();

            // Only TeacherClasses — no FormTeacherId union.
            return await _context.TeacherClasses
                .AsNoTracking()
                .Where(tc => tc.TeacherId == teacher.Id)
                .Select(tc => tc.SchoolClassId)
                .ToListAsync();
        }
        // ============================================================
        // EDIT STUDENT SUBJECTS — replace the enrolment set
        // ============================================================
        public async Task<ServiceResponse<int>> EditStudentSubjectsAsync(
            Guid studentId, List<Guid> subjectIds, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<int>();

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                response.Success = false;
                response.Message = "Student not found.";
                response.StatusCode = 404;
                return response;
            }

            var currentSession = await _context.AcademicSessions
                .OrderByDescending(a => a.StartDate)   // ← adjust to your property
                .FirstOrDefaultAsync();

            if (currentSession == null)
            {
                response.Success = false;
                response.Message = "No academic session exists. Create one before editing subjects.";
                response.StatusCode = 400;
                return response;
            }

            var distinctSubjectIds = (subjectIds ?? new List<Guid>())
                .Distinct()
                .ToList();

            // Validate every subject id exists
            if (distinctSubjectIds.Count > 0)
            {
                var validCount = await _context.Subjects
                    .Where(s => distinctSubjectIds.Contains(s.Id))
                    .CountAsync();

                if (validCount != distinctSubjectIds.Count)
                {
                    response.Success = false;
                    response.Message = "One or more subject ids are invalid.";
                    response.StatusCode = 400;
                    return response;
                }
            }

            // Remove existing enrolments for this student
            var existing = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId)
                .ToListAsync();

            if (existing.Count > 0)
                _context.StudentSubjects.RemoveRange(existing);

            // Add the new set
            foreach (var subjectId in distinctSubjectIds)
            {
                _context.StudentSubjects.Add(new StudentSubject
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    SubjectId = subjectId,
                    AcademicSessionId = currentSession.Id,
                });
            }

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "Student Subjects Updated",
                $"Student {student.FirstName} {student.LastName}: " +
                $"{distinctSubjectIds.Count} subjects set.",
                ipAddress);

            response.Success = true;
            response.Message = $"Student now enrolled in {distinctSubjectIds.Count} subject(s).";
            response.StatusCode = 200;
            response.Data = distinctSubjectIds.Count;
            return response;
        }

        // ============================================================
        // GET a single student's subjects (admin view)
        // ============================================================
        public async Task<ServiceResponse<List<StudentSubjectDto>>> GetStudentSubjectsAsync(Guid studentId)
        {
            var response = new ServiceResponse<List<StudentSubjectDto>>();

            // Load the student so we have SchoolClassId for the teacher filter
            var student = await _context.Students
                .AsNoTracking()
                .Where(s => s.Id == studentId)
                .Select(s => new { s.Id, s.SchoolClassId })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                response.Success = false;
                response.Message = "Student not found.";
                response.StatusCode = 404;
                return response;
            }

            var subjects = await _context.StudentSubjects
                .Where(ss => ss.StudentId == studentId)
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s!.TeacherSubjects)
                        .ThenInclude(ts => ts.Teacher)
                .Select(ss => new StudentSubjectDto
                {
                    subjectId = ss.Subject!.Id,
                    subjectName = ss.Subject.Name,
                    subjectCode = ss.Subject.Code,
                    teacherName = ss.Subject.TeacherSubjects
                        .Where(ts => ts.Teacher != null
                                  && _context.TeacherClasses.Any(tc =>
                                         tc.TeacherId == ts.TeacherId
                                      && tc.SchoolClassId == student.SchoolClassId))
                        .OrderBy(ts => ts.Teacher!.FirstName)
                        .Select(ts => ts.Teacher!.FirstName + " " + ts.Teacher.LastName)
                        .FirstOrDefault() ?? "—",
                })
                .Distinct()
                .ToListAsync();

            response.Success = true;
            response.StatusCode = 200;
            response.Data = subjects;
            return response;
        }

        // ============================================================
        // GET all subjects (for the picker modal)
        // ============================================================
        public async Task<ServiceResponse<List<SubjectOptionDto>>> GetAllSubjectsAsync()
        {
            var response = new ServiceResponse<List<SubjectOptionDto>>();

            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .Select(s => new SubjectOptionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                })
                .ToListAsync();

            response.Success = true;
            response.StatusCode = 200;
            response.Data = subjects;
            return response;
        }

        // ==========================================================
        // LIST (paginated + filterable + role-scoped)
        // ==========================================================
        #region GetStudentsAsync
        public async Task<ServiceResponse<PaginatedViewModel<StudentListRowDto>>> GetStudentsAsync(
            StudentFilterDto filter)
        {
            var response = new ServiceResponse<PaginatedViewModel<StudentListRowDto>>();

            try
            {
                filter.PageNumber = Math.Max(1, filter.PageNumber);
                filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize));

                var query = _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.SchoolClass)
                    .AsQueryable();

                // ---- ROLE FILTER: teachers only see their classes ----
                var allowed = await GetAllowedClassIdsAsync();
                if (allowed != null)
                {
                    query = query.Where(s => allowed.Contains(s.SchoolClassId));
                }

                // ---- User-supplied filters ----
                if (filter.ClassId.HasValue)
                    query = query.Where(s => s.SchoolClassId == filter.ClassId.Value);

                if (filter.Section.HasValue)
                    query = query.Where(s => s.SchoolClass.AcademicSection == filter.Section.Value);

                if (filter.Stream.HasValue)
                    query = query.Where(s => s.Stream == filter.Stream.Value);

                if (filter.IsActive.HasValue)
                    query = query.Where(s => s.User!.IsActive == filter.IsActive.Value);

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var s = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(x =>
                        x.FirstName.ToLower().Contains(s) ||
                        x.LastName.ToLower().Contains(s) ||
                        x.AdmissionNumber.ToLower().Contains(s));
                }

                int totalCount = await query.CountAsync();

                var students = await query
                    .OrderBy(s => s.SchoolClass.DisplayOrder)
                    .ThenBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var studentIds = students.Select(s => s.Id).ToList();

                // ---- Latest enrollment for balance info ----
                var latestEnrollments = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Where(e => studentIds.Contains(e.StudentProfileId))
                    .Include(e => e.Term)
                    .ToListAsync();

                var latestEnrollmentByStudent = latestEnrollments
                    .GroupBy(e => e.StudentProfileId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(e => e.Term!.StartDate).First());

                // ---- Primary guardian phone ----
                var primaryGuardians = await _context.ParentStudents
                    .AsNoTracking()
                    .Include(ps => ps.Parent)
                    .Where(ps => studentIds.Contains(ps.StudentProfileId) && ps.IsPrimaryContact)
                    .ToListAsync();

                var guardianPhoneByStudent = primaryGuardians
                    .GroupBy(ps => ps.StudentProfileId)
                    .ToDictionary(g => g.Key, g => g.First().Parent?.PhoneNumber);

                // ---- DTO ----
                var rows = students.Select(s =>
                {
                    latestEnrollmentByStudent.TryGetValue(s.Id, out var enrollment);

                    return new StudentListRowDto
                    {
                        Id = s.Id,
                        AdmissionNumber = s.AdmissionNumber,
                        FullName = $"{s.FirstName} {s.LastName}",
                        ClassName = s.SchoolClass?.Name ?? "—",
                        Stream = s.Stream,
                        ProfilePictureUrl = s.User?.ProfilePictureUrl,
                        IsActive = s.User?.IsActive ?? false,
                        OutstandingBalance = enrollment?.OutstandingBalance ?? 0m,
                        IsFullyPaid = enrollment?.IsFullyPaid ?? false,
                        PrimaryGuardianPhone = guardianPhoneByStudent.TryGetValue(s.Id, out var phone)
                            ? phone
                            : null,
                    };
                }).ToList();

                // ---- Filter on outstanding AFTER mapping ----
                if (filter.HasOutstanding == true)
                    rows = rows.Where(r => r.OutstandingBalance > 0).ToList();
                else if (filter.HasOutstanding == false)
                    rows = rows.Where(r => r.OutstandingBalance == 0).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<StudentListRowDto>
                {
                    Items = rows,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalCount > 0
                        ? (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                        : 0,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // GET BY ID — scoped
        // ==========================================================
        #region GetStudentByIdAsync
        public async Task<ServiceResponse<StudentDto>> GetStudentByIdAsync(Guid id)
        {
            var allowed = await GetAllowedClassIdsAsync();

            if (allowed != null)
            {
                var classId = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => s.SchoolClassId)
                    .FirstOrDefaultAsync();

                if (classId == Guid.Empty)
                {
                    return new ServiceResponse<StudentDto>
                    {
                        Success = false,
                        Message = "Student not found.",
                        StatusCode = 404,
                    };
                }

                if (!allowed.Contains(classId))
                {
                    return new ServiceResponse<StudentDto>
                    {
                        Success = false,
                        Message = "You are not authorized to view this student.",
                        StatusCode = 403,
                    };
                }
            }

            return await LoadStudentDetailAsync(s => s.Id == id);
        }
        #endregion

        // ==========================================================
        // GET BY ADMISSION NUMBER — scoped
        // ==========================================================
        #region GetStudentByAdmissionNumberAsync
        public async Task<ServiceResponse<StudentDto>> GetStudentByAdmissionNumberAsync(string admissionNumber)
        {
            if (string.IsNullOrWhiteSpace(admissionNumber))
            {
                return new ServiceResponse<StudentDto>
                {
                    Success = false,
                    Message = "Admission number is required.",
                    StatusCode = 400,
                };
            }

            var trimmed = admissionNumber.Trim();

            var allowed = await GetAllowedClassIdsAsync();

            if (allowed != null)
            {
                var classId = await _context.Students
                    .AsNoTracking()
                    .Where(s => s.AdmissionNumber == trimmed)
                    .Select(s => s.SchoolClassId)
                    .FirstOrDefaultAsync();

                if (classId == Guid.Empty)
                {
                    return new ServiceResponse<StudentDto>
                    {
                        Success = false,
                        Message = "Student not found.",
                        StatusCode = 404,
                    };
                }

                if (!allowed.Contains(classId))
                {
                    return new ServiceResponse<StudentDto>
                    {
                        Success = false,
                        Message = "You are not authorized to view this student.",
                        StatusCode = 403,
                    };
                }
            }

            return await LoadStudentDetailAsync(s => s.AdmissionNumber == trimmed);
        }
        #endregion

        // ==========================================================
        // GET BY CLASS — scoped
        // ==========================================================
        #region GetStudentsByClassAsync
        public async Task<ServiceResponse<List<StudentListRowDto>>> GetStudentsByClassAsync(
            Guid classId, bool onlyActive = true)
        {
            var response = new ServiceResponse<List<StudentListRowDto>>();

            try
            {
                // Teachers must be assigned to this class
                var allowed = await GetAllowedClassIdsAsync();
                if (allowed != null && !allowed.Contains(classId))
                {
                    response.Success = false;
                    response.Message = "You are not assigned to this class.";
                    response.StatusCode = 403;
                    return response;
                }

                var query = _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.SchoolClass)
                    .Where(s => s.SchoolClassId == classId);

                if (onlyActive)
                    query = query.Where(s => s.User!.IsActive && !s.User.IsDeleted);

                var students = await query
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .ToListAsync();

                var rows = students.Select(s => new StudentListRowDto
                {
                    Id = s.Id,
                    AdmissionNumber = s.AdmissionNumber,
                    FullName = $"{s.FirstName} {s.LastName}",
                    ClassName = s.SchoolClass?.Name ?? "—",
                    Stream = s.Stream,
                    ProfilePictureUrl = s.User?.ProfilePictureUrl,
                    IsActive = s.User?.IsActive ?? false,
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = rows;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by class {ClassId}", classId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // UPDATE — authorization already handled (editors only)
        // ==========================================================
        #region UpdateStudentAsync
        public async Task<ServiceResponse<StudentDto>> UpdateStudentAsync(
            Guid id, UpdateStudentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<StudentDto>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.Message = "Unauthorized.";
                    response.StatusCode = 401;
                    return response;
                }

                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => StudentEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to edit student records.";
                    response.StatusCode = 403;

                    await _auditLog.LogActivityAsync(
                        currentUserId, "Student Update DENIED",
                        $"Attempted to update student {id}.", ipAddress);
                    return response;
                }

                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var targetClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId);

                if (targetClass == null)
                {
                    response.Success = false;
                    response.Message = "Target class not found.";
                    response.StatusCode = 400;
                    return response;
                }

                if (targetClass.AcademicSection == Enums.AcademicSection.SeniorSecondary
                    && request.Stream == null)
                {
                    response.Success = false;
                    response.Message = "A stream is required for Senior Secondary students.";
                    response.StatusCode = 400;
                    return response;
                }

                if (targetClass.AcademicSection != Enums.AcademicSection.SeniorSecondary
                    && request.Stream != null)
                {
                    response.Success = false;
                    response.Message = "Streams only apply to Senior Secondary students.";
                    response.StatusCode = 400;
                    return response;
                }

                var oldName = $"{student.FirstName} {student.LastName}";
                var oldClass = student.SchoolClassId;
                var oldStream = student.Stream;
                var oldActive = student.User?.IsActive ?? false;

                student.FirstName = request.FirstName.Trim();
                student.LastName = request.LastName.Trim();
                student.SchoolClassId = request.SchoolClassId;
                student.Stream = request.Stream;

                if (student.User != null)
                {
                    student.User.FirstName = student.FirstName;
                    student.User.LastName = student.LastName;
                    student.User.IsActive = request.IsActive;
                    student.User.ClassId = request.SchoolClassId;

                    if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                        student.User.PhoneNumber = request.PhoneNumber.Trim();
                }

                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId, "Student Updated",
                    $"Student '{oldName}' ({student.AdmissionNumber}) updated. " +
                    $"Class: {oldClass} → {student.SchoolClassId}, " +
                    $"Stream: {oldStream} → {student.Stream}, " +
                    $"Active: {oldActive} → {request.IsActive}.",
                    ipAddress);

                return await LoadStudentDetailAsync(s => s.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", id);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // SOFT DELETE
        // ==========================================================
        #region SoftDeleteStudentAsync
        public async Task<ServiceResponse<bool>> SoftDeleteStudentAsync(
            Guid id, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.StatusCode = 401;
                    return response;
                }

                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => StudentEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to delete students.";
                    response.StatusCode = 403;
                    return response;
                }

                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (student.User != null)
                {
                    student.User.IsActive = false;
                    student.User.IsDeleted = true;
                    student.User.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId, "Student SoftDeleted",
                    $"Student '{student.FirstName} {student.LastName}' ({student.AdmissionNumber}) soft-deleted.",
                    ipAddress);

                response.Success = true;
                response.Message = "Student soft-deleted successfully.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting student {StudentId}", id);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // RESTORE
        // ==========================================================
        #region RestoreStudentAsync
        public async Task<ServiceResponse<bool>> RestoreStudentAsync(
            Guid id, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.StatusCode = 401;
                    return response;
                }

                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => StudentEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to restore students.";
                    response.StatusCode = 403;
                    return response;
                }

                var student = await _context.Students
                    .IgnoreQueryFilters()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (student.User != null)
                {
                    student.User.IsActive = true;
                    student.User.IsDeleted = false;
                    student.User.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId, "Student Restored",
                    $"Student '{student.FirstName} {student.LastName}' ({student.AdmissionNumber}) restored.",
                    ipAddress);

                response.Success = true;
                response.Message = "Student restored successfully.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring student {StudentId}", id);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // HELPER — load full student detail
        // ==========================================================
        #region Helper — LoadStudentDetailAsync
        private async Task<ServiceResponse<StudentDto>> LoadStudentDetailAsync(
            System.Linq.Expressions.Expression<Func<Entities.Student, bool>> predicate)
        {
            var response = new ServiceResponse<StudentDto>();

            try
            {
                var student = await _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.SchoolClass)
                    .FirstOrDefaultAsync(predicate);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var latestEnrollment = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Include(e => e.Term)
                    .Where(e => e.StudentProfileId == student.Id)
                    .OrderByDescending(e => e.Term!.StartDate)
                    .FirstOrDefaultAsync();

                var latestResult = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.Term)
                    .Where(tr => tr.StudentId == student.UserId)
                    .OrderByDescending(tr => tr.Term!.StartDate)
                    .FirstOrDefaultAsync();

                var parentLinks = await _context.ParentStudents
                    .AsNoTracking()
                    .Include(ps => ps.Parent)
                    .Where(ps => ps.StudentProfileId == student.Id)
                    .ToListAsync();

                var parents = parentLinks.Select(ps => new StudentParentLinkDto
                {
                    ParentUserId = ps.ParentUserId,
                    FullName = ps.Parent != null
                        ? $"{ps.Parent.FirstName} {ps.Parent.LastName}"
                        : "—",
                    PhoneNumber = ps.Parent?.PhoneNumber,
                    Email = ps.Parent?.Email,
                    ProfilePictureUrl = ps.Parent?.ProfilePictureUrl,
                    Relationship = ps.Relationship,
                    IsPrimaryContact = ps.IsPrimaryContact,
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new StudentDto
                {
                    Id = student.Id,
                    UserId = student.UserId,
                    AdmissionNumber = student.AdmissionNumber,
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.User?.Email,
                    PhoneNumber = student.User?.PhoneNumber,
                    ProfilePictureUrl = student.User?.ProfilePictureUrl,
                    SchoolClassId = student.SchoolClassId,
                    ClassName = student.SchoolClass?.Name ?? "—",
                    Section = student.SchoolClass?.AcademicSection ?? default,
                    Stream = student.Stream,
                    IsActive = student.User?.IsActive ?? false,
                    CreatedAt = student.CreatedAt,

                    OutstandingBalance = latestEnrollment?.OutstandingBalance ?? 0m,
                    IsFullyPaid = latestEnrollment?.IsFullyPaid ?? false,
                    LatestAverageScore = latestResult?.AverageScore ?? 0m,
                    LatestClassPosition = latestResult?.Position ?? 0,
                    LatestTermName = latestResult?.Term?.Name,

                    Parents = parents,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student detail.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        public async Task<ServiceResponse<StudentBillDto>> GetMyBillAsync(
      string userId,
      Guid? termId = null,
      Guid? academicSessionId = null)
        {
            var response = new ServiceResponse<StudentBillDto>();

            try
            {
                // ---------- 1. Resolve "me" → Student profile ----------
                var student = await _context.Students
                    .AsNoTracking()
                    .Include(s => s.SchoolClass)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    response.Success = false;
                    response.Message = "Student profile not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- 2. Resolve session ----------
                Guid resolvedSessionId;
                if (academicSessionId.HasValue && academicSessionId.Value != Guid.Empty)
                {
                    resolvedSessionId = academicSessionId.Value;
                }
                else
                {
                    var currentSession = await _context.AcademicSessions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.IsCurrentSession);

                    if (currentSession == null)
                    {
                        response.Success = false;
                        response.Message = "No active academic session.";
                        response.StatusCode = 404;
                        return response;
                    }
                    resolvedSessionId = currentSession.Id;
                }

                // ---------- 3. Resolve term ----------
                Guid resolvedTermId;
                if (termId.HasValue && termId.Value != Guid.Empty)
                {
                    resolvedTermId = termId.Value;
                }
                else
                {
                    var currentTerm = await _context.SchoolTerms
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t =>
                            t.AcademicSessionId == resolvedSessionId && t.IsCurrentTerm);

                    if (currentTerm == null)
                    {
                        response.Success = false;
                        response.Message = "No current term.";
                        response.StatusCode = 404;
                        return response;
                    }
                    resolvedTermId = currentTerm.Id;
                }

                var term = await _context.SchoolTerms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == resolvedTermId);

                if (term == null)
                {
                    response.Success = false;
                    response.Message = "Term not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- 4. Fee items for this student's class ----------
                var classFees = await _context.ClassFeePrices
                    .AsNoTracking()
                    .Include(cfp => cfp.Fee)
                    .Where(cfp =>
                        cfp.SchoolClassId == student.SchoolClassId &&
                        cfp.Fee.TermId == resolvedTermId &&
                        cfp.Fee.AcademicSessionId == resolvedSessionId)
                    .ToListAsync();

                // ---------- 5. Successful payments ----------
                var payments = await _context.StudentPayments
                    .AsNoTracking()
                    .Where(p =>
                        p.StudentProfileId == student.Id &&
                        p.TermId == resolvedTermId &&
                        p.AcademicSessionId == resolvedSessionId &&
                        p.Status == PaymentStatus.Successful)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                // ---------- 6. Allocations ----------
                var paymentIds = payments.Select(p => p.Id).ToList();

                var allocations = await _context.StudentPaymentItems
                    .AsNoTracking()
                    .Where(item => paymentIds.Contains(item.StudentPaymentId))
                    .ToListAsync();

                var paidByFee = allocations
                    .Where(a => a.FeeId.HasValue)
                    .GroupBy(a => a.FeeId!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(a => a.AmountAllocated));

                // ---------- 7. Build fee items ----------
                var feeItems = classFees
                    .Where(cfp => cfp.Fee != null)
                    .Select(cfp =>
                    {
                        var paidTowardThis = paidByFee.TryGetValue(cfp.FeeId, out var v) ? v : 0m;
                        return new FeeItemBreakdownDto
                        {
                            FeeId = cfp.FeeId,
                            FeeName = cfp.Fee!.FeeName,
                            Description = cfp.Fee.Description,
                            Amount = cfp.Amount,
                            MinimumAmountToPay = cfp.MinimumAmountToPay,   // ← ADD THIS
                            AmountPaidTowardThis = paidTowardThis,
                            IsFullyPaid = cfp.Amount > 0 && paidTowardThis >= cfp.Amount,
                        };
                    })
                    .OrderBy(f => f.FeeName)
                    .ToList();

                // ---------- 8. Totals ----------
                var totalBilled = feeItems.Sum(f => f.Amount);
                var totalPaid = payments.Sum(p => p.AmountPaid);
                var balance = Math.Max(0, totalBilled - totalPaid);

                // Minimum required = sum of unmet minimums across unpaid fees
                var minimumRequired = feeItems
                    .Where(f => !f.IsFullyPaid)
                    .Sum(f => Math.Max(0, f.MinimumAmountToPay - f.AmountPaidTowardThis));

                // ---------- 9. Response ----------
                response.Success = true;
                response.StatusCode = 200;
                response.Data = new StudentBillDto
                {
                    StudentProfileId = student.Id,                  // ← ADD THIS
                    AcademicSessionId = resolvedSessionId,          // ← ADD THIS
                    TermId = resolvedTermId,                        // ← ADD THIS
                    TermName = term.Name,
                    TotalBilledAmount = totalBilled,
                    TotalPaidSoFar = totalPaid,
                    TotalBalanceRemaining = balance,
                    MinimumAmountRequired = minimumRequired,
                    FeeItems = feeItems,
                    PaymentHistory = payments.Select(p => new PaymentHistoryDto
                    {
                        PaymentId = p.Id,
                        ReferenceNumber = p.ReferenceNumber,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                    }).ToList(),
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student bill for user {UserId}", userId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
    }
}