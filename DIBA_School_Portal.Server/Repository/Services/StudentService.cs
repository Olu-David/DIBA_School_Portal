using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        // Who can edit / delete student records
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

        // ==========================================================
        // LIST (paginated + filterable)
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

                // ---- Filters ----
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

                // ---- Filter on outstanding AFTER mapping (needs computed value) ----
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
        // GET BY ID
        // ==========================================================
        #region GetStudentByIdAsync
        public async Task<ServiceResponse<StudentDto>> GetStudentByIdAsync(Guid id)
        {
            return await LoadStudentDetailAsync(s => s.Id == id);
        }
        #endregion

        // ==========================================================
        // GET BY ADMISSION NUMBER
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
            return await LoadStudentDetailAsync(s => s.AdmissionNumber == trimmed);
        }
        #endregion

        // ==========================================================
        // GET BY CLASS
        // ==========================================================
        #region GetStudentsByClassAsync
        public async Task<ServiceResponse<List<StudentListRowDto>>> GetStudentsByClassAsync(
            Guid classId, bool onlyActive = true)
        {
            var response = new ServiceResponse<List<StudentListRowDto>>();

            try
            {
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
        // UPDATE
        // ==========================================================
        #region UpdateStudentAsync
        public async Task<ServiceResponse<StudentDto>> UpdateStudentAsync(
            Guid id, UpdateStudentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<StudentDto>();

            try
            {
                // ---- Authorization ----
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

                // ---- Load ----
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

                // ---- Validate class exists ----
                var targetClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId);

                if (targetClass == null)
                {
                    response.Success = false;
                    response.Message = "Target class not found.";
                    response.StatusCode = 400;
                    return response;
                }

                bool isSenior = targetClass.AcademicSection == Enums.AcademicSection.SeniorSecondary;

                // ---- Stream validation (both directions) ----
                if (isSenior && request.Stream == null)
                {
                    response.Success = false;
                    response.Message = "A stream is required for Senior Secondary students.";
                    response.StatusCode = 400;
                    return response;
                }

                if (!isSenior && request.Stream != null)
                {
                    response.Success = false;
                    response.Message = "Streams only apply to Senior Secondary students.";
                    response.StatusCode = 400;
                    return response;
                }

                // ---- Snapshot for audit ----
                var oldName = $"{student.FirstName} {student.LastName}";
                var oldClass = student.SchoolClassId;
                var oldStream = student.Stream;
                var oldActive = student.User?.IsActive ?? false;

                // ---- Apply changes ----
                student.FirstName = request.FirstName.Trim();
                student.LastName = request.LastName.Trim();
                student.SchoolClassId = request.SchoolClassId;
                student.Stream = isSenior ? request.Stream : null;

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

                // ---- Audit ----
                await _auditLog.LogActivityAsync(
                    currentUserId, "Student Updated",
                    $"Student '{oldName}' ({student.AdmissionNumber}) updated. " +
                    $"Class: {oldClass} → {student.SchoolClassId}, " +
                    $"Stream: {oldStream} → {student.Stream}, " +
                    $"Active: {oldActive} → {request.IsActive}.",
                    ipAddress);

                // ---- Return fresh detail ----
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

                // ---- Latest enrollment ----
                var latestEnrollment = await _context.StudentEnrollments
                    .AsNoTracking()
                    .Include(e => e.Term)
                    .Where(e => e.StudentProfileId == student.Id)
                    .OrderByDescending(e => e.Term!.StartDate)
                    .FirstOrDefaultAsync();

                // ---- Latest term result ----
                var latestResult = await _context.TermResults
                    .AsNoTracking()
                    .Include(tr => tr.Term)
                    .Where(tr => tr.StudentId == student.UserId)
                    .OrderByDescending(tr => tr.Term!.StartDate)
                    .FirstOrDefaultAsync();

                // ---- Parents ----
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
    }
}