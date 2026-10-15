using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
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
        private readonly IAuditLog _auditLog;
        private readonly IHttpContextAccessor _http;

        public SchoolFeeService(
            AppDbContext context,
            ILogger<SchoolFeeService> logger,
            IAuditLog auditLog,
            IHttpContextAccessor http)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        private string Ip =>
            _http.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        private string CallerId =>
            _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? "System";

        // ==========================================================
        // SORT HELPER — Nursery 1, Nursery 2, ..., Primary 1, ..., SS 3
        // ==========================================================
        private static int ClassRank(AcademicSection section, string name)
        {
            int sec = section switch
            {
                AcademicSection.PreNursery => 0,
                AcademicSection.Nursery => 1000,
                AcademicSection.Primary => 2000,
                AcademicSection.JuniorSecondary => 3000,
                AcademicSection.SeniorSecondary => 4000,
                _ => 9000,
            };

            var match = System.Text.RegularExpressions.Regex.Match(name ?? "", @"\d+");
            int num = match.Success ? int.Parse(match.Value) : 0;

            return sec + num;
        }

        // ==========================================================
        // METADATA — sessions, classes (sorted), sections, streams
        // ==========================================================
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
                        Terms = s.Terms
                            .OrderBy(t => t.StartDate)
                            .Select(t => new TermDropdownItemDto
                            {
                                Id = t.Id,
                                Name = t.Name,
                                IsCurrentTerm = t.IsCurrentTerm
                            }).ToList()
                    })
                    .ToListAsync();

                var rawClasses = await _context.SchoolClasses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => new SchoolClassDropdownItemDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Section = c.AcademicSection
                    })
                    .ToListAsync();

                var schoolClasses = rawClasses
                    .OrderBy(c => ClassRank(c.Section, c.Name))
                    .ThenBy(c => c.Name)
                    .ToList();

                var sections = Enum.GetValues(typeof(AcademicSection))
                    .Cast<AcademicSection>()
                    .Select(e => new EnumLookupDto { Id = (int)e, Name = e.ToString() })
                    .ToList();

                var streams = Enum.GetValues(typeof(ClassStream))
                    .Cast<ClassStream>()
                    .Select(e => new EnumLookupDto { Id = (int)e, Name = e.ToString() })
                    .ToList();

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

        // ==========================================================
        // CREATE FEE — a catalog entry + one ClassFeePrice per class
        // ==========================================================
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

                if (string.IsNullOrWhiteSpace(feeDto.FeeName))
                {
                    response.Success = false;
                    response.Message = "Fee name is required.";
                    response.StatusCode = 400;
                    return response;
                }

                if (feeDto.ClassPrices == null || !feeDto.ClassPrices.Any())
                {
                    response.Success = false;
                    response.Message = "At least one class price is required.";
                    response.StatusCode = 400;
                    return response;
                }

                // Validate term belongs to the session
                var termExists = await _context.SchoolTerms
                    .AnyAsync(t => t.Id == feeDto.TermId && t.AcademicSessionId == feeDto.AcademicSessionId);

                if (!termExists)
                {
                    response.Success = false;
                    response.Message = "The selected term does not belong to the specified academic session.";
                    response.StatusCode = 400;
                    return response;
                }

                // Validate every class in the payload exists
                var classIds = feeDto.ClassPrices.Select(cp => cp.SchoolClassId).Distinct().ToList();
                var validClassIds = await _context.SchoolClasses
                    .Where(c => classIds.Contains(c.Id) && !c.IsDeleted)
                    .Select(c => c.Id)
                    .ToListAsync();

                var missing = classIds.Except(validClassIds).ToList();
                if (missing.Any())
                {
                    response.Success = false;
                    response.Message = $"The following class(es) do not exist: {string.Join(", ", missing)}.";
                    response.StatusCode = 400;
                    return response;
                }

                // Fee catalog entry — no class / section / stream tied to the Fee itself
                var fee = new Fee
                {
                    Id = Guid.NewGuid(),
                    FeeName = feeDto.FeeName.Trim(),
                    Description = feeDto.Description?.Trim() ?? "",
                    AcademicSessionId = feeDto.AcademicSessionId,
                    TermId = feeDto.TermId,
                    SchoolClassId = null,
                    Section = null,
                    ClassStream = null,
                    IsDeleted = false,
                };

                await _context.Fees.AddAsync(fee);

                foreach (var cp in feeDto.ClassPrices)
                {
                    var classFeePrice = new ClassFeePrice
                    {
                        Id = Guid.NewGuid(),
                        FeeId = fee.Id,
                        SchoolClassId = cp.SchoolClassId,
                        Amount = cp.Amount,
                        MinimumAmountToPay = cp.MinimumAmountToPay,
                    };

                    await _context.ClassFeePrices.AddAsync(classFeePrice);
                }

                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(
                    CallerId,
                    "Fee Created",
                    $"Fee '{fee.FeeName}' created for term {fee.TermId} with {feeDto.ClassPrices.Count} class-price row(s).",
                    Ip);

                response.Success = true;
                response.Message = "Fee structure created successfully.";
                response.StatusCode = 200;
                response.Data = true;
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                while (inner?.InnerException != null) inner = inner.InnerException;

                _logger.LogError(dbEx, "DB error creating fee structure.");
                response.Success = false;
                response.Message = $"Database error: {inner?.Message ?? dbEx.Message}";
                response.StatusCode = 500;
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

        // ==========================================================
        // LIST FEES
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<FeeListItemDto>>> GetFeesAsync(
            Guid? sessionId, Guid? termId, bool includeDeleted)
        {
            var response = new ServiceResponse<IEnumerable<FeeListItemDto>>();
            try
            {
                var query = _context.Fees.AsNoTracking().AsQueryable();

                if (includeDeleted) query = query.IgnoreQueryFilters();

                if (sessionId.HasValue)
                    query = query.Where(f => f.AcademicSessionId == sessionId.Value);

                if (termId.HasValue)
                    query = query.Where(f => f.TermId == termId.Value);

                var fees = await query
                    .OrderBy(f => f.FeeName)
                    .Select(f => new FeeListItemDto
                    {
                        Id = f.Id,
                        FeeName = f.FeeName,
                        Description = f.Description,
                        AcademicSessionId = f.AcademicSessionId,
                        TermId = f.TermId,
                        IsDeleted = f.IsDeleted,
                        ClassPrices = f.ClassFeePrices.Select(p => new FeeClassPriceDto
                        {
                            SchoolClassId = p.SchoolClassId,
                            ClassName = p.SchoolClass != null ? p.SchoolClass.Name : "—",
                            Amount = p.Amount,
                            MinimumAmountToPay = p.MinimumAmountToPay,
                        }).ToList(),
                    })
                    .ToListAsync();

                response.Success = true;
                response.Data = fees;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing fees.");
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }

        // ==========================================================
        // GET FEE BY ID
        // ==========================================================
        public async Task<ServiceResponse<FeeListItemDto>> GetFeeByIdAsync(Guid id)
        {
            var response = new ServiceResponse<FeeListItemDto>();
            try
            {
                var fee = await _context.Fees
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(f => f.Id == id)
                    .Select(f => new FeeListItemDto
                    {
                        Id = f.Id,
                        FeeName = f.FeeName,
                        Description = f.Description,
                        AcademicSessionId = f.AcademicSessionId,
                        TermId = f.TermId,
                        IsDeleted = f.IsDeleted,
                        ClassPrices = f.ClassFeePrices.Select(p => new FeeClassPriceDto
                        {
                            SchoolClassId = p.SchoolClassId,
                            ClassName = p.SchoolClass != null ? p.SchoolClass.Name : "—",
                            Amount = p.Amount,
                            MinimumAmountToPay = p.MinimumAmountToPay,
                        }).ToList(),
                    })
                    .FirstOrDefaultAsync();

                if (fee == null)
                {
                    response.Success = false;
                    response.Message = "Fee not found.";
                    response.StatusCode = 404;
                    return response;
                }

                response.Success = true;
                response.Data = fee;
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching fee {Id}.", id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }

        // ==========================================================
        // SOFT DELETE FEE
        // ==========================================================
        public async Task<ServiceResponse<string>> SoftDeleteFeeAsync(
            Guid id, string currentUserId, string ip)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var fee = await _context.Fees.FirstOrDefaultAsync(f => f.Id == id);
                if (fee == null)
                {
                    response.Success = false;
                    response.Message = "Fee not found.";
                    response.StatusCode = 404;
                    return response;
                }

                fee.IsDeleted = true;
                fee.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(currentUserId,
                    "Fee SoftDeleted", $"Fee '{fee.FeeName}' (ID {id}) deleted.", ip);

                response.Success = true;
                response.Message = "Fee deleted.";
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fee {Id}.", id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }

        // ==========================================================
        // RESTORE FEE
        // ==========================================================
        public async Task<ServiceResponse<string>> RestoreFeeAsync(
            Guid id, string currentUserId, string ip)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var fee = await _context.Fees
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fee == null)
                {
                    response.Success = false;
                    response.Message = "Fee not found.";
                    response.StatusCode = 404;
                    return response;
                }

                if (!fee.IsDeleted)
                {
                    response.Success = false;
                    response.Message = "Fee is not deleted.";
                    response.StatusCode = 400;
                    return response;
                }

                fee.IsDeleted = false;
                fee.DeletedAt = null;
                await _context.SaveChangesAsync();

                await _auditLog.LogActivityAsync(currentUserId,
                    "Fee Restored", $"Fee '{fee.FeeName}' (ID {id}) restored.", ip);

                response.Success = true;
                response.Message = "Fee restored.";
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring fee {Id}.", id);
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 500;
            }
            return response;
        }
    }
}
