using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class AcademicSessionService : IAcademicSession
    {
        private readonly AppDbContext _Context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AcademicSessionService> _logger;
        private readonly IDistributedCache _Cache;

        public AcademicSessionService(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<AcademicSessionService> logger, IDistributedCache cache)
        {
            _Context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #region CreateSession
        public async Task<ServiceResponse<bool>> CreateAcademicSessionAsync(AcademicSessionDto Session)
        {
            var response = new ServiceResponse<bool>();

            if (Session == null)
            {
                response.Success = false;
                response.Message = "Session model cannot be null";
                response.StatusCode = 400;
                return response;
            }

            var user = await _userManager.FindByIdAsync(Session.UserId ?? "No User");
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found";
                response.StatusCode = 404;
                return response;
            }

            bool IsSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            bool IsPrincipal = await _userManager.IsInRoleAsync(user, "Principal");

            if (!IsSuperAdmin && !IsPrincipal)
            {
                response.Success = false;
                response.Message = "User does not have permission to create an academic session";
                response.StatusCode = 403;
                return response;
            }

            bool AcademicSessionExists = await _Context.AcademicSessions.AnyAsync(s => s.Name == Session.Name);
            if (AcademicSessionExists)
            {
                response.Success = false;
                response.Message = "Academic Session Already Exists";
                response.StatusCode = 409;
                return response;
            }

            var strategy = _Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _Context.Database.BeginTransactionAsync();
                try
                {
                    // Handle nullable DateTime safely
                    var startDate = Session.StartDate ?? DateTime.UtcNow;
                    var endDate = Session.EndDate ?? startDate.AddYears(1);

                    var newSession = new AcademicSession
                    {
                        Id = Guid.NewGuid(),
                        Name = Session.Name.Trim(),
                        IsCurrentSession = Session.IsCurrentSession,
                        StartDate = startDate,
                        EndDate = endDate,
                        IsActive = Session.IsActive
                    };

                    await _Context.AcademicSessions.AddAsync(newSession);
                    await _Context.SaveChangesAsync();

                    // Automatically generate the 3 mandatory terms for the session
                    var terms = new List<SchoolTerm>
                    {
                        new SchoolTerm { Id = Guid.NewGuid(), Name = "First Term", AcademicSessionId = newSession.Id, StartDate = startDate, EndDate = startDate.AddMonths(4) },
                        new SchoolTerm { Id = Guid.NewGuid(), Name = "Second Term", AcademicSessionId = newSession.Id, StartDate = startDate.AddMonths(4), EndDate = startDate.AddMonths(8) },
                        new SchoolTerm { Id = Guid.NewGuid(), Name = "Third Term", AcademicSessionId = newSession.Id, StartDate = startDate.AddMonths(8), EndDate = endDate }
                    };

                    await _Context.SchoolTerms.AddRangeAsync(terms);
                    await _Context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    response.Success = true;
                    response.Message = "Academic session and its 3 standard terms created successfully.";
                    response.StatusCode = 200;
                    response.Data = true;
                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while creating academic session.");

                    response.Success = false;
                    response.Message = $"An error occurred while creating the session: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            });
        }
        #endregion

        #region GetAllSessionsAsync
        public async Task<ServiceResponse<PaginatedViewModel<AcademicSessionDto>>> GetAllSessionsAsync(Guid SessionID, int pageNumber = 1, int pageSize = 10)
        {
            var response = new ServiceResponse<PaginatedViewModel<AcademicSessionDto>>();
            try
            {
                pageNumber = Math.Max(1, pageNumber);
                pageSize = Math.Max(1, pageSize);
                int skip = (pageNumber - 1) * pageSize;

                string cacheKey = $"AcademicSessions_Page_{SessionID}_{pageNumber}_Size_{pageSize}";
                string cacheCountKey = $"AcademicSessions_Count_{SessionID}";

                var query = _Context.AcademicSessions.Include(m => m.Terms).AsQueryable();

                if (SessionID != Guid.Empty)
                {
                    query = query.Where(m => m.Id == SessionID);
                }

                int count;
                string? countedSessions = _Cache.GetString(cacheCountKey);
                if (countedSessions == null)
                {
                    count = await query.CountAsync();
                    _Cache.SetString(cacheCountKey, count.ToString(), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                }
                else
                {
                    count = int.Parse(countedSessions);
                }

                string? cachedSessions = _Cache.GetString(cacheKey);
                if (cachedSessions != null)
                {
                    var cachedData = System.Text.Json.JsonSerializer.Deserialize<List<AcademicSessionDto>>(cachedSessions);
                    response.Data = new PaginatedViewModel<AcademicSessionDto>
                    {
                        Items = cachedData ?? new List<AcademicSessionDto>(),
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = count,
                        TotalPages = count > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0
                    };
                    response.Success = true;
                    response.StatusCode = 200;
                    return response;
                }
                else
                {
                    var sessions = await query.OrderByDescending(s => s.StartDate).Skip(skip).Take(pageSize).ToListAsync();
                    var sessionDtos = sessions.Select(s => new AcademicSessionDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        IsCurrentSession = s.IsCurrentSession,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        IsActive = s.IsActive,
                        Terms = s.Terms.Select(t => new SchoolTermDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            StartDate = t.StartDate,
                            EndDate = t.EndDate,
                            IsCurrentTerm = t.IsCurrentTerm
                        }).ToList()
                    }).ToList();

                    _Cache.SetString(cacheKey, System.Text.Json.JsonSerializer.Serialize(sessionDtos), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });

                    response.Data = new PaginatedViewModel<AcademicSessionDto>
                    {
                        Items = sessionDtos,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = count,
                        TotalPages = count > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0
                    };
                    response.Success = true;
                    response.StatusCode = 200;
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching academic sessions.");
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        #region GetSessionByIdAsync
        public async Task<AcademicSessionDto?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _Context.AcademicSessions
                .Include(s => s.Terms)
                .Where(m => m.Id == sessionId)
                .Select(m => new AcademicSessionDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    IsActive = m.IsActive,
                    IsCurrentSession = m.IsCurrentSession,
                    Terms = m.Terms.Select(t => new SchoolTermDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        IsCurrentTerm = t.IsCurrentTerm
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
        #endregion

        #region SetCurrentSessionAsync
        public async Task<bool> SetCurrentSessionAsync(Guid sessionId, Guid termId)
        {
            var session = await _Context.AcademicSessions
                .Include(s => s.Terms)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return false;

            var termToSetCurrent = session.Terms.FirstOrDefault(t => t.Id == termId);
            if (termToSetCurrent == null) return false;

            var allSessions = await _Context.AcademicSessions.Where(s => s.IsCurrentSession).ToListAsync();
            foreach (var s in allSessions)
            {
                s.IsCurrentSession = false;
            }
            session.IsCurrentSession = true;

            var allTerms = await _Context.SchoolTerms.Where(t => t.IsCurrentTerm).ToListAsync();
            foreach (var t in allTerms)
            {
                t.IsCurrentTerm = false;
            }
            termToSetCurrent.IsCurrentTerm = true;

            await _Context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region UpdateAcademicSessionAsync
        public async Task<ServiceResponse<string>> UpdateAcademicSessionAsync(AcademicSessionDto sessionDto, SchoolTermDto termDto)
        {
            var response = new ServiceResponse<string>();

            if (sessionDto == null)
            {
                response.Success = false;
                response.Message = "Session model cannot be null";
                response.StatusCode = 400;
                return response;
            }

            var existingSession = await _Context.AcademicSessions
                .Include(s => s.Terms)
                .FirstOrDefaultAsync(s => s.Id == sessionDto.Id);

            if (existingSession == null)
            {
                response.Success = false;
                response.Message = "Academic session not found";
                response.StatusCode = 404;
                return response;
            }

            bool nameExists = await _Context.AcademicSessions
                .AnyAsync(s => s.Name == sessionDto.Name && s.Id != sessionDto.Id);
            if (nameExists)
            {
                response.Success = false;
                response.Message = "Another academic session with this name already exists";
                response.StatusCode = 409;
                return response;
            }

            if (termDto != null)
            {
                if (termDto.StartDate < sessionDto.StartDate || termDto.EndDate > sessionDto.EndDate)
                {
                    response.Success = false;
                    response.Message = "Term dates must fall within the academic session start and end dates.";
                    response.StatusCode = 400;
                    return response;
                }
            }

            var strategy = _Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _Context.Database.BeginTransactionAsync();
                try
                {
                    existingSession.Name = sessionDto.Name.Trim();
                    existingSession.StartDate = sessionDto.StartDate;
                    existingSession.EndDate = sessionDto.EndDate;
                    existingSession.IsActive = sessionDto.IsActive;
                    existingSession.IsCurrentSession = sessionDto.IsCurrentSession;

                    if (termDto != null)
                    {
                        var existingTerm = existingSession.Terms.FirstOrDefault(t => t.Id == termDto.Id);
                        if (existingTerm != null)
                        {
                            existingTerm.Name = termDto.Name;
                            existingTerm.StartDate = termDto.StartDate;
                            existingTerm.EndDate = termDto.EndDate;
                            existingTerm.IsCurrentTerm = termDto.IsCurrentTerm;
                        }
                    }

                    await _Context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Success = true;
                    response.Message = "Academic session updated successfully.";
                    response.StatusCode = 200;
                    response.Data = existingSession.Id.ToString();
                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating academic session.");

                    response.Success = false;
                    response.Message = $"An error occurred while updating the session: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            });
        }
        #endregion
    }
}