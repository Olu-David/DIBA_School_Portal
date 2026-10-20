using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.PromotionDto;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.EntityFrameworkCore;

namespace DIBA_School_Portal.Server.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly AppDbContext _context;
        private readonly IAuditLog _auditLog;

        public PromotionService(AppDbContext context, IAuditLog auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        // ==========================================================
        // PREVIEW
        // ==========================================================
        public async Task<ServiceResponse<PromotionPreviewDto>> BuildPreviewAsync(Guid? sessionId = null)
        {
            var session = sessionId.HasValue
                ? await _context.AcademicSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value)
                : await _context.AcademicSessions.AsNoTracking()
                    .Where(s => s.IsCurrentSession)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

            if (session == null)
                return Fail<PromotionPreviewDto>("No academic session found.");   // ✅

            var thirdTerm = await _context.SchoolTerms.AsNoTracking()
                .Where(t => t.AcademicSessionId == session.Id && t.TermOrder == 3)
                .FirstOrDefaultAsync();

            var termEnded = thirdTerm != null && thirdTerm.EndDate <= DateTime.UtcNow;

            var classes = await _context.SchoolClasses.AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            var classById = classes.ToDictionary(c => c.Id);

            var students = await _context.Students.AsNoTracking()
                .Where(s => !s.IsGraduated)
                .OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
                .ToListAsync();

            var classGroups = new List<ClassPromotionPreviewDto>();

            foreach (var group in students.GroupBy(s => s.SchoolClassId))
            {
                var fromClass = classById.GetValueOrDefault(group.Key);
                if (fromClass == null) continue;

                var toClass = fromClass.NextClassId.HasValue
                    ? classById.GetValueOrDefault(fromClass.NextClassId.Value)
                    : null;

                var isGraduating = toClass == null;
                var needsStream =
                    fromClass.AcademicSection == AcademicSection.JuniorSecondary &&
                    toClass?.AcademicSection == AcademicSection.SeniorSecondary;

                classGroups.Add(new ClassPromotionPreviewDto
                {
                    ClassId = group.Key,
                    ClassName = fromClass.Name,
                    AcademicSection = fromClass.AcademicSection,
                    StudentCount = group.Count(),
                    DestinationClassId = toClass?.Id,
                    DestinationClassName = toClass?.Name ?? "Graduating",
                    IsGraduating = isGraduating,
                    RequiresStreamSelection = needsStream,
                    Students = group.Select(s => new StudentPreviewDto
                    {
                        Id = s.Id,
                        FullName = $"{s.FirstName} {s.LastName}",
                        AdmissionNumber = s.AdmissionNumber,
                        CurrentStream = s.Stream,
                        NeedsStream = needsStream && !s.Stream.HasValue,
                    }).ToList(),
                });
            }

            var dto = new PromotionPreviewDto
            {
                SessionId = session.Id,
                SessionName = session.Name,
                ThirdTermEndDate = thirdTerm?.EndDate,
                ThirdTermEnded = termEnded,
                IsReady = termEnded,
                Classes = classGroups,
                TotalStudents = students.Count,
                TotalGraduating = classGroups.Where(c => c.IsGraduating).Sum(c => c.StudentCount),
                TotalRequiresStream = classGroups
                    .Where(c => c.RequiresStreamSelection)
                    .SelectMany(c => c.Students)
                    .Count(s => s.NeedsStream),
            };

            return Ok(dto);   // Ok<T> infers from `dto` — fine as-is
        }

        // ==========================================================
        // EXECUTE
        // ==========================================================
        public async Task<ServiceResponse<PromotionResultDto>> ExecuteAsync(
            Guid sessionId,
            List<StudentStreamOverride> overrides,
            string currentUserId)
        {
            var session = await _context.AcademicSessions.FindAsync(sessionId);
            if (session == null)
                return Fail<PromotionResultDto>("Session not found.");         // ✅

            var thirdTerm = await _context.SchoolTerms
                .Where(t => t.AcademicSessionId == sessionId && t.TermOrder == 3)
                .FirstOrDefaultAsync();

            if (thirdTerm == null || thirdTerm.EndDate > DateTime.UtcNow)
                return Fail<PromotionResultDto>("Session has not ended yet — promotion is locked.");   // ✅

            var alreadyPromoted = await _context.PromotionRecords
                .AnyAsync(r => r.AcademicSessionId == sessionId);
            if (alreadyPromoted)
                return Fail<PromotionResultDto>("Promotion has already been executed for this session.");   // ✅

            var classes = await _context.SchoolClasses.ToListAsync();
            var classById = classes.ToDictionary(c => c.Id);

            var students = await _context.Students
                .Where(s => !s.IsGraduated)
                .ToListAsync();

            var overrideMap = (overrides ?? new List<StudentStreamOverride>())
                .GroupBy(o => o.StudentId)
                .ToDictionary(g => g.Key, g => g.First().Stream);

            var jss3 = classes.FirstOrDefault(c => c.Name == "JSS 3");
            if (jss3 != null)
            {
                var missing = students
                    .Where(s => s.SchoolClassId == jss3.Id)
                    .Where(s => !s.Stream.HasValue && !overrideMap.ContainsKey(s.Id))
                    .Select(s => $"{s.FirstName} {s.LastName}")
                    .ToList();

                if (missing.Any())
                    return Fail<PromotionResultDto>(
                        $"Stream required for: {string.Join(", ", missing)}");   // ✅
            }

            var batchId = Guid.NewGuid();
            var promoted = 0;
            var graduated = 0;
            var warnings = new List<string>();
            var records = new List<PromotionRecord>();

            foreach (var student in students)
            {
                var fromClass = classById.GetValueOrDefault(student.SchoolClassId);
                if (fromClass == null)
                {
                    warnings.Add($"Student {student.AdmissionNumber} has no class — skipped.");
                    continue;
                }

                var toClass = fromClass.NextClassId.HasValue
                    ? classById.GetValueOrDefault(fromClass.NextClassId.Value)
                    : null;

                if (overrideMap.TryGetValue(student.Id, out var overrideStream))
                    student.Stream = overrideStream;

                if (toClass == null)
                {
                    student.IsGraduated = true;
                    student.GraduationDate = DateTime.UtcNow;
                    student.GraduationSession = session.Name;
                    graduated++;
                }
                else
                {
                    student.SchoolClassId = toClass.Id;
                    promoted++;
                }

                records.Add(new PromotionRecord
                {
                    StudentId = student.Id,
                    FromClassId = fromClass.Id,
                    ToClassId = toClass?.Id,
                    AcademicSessionId = sessionId,
                    Stream = student.Stream,
                    PromotedAt = DateTime.UtcNow,
                    PerformedByUserId = currentUserId,
                    BatchId = batchId,
                });
            }

            _context.PromotionRecords.AddRange(records);
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId,
                "Session Promotion Executed",
                $"Session {session.Name}: {promoted} promoted, {graduated} graduated (batch {batchId}).",
                "n/a");

            return Ok(new PromotionResultDto
            {
                BatchId = batchId,
                PromotedCount = promoted,
                GraduatedCount = graduated,
                TotalCount = promoted + graduated,
                Warnings = warnings,
            });
        }

        // ==========================================================
        // HISTORY
        // ==========================================================
        public async Task<ServiceResponse<IEnumerable<PromotionRecordDto>>> GetHistoryAsync(Guid sessionId)
        {
            var records = await _context.PromotionRecords.AsNoTracking()
                .Where(r => r.AcademicSessionId == sessionId)
                .OrderByDescending(r => r.PromotedAt)
                .Select(r => new PromotionRecordDto
                {
                    Id = r.Id,
                    BatchId = r.BatchId,
                    StudentName = r.Student!.FirstName + " " + r.Student.LastName,
                    AdmissionNumber = r.Student.AdmissionNumber,
                    FromClassName = r.FromClass!.Name,
                    ToClassName = r.ToClass != null ? r.ToClass.Name : null,
                    Stream = r.Stream,
                    PromotedAt = r.PromotedAt,
                })
                .ToListAsync();

            return Ok<IEnumerable<PromotionRecordDto>>(records);
        }

        // ==========================================================
        // helpers
        // ==========================================================
        private static ServiceResponse<T> Ok<T>(T data) =>
            new() { Success = true, Data = data };

        private static ServiceResponse<T> Fail<T>(string message) =>
            new() { Success = false, Message = message };
    }
}