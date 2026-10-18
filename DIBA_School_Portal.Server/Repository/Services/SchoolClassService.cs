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
        // HELPER — Which classes can the current user see?
        // ==========================================================
        private async Task<List<Guid>?> GetAllowedClassIdsAsync()
        {
            var userId = _http.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            var adminRoles = new[]
            {
                "SuperAdmin", "Principal", "Headmaster", "Headmistress",
                "VicePrincipal", "SchoolAdmin", "Registrar", "Accountant",
                "Cashier",
            };

            if (roles.Any(r => adminRoles.Contains(r))) return null;

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

        // ==========================================================
        // CREATE
        // ==========================================================
        public async Task<ServiceResponse<string>> CreateSchoolClassAsync(
            SchoolClassDTO model, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") &&
                !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var existingClass = await _context.SchoolClasses
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower());

            if (existingClass != null)
                return new ServiceResponse<string> { Success = false, Message = "A class with this name already exists." };

            var schoolClass = new SchoolClass
            {
                Name = model.Name.Trim(),
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
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") &&
                !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var schoolClass = await _context.SchoolClasses.FindAsync(model.Id);
            if (schoolClass == null)
                return new ServiceResponse<string> { Success = false, Message = "School class not found." };

            var oldName = schoolClass.Name;

            schoolClass.Name = model.Name.Trim();
            schoolClass.AcademicSection = model.AcademicSection;

            _context.SchoolClasses.Update(schoolClass);
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
        // SOFT DELETE
        // ==========================================================
        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") &&
                !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null) return false;

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "SchoolClass SoftDeleted", $"Class ID {id}.", Ip);

            return true;
        }

        // ==========================================================
        // RESTORE
        // ==========================================================
        public async Task<bool> RestoreSchoolClass(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") &&
                !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var schoolClass = await _context.SchoolClasses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return false;

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "SchoolClass Restored", $"Class ID {id}.", Ip);

            return true;
        }

        // ==========================================================
        // UPDATE CLASS SUBJECTS — with stream per subject
        // ==========================================================
        public async Task<ServiceResponse<string>> UpdateClassSubjectsAsync(
            Guid classId, List<ClassSubjectAssignmentDto> items, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") &&
                !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return new ServiceResponse<string> { Success = false, Message = "School class not found." };

            // Normalize input
            items ??= new List<ClassSubjectAssignmentDto>();
            var deduped = items
                .Where(x => x.SubjectId != Guid.Empty)
                .GroupBy(x => x.SubjectId)
                .Select(g => g.First())
                .ToList();

            // Validate subject IDs exist
            if (deduped.Any())
            {
                var validIds = await _context.Subjects
                    .Where(s => deduped.Select(d => d.SubjectId).Contains(s.Id))
                    .Select(s => s.Id)
                    .ToListAsync();

                if (validIds.Count != deduped.Count)
                    return new ServiceResponse<string>
                    {
                        Success = false,
                        Message = "One or more subject IDs are invalid.",
                    };
            }

            // Remove existing
            _context.ClassSubjects.RemoveRange(schoolClass.ClassSubjects);

            // Insert new
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
            {
                return new ServiceResponse<SchoolClassDTO>
                {
                    Success = false,
                    Message = "You are not assigned to this class.",
                    StatusCode = 403,
                };
            }

            var schoolClass = await _context.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
            {
                return new ServiceResponse<SchoolClassDTO>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };
            }

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
            {
                query = query.Where(c => allowed.Contains(c.Id));
            }

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
        // GET SUBJECTS FOR CLASS (read-only) — includes stream
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var allowed = await GetAllowedClassIdsAsync();
            if (allowed != null && !allowed.Contains(classId))
            {
                return new ServiceResponse<IEnumerable<SubjectDto>>
                {
                    Success = false,
                    Message = "You are not assigned to this class.",
                    StatusCode = 403,
                };
            }

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
            {
                return new ServiceResponse<IEnumerable<SubjectDto>>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };
            }

            var subjects = schoolClass.ClassSubjects
                .Select(cs => new SubjectDto
                {
                    Id = cs.Subject.Id,
                    Name = cs.Subject.Name,
                    Code = cs.Subject.Code,
                    Stream = cs.Stream,   // 👈 new
                })
                .ToList();

            return new ServiceResponse<IEnumerable<SubjectDto>> { Success = true, Data = subjects };
        }

        // ==========================================================
        // GET SUBJECTS WITH CHECKBOX STATE (admin only) — includes stream
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId)
        {
            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
            {
                return new ServiceResponse<IEnumerable<SubjectCheckboxDto>>
                {
                    Success = false,
                    Message = "School class not found.",
                    StatusCode = 404,
                };
            }

            // Map of assigned subject → stream (or null)
            var assigned = schoolClass.ClassSubjects
                .Where(cs => cs.SubjectId.HasValue)
                .ToDictionary(cs => cs.SubjectId!.Value, cs => cs.Stream);

            // 👇 DB query — no reference to `assigned` inside
            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name, s.Code })
                .ToListAsync();

            // 👇 In-memory projection — now we can freely use `assigned`
            var allSubjects = subjects
                .Select(s => new SubjectCheckboxDto
                {
                    SubjectId = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Stream = assigned.TryGetValue(s.Id, out var stream) ? stream : null,
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