using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ServiceResponse<string>> CreateSubjectAsync(SubjectDto model, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var existing = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code.ToLower() == model.Code.ToLower());

            if (existing != null)
                return new ServiceResponse<string> { Success = false, Message = "A subject with this code already exists." };

            var subject = new Subject
            {
                Name = model.Name.Trim(),
                Code = model.Code.ToUpper().Trim()
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Subject created successfully.",
                Data = subject.Id.ToString()
            };
        }

        public async Task<ServiceResponse<string>> UpdateSubjectAsync(SubjectDto model, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null)
                return new ServiceResponse<string> { Success = false, Message = "Unauthorized: User not found." };

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return new ServiceResponse<string> { Success = false, Message = "Forbidden: Insufficient privileges." };

            var subject = await _context.Subjects.FindAsync(model.Id);
            if (subject == null)
                return new ServiceResponse<string> { Success = false, Message = "Subject not found." };

            subject.Name = model.Name.Trim();
            subject.Code = model.Code.ToUpper().Trim();

            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();

            return new ServiceResponse<string> { Success = true, Message = "Subject updated successfully." };
        }

        public async Task<bool> SoftDeleteSubject(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreSubject(Guid id, string currentUserId)
        {
            var user = await _userManager.FindByIdAsync(currentUserId);
            if (user == null) return false;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin") && !await _userManager.IsInRoleAsync(user, "Principal"))
                return false;

            var subject = await _context.Subjects
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return false;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<ServiceResponse<SubjectDto>> GetSubjectByIdAsync(Guid id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return new ServiceResponse<SubjectDto> { Success = false, Message = "Subject not found." };
            }

            var dto = new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code
            };

            return new ServiceResponse<SubjectDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetAllSubjectsAsync()
        {
            var subjects = await _context.Subjects
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code
                })
                .ToListAsync();

            return new ServiceResponse<IEnumerable<SubjectDto>> { Success = true, Data = subjects };
        }
    }
}