using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.ClassDto;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_School_Portal.Server.Services
{
    public class SchoolClassService : ISchoolClassService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditLog _auditLog;
        private readonly IHttpContextAccessor _http;

        // Roles that can see every class (read) AND edit classes/subjects (write).
        // Kept in sync with the ADMIN_ROLES / MANAGERS arrays on the frontend.
        private static readonly string[] AdminRoles =
        {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin", "Registrar", "Accountant", "Cashier",
        };

        // Stricter list — only these can CREATE / UPDATE / DELETE classes and subjects.
        // Matches the [Authorize] on the write endpoints in your controller.
        private static readonly string[] WriteRoles =
        {
            "SuperAdmin", "Principal", "SchoolAdmin",
        };

        public SchoolClassService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditLog auditLog,
            IHttpContextAccessor http)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _http = http;
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        // ==========================================================
        // HELPERS
        // ==========================================================

        /// <summary>
        /// Returns null when the current user is a staff admin (they see all classes).
        /// Returns an empty list when the user is neither admin nor teacher.
        /// Returns the teacher's assigned class IDs otherwise.
        /// </summary>
        private async Task<List<Guid>?> GetAllowedClassIdsAsync()
        {
            var userId = _http.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => AdminRoles.Contains(r))) return null;

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher == null) return new List<Guid>();

            return await _context.TeacherClasses
                .AsNoTracking()
                .Where(tc => tc.TeacherId == teacher.Id)
                .Select(tc => tc.SchoolClassId)
                .ToListAsync();
        }

        /// <summary>
        /// Strict admin check for write operations. Grants access to
        /// SuperAdmin / Principal / SchoolAdmin — the same trio that the
        /// frontend routing exposes for classes and promotions.
        /// </summary>
        private async Task<bool> IsAdminAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Any(r => WriteRoles.Contains(r));
        }

        private static ServiceResponse<T> Ok<T>(T data) =>
            new() { Success = true, Data = data };

        private static ServiceResponse<T> Fail<T>(string message, int status ) =>
            new() { Success = false, Message = message, StatusCode = status  };

        // ==========================================================
        // CREATE
        // ==========================================================
        public async Task<ServiceResponse<string>> CreateSchoolClassAsync(
            SchoolClassDTO model, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId))
                return Fail<string>("Forbidden: Insufficient privileges.", 403);

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Fail<string>("Class name is required.", 400);

            var exists = await _context.SchoolClasses
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (exists)
                return Fail<string>("A class with this name already exists.", 409);

            var schoolClass = new SchoolClass
            {
                Name = name,
                AcademicSection = model.AcademicSection,
                DisplayOrder = model.DisplayOrder,
            };

            _context.SchoolClasses.Add(schoolClass);
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId,
                "SchoolClass Created",
                $"Class '{schoolClass.Name}' ({schoolClass.AcademicSection}) created.",
                Ip);

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "School class created successfully.",
                Data = schoolClass.Id.ToString(),
            };
        }

        // ==========================================================
        // UPDATE
        // ==========================================================
        public async Task<ServiceResponse<string>> UpdateSchoolClassAsync(
            SchoolClassDTO model, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId))
                return Fail<string>("Forbidden: Insufficient privileges.", 403);

            var schoolClass = await _context.SchoolClasses.FindAsync(model.Id);
            if (schoolClass == null)
                return Fail<string>("School class not found.", 404);

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Fail<string>("Class name is required.", 400);

            var nameTaken = await _context.SchoolClasses
                .AnyAsync(c => c.Id != model.Id && c.Name.ToLower() == name.ToLower());

            if (nameTaken)
                return Fail<string>("Another class with this name already exists.", 409);

            var oldName = schoolClass.Name;
            schoolClass.Name = name;
            schoolClass.AcademicSection = model.AcademicSection;
            schoolClass.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId,
                "SchoolClass Updated",
                $"Class '{oldName}' → '{schoolClass.Name}'.",
                Ip);

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "School class updated successfully.",
            };
        }

        // ==========================================================
        // SOFT DELETE / RESTORE
        // ==========================================================
        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId)) return false;

            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null) return false;

            schoolClass.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "SchoolClass SoftDeleted", $"Class ID {id}.", Ip);

            return true;
        }

        public async Task<bool> RestoreSchoolClass(Guid id, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId)) return false;

            var schoolClass = await _context.SchoolClasses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return false;

            schoolClass.IsDeleted = false;
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "SchoolClass Restored", $"Class ID {id}.", Ip);

            return true;
        }

        // ==========================================================
        // UPDATE CLASS SUBJECTS (replace all — one row per subject)
        // ==========================================================
        public async Task<ServiceResponse<string>> UpdateClassSubjectsAsync(
            Guid classId, List<ClassSubjectAssignmentDto> items, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId))
                return Fail<string>("Forbidden: Insufficient privileges.", 403);

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return Fail<string>("School class not found.", 404);

            // ---- Normalize input: drop empties, dedupe, strip streams for non-SS ----
            items ??= new List<ClassSubjectAssignmentDto>();
            var isSeniorSecondary =
                schoolClass.AcademicSection == AcademicSection.SeniorSecondary;

            var deduped = items
                .Where(x => x.SubjectId != Guid.Empty)
                .GroupBy(x => x.SubjectId)
                .Select(g =>
                {
                    var first = g.First();
                    return new ClassSubjectAssignmentDto
                    {
                        SubjectId = first.SubjectId,
                        Stream = isSeniorSecondary ? first.Stream : null,
                    };
                })
                .ToList();

            // ---- Validate every subject exists ----
            if (deduped.Count > 0)
            {
                var ids = deduped.Select(d => d.SubjectId).ToList();
                var validCount = await _context.Subjects
                    .CountAsync(s => ids.Contains(s.Id));

                if (validCount != deduped.Count)
                    return Fail<string>("One or more subject IDs are invalid.", 400);
            }

            // ---- Replace: remove existing, insert new ----
            _context.ClassSubjects.RemoveRange(schoolClass.ClassSubjects);

            foreach (var item in deduped)
            {
                _context.ClassSubjects.Add(new ClassSubject
                {
                    SchoolClassId = classId,
                    SubjectId = item.SubjectId,
                    Stream = item.Stream,
                });
            }

            await _context.SaveChangesAsync();

            var streamSpecific = deduped.Count(d => d.Stream != null);
            await _auditLog.LogActivityAsync(
                currentUserId,
                "ClassSubjects Updated",
                $"Class ID {classId} → {deduped.Count} subject(s) " +
                $"({streamSpecific} stream-specific).",
                Ip);

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Class subjects updated successfully!",
            };
        }

        // ==========================================================
        // GET BY ID  — now includes NextClass info
        // ==========================================================
        public async Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(id))
                return Fail<SchoolClassDTO>("You are not assigned to this class.", 403);

            var schoolClass = await _context.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
                return Fail<SchoolClassDTO>("School class not found.", 404);

            // Fetch the "next class" name only when NextClassId is set — one extra
            // row read instead of a join across the whole class list.
            string? nextClassName = null;
            if (schoolClass.NextClassId.HasValue)
            {
                nextClassName = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => c.Id == schoolClass.NextClassId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            var dto = new SchoolClassDTO
            {
                Id = schoolClass.Id,
                Name = schoolClass.Name,
                AcademicSection = schoolClass.AcademicSection,
                DisplayOrder = schoolClass.DisplayOrder,
                StudentCount = await _context.Students
                    .CountAsync(s => s.SchoolClassId == schoolClass.Id),
                NextClassId = schoolClass.NextClassId,
                NextClassName = nextClassName,
            };

            return Ok(dto);
        }

        // ==========================================================
        // GET ALL (role-filtered, non-deleted only)
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync()
        {
            var allowed = await GetAllowedClassIdsAsync();

            var query = _context.SchoolClasses
                .AsNoTracking()
                .Where(c => !c.IsDeleted);   // explicit even if a global filter exists

            if (allowed != null)
                query = query.Where(c => allowed.Contains(c.Id));

            var classes = await query
                .Select(c => new SchoolClassDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    AcademicSection = c.AcademicSection,
                    DisplayOrder = c.DisplayOrder,
                    StudentCount = _context.Students.Count(s => s.SchoolClassId == c.Id),
                    NextClassId = c.NextClassId,
                    NextClassName = c.NextClass != null ? c.NextClass.Name : null,
                })
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return Ok<IEnumerable<SchoolClassDTO>>(classes);
        }

        // ==========================================================
        // GET SUBJECTS FOR CLASS (read-only)
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(classId))
                return Fail<IEnumerable<SubjectDto>>(
                    "You are not assigned to this class.", 403);

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return Fail<IEnumerable<SubjectDto>>("School class not found.", 404);

            var subjects = schoolClass.ClassSubjects
                .Where(cs => cs.Subject != null)
                .GroupBy(cs => cs.SubjectId)          // dedupe defensive
                .Select(g => g.First())
                .Select(cs => new SubjectDto
                {
                    Id = cs.Subject!.Id,
                    Name = cs.Subject.Name,
                    Code = cs.Subject.Code,
                    Stream = cs.Stream,
                })
                .OrderBy(s => s.Name)
                .ToList();

            return Ok<IEnumerable<SubjectDto>>(subjects);
        }

        // ==========================================================
        // GET SUBJECTS WITH CHECKBOX STATE (admin/editor path)
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(classId))
                return Fail<IEnumerable<SubjectCheckboxDto>>(
                    "You are not assigned to this class.", 403);

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return Fail<IEnumerable<SubjectCheckboxDto>>(
                    "School class not found.", 404);

            // Map: SubjectId → Stream (nullable). Duplicates in legacy data
            // collapse to the first row so ToDictionary can't throw.
            var assigned = schoolClass.ClassSubjects
                .Where(cs => cs.SubjectId.HasValue)
                .GroupBy(cs => cs.SubjectId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Stream);

            // Materialize the catalogue, then project in memory — lets us use
            // `assigned` freely without EF trying to translate dictionary lookups.
            var catalogue = await _context.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name, s.Code })
                .ToListAsync();

            var allSubjects = catalogue
                .Select(s =>
                {
                    var isAssigned = assigned.ContainsKey(s.Id);
                    return new SubjectCheckboxDto
                    {
                        SubjectId = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        IsAssigned = isAssigned,
                        Stream = isAssigned ? assigned[s.Id] : null,
                    };
                })
                .ToList();

            return Ok<IEnumerable<SubjectCheckboxDto>>(allSubjects);
        }
        public async Task<ServiceResponse<int>> AssignClassSubjectsAsync(Guid classId,  List<AssignClassSubjectItemDto> subjects, string currentUserId,string ipAddress)
        {
            var response = new ServiceResponse<int>();

            var schoolClass = await _context.SchoolClasses
                .FirstOrDefaultAsync(c => c.Id == classId);
            if (schoolClass == null)
            {
                response.Success = false;
                response.Message = "Class not found.";
                response.StatusCode = 404;
                return response;
            }

            var subjectIds = subjects.Select(s => s.SubjectId).Distinct().ToList();

            if (subjectIds.Count > 0)
            {
                var validCount = await _context.Subjects
                    .Where(s => subjectIds.Contains(s.Id))
                    .CountAsync();

                if (validCount != subjectIds.Count)
                {
                    response.Success = false;
                    response.Message = "One or more subject IDs are invalid.";
                    response.StatusCode = 400;
                    return response;
                }
            }

            // Streams only for Senior Secondary
            if (schoolClass.AcademicSection != AcademicSection.SeniorSecondary &&
                subjects.Any(s => s.Stream.HasValue))
            {
                response.Success = false;
                response.Message = "Streams only apply to Senior Secondary classes.";
                response.StatusCode = 400;
                return response;
            }

            // Remove existing
            var existing = await _context.ClassSubjects
                .Where(cs => cs.SchoolClassId == classId)
                .ToListAsync();
            if (existing.Count > 0)
                _context.ClassSubjects.RemoveRange(existing);

            // Add new
            foreach (var item in subjects)
            {
                _context.ClassSubjects.Add(new ClassSubject
                {
                    Id = Guid.NewGuid(),
                    SchoolClassId = classId,
                    SubjectId = item.SubjectId,
                    Stream = item.Stream,
                });
            }

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId,
                "Class Subjects Updated",
                $"Class '{schoolClass.Name}': {subjects.Count} subject(s) assigned.",
                ipAddress);

            response.Success = true;
            response.Message = $"{subjects.Count} subject(s) assigned.";
            response.StatusCode = 200;
            response.Data = subjects.Count;
            return response;
        }
    }
}