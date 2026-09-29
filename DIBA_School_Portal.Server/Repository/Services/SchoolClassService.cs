using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Services
{
    public class SchoolClassService : ISchoolClassService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchoolClassService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResponse<string>> CreateSchoolClassAsync(SchoolClassDTO model, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var existingClass = await _context.SchoolClasses
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower());

            if (existingClass != null)
                return new ServiceResponse<string> { Success = false, Message = "A class with this name already exists." };

            var schoolClass = new SchoolClass
            {
                Name = model.Name.Trim()
            };

            _context.SchoolClasses.Add(schoolClass);
            await _context.SaveChangesAsync();

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "School class created successfully.",
                Data = schoolClass.Id.ToString()
            };
        }

        public async Task<ServiceResponse<string>> UpdateSchoolClassAsync(SchoolClassDTO model, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var schoolClass = await _context.SchoolClasses.FindAsync(model.Id);
            if (schoolClass == null)
                return new ServiceResponse<string> { Success = false, Message = "School class not found." };

            schoolClass.Name = model.Name.Trim();

            _context.SchoolClasses.Update(schoolClass);
            await _context.SaveChangesAsync();

            return new ServiceResponse<string> { Success = true, Message = "School class updated successfully." };
        }

        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null) return false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreSchoolClass(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var schoolClass = await _context.SchoolClasses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null) return false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ServiceResponse<string>> UpdateClassSubjectsAsync(Guid classId, List<Guid> subjectIds, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
                return new ServiceResponse<string> { Success = false, Message = "School class not found." };

            _context.ClassSubjects.RemoveRange(schoolClass.ClassSubjects);

            if (subjectIds != null && subjectIds.Any())
            {
                foreach (var subjectId in subjectIds)
                {
                    _context.ClassSubjects.Add(new ClassSubject
                    {
                        SchoolClassId = classId,
                        SubjectId = subjectId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Class subjects updated successfully!"
            };
        }
        public async Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id)
        {
            var schoolClass = await _context.SchoolClasses.FindAsync(id);
            if (schoolClass == null)
            {
                return new ServiceResponse<SchoolClassDTO> { Success = false, Message = "School class not found." };
            }

            var dto = new SchoolClassDTO
            {
                Id = schoolClass.Id,
                Name = schoolClass.Name
            };

            return new ServiceResponse<SchoolClassDTO> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync()
        {
            var classes = await _context.SchoolClasses
                .Select(c => new SchoolClassDTO
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return new ServiceResponse<IEnumerable<SchoolClassDTO>> { Success = true, Data = classes };
        }

        // Handy method to fetch all subjects currently linked to a specific class (great for populating frontend checkboxes!)
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var schoolClass = await _context.SchoolClasses
                .Include(c => c.ClassSubjects)
                .ThenInclude(cs => cs.Subject)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (schoolClass == null)
            {
                return new ServiceResponse<IEnumerable<SubjectDto>> { Success = false, Message = "School class not found." };
            }

            var subjects = schoolClass.ClassSubjects
                .Select(cs => new SubjectDto
                {
                    Id = cs.Subject.Id,
                    Name = cs.Subject.Name,
                    Code = cs.Subject.Code
                })
                .ToList();

            return new ServiceResponse<IEnumerable<SubjectDto>> { Success = true, Data = subjects };
        }
    }
}