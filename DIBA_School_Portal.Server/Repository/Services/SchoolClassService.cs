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

        private static readonly string[] AdminRoles =
        {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin", "Registrar", "Accountant", "Cashier",
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
        private async Task<List<Guid>?> GetAllowedClassIdsAsync()
        {
            var userId = _http.HttpContext?.User?
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

        private async Task<bool> IsAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, "SuperAdmin")
                || await _userManager.IsInRoleAsync(user, "Principal");
        }

        // ==========================================================
        // CREATE
        // ==========================================================
        public async Task<ServiceResponse<string>> CreateSchoolClassAsync(
            SchoolClassDTO model, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId))
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Forbidden: Insufficient privileges.",
                };

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Class name is required.",
                };

            var exists = await _context.SchoolClasses
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());

            if (exists)
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "A class with this name already exists.",
                };

            var schoolClass = new SchoolClass
            {
                Name = name,
                AcademicSection = model.AcademicSection,
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
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Forbidden: Insufficient privileges.",
                };

            var schoolClass = await _context.SchoolClasses.FindAsync(model.Id);
            if (schoolClass == null)
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "School class not found.",
                };

            var name = model.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Class name is required.",
                };

            var nameTaken = await _context.SchoolClasses
                .AnyAsync(c => c.Id != model.Id && c.Name.ToLower() == name.ToLower());

            if (nameTaken)
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Another class with this name already exists.",
                };

            var oldName = schoolClass.Name;
            schoolClass.Name = name;
            schoolClass.AcademicSection = model.AcademicSection;

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
        //   NOTE: adjust the "IsDeleted" property below to whatever
        //   your SchoolClass entity actually uses for soft-delete.
        // ==========================================================
        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            if (!await IsAdminAsync(currentUserId)) return false;

            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null) return false;

            schoolClass.IsDeleted = true;                 // ← adjust to your entity
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

            schoolClass.IsDeleted = false;                // ← adjust to your entity
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
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Forbidden: Insufficient privileges.",
                };

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "School class not found.",
                };

            // ---- Normalize input (dedupe + drop empty + validate stream) ----
            items ??= new List<ClassSubjectAssignmentDto>();
            var isSeniorSecondary = schoolClass.AcademicSection == AcademicSection.SeniorSecondary;

            var deduped = items
                .Where(x => x.SubjectId != Guid.Empty)
                .GroupBy(x => x.SubjectId)
                .Select(g =>
                {
                    var first = g.First();
                    // Streams only make sense for SS classes
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
                    return new ServiceResponse<string>
                    {
                        Success = false,
                        Message = "One or more subject IDs are invalid.",
                    };
            }

            // ---- Replace ----
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

            await _auditLog.LogActivityAsync(
                currentUserId,
                "ClassSubjects Updated",
                $"Class ID {classId} → {deduped.Count} subject(s) " +
                $"({deduped.Count(d => d.Stream != null)} stream-specific).",
                Ip);

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Class subjects updated successfully!",
            };
        }

        // ==========================================================
        // GET BY ID
        // ==========================================================
        public async Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(id))
                return new ServiceResponse<SchoolClassDTO>
                {
                    Success = false,
                    Message = "You are not assigned to this class.",
                    StatusCode = 403,
                };

            var schoolClass = await _context.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
                return new ServiceResponse<SchoolClassDTO>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };

            var dto = new SchoolClassDTO
            {
                Id = schoolClass.Id,
                Name = schoolClass.Name,
                AcademicSection = schoolClass.AcademicSection,
                DisplayOrder = schoolClass.DisplayOrder,
                StudentCount = await _context.Students
                    .CountAsync(s => s.SchoolClassId == schoolClass.Id),
            };

            return new ServiceResponse<SchoolClassDTO> { Success = true, Data = dto };
        }

        // ==========================================================
        // GET ALL (role-filtered)
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync()
        {
            var allowed = await GetAllowedClassIdsAsync();

            var query = _context.SchoolClasses.AsNoTracking();
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
                })
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return new ServiceResponse<IEnumerable<SchoolClassDTO>>
            {
                Success = true,
                Data = classes,
            };
        }

        // ==========================================================
        // GET SUBJECTS FOR CLASS (read-only)
        //   Returns ONLY assigned subjects. Dedupes in case legacy
        //   duplicate rows still exist before the index is applied.
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(classId))
                return new ServiceResponse<IEnumerable<SubjectDto>>
                {
                    Success = false,
                    Message = "You are not assigned to this class.",
                    StatusCode = 403,
                };

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return new ServiceResponse<IEnumerable<SubjectDto>>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };

            var subjects = schoolClass.ClassSubjects
                .Where(cs => cs.Subject != null)
                .GroupBy(cs => cs.SubjectId)                 // dedupe
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

            return new ServiceResponse<IEnumerable<SubjectDto>>
            {
                Success = true,
                Data = subjects,
            };
        }

        // ==========================================================
        // GET SUBJECTS WITH CHECKBOX STATE  (admin/editor path)
        //   ⚠️ FIXED: now returns IsAssigned so the UI can rebuild
        //   `savedSubjectIds` on refresh.
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(classId))
                return new ServiceResponse<IEnumerable<SubjectCheckboxDto>>
                {
                    Success = false,
                    Message = "You are not assigned to this class.",
                    StatusCode = 403,
                };

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return new ServiceResponse<IEnumerable<SubjectCheckboxDto>>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };

            // Dedupe defensively — legacy data may have duplicate rows.
            var assigned = schoolClass.ClassSubjects
                .Where(cs => cs.SubjectId.HasValue)
                .GroupBy(cs => cs.SubjectId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Stream);

            // Materialize first, then project — no closures inside the SQL.
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

            return new ServiceResponse<IEnumerable<SubjectCheckboxDto>>
            {
                Success = true,
                Data = allSubjects,
            };
        }
    }
}