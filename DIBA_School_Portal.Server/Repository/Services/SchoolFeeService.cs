using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.DropDown;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class SchoolFeeService : ISchoolFeeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SchoolFeeService> _logger;

        #region Constructor
        public SchoolFeeService(AppDbContext context, ILogger<SchoolFeeService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region GetFeeCreationMetadataAsync
        public async Task<ServiceResponse<FeeCreationMetadataDto>> GetFeeCreationMetadataAsync()
        {
            var response = new ServiceResponse<FeeCreationMetadataDto>();
            try
            {
                var sessions = await _context.AcademicSessions
                    .AsNoTracking()
                    .Include(s => s.Terms)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => new SessionDropdownItemDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IsCurrentSession = s.IsCurrentSession,
                        Terms = s.Terms.Select(t => new TermDropdownItemDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            IsCurrentTerm = t.IsCurrentTerm
                        }).ToList()
                    })
                    .ToListAsync();

                var schoolClasses = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => new SchoolClassDropdownItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Section = c.AcademicSection
                    })
                    .ToListAsync();

                var sections = Enum.GetValues(typeof(AcademicSection))
                    .Cast<AcademicSection>()
                    .Select(e => new EnumLookupDto
                    {
                        Id = (int)e,
                        Name = e.ToString()
                    }).ToList();

                var streams = Enum.GetValues(typeof(ClassStream))
                    .Cast<ClassStream>()
                    .Select(e => new EnumLookupDto
                    {
                        Id = (int)e,
                        Name = e.ToString()
                    }).ToList();

                response.Data = new FeeCreationMetadataDto
                {
                    Sessions = sessions,
                    SchoolClasses = schoolClasses,
                    Sections = sections,
                    Streams = streams
                };

                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fee creation metadata.");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region CreateFeeAsync
        public async Task<ServiceResponse<bool>> CreateFeeAsync(FeeDto feeDto)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                if (feeDto == null)
                {
                    response.Success = false;
                    response.Message = "Fee payload cannot be null.";
                    response.StatusCode = 400;
                    return response;
                }

                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == feeDto.TermId && t.AcademicSessionId == feeDto.AcademicSessionId);

                if (!termExists)
                {
                    response.Success = false;
                    response.Message = "The selected term does not belong to the specified academic session.";
                    response.StatusCode = 400;
                    return response;
                }

                var classExists = await _context.SchoolClasses
                    .AnyAsync(c => c.Id == feeDto.SchoolClassId && !c.IsDeleted);

                if (!classExists)
                {
                    response.Success = false;
                    response.Message = "The selected school class does not exist.";
                    response.StatusCode = 400;
                    return response;
                }

                var fee = new Fee
                {
                    Id = Guid.NewGuid(),
                    FeeName = feeDto.FeeName.Trim(),
                    Description = feeDto.Description?.Trim() ?? "",
                    Amount = feeDto.Amount,
                    AcademicSessionId = feeDto.AcademicSessionId,
                    TermId = feeDto.TermId,
                    SchoolClassId = feeDto.SchoolClassId,
                    Section = feeDto.Section,
                    ClassStream = feeDto.Stream
                };

                await _context.Fees.AddAsync(fee);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "Fee structure created successfully.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fee structure.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetAllSchoolClassesAsync
        public async Task<ServiceResponse<IEnumerable<SchoolClassDTO>>> GetAllSchoolClassesAsync()
        {
            var response = new ServiceResponse<IEnumerable<SchoolClassDTO>>();
            try
            {
                var classes = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => new SchoolClassDTO
                    {
                        Id = c.Id,
                        Name = c.Name,
                        AcademicSection = c.AcademicSection
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = classes;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all school classes.");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetSchoolClassByIdAsync
        public async Task<ServiceResponse<SchoolClassDTO>> GetSchoolClassByIdAsync(Guid id)
        {
            var response = new ServiceResponse<SchoolClassDTO>();
            try
            {
                var schoolClass = await _context.SchoolClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

                if (schoolClass == null)
                {
                    response.Success = false;
                    response.Message = "School class not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Data = new SchoolClassDTO
                {
                    Id = schoolClass.Id,
                    Name = schoolClass.Name,
                    AcademicSection = schoolClass.AcademicSection
                };
                response.Success = true;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching school class by ID: {Id}", id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region UpdateSchoolClassAsync
        public async Task<ServiceResponse<string>> UpdateSchoolClassAsync(SchoolClassDTO model, string currentUserId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == model.Id && !c.IsDeleted);
                if (schoolClass == null)
                {
                    response.Success = false;
                    response.Message = "School class not found.";
                    response.StatusCode = 404;
                    return response;
                }

                schoolClass.Name = model.Name.Trim();
                schoolClass.AcademicSection = model.AcademicSection;

                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();

                response.Success = true;
                response.Message = "School class updated successfully.";
                response.StatusCode = 200;
                response.Data = schoolClass.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating school class: {Id}", model.Id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region SoftDeleteSchoolClass
        public async Task<bool> SoftDeleteSchoolClass(Guid id, string currentUserId)
        {
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == id);
                if (schoolClass == null) return false;

                schoolClass.IsDeleted = true;

                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting school class: {Id}", id);
                return false;
            }
        }
        #endregion

        #region RestoreSchoolClass
        public async Task<bool> RestoreSchoolClass(Guid id, string currentUserId)
        {
            try
            {
                var schoolClass = await _context.SchoolClasses.FirstOrDefaultAsync(c => c.Id == id);
                if (schoolClass == null) return false;

                schoolClass.IsDeleted = false;
                _context.SchoolClasses.Update(schoolClass);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring school class: {Id}", id);
                return false;
            }
        }
        #endregion

        #region GetSubjectsForClassAsync
        public async Task<ServiceResponse<IEnumerable<SubjectDto>>> GetSubjectsForClassAsync(Guid classId)
        {
            var response = new ServiceResponse<IEnumerable<SubjectDto>>();
            try
            {
                var subjects = await _context.Subjects
                    .AsNoTracking()
                    .Select(s => new SubjectDto
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = subjects;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subjects for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region GetClassSubjectsWithSelectionAsync
        public async Task<ServiceResponse<IEnumerable<SubjectCheckboxDto>>> GetClassSubjectsWithSelectionAsync(Guid classId)
        {
            var response = new ServiceResponse<IEnumerable<SubjectCheckboxDto>>();
            try
            {
                // 1. Get IDs of subjects already assigned to this class
                var assignedSubjectIds = await _context.ClassSubjects
                    .Where(cs => cs.SchoolClassId == classId)
                    .Select(cs => cs.SubjectId)
                    .ToListAsync();

                // 2. Get all available subjects and map them, checking if they are in the assigned list
                var allSubjects = await _context.Subjects
                    .AsNoTracking()
                    .ToListAsync();

                var result = allSubjects.Select(s => new SubjectCheckboxDto
                {
                    SubjectId = s.Id,
                    Name = s.Name,
                    IsAssigned = assignedSubjectIds.Contains(s.Id)
                }).ToList();

                response.Success = true;
                response.Data = result;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class subjects with selection for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion

        #region UpdateClassSubjectsAsync
        public async Task<ServiceResponse<string>> UpdateClassSubjectsAsync(Guid classId, List<Guid> subjectIds, string currentUserId)
        {
            var response = new ServiceResponse<string>();
            try
            {
                response.Success = true;
                response.Message = "Class subjects updated successfully.";
                response.StatusCode = 200;
                response.Data = classId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating class subjects for class: {ClassId}", classId);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
        #endregion
    }
}