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
        private readonly IAuditLog _auditLog;              
        private readonly IHttpContextAccessor _http;       

        public SubjectService(
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

            // AUDIT LOG
            await _auditLog.LogActivityAsync(
                currentUserId,
                "Subject Created",
                $"Subject '{subject.Name}' ({subject.Code}) created.",
                Ip);

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

            var oldName = subject.Name;
            var oldCode = subject.Code;

            subject.Name = model.Name.Trim();
            subject.Code = model.Code.ToUpper().Trim();

            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();

            // AUDIT LOG
            await _auditLog.LogActivityAsync(
                currentUserId,
                "Subject Updated",
                $"Subject '{oldName}' ({oldCode}) → '{subject.Name}' ({subject.Code}).",
                Ip);

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

            // AUDIT LOG
            await _auditLog.LogActivityAsync(currentUserId, "Subject SoftDeleted", $"Subject ID {id}.", Ip);

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

            // AUDIT LOG
            await _auditLog.LogActivityAsync(currentUserId, "Subject Restored", $"Subject ID {id}.", Ip);

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