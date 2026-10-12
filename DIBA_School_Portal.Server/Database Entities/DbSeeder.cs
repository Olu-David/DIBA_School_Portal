using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DIBA_School_Portal.Server.Database_Entities
{
    public class DbSeeder
    {
        // ---------------- All roles ----------------
        private static readonly string[] Roles = new[]
        {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin", "Accountant", "Registrar",
            "Teacher", "FormTeacher", "Student", "Parent"
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>();

            try
            {
                // -------- 1. Roles --------
                await SeedRolesAsync(roleManager, logger);

                // -------- 2. Academic session + terms --------
                await SeedSessionAsync(context, logger);

                // -------- 3. Classes --------
                var classes = await SeedClassesAsync(context, logger);

                // -------- 4. Users (all roles) --------
                var admissionCounter = await SeedUsersAsync(userManager, context, classes, logger);

                // -------- 5. Subjects --------
                await SeedSubjectsAsync(context, logger);

                // -------- 6. Link parents to students --------
                await SeedParentLinksAsync(userManager, context, logger);

                logger.LogInformation("Database seeding completed. {Count} students admitted.",
                    admissionCounter);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database seeding.");
                throw;
            }
        }

        // =====================================================
        // 1. ROLES
        // =====================================================
        private static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            foreach (var roleName in Roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    logger.LogInformation("Role created: {Role}", roleName);
                }
            }
        }

        // =====================================================
        // 2. ACADEMIC SESSION + 3 TERMS
        // =====================================================
        private static async Task SeedSessionAsync(AppDbContext context, ILogger logger)
        {
            const string sessionName = "2026/2027";

            if (await context.AcademicSessions.AnyAsync(s => s.Name == sessionName))
                return;

            var startDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2027, 7, 31, 0, 0, 0, DateTimeKind.Utc);

            var session = new AcademicSession
            {
                Id = Guid.NewGuid(),
                Name = sessionName,
                IsCurrentSession = true,
                IsActive = true,
                StartDate = startDate,
                EndDate = endDate,
            };

            context.AcademicSessions.Add(session);

            context.SchoolTerms.AddRange(
                new SchoolTerm
                {
                    Id = Guid.NewGuid(),
                    Name = "First Term",
                    AcademicSessionId = session.Id,
                    StartDate = startDate,
                    EndDate = startDate.AddMonths(4),
                    IsCurrentTerm = true,
                },
                new SchoolTerm
                {
                    Id = Guid.NewGuid(),
                    Name = "Second Term",
                    AcademicSessionId = session.Id,
                    StartDate = startDate.AddMonths(4),
                    EndDate = startDate.AddMonths(8),
                    IsCurrentTerm = false,
                },
                new SchoolTerm
                {
                    Id = Guid.NewGuid(),
                    Name = "Third Term",
                    AcademicSessionId = session.Id,
                    StartDate = startDate.AddMonths(8),
                    EndDate = endDate,
                    IsCurrentTerm = false,
                }
            );

            await context.SaveChangesAsync();
            logger.LogInformation("Academic session {Session} seeded.", sessionName);
        }

        // =====================================================
        // 3. CLASSES
        // =====================================================
        private static async Task<Dictionary<string, Guid>> SeedClassesAsync(
            AppDbContext context, ILogger logger)
        {
            var wanted = new (string Name, AcademicSection Section, int Order)[]
            {
                ("Nursery 1", AcademicSection.Nursery,         1),
                ("Nursery 2", AcademicSection.Nursery,         2),
                ("Primary 1", AcademicSection.Primary,        10),
                ("Primary 2", AcademicSection.Primary,        11),
                ("Primary 3", AcademicSection.Primary,        12),
                ("Primary 4", AcademicSection.Primary,        13),
                ("Primary 5", AcademicSection.Primary,        14),
                ("Primary 6", AcademicSection.Primary,        15),
                ("JSS 1",     AcademicSection.JuniorSecondary, 20),
                ("JSS 2",     AcademicSection.JuniorSecondary, 21),
                ("JSS 3",     AcademicSection.JuniorSecondary, 22),
                ("SS 1",      AcademicSection.SeniorSecondary, 30),
                ("SS 2",      AcademicSection.SeniorSecondary, 31),
                ("SS 3",      AcademicSection.SeniorSecondary, 32),
            };

            var result = new Dictionary<string, Guid>();

            foreach (var c in wanted)
            {
                var existing = await context.SchoolClasses
                    .FirstOrDefaultAsync(x => x.Name == c.Name);

                if (existing != null)
                {
                    // Backfill in case it was created without order/section
                    existing.AcademicSection = c.Section;
                    existing.DisplayOrder = c.Order;
                    result[c.Name] = existing.Id;
                    continue;
                }

                var newClass = new SchoolClass
                {
                    Id = Guid.NewGuid(),
                    Name = c.Name,
                    AcademicSection = c.Section,
                    DisplayOrder = c.Order,
                };

                context.SchoolClasses.Add(newClass);
                result[c.Name] = newClass.Id;
                logger.LogInformation("Class created: {Name}", c.Name);
            }

            await context.SaveChangesAsync();
            return result;
        }

        // =====================================================
        // 4. USERS
        // =====================================================
        private static async Task<int> SeedUsersAsync(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            Dictionary<string, Guid> classes,
            ILogger logger)
        {
            // (Email, First, Last, Role, Password, Phone, ClassName, Stream)
            // Stream is only used for SS students. Pass null for everyone else.
            var seeds = new (
                string Email,
                string First,
                string Last,
                string Role,
                string Password,
                string Phone,
                string? ClassName,
                ClassStream? Stream
            )[]
            {
                // ---- Admin tier ----
                ("superadmin@diba.com.ng",  "System",   "Administrator", "SuperAdmin",    "SuperAdmin@2026", "08000000001", null, null),
                ("principal@diba.com.ng",   "Ada",      "Obi",           "Principal",     "Test@1234",       "08000000002", null, null),
                ("headmaster@diba.com.ng",  "Emeka",    "Nwosu",         "Headmaster",    "Test@1234",       "08000000003", null, null),
                ("headmistress@diba.com.ng","Ngozi",    "Eze",           "Headmistress",  "Test@1234",       "08000000004", null, null),
                ("vp@diba.com.ng",          "Samuel",   "Adeyemi",       "VicePrincipal", "Test@1234",       "08000000005", null, null),
                ("admin@diba.com.ng",       "Blessing", "Ige",           "SchoolAdmin",   "Test@1234",       "08000000006", null, null),
                ("accountant@diba.com.ng",  "Kunle",    "Ojo",           "Accountant",    "Test@1234",       "08000000007", null, null),
                ("registrar@diba.com.ng",   "Halima",   "Yusuf",         "Registrar",     "Test@1234",       "08000000008", null, null),

                // ---- Teachers ----
                ("teacher1@diba.com.ng",    "Grace",    "Uche",          "Teacher",       "Test@1234",       "08000000009", null, null),
                ("teacher2@diba.com.ng",    "Peter",    "Bello",         "Teacher",       "Test@1234",       "08000000010", null, null),
                ("formteacher@diba.com.ng", "Chioma",   "Okeke",         "FormTeacher",   "Test@1234",       "08000000011", null, null),

                // ---- Students — JSS (no stream) ----
                ("student1@diba.com.ng",    "Chidi",    "Okafor",        "Student",       "Test@1234",       "08000000012", "JSS 1", null),
                ("student2@diba.com.ng",    "Amaka",    "Uche",          "Student",       "Test@1234",       "08000000013", "JSS 1", null),
                ("student3@diba.com.ng",    "Tunde",    "Adeyemi",       "Student",       "Test@1234",       "08000000014", "JSS 2", null),
                ("student4@diba.com.ng",    "Fatima",   "Bello",         "Student",       "Test@1234",       "08000000015", "JSS 2", null),

                // ---- Students — SS (with streams) ----
                ("student5@diba.com.ng",    "Ibrahim",  "Musa",          "Student",       "Test@1234",       "08000000016", "SS 1", ClassStream.Science),
                ("student6@diba.com.ng",    "Ngozi",    "Eze",           "Student",       "Test@1234",       "08000000017", "SS 1", ClassStream.Arts),
                ("student7@diba.com.ng",    "Samuel",   "Ade",           "Student",       "Test@1234",       "08000000018", "SS 2", ClassStream.Commercial),
                ("student8@diba.com.ng",    "Rachael",  "John",          "Student",       "Test@1234",       "08000000019", "SS 2", ClassStream.Science),

                // ---- Parents ----
                ("parent1@diba.com.ng",     "Yemi",     "Okafor",        "Parent",        "Test@1234",       "08111111111", null, null),
                ("parent2@diba.com.ng",     "Kemi",     "Bello",         "Parent",        "Test@1234",       "08222222222", null, null),
            };

            int admissionCounter = 1;
            int staffCounter = 1;

            foreach (var s in seeds)
            {
                if (await userManager.FindByEmailAsync(s.Email) != null) continue;

                var isStudent = s.Role == "Student";
                var isParent = s.Role == "Parent";

                // Generate username:
                //   students → admission number (set after creation)
                //   parents  → phone
                //   others   → email
                var username = isParent ? s.Phone : s.Email;

                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = s.Email,
                    EmailConfirmed = true,
                    PhoneNumber = s.Phone,
                    FirstName = s.First,
                    LastName = s.Last,
                    PrimaryRole = s.Role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                // Attach class for students
                Guid? classIdForUser = null;
                if (isStudent && s.ClassName != null && classes.TryGetValue(s.ClassName, out var cid))
                {
                    classIdForUser = cid;
                    user.ClassId = cid;
                }

                var result = await userManager.CreateAsync(user, s.Password);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create {Email}: {Err}",
                        s.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await userManager.AddToRoleAsync(user, s.Role);

                // -------- Student profile --------
                if (isStudent && classIdForUser.HasValue)
                {
                    var year = DateTime.UtcNow.Year;
                    var admissionNumber = $"DIBA/{year}/{admissionCounter:D3}";
                    admissionCounter++;

                    // Update username to admission number
                    user.UserName = admissionNumber;
                    await userManager.UpdateAsync(user);

                    // Stream is only meaningful for Senior Secondary.
                    // For JSS/Primary/Nursery we force null even if the seed row
                    // accidentally carries a value.
                    Enums.ClassStream? parsedStream = null;

                    var targetClass = await context.SchoolClasses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == classIdForUser.Value);

                    if (targetClass?.AcademicSection == AcademicSection.SeniorSecondary)
                    {
                        parsedStream = s.Stream;
                    }

                    context.Students.Add(new Student
                    {
                        UserId = user.Id,
                        AdmissionNumber = admissionNumber,
                        FirstName = user.FirstName!,
                        LastName = user.LastName!,
                        SchoolClassId = classIdForUser.Value,
                        Stream = parsedStream,
                        CreatedAt = DateTime.UtcNow,
                    });

                    logger.LogInformation(
                        "Student created: {Admission} ({First} {Last}) — {Class} — Stream: {Stream}",
                        admissionNumber,
                        user.FirstName,
                        user.LastName,
                        targetClass?.Name ?? "?",
                        parsedStream?.ToString() ?? "none");
                }

                // -------- Teacher record --------
                if (s.Role == "Teacher" || s.Role == "FormTeacher")
                {
                    var staffId = $"DIBA/STF/{staffCounter:D3}";
                    staffCounter++;

                    context.Teachers.Add(new Teacher
                    {
                        UserId = user.Id,
                        StaffId = staffId,
                        FirstName = user.FirstName!,
                        LastName = user.LastName!,
                        Qualification = "B.Ed",
                        DateEmployed = DateTime.UtcNow,
                    });
                }

                logger.LogInformation("User created: {Email} ({Role})", s.Email, s.Role);
            }

            await context.SaveChangesAsync();
            return admissionCounter - 1;
        }

        // =====================================================
        // 5. SUBJECTS
        // =====================================================
        private static async Task SeedSubjectsAsync(AppDbContext context, ILogger logger)
        {
            var subjects = new (string Name, string Code)[]
            {
                ("Mathematics",       "MTH"),
                ("English Language",  "ENG"),
                ("Basic Science",     "BSC"),
                ("Social Studies",    "SOS"),
                ("Civic Education",   "CIV"),
                ("Computer Studies",  "CMP"),
                ("Agricultural Science", "AGR"),
                ("Business Studies",  "BUS"),
                ("Physics",           "PHY"),
                ("Chemistry",         "CHM"),
                ("Biology",           "BIO"),
                ("Further Mathematics","FMT"),
                ("Economics",         "ECO"),
                ("Government",        "GOV"),
                ("Literature",        "LIT"),
            };

            foreach (var s in subjects)
            {
                if (await context.Subjects.AnyAsync(x => x.Code == s.Code)) continue;

                context.Subjects.Add(new Subject
                {
                    Id = Guid.NewGuid(),
                    Name = s.Name,
                    Code = s.Code,
                });
                logger.LogInformation("Subject created: {Name}", s.Name);
            }

            await context.SaveChangesAsync();
        }

        // =====================================================
        // 6. LINK PARENTS TO STUDENTS
        // =====================================================
        private static async Task SeedParentLinksAsync(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            ILogger logger)
        {
            var parent1 = await userManager.FindByEmailAsync("parent1@diba.com.ng");
            var parent2 = await userManager.FindByEmailAsync("parent2@diba.com.ng");

            if (parent1 == null || parent2 == null) return;

            // Parent1's children
            var p1Children = await context.Students
                .Where(p => p.FirstName == "Chidi" || p.FirstName == "Tunde")
                .ToListAsync();

            foreach (var child in p1Children)
            {
                bool exists = await context.ParentStudents.AnyAsync(ps =>
                    ps.ParentUserId == parent1.Id && ps.StudentProfileId == child.Id);

                if (!exists)
                {
                    context.ParentStudents.Add(new ParentStudent
                    {
                        ParentUserId = parent1.Id,
                        StudentProfileId = child.Id,
                        Relationship = "Father",
                        IsPrimaryContact = true,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            // Parent2's child
            var p2Child = await context.Students
                .FirstOrDefaultAsync(p => p.FirstName == "Fatima");

            if (p2Child != null)
            {
                bool exists = await context.ParentStudents.AnyAsync(ps =>
                    ps.ParentUserId == parent2.Id && ps.StudentProfileId == p2Child.Id);

                if (!exists)
                {
                    context.ParentStudents.Add(new ParentStudent
                    {
                        ParentUserId = parent2.Id,
                        StudentProfileId = p2Child.Id,
                        Relationship = "Mother",
                        IsPrimaryContact = true,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Parent links seeded.");
        }
    }
}