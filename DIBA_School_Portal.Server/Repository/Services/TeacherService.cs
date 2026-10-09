using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.Teacher;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class TeacherService : ITeacherService
    {
        #region Fields
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLog _auditLog;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<TeacherService> _logger;

        // Who can edit teacher records / assignments
        private static readonly string[] TeacherEditors =
            { "SuperAdmin", "Principal", "SchoolAdmin" };
        #endregion

        #region Constructor
        public TeacherService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLog auditLog,
            IHttpContextAccessor http,
            ILogger<TeacherService> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _http = http;
            _logger = logger;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        private string CurrentUserId =>
            _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "System";
        #endregion

        // ==========================================================
        // LIST (paginated + filterable)
        // ==========================================================
        #region GetTeachersAsync
        public async Task<ServiceResponse<PaginatedViewModel<TeacherRowDto>>> GetTeachersAsync(
            TeacherFilterDto filter)
        {
            var response = new ServiceResponse<PaginatedViewModel<TeacherRowDto>>();

            try
            {
                filter.PageNumber = Math.Max(1, filter.PageNumber);
                filter.PageSize = Math.Max(1, Math.Min(100, filter.PageSize));

                var query = _context.Teachers
                    .AsNoTracking()
                    .Include(t => t.User)
                    .AsQueryable();

                // ---------- Filters ----------
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var s = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(t =>
                        t.FirstName.ToLower().Contains(s) ||
                        t.LastName.ToLower().Contains(s) ||
                        t.StaffId.ToLower().Contains(s) ||
                        (t.User != null && t.User.Email != null && t.User.Email.ToLower().Contains(s)));
                }

                if (!string.IsNullOrWhiteSpace(filter.Qualification))
                {
                    var q = filter.Qualification.Trim().ToLower();
                    query = query.Where(t => t.Qualification.ToLower().Contains(q));
                }

                if (!string.IsNullOrWhiteSpace(filter.PrimaryRole))
                {
                    var role = filter.PrimaryRole.Trim();
                    query = query.Where(t => t.User != null && t.User.PrimaryRole == role);
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(t => t.User != null && t.User.IsActive == filter.IsActive.Value);
                }

                int totalCount = await query.CountAsync();

                var teachers = await query
                    .OrderBy(t => t.FirstName)
                    .ThenBy(t => t.LastName)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var rows = teachers.Select(t => new TeacherRowDto
                {
                    Id = t.Id,
                    StaffId = t.StaffId,
                    FullName = $"{t.FirstName} {t.LastName}",
                    Email = t.User?.Email,
                    PhoneNumber = t.User?.PhoneNumber,
                    ProfilePictureUrl = t.User?.ProfilePictureUrl,
                    Qualification = t.Qualification,
                    PrimaryRole = t.User?.PrimaryRole ?? "Teacher",
                    IsActive = t.User?.IsActive ?? false,
                    DateEmployed = t.DateEmployed,
                }).ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new PaginatedViewModel<TeacherRowDto>
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
                _logger.LogError(ex, "Error fetching teachers.");
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
        #region GetTeacherByIdAsync
        public async Task<ServiceResponse<TeacherDto>> GetTeacherByIdAsync(Guid id)
        {
            return await LoadTeacherDetailAsync(t => t.Id == id);
        }
        #endregion

        // ==========================================================
        // GET BY STAFF ID
        // ==========================================================
        #region GetTeacherByStaffIdAsync
        public async Task<ServiceResponse<TeacherDto>> GetTeacherByStaffIdAsync(string staffId)
        {
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return new ServiceResponse<TeacherDto>
                {
                    Success = false,
                    Message = "Staff ID is required.",
                    StatusCode = 400,
                };
            }

            var trimmed = staffId.Trim();
            return await LoadTeacherDetailAsync(t => t.StaffId == trimmed);
        }
        #endregion

        // ==========================================================
        // GET BY USER ID
        // ==========================================================
        #region GetTeacherByUserIdAsync
        public async Task<ServiceResponse<TeacherDto>> GetTeacherByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ServiceResponse<TeacherDto>
                {
                    Success = false,
                    Message = "User ID is required.",
                    StatusCode = 400,
                };
            }

            return await LoadTeacherDetailAsync(t => t.UserId == userId);
        }
        #endregion

        // ==========================================================
        // UPDATE
        // ==========================================================
        #region UpdateTeacherAsync
        public async Task<ServiceResponse<TeacherDto>> UpdateTeacherAsync(
            Guid id, UpdateTeacherDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<TeacherDto>();

            try
            {
                // ---------- Authorization ----------
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.Message = "Unauthorized.";
                    response.StatusCode = 401;
                    return response;
                }

                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to edit teacher records.";
                    response.StatusCode = 403;

                    await _auditLog.LogActivityAsync(
                        currentUserId, "Teacher Update DENIED",
                        $"Attempted to update teacher {id}.", ipAddress);

                    return response;
                }

                // ---------- Load ----------
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- Snapshot for audit ----------
                var oldName = $"{teacher.FirstName} {teacher.LastName}";
                var oldQualification = teacher.Qualification;
                var oldActive = teacher.User?.IsActive ?? false;

                // ---------- Apply changes ----------
                teacher.FirstName = request.FirstName.Trim();
                teacher.LastName = request.LastName.Trim();
                teacher.Qualification = request.Qualification?.Trim() ?? "";

                if (teacher.User != null)
                {
                    teacher.User.FirstName = teacher.FirstName;
                    teacher.User.LastName = teacher.LastName;
                    teacher.User.IsActive = request.IsActive;

                    if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                        teacher.User.PhoneNumber = request.PhoneNumber.Trim();
                }

                _context.Teachers.Update(teacher);
                await _context.SaveChangesAsync();

                // ---------- Audit ----------
                await _auditLog.LogActivityAsync(
                    currentUserId, "Teacher Updated",
                    $"Teacher '{oldName}' ({teacher.StaffId}) updated. " +
                    $"Qualification: '{oldQualification}' → '{teacher.Qualification}', " +
                    $"Active: {oldActive} → {request.IsActive}.",
                    ipAddress);

                return await LoadTeacherDetailAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating teacher {TeacherId}", id);
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
        #region SoftDeleteTeacherAsync
        public async Task<ServiceResponse<bool>> SoftDeleteTeacherAsync(
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
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to delete teachers.";
                    response.StatusCode = 403;
                    return response;
                }

                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (teacher.User != null)
                {
                    teacher.User.IsActive = false;
                    teacher.User.IsDeleted = true;
                    teacher.User.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId, "Teacher SoftDeleted",
                    $"Teacher '{teacher.FirstName} {teacher.LastName}' ({teacher.StaffId}) soft-deleted.",
                    ipAddress);

                response.Success = true;
                response.Message = "Teacher soft-deleted successfully.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting teacher {TeacherId}", id);
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
        #region RestoreTeacherAsync
        public async Task<ServiceResponse<bool>> RestoreTeacherAsync(
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
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to restore teachers.";
                    response.StatusCode = 403;
                    return response;
                }

                var teacher = await _context.Teachers
                    .IgnoreQueryFilters()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (teacher.User != null)
                {
                    teacher.User.IsActive = true;
                    teacher.User.IsDeleted = false;
                    teacher.User.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId, "Teacher Restored",
                    $"Teacher '{teacher.FirstName} {teacher.LastName}' ({teacher.StaffId}) restored.",
                    ipAddress);

                response.Success = true;
                response.Message = "Teacher restored successfully.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring teacher {TeacherId}", id);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // ASSIGN SUBJECTS
        // ==========================================================
        #region AssignSubjectsAsync
        public async Task<ServiceResponse<bool>> AssignSubjectsAsync(
            Guid teacherId, List<Guid> subjectIds, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                // ---------- Authorization ----------
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.StatusCode = 401;
                    return response;
                }
                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "You do not have permission to assign subjects.";
                    response.StatusCode = 403;
                    return response;
                }

                // ---------- Load teacher ----------
                var teacher = await _context.Teachers
                    .Include(t => t.TeacherSubjects)
                    .FirstOrDefaultAsync(t => t.Id == teacherId);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- Normalize input ----------
                subjectIds ??= new List<Guid>();
                subjectIds = subjectIds.Distinct().ToList();

                // ---------- Validate subject IDs ----------
                if (subjectIds.Any())
                {
                    var validSubjectIds = await _context.Subjects
                        .Where(s => subjectIds.Contains(s.Id))
                        .Select(s => s.Id)
                        .ToListAsync();

                    if (validSubjectIds.Count != subjectIds.Count)
                    {
                        response.Success = false;
                        response.Message = "One or more subject IDs are invalid.";
                        response.StatusCode = 400;
                        return response;
                    }
                }

                // ---------- Remove assignments not in the new list ----------
                var toRemove = teacher.TeacherSubjects
                    .Where(ts => !subjectIds.Contains(ts.SubjectId))
                    .ToList();

                if (toRemove.Any())
                    _context.TeacherSubjects.RemoveRange(toRemove);

                // ---------- Add new assignments ----------
                var existingSubjectIds = teacher.TeacherSubjects
                    .Select(ts => ts.SubjectId)
                    .ToHashSet();

                var toAdd = subjectIds
                    .Where(sid => !existingSubjectIds.Contains(sid))
                    .Select(sid => new TeacherSubject
                    {
                        Id = Guid.NewGuid(),
                        TeacherId = teacher.Id,
                        SubjectId = sid,
                        AssignedAt = DateTime.UtcNow,
                    })
                    .ToList();

                if (toAdd.Any())
                    await _context.TeacherSubjects.AddRangeAsync(toAdd);

                await _context.SaveChangesAsync();

                // ---------- Audit ----------
                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Teacher Subjects Updated",
                    $"Teacher '{teacher.FirstName} {teacher.LastName}' ({teacher.StaffId}) now assigned " +
                    $"to {subjectIds.Count} subject(s).",
                    ipAddress);

                response.Success = true;
                response.Message = $"{subjectIds.Count} subject(s) assigned.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subjects to teacher {TeacherId}", teacherId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // UNASSIGN SUBJECT
        // ==========================================================
        #region UnassignSubjectAsync
        public async Task<ServiceResponse<bool>> UnassignSubjectAsync(
            Guid teacherId, Guid subjectId, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null) { response.Success = false; response.StatusCode = 401; return response; }
                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "Not authorized.";
                    response.StatusCode = 403;
                    return response;
                }

                var link = await _context.TeacherSubjects
                    .Include(ts => ts.Teacher)
                    .Include(ts => ts.Subject)
                    .FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);

                if (link == null)
                {
                    response.Success = false;
                    response.Message = "Assignment not found.";
                    response.StatusCode = 404;
                    return response;
                }

                _context.TeacherSubjects.Remove(link);
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Teacher Subject Unassigned",
                    $"Removed subject '{link.Subject?.Name}' from {link.Teacher?.FirstName} {link.Teacher?.LastName}.",
                    ipAddress);

                response.Success = true;
                response.Message = "Subject unassigned.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning subject {SubjectId} from teacher {TeacherId}", subjectId, teacherId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // ASSIGN CLASSES
        // ==========================================================
        #region AssignClassesAsync
        public async Task<ServiceResponse<bool>> AssignClassesAsync(
            Guid teacherId, List<Guid> classIds, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null) { response.Success = false; response.StatusCode = 401; return response; }
                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "Not authorized.";
                    response.StatusCode = 403;
                    return response;
                }

                var teacher = await _context.Teachers
                    .Include(t => t.TeacherClasses)
                    .FirstOrDefaultAsync(t => t.Id == teacherId);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- Normalize input ----------
                classIds ??= new List<Guid>();
                classIds = classIds.Distinct().ToList();

                // ---------- Validate class IDs ----------
                if (classIds.Any())
                {
                    var validClassIds = await _context.SchoolClasses
                        .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                        .Select(c => c.Id)
                        .ToListAsync();

                    if (validClassIds.Count != classIds.Count)
                    {
                        response.Success = false;
                        response.Message = "One or more class IDs are invalid.";
                        response.StatusCode = 400;
                        return response;
                    }
                }

                // ---------- Remove not-in-new-list ----------
                var toRemove = teacher.TeacherClasses
                    .Where(tc => !classIds.Contains(tc.SchoolClassId))
                    .ToList();
                if (toRemove.Any())
                    _context.TeacherClasses.RemoveRange(toRemove);

                // ---------- Add new ----------
                var existingClassIds = teacher.TeacherClasses
                    .Select(tc => tc.SchoolClassId)
                    .ToHashSet();

                var toAdd = classIds
                    .Where(cid => !existingClassIds.Contains(cid))
                    .Select(cid => new TeacherClass
                    {
                        Id = Guid.NewGuid(),
                        TeacherId = teacher.Id,
                        SchoolClassId = cid,
                        AssignedAt = DateTime.UtcNow,
                    })
                    .ToList();

                if (toAdd.Any())
                    await _context.TeacherClasses.AddRangeAsync(toAdd);

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Teacher Classes Updated",
                    $"Teacher '{teacher.FirstName} {teacher.LastName}' ({teacher.StaffId}) now assigned " +
                    $"to {classIds.Count} class(es).",
                    ipAddress);

                response.Success = true;
                response.Message = $"{classIds.Count} class(es) assigned.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning classes to teacher {TeacherId}", teacherId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // UNASSIGN CLASS
        // ==========================================================
        #region UnassignClassAsync
        public async Task<ServiceResponse<bool>> UnassignClassAsync(
            Guid teacherId, Guid classId, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null) { response.Success = false; response.StatusCode = 401; return response; }
                var callerRoles = await _userManager.GetRolesAsync(caller);
                if (!callerRoles.Any(r => TeacherEditors.Contains(r)))
                {
                    response.Success = false;
                    response.Message = "Not authorized.";
                    response.StatusCode = 403;
                    return response;
                }

                var link = await _context.TeacherClasses
                    .Include(tc => tc.Teacher)
                    .Include(tc => tc.SchoolClass)
                    .FirstOrDefaultAsync(tc => tc.TeacherId == teacherId && tc.SchoolClassId == classId);

                if (link == null)
                {
                    response.Success = false;
                    response.Message = "Assignment not found.";
                    response.StatusCode = 404;
                    return response;
                }

                _context.TeacherClasses.Remove(link);
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "Teacher Class Unassigned",
                    $"Removed class '{link.SchoolClass?.Name}' from {link.Teacher?.FirstName} {link.Teacher?.LastName}.",
                    ipAddress);

                response.Success = true;
                response.Message = "Class unassigned.";
                response.StatusCode = 200;
                response.Data = true;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning class {ClassId} from teacher {TeacherId}", classId, teacherId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ==========================================================
        // HELPER — load full teacher detail (with subjects + classes)
        // ==========================================================
        #region Helper — LoadTeacherDetailAsync
        private async Task<ServiceResponse<TeacherDto>> LoadTeacherDetailAsync(
            System.Linq.Expressions.Expression<Func<Teacher, bool>> predicate)
        {
            var response = new ServiceResponse<TeacherDto>();

            try
            {
                var teacher = await _context.Teachers
                    .AsNoTracking()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(predicate);

                if (teacher == null)
                {
                    response.Success = false;
                    response.Message = "Teacher not found.";
                    response.StatusCode = 404;
                    return response;
                }

                // ---------- Subjects taught (from TeacherSubject) ----------
                var teacherSubjects = await _context.TeacherSubjects
                    .AsNoTracking()
                    .Include(ts => ts.Subject)
                    .Where(ts => ts.TeacherId == teacher.Id)
                    .ToListAsync();

                var subjects = teacherSubjects
                    .Where(ts => ts.Subject != null)
                    .Select(ts => new TeacherSubjectSummaryDto
                    {
                        SubjectId = ts.SubjectId,
                        SubjectName = ts.Subject!.Name,
                        SubjectCode = ts.Subject.Code,
                    })
                    .ToList();

                // ---------- Classes taught (from TeacherClass) ----------
                var teacherClasses = await _context.TeacherClasses
                    .AsNoTracking()
                    .Include(tc => tc.SchoolClass)
                    .Where(tc => tc.TeacherId == teacher.Id)
                    .ToListAsync();

                var classes = teacherClasses
                    .Where(tc => tc.SchoolClass != null)
                    .Select(tc => new TeacherClassSummaryDto
                    {
                        ClassId = tc.SchoolClassId,
                        ClassName = tc.SchoolClass!.Name,
                        Section = tc.SchoolClass.AcademicSection,
                    })
                    .ToList();

                response.Success = true;
                response.StatusCode = 200;
                response.Data = new TeacherDto
                {
                    Id = teacher.Id,
                    UserId = teacher.UserId,
                    StaffId = teacher.StaffId,
                    FirstName = teacher.FirstName,
                    LastName = teacher.LastName,
                    Email = teacher.User?.Email,
                    PhoneNumber = teacher.User?.PhoneNumber,
                    ProfilePictureUrl = teacher.User?.ProfilePictureUrl,
                    Qualification = teacher.Qualification,
                    DateEmployed = teacher.DateEmployed,
                    PrimaryRole = teacher.User?.PrimaryRole ?? "Teacher",
                    IsActive = teacher.User?.IsActive ?? false,
                    CreatedAt = teacher.User?.CreatedAt ?? teacher.DateEmployed,
                    SubjectCount = subjects.Count,
                    ClassCount = classes.Count,
                    Subjects = subjects,
                    Classes = classes,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading teacher detail.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion
    }
}