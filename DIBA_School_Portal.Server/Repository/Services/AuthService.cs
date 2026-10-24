using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;
using DIBA_School_Portal.Server.Database_Entities;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Enums;
using DIBA_School_Portal.Server.Repository.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DIBA_School_Portal.Server.Repository.Services
{
    public class AuthService : IAuthService
    {
        #region Fields
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IAuditLog _auditLog;
        private readonly ISupaBaseFileService _files;
        private readonly ILogger<AuthService> _logger;
        private readonly INotificationLog _notification;

        private const string ProfilePictureBucket = "DibaSchool";
        private const string ProfilePictureFolder = "profile-pictures";
        private const long MaxProfilePictureBytes = 200 * 1024;
        private static readonly string[] AllowedImageMime =
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private static readonly string[] AccountCreators =
            { "SuperAdmin", "Principal", "Registrar" };

        private static readonly string[] AssignableRoles =
        {
            "Principal", "Headmaster", "Headmistress", "VicePrincipal",
            "SchoolAdmin", "Accountant", "Cashier",
            "Registrar",
            "Teacher", "FormTeacher",
            "Student", "Parent"
        };

        private static readonly string[] StaffRoles =
        {
            "Teacher", "FormTeacher",
            "Principal", "VicePrincipal", "Headmaster", "Headmistress",
            "SchoolAdmin", "Accountant", "Cashier", "Registrar",
        };

        private static readonly string[] StudentPortalRoles = { "Student" };
        private static readonly string[] ParentPortalRoles = { "Parent" };
        private static readonly string[] StaffPortalRoles =
        {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin", "Accountant", "Cashier",
            "Registrar",
            "Teacher", "FormTeacher"
        };
        #endregion

        #region Constructor
        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IConfiguration config,
            IAuditLog auditLog,
            ISupaBaseFileService files,
            INotificationLog notification,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _config = config;
            _auditLog = auditLog;
            _files = files;
            _notification = notification;
            _logger = logger;
        }
        #endregion

        // ============================================================
        // LOGIN — unchanged
        // ============================================================
        #region Login - Student
        public async Task<ServiceResponse<LoginResponseDto>> StudentLoginAsync(
            StudentLoginRequestDto request, string ipAddress)
        {
            var response = new ServiceResponse<LoginResponseDto>();

            if (string.IsNullOrWhiteSpace(request.AdmissionNumber) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                response.Success = false;
                response.Message = "Admission number and password are required.";
                response.StatusCode = 400;
                return response;
            }

            var admission = request.AdmissionNumber.Trim();

            var profile = await _context.Students
                .Include(p => p.User)
                .Include(p => p.SchoolClass)
                .FirstOrDefaultAsync(p => p.AdmissionNumber == admission);

            if (profile?.User == null)
            {
                response.Success = false;
                response.Message = "Invalid admission number or password.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    admission, "Student Login FAILED",
                    "Admission number not found.", ipAddress);
                return response;
            }

            var user = profile.User;

            if (!user.IsActive || user.IsDeleted)
            {
                response.Success = false;
                response.Message = "Account is inactive. Contact the school.";
                response.StatusCode = 403;
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Any(r => StudentPortalRoles.Contains(r)))
            {
                response.Success = false;
                response.Message = "This is not a student account. Please use the staff login.";
                response.StatusCode = 403;
                await _auditLog.LogActivityAsync(
                    user.Id, "Student Login DENIED",
                    "Non-student tried student portal.", ipAddress);
                return response;
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                response.Success = false;
                response.Message = "Invalid admission number or password.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    user.Id, "Student Login FAILED",
                    "Wrong password.", ipAddress);
                return response;
            }

            var wasFirstLogin = user.LastLoginAt == null;
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var token = GenerateJwtToken(user, roles, "Student");

            response.Success = true;
            response.StatusCode = 200;
            response.Data = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    AdmissionNumber = profile.AdmissionNumber,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Role = "Student",
                    ClassId = profile.SchoolClassId,
                    ClassName = profile.SchoolClass?.Name,
                    MustChangePassword = wasFirstLogin,
                }
            };

            await _auditLog.LogActivityAsync(
                user.Id, "Student Login Success",
                $"{profile.AdmissionNumber} logged in.", ipAddress);

            return response;
        }
        #endregion

        #region Login - Staff
        public async Task<ServiceResponse<LoginResponseDto>> StaffLoginAsync(
            StaffLoginRequestDto request, string ipAddress)
        {
            var response = new ServiceResponse<LoginResponseDto>();

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                response.Success = false;
                response.Message = "Email and password are required.";
                response.StatusCode = 400;
                return response;
            }

            var email = request.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                response.Success = false;
                response.Message = "Invalid email or password.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    email, "Staff Login FAILED",
                    "Email not found.", ipAddress);
                return response;
            }

            if (!user.IsActive || user.IsDeleted)
            {
                response.Success = false;
                response.Message = "Account is inactive. Contact the school administrator.";
                response.StatusCode = 403;
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.All(r => StudentPortalRoles.Contains(r) || ParentPortalRoles.Contains(r)))
            {
                response.Success = false;
                response.Message = "This is not a staff account. Please use the correct portal.";
                response.StatusCode = 403;
                await _auditLog.LogActivityAsync(
                    user.Id, "Staff Login DENIED",
                    "Non-staff tried staff portal.", ipAddress);
                return response;
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                response.Success = false;
                response.Message = "Invalid email or password.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    user.Id, "Staff Login FAILED",
                    "Wrong password.", ipAddress);
                return response;
            }

            var wasFirstLogin = user.LastLoginAt == null;
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var primaryRole = user.PrimaryRole ?? roles.FirstOrDefault() ?? "Staff";
            var token = GenerateJwtToken(user, roles, primaryRole);

            string? className = null;
            if (user.ClassId.HasValue)
            {
                className = await _context.SchoolClasses
                    .Where(c => c.Id == user.ClassId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            response.Success = true;
            response.StatusCode = 200;
            response.Data = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Role = primaryRole,
                    ClassId = user.ClassId,
                    ClassName = className,
                    MustChangePassword = wasFirstLogin,
                }
            };

            await _auditLog.LogActivityAsync(
                user.Id, "Staff Login Success",
                $"{user.FirstName} {user.LastName} logged in as {primaryRole}.",
                ipAddress);

            return response;
        }
        #endregion

        #region Login - Parent
        public async Task<ServiceResponse<LoginResponseDto>> ParentLoginAsync(
            ParentLoginRequestDto request, string ipAddress)
        {
            var response = new ServiceResponse<LoginResponseDto>();

            if (string.IsNullOrWhiteSpace(request.Identifier) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                response.Success = false;
                response.Message = "Phone number/email and password are required.";
                response.StatusCode = 400;
                return response;
            }

            var identifier = request.Identifier.Trim();

            ApplicationUser? user = null;

            if (identifier.Contains("@"))
                user = await _userManager.FindByEmailAsync(identifier);
            else
                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == identifier);

            if (user == null)
            {
                response.Success = false;
                response.Message = "Invalid credentials.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    identifier, "Parent Login FAILED",
                    "Identifier not found.", ipAddress);
                return response;
            }

            if (!user.IsActive || user.IsDeleted)
            {
                response.Success = false;
                response.Message = "Account is inactive. Contact the school.";
                response.StatusCode = 403;
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Any(r => ParentPortalRoles.Contains(r)))
            {
                response.Success = false;
                response.Message = "This is not a parent account. Please use the correct portal.";
                response.StatusCode = 403;
                await _auditLog.LogActivityAsync(
                    user.Id, "Parent Login DENIED",
                    "Non-parent tried parent portal.", ipAddress);
                return response;
            }

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
            {
                response.Success = false;
                response.Message = "Invalid credentials.";
                response.StatusCode = 401;
                await _auditLog.LogActivityAsync(
                    user.Id, "Parent Login FAILED",
                    "Wrong password.", ipAddress);
                return response;
            }

            var wasFirstLogin = user.LastLoginAt == null;
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var children = await _context.ParentStudents
                .Where(ps => ps.ParentUserId == user.Id)
                .Include(ps => ps.StudentProfile)
                    .ThenInclude(sp => sp.SchoolClass)
                .Select(ps => new ParentChildDto
                {
                    StudentProfileId = ps.StudentProfileId,
                    AdmissionNumber = ps.StudentProfile.AdmissionNumber,
                    FullName = ps.StudentProfile.FirstName + " " + ps.StudentProfile.LastName,
                    ClassName = ps.StudentProfile.SchoolClass.Name,
                    Relationship = ps.Relationship,
                    IsPrimaryContact = ps.IsPrimaryContact,
                })
                .ToListAsync();

            var token = GenerateJwtToken(user, roles, "Parent");

            response.Success = true;
            response.StatusCode = 200;
            response.Data = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Role = "Parent",
                    MustChangePassword = wasFirstLogin,
                    Children = children,
                }
            };

            await _auditLog.LogActivityAsync(
                user.Id, "Parent Login Success",
                $"{user.FirstName} {user.LastName} logged in with {children.Count} child(ren).",
                ipAddress);

            return response;
        }
        #endregion


        // ============================================================
        // CREATE ACCOUNT — with Teachers row for every staff role
        // ============================================================
        #region Create Account
        public async Task<ServiceResponse<CreateAccountResponseDto>> CreateAccountAsync(
            CreateAccountDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateAccountResponseDto>();

            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null)
            {
                response.Success = false;
                response.Message = "Unauthorized.";
                response.StatusCode = 401;
                return response;
            }

            var callerRoles = await _userManager.GetRolesAsync(caller);
            if (!callerRoles.Any(r => AccountCreators.Contains(r)))
            {
                response.Success = false;
                response.Message = "You do not have permission to create accounts.";
                response.StatusCode = 403;
                await _auditLog.LogActivityAsync(
                    currentUserId, "Account Create DENIED",
                    $"Attempted to create {request.Role}.", ipAddress);
                return response;
            }

            if (!AssignableRoles.Contains(request.Role))
            {
                response.Success = false;
                response.Message = $"Role '{request.Role}' cannot be assigned.";
                response.StatusCode = 400;
                return response;
            }

            if (callerRoles.Contains("Registrar") &&
                (request.Role == "Principal" || request.Role == "Registrar"))
            {
                response.Success = false;
                response.Message = "Registrar cannot create this role.";
                response.StatusCode = 403;
                return response;
            }

            var isStudent = request.Role == "Student";
            var isParent = request.Role == "Parent";
            var email = request.Email?.Trim();
            var phone = request.PhoneNumber?.Trim();

            if (!isStudent && !isParent && string.IsNullOrWhiteSpace(email))
            {
                response.Success = false;
                response.Message = "Email is required for staff accounts.";
                response.StatusCode = 400;
                return response;
            }

            if (isStudent && request.SchoolClassId == null)
            {
                response.Success = false;
                response.Message = "A class must be selected for a student.";
                response.StatusCode = 400;
                return response;
            }

            if (isParent && string.IsNullOrWhiteSpace(phone))
            {
                response.Success = false;
                response.Message = "Phone number is required for parent accounts.";
                response.StatusCode = 400;
                return response;
            }

            ClassStream? parsedStream = null;

            if (isStudent)
            {
                var targetClass = await _context.SchoolClasses
                    .FirstOrDefaultAsync(c => c.Id == request.SchoolClassId!.Value);

                if (targetClass == null)
                {
                    response.Success = false;
                    response.Message = "The selected class does not exist.";
                    response.StatusCode = 400;
                    return response;
                }

                bool isSenior = targetClass.AcademicSection == AcademicSection.SeniorSecondary;

                if (isSenior)
                {
                    if (string.IsNullOrWhiteSpace(request.Stream))
                    {
                        response.Success = false;
                        response.Message = "A stream is required for Senior Secondary students.";
                        response.StatusCode = 400;
                        return response;
                    }

                    if (!Enum.TryParse<ClassStream>(
                            request.Stream.Trim(), ignoreCase: true, out var s))
                    {
                        response.Success = false;
                        response.Message =
                            $"Invalid stream '{request.Stream}'. Valid values: Science, Arts, Commercial.";
                        response.StatusCode = 400;
                        return response;
                    }

                    parsedStream = s;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(request.Stream))
                    {
                        response.Success = false;
                        response.Message = "Streams only apply to Senior Secondary students.";
                        response.StatusCode = 400;
                        return response;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                await _userManager.FindByEmailAsync(email) != null)
            {
                response.Success = false;
                response.Message = "A user with this email already exists.";
                response.StatusCode = 409;
                return response;
            }

            if (isParent && !string.IsNullOrWhiteSpace(phone) &&
                await _userManager.Users.AnyAsync(u => u.PhoneNumber == phone))
            {
                response.Success = false;
                response.Message = "A user with this phone number already exists.";
                response.StatusCode = 409;
                return response;
            }

            string? admissionNumber = null;
            string username;

            if (isStudent)
            {
                admissionNumber = await GenerateAdmissionNumberAsync();
                username = admissionNumber;
            }
            else if (isParent)
            {
                username = phone!;
            }
            else
            {
                username = email!;
            }

            var tempPassword = GenerateTemporaryPassword();

            var newUser = new ApplicationUser
            {
                UserName = username,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(email),
                PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PrimaryRole = request.Role,
                ClassId = isStudent ? request.SchoolClassId : null,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            var createResult = await _userManager.CreateAsync(newUser, tempPassword);
            if (!createResult.Succeeded)
            {
                response.Success = false;
                response.Message = "Failed to create account: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description));
                response.StatusCode = 400;
                return response;
            }

            await _userManager.AddToRoleAsync(newUser, request.Role);

            Guid? studentProfileId = null;

            if (isStudent)
            {
                var studentProfile = new Student
                {
                    UserId = newUser.Id,
                    AdmissionNumber = admissionNumber!,
                    FirstName = newUser.FirstName!,
                    LastName = newUser.LastName!,
                    SchoolClassId = request.SchoolClassId!.Value,
                    Stream = parsedStream,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.Students.Add(studentProfile);
                studentProfileId = studentProfile.Id;

                // ==========================================================
                // CREATE ENROLLMENT for the current term
                // Every registered student gets one — even with no fees.
                // ==========================================================
                var currentTerm = await _context.SchoolTerms
                    .Include(t => t.AcademicSession)
                    .Where(t => t.IsCurrentTerm && t.AcademicSession.IsCurrentSession)
                    .Select(t => new { t.Id, t.AcademicSessionId })
                    .FirstOrDefaultAsync();

                if (currentTerm != null)
                {
                    var billedAmount = await _context.ClassFeePrices
                        .Include(cfp => cfp.Fee)
                        .Where(cfp => cfp.SchoolClassId == request.SchoolClassId!.Value &&
                                      cfp.Fee.TermId == currentTerm.Id)
                        .SumAsync(cfp => (decimal?)cfp.Amount) ?? 0m;

                    _context.StudentEnrollments.Add(new StudentEnrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = studentProfile.Id,
                        AcademicSessionId = currentTerm.AcademicSessionId,
                        TermId = currentTerm.Id,
                        SchoolClassId = request.SchoolClassId!.Value,
                        TotalAmountPaid = 0m,
                        OutstandingBalance = billedAmount,
                        IsFullyPaid = billedAmount == 0,
                        Status = billedAmount == 0
                            ? StudentEnrollmentStatus.Active
                            : StudentEnrollmentStatus.Pending,
                        IsActive = billedAmount == 0,
                        EnrolledAt = DateTime.UtcNow,
                    });
                }
                // ==========================================================
            }
            // 👈 FIX — every staff role gets a Teachers (staff profile) row
            else if (StaffRoles.Contains(request.Role))
            {
                var staffId = await GenerateStaffIdAsync();
                _context.Teachers.Add(new Teacher
                {
                    UserId = newUser.Id,
                    StaffId = staffId,
                    FirstName = newUser.FirstName!,
                    LastName = newUser.LastName!,
                    Qualification = "",
                    DateEmployed = DateTime.UtcNow,
                });
            }

            // ==========================================================
            // AUTO-ENROL — every subject offered by the student's class
            // ==========================================================
            if (isStudent && studentProfileId.HasValue)
            {
                var currentSession = await _context.AcademicSessions
                    .OrderByDescending(a => a.StartDate)
                    .FirstOrDefaultAsync();

                if (currentSession == null)
                {
                    response.Success = false;
                    response.Message = "No academic session exists. Create one before adding students.";
                    response.StatusCode = 400;
                    return response;
                }

                var subjectIds = await _context.ClassSubjects.Where(cs => cs.SchoolClassId == request.SchoolClassId!.Value && (cs.Stream == null || cs.Stream == parsedStream))
                .Select(cs => cs.SubjectId).Distinct().ToListAsync();

                foreach (var sid in subjectIds)
                {
                    _context.StudentSubjects.Add(new StudentSubject
                    {
                        Id = Guid.NewGuid(),
                        StudentId = studentProfileId.Value,
                        SubjectId = sid,
                        AcademicSessionId = currentSession.Id
                        
                    });
                }
            }
            // ==========================================================

            await _context.SaveChangesAsync();
            string? pictureWarning = null;

            if (request.ProfilePicture != null && request.ProfilePicture.Length > 0)
            {
                try
                {
                    var url = await UploadProfilePictureAsync(
                        newUser.Id, request.Role, request.ProfilePicture);

                    if (!string.IsNullOrEmpty(url))
                    {
                        newUser.ProfilePictureUrl = url;
                        await _userManager.UpdateAsync(newUser);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    pictureWarning = ex.Message;
                    _logger.LogWarning(ex,
                        "Profile picture rejected for new user {UserId}", newUser.Id);
                }
                catch (Exception ex)
                {
                    pictureWarning = "Profile picture could not be uploaded.";
                    _logger.LogError(ex,
                        "Failed to upload profile picture for user {UserId}", newUser.Id);
                }
            }

            await _auditLog.LogActivityAsync(
                currentUserId, "Account Created",
                $"Created {request.Role} '{newUser.FirstName} {newUser.LastName}' (username: {username})." +
                (parsedStream.HasValue ? $" Stream: {parsedStream}." : "") +
                (pictureWarning != null ? $" Warning: {pictureWarning}" : ""),
                ipAddress);

            response.Success = true;
            response.Message = pictureWarning == null
                ? "Account created successfully."
                : $"Account created, but profile picture was not saved: {pictureWarning}";
            response.StatusCode = 201;
            response.Data = new CreateAccountResponseDto
            {
                UserId = newUser.Id,
                StudentProfileId = studentProfileId?.ToString(),
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber ?? "",
                AdmissionNumber = admissionNumber,
                TemporaryPassword = tempPassword,
                Role = request.Role,
                ProfilePictureUrl = newUser.ProfilePictureUrl,
            };
            return response;
        }
        #endregion




        // ============================================================
        // UPDATE PROFILE — unchanged (already syncs Teacher + Student)
        // ============================================================
        #region Update Profile
        public async Task<ServiceResponse<UserProfileDto>> UpdateProfileAsync(
            string userId, UpdateProfileDto request, string ipAddress)
        {
            var response = new ServiceResponse<UserProfileDto>();

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                response.Success = false;
                response.Message = "First and last name are required.";
                response.StatusCode = 400;
                return response;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                response.StatusCode = 404;
                return response;
            }

            var oldName = $"{user.FirstName} {user.LastName}";
            var oldPhone = user.PhoneNumber;

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = string.Join(", ", result.Errors.Select(e => e.Description));
                response.StatusCode = 400;
                return response;
            }

            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher != null)
            {
                teacher.FirstName = user.FirstName!;
                teacher.LastName = user.LastName!;
                await _context.SaveChangesAsync();
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student != null)
            {
                student.FirstName = user.FirstName!;
                student.LastName = user.LastName!;
                await _context.SaveChangesAsync();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = user.PrimaryRole ?? roles.FirstOrDefault() ?? "Staff";

            await _auditLog.LogActivityAsync(
                userId,
                "Profile Updated",
                $"Name: '{oldName}' → '{user.FirstName} {user.LastName}'. " +
                $"Phone: '{oldPhone ?? "—"}' → '{user.PhoneNumber ?? "—"}'. ",
                ipAddress);

            response.Success = true;
            response.StatusCode = 200;
            response.Message = "Profile updated.";
            response.Data = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = primaryRole,
                ClassId = user.ClassId,
            };
            return response;
        }
        #endregion

        #region Update Profile Picture
        public async Task<ServiceResponse<string>> UpdateProfilePictureAsync(
            string userId, IFormFile picture, string ipAddress)
        {
            var response = new ServiceResponse<string>();

            if (picture == null || picture.Length == 0)
            {
                response.Success = false;
                response.Message = "No picture uploaded.";
                response.StatusCode = 400;
                return response;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                response.StatusCode = 404;
                return response;
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = user.PrimaryRole ?? roles.FirstOrDefault() ?? "Staff";

                var url = await UploadProfilePictureAsync(user.Id, primaryRole, picture);

                if (string.IsNullOrEmpty(url))
                {
                    response.Success = false;
                    response.Message = "Upload failed.";
                    response.StatusCode = 500;
                    return response;
                }

                user.ProfilePictureUrl = url;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _auditLog.LogActivityAsync(
                    userId, "Profile Picture Updated", $"New picture uploaded.", ipAddress);

                response.Success = true;
                response.StatusCode = 200;
                response.Message = "Profile picture updated.";
                response.Data = url;
                return response;
            }
            catch (InvalidOperationException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.StatusCode = 400;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload profile picture for user {UserId}", userId);
                response.Success = false;
                response.Message = "Could not upload profile picture.";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ============================================================
        // CHANGE ROLE — creates Teachers row for every staff role
        // ============================================================
        #region Change Role
        public async Task<ServiceResponse<UserProfileDto>> ChangeRoleAsync(
            string targetUserId,
            ChangeRoleDto request,
            string currentUserId,
            string ipAddress)
        {
            var response = new ServiceResponse<UserProfileDto>();

            try
            {
                var caller = await _userManager.FindByIdAsync(currentUserId);
                if (caller == null)
                {
                    response.Success = false;
                    response.Message = "Unauthorized.";
                    response.StatusCode = 401;
                    return response;
                }

                var callerRoles = await _userManager.GetRolesAsync(caller);
                var isSuperAdmin = callerRoles.Contains("SuperAdmin");
                var isPrincipal = callerRoles.Contains("Principal");

                if (!isSuperAdmin && !isPrincipal)
                {
                    response.Success = false;
                    response.Message = "Only SuperAdmin or Principal can change roles.";
                    response.StatusCode = 403;
                    return response;
                }

                if (string.IsNullOrWhiteSpace(request.NewRole))
                {
                    response.Success = false;
                    response.Message = "New role is required.";
                    response.StatusCode = 400;
                    return response;
                }

                var newRole = request.NewRole.Trim();

                var roleWhitelist = new[]
                {
                    "Principal", "Headmaster", "Headmistress", "VicePrincipal",
                    "SchoolAdmin", "Accountant", "Cashier",
                    "Registrar",
                    "Teacher", "FormTeacher",
                };

                if (!roleWhitelist.Contains(newRole))
                {
                    response.Success = false;
                    response.Message = $"Role '{newRole}' cannot be assigned via this endpoint.";
                    response.StatusCode = 400;
                    return response;
                }

                var target = await _userManager.FindByIdAsync(targetUserId);
                if (target == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    response.StatusCode = 404;
                    return response;
                }

                var currentRoles = await _userManager.GetRolesAsync(target);

                if (currentRoles.Contains("SuperAdmin"))
                {
                    response.Success = false;
                    response.Message = "SuperAdmin accounts cannot be modified via this endpoint.";
                    response.StatusCode = 403;
                    return response;
                }

                if (currentRoles.Contains("Student") || currentRoles.Contains("Parent"))
                {
                    response.Success = false;
                    response.Message = "Cannot change the role of a Student or Parent via this endpoint.";
                    response.StatusCode = 400;
                    return response;
                }

                if (!isSuperAdmin)
                {
                    var leadershipRoles = new[] { "Principal", "Headmaster", "Headmistress" };

                    if (currentRoles.Any(r => leadershipRoles.Contains(r)))
                    {
                        response.Success = false;
                        response.Message = "Only SuperAdmin can modify leadership accounts.";
                        response.StatusCode = 403;
                        return response;
                    }

                    if (leadershipRoles.Contains(newRole))
                    {
                        response.Success = false;
                        response.Message = "Only SuperAdmin can assign leadership roles.";
                        response.StatusCode = 403;
                        return response;
                    }
                }

                if (target.Id == caller.Id)
                {
                    response.Success = false;
                    response.Message = "You cannot change your own role.";
                    response.StatusCode = 403;
                    return response;
                }

                var oldPrimaryRole = target.PrimaryRole
                                     ?? currentRoles.FirstOrDefault()
                                     ?? "";

                if (currentRoles.Count == 1 && currentRoles[0] == newRole)
                {
                    response.Success = false;
                    response.Message = $"User is already a {newRole}.";
                    response.StatusCode = 400;
                    return response;
                }

                var removeResult = await _userManager.RemoveFromRolesAsync(target, currentRoles);
                if (!removeResult.Succeeded)
                {
                    response.Success = false;
                    response.Message = "Failed to remove old roles: " +
                        string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    response.StatusCode = 400;
                    return response;
                }

                var addResult = await _userManager.AddToRoleAsync(target, newRole);
                if (!addResult.Succeeded)
                {
                    await _userManager.AddToRolesAsync(target, currentRoles);
                    response.Success = false;
                    response.Message = "Failed to add new role: " +
                        string.Join(", ", addResult.Errors.Select(e => e.Description));
                    response.StatusCode = 400;
                    return response;
                }

                target.PrimaryRole = newRole;
                target.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(target);

                // 👈 FIX — ensure a Teachers row for any staff role
                if (StaffRoles.Contains(newRole))
                {
                    var existingTeacher = await _context.Teachers
                        .FirstOrDefaultAsync(t => t.UserId == target.Id);

                    if (existingTeacher == null)
                    {
                        var staffId = await GenerateStaffIdAsync();
                        _context.Teachers.Add(new Teacher
                        {
                            UserId = target.Id,
                            StaffId = staffId,
                            FirstName = target.FirstName ?? "",
                            LastName = target.LastName ?? "",
                            Qualification = "",
                            DateEmployed = DateTime.UtcNow,
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                await _userManager.UpdateSecurityStampAsync(target);

                await _auditLog.LogActivityAsync(
                    currentUserId,
                    "User Role Changed",
                    $"Changed {target.FirstName} {target.LastName} ({target.UserName}) " +
                    $"from '{oldPrimaryRole}' to '{newRole}'.",
                    ipAddress);

                try
                {
                    await _notification.SendNotificationAsync(
                        target.Id,
                        "Your role has changed",
                        $"Your role was updated from '{oldPrimaryRole}' to '{newRole}'. " +
                        "Please log out and log back in for the changes to take effect.",
                        category: "Account");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to notify user {UserId} about role change.", target.Id);
                }

                response.Success = true;
                response.StatusCode = 200;
                response.Message =
                    $"Role changed from '{oldPrimaryRole}' to '{newRole}'. " +
                    "The user must log out and log back in for the new permissions to apply.";
                response.Data = new UserProfileDto
                {
                    Id = target.Id,
                    Email = target.Email,
                    FirstName = target.FirstName ?? "",
                    LastName = target.LastName ?? "",
                    ProfilePictureUrl = target.ProfilePictureUrl,
                    Role = newRole,
                    ClassId = target.ClassId,
                };
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing role for user {UserId}", targetUserId);
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.StatusCode = 500;
                return response;
            }
        }
        #endregion

        // ============================================================
        // LINK PARENT — unchanged
        // ============================================================
        #region Link Parent To Student
        public async Task<ServiceResponse<CreateAccountResponseDto>> LinkParentToStudentAsync(
            LinkParentToStudentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateAccountResponseDto>();

            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null)
            {
                response.Success = false;
                response.Message = "Unauthorized.";
                response.StatusCode = 401;
                return response;
            }

            var callerRoles = await _userManager.GetRolesAsync(caller);
            if (!callerRoles.Any(r => AccountCreators.Contains(r)))
            {
                response.Success = false;
                response.Message = "Not authorized to link parents.";
                response.StatusCode = 403;
                return response;
            }

            var student = await _context.Students
                .Include(s => s.SchoolClass)
                .FirstOrDefaultAsync(p => p.Id == request.StudentProfileId);

            if (student == null)
            {
                response.Success = false;
                response.Message = "Student not found.";
                response.StatusCode = 404;
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.ParentPhone))
            {
                response.Success = false;
                response.Message = "Parent phone number is required.";
                response.StatusCode = 400;
                return response;
            }

            var parentPhone = request.ParentPhone.Trim();
            var parentEmail = string.IsNullOrWhiteSpace(request.ParentEmail)
                ? null
                : request.ParentEmail.Trim();

            var parent = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == parentPhone);

            bool parentWasCreated = false;
            string? tempPassword = null;

            if (parent == null)
            {
                if (!string.IsNullOrWhiteSpace(parentEmail) &&
                    await _userManager.FindByEmailAsync(parentEmail) != null)
                {
                    response.Success = false;
                    response.Message = "A user with this email already exists.";
                    response.StatusCode = 409;
                    return response;
                }

                tempPassword = GenerateTemporaryPassword();

                parent = new ApplicationUser
                {
                    UserName = parentPhone,
                    Email = parentEmail,
                    EmailConfirmed = !string.IsNullOrWhiteSpace(parentEmail),
                    PhoneNumber = parentPhone,
                    FirstName = request.ParentFirstName.Trim(),
                    LastName = request.ParentLastName.Trim(),
                    PrimaryRole = "Parent",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                var createResult = await _userManager.CreateAsync(parent, tempPassword);
                if (!createResult.Succeeded)
                {
                    response.Success = false;
                    response.Message = "Failed to create parent: " +
                        string.Join(", ", createResult.Errors.Select(e => e.Description));
                    response.StatusCode = 400;
                    return response;
                }

                await _userManager.AddToRoleAsync(parent, "Parent");
                parentWasCreated = true;

                if (request.ParentProfilePicture != null &&
                    request.ParentProfilePicture.Length > 0)
                {
                    try
                    {
                        var url = await UploadProfilePictureAsync(
                            parent.Id, "Parent", request.ParentProfilePicture);

                        if (!string.IsNullOrEmpty(url))
                        {
                            parent.ProfilePictureUrl = url;
                            await _userManager.UpdateAsync(parent);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to upload parent picture for {ParentId}", parent.Id);
                    }
                }
            }
            else if (!await _userManager.IsInRoleAsync(parent, "Parent"))
            {
                await _userManager.AddToRoleAsync(parent, "Parent");
            }

            var alreadyLinked = await _context.ParentStudents
                .AnyAsync(ps => ps.ParentUserId == parent.Id
                             && ps.StudentProfileId == request.StudentProfileId);

            if (alreadyLinked)
            {
                response.Success = false;
                response.Message = $"'{parent.FirstName} {parent.LastName}' is already linked to {student.FirstName} {student.LastName}.";
                response.StatusCode = 409;
                return response;
            }

            _context.ParentStudents.Add(new ParentStudent
            {
                ParentUserId = parent.Id,
                StudentProfileId = request.StudentProfileId,
                Relationship = string.IsNullOrWhiteSpace(request.Relationship)
                    ? "Guardian"
                    : request.Relationship.Trim(),
                IsPrimaryContact = request.IsPrimaryContact,
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId,
                parentWasCreated ? "Parent Created & Linked" : "Parent Linked",
                $"{(parentWasCreated ? "Created and linked" : "Linked")} parent '{parent.FirstName} {parent.LastName}' " +
                $"({parentPhone}) to {student.FirstName} {student.LastName} ({student.AdmissionNumber}).",
                ipAddress);

            response.Success = true;
            response.Message = parentWasCreated
                ? "Parent created and linked successfully."
                : "Parent linked successfully.";
            response.StatusCode = 201;
            response.Data = new CreateAccountResponseDto
            {
                UserId = parent.Id,
                Email = parent.Email,
                PhoneNumber = parent.PhoneNumber ?? "",
                TemporaryPassword = tempPassword ?? string.Empty,
                Role = "Parent",
                ProfilePictureUrl = parent.ProfilePictureUrl,
            };
            return response;
        }
        #endregion

        #region Change Password (Self)
        public async Task<ServiceResponse<bool>> ChangePasswordAsync(
            string userId, ChangePasswordDto request, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                response.Success = false;
                response.Message = "New passwords do not match.";
                response.StatusCode = 400;
                return response;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
                response.StatusCode = 404;
                return response;
            }

            var result = await _userManager.ChangePasswordAsync(
                user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = string.Join(", ", result.Errors.Select(e => e.Description));
                response.StatusCode = 400;
                return response;
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _auditLog.LogActivityAsync(
                userId, "Password Changed",
                $"{user.FirstName} {user.LastName} changed password.",
                ipAddress);

            response.Success = true;
            response.Message = "Password changed successfully.";
            response.StatusCode = 200;
            response.Data = true;
            return response;
        }
        #endregion

        #region Admin Reset Password
        public async Task<ServiceResponse<bool>> AdminResetPasswordAsync(
     string currentUserId, AdminResetPasswordDto request, string ipAddress)
        {
            var response = new ServiceResponse<bool>();

            // ============================================================
            // 1. Authorize caller
            // ============================================================
            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null)
                return Fail<bool>("Unauthorized.", 401);

            var callerRoles = await _userManager.GetRolesAsync(caller);
            var passwordResetters = new[]
            {
        "SuperAdmin", "Principal", "SchoolAdmin", "Registrar"
    };

            if (!callerRoles.Any(r => passwordResetters.Contains(r)))
                return Fail<bool>("Not authorized to reset passwords.", 403);

            // ============================================================
            // 2. Resolve target user — with honest diagnostics
            // ============================================================
            ApplicationUser? target = null;
            string resolveReason = "n/a (neither identifier branch fired)";

            if (!string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                var trimmedId = request.TargetUserId.Trim();
                target = await _userManager.FindByIdAsync(trimmedId);
                resolveReason = target != null
                    ? $"resolved from targetUserId {trimmedId}"
                    : $"targetUserId '{trimmedId}' does not match any user";
            }
            else if (request.StudentProfileId.HasValue &&
                     request.StudentProfileId.Value != Guid.Empty)
            {
                var student = await _context.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.StudentProfileId.Value);

                if (student == null)
                {
                    resolveReason =
                        $"studentProfileId '{request.StudentProfileId}' does not exist in Students table";
                }
                else if (string.IsNullOrEmpty(student.UserId))
                {
                    resolveReason =
                        $"student {student.AdmissionNumber} has no linked UserId (data integrity issue)";
                }
                else
                {
                    target = await _userManager.FindByIdAsync(student.UserId);
                    resolveReason = target != null
                        ? $"resolved via student {student.AdmissionNumber} → user {student.UserId}"
                        : $"student {student.AdmissionNumber} points to UserId {student.UserId} which does not exist";
                }
            }

            if (target == null)
            {
                // 👇 Diagnostic — proves what the server actually received.
                //    If this prints "studentProfileId=null" while the browser
                //    clearly sent a GUID, the DTO binder failed → check
                //    AdminResetPasswordDto.StudentProfileId is Guid? and the
                //    controller action is [FromBody], not [FromForm].
                var diagnostic =
                    $"targetUserId='{request.TargetUserId ?? "null"}', " +
                    $"studentProfileId={(request.StudentProfileId?.ToString() ?? "null")}, " +
                    $"studentProfileIdHasValue={request.StudentProfileId.HasValue}, " +
                    $"reason='{resolveReason}'";

                await _auditLog.LogActivityAsync(
                    currentUserId, "Password Reset FAILED",
                    $"Could not resolve target. {diagnostic}", ipAddress);

                return Fail<bool>($"User not found. Server received: {diagnostic}", 404);
            }

            // ============================================================
            // 3. Validate new password
            // ============================================================
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
                return Fail<bool>("Password must be at least 8 characters.", 400);

            // ============================================================
            // 4. Privilege-escalation guard
            // ============================================================
            var targetRoles = await _userManager.GetRolesAsync(target);
            var isSuperAdmin = callerRoles.Contains("SuperAdmin");
            var isPrincipal = callerRoles.Contains("Principal");

            if (!isSuperAdmin && !isPrincipal)
            {
                var protectedRoles = new[]
                {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin"
        };

                if (targetRoles.Any(r => protectedRoles.Contains(r)))
                {
                    await _auditLog.LogActivityAsync(
                        currentUserId, "Password Reset DENIED",
                        $"Attempted to reset password for protected user {target.Id} " +
                        $"({string.Join(",", targetRoles)}).", ipAddress);

                    return Fail<bool>(
                        "You don't have permission to reset this user's password. " +
                        "Only SuperAdmin or Principal can reset higher-role accounts.",
                        403);
                }
            }

            // ============================================================
            // 5. Reset
            // ============================================================
            var token = await _userManager.GeneratePasswordResetTokenAsync(target);
            var result = await _userManager.ResetPasswordAsync(
                target, token, request.NewPassword);

            if (!result.Succeeded)
                return Fail<bool>(
                    string.Join(", ", result.Errors.Select(e => e.Description)), 400);

            // Invalidate any existing sessions
            await _userManager.UpdateSecurityStampAsync(target);

            // ============================================================
            // 6. Audit
            // ============================================================
            var targetRoleLabel = targetRoles.FirstOrDefault() ?? "User";
            await _auditLog.LogActivityAsync(
                currentUserId, "Password Reset",
                $"Reset password for {target.FirstName} {target.LastName} " +
                $"({target.UserName ?? target.Email}) — role: {targetRoleLabel}.",
                ipAddress);

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset successfully.",
                StatusCode = 200,
                Data = true,
            };
        }

        // ============================================================
        // Helper
        // ============================================================
        private static ServiceResponse<T> Fail<T>(string message, int status) =>
            new()
            {
                Success = false,
                Message = message,
                StatusCode = status,
            };
        #endregion

        #region Helper - Profile Picture Upload (Supabase)
        private async Task<string?> UploadProfilePictureAsync(
            string userId, string role, IFormFile picture)
        {
            if (picture == null || picture.Length == 0)
                return null;

            if (picture.Length > MaxProfilePictureBytes)
                throw new InvalidOperationException(
                    $"Profile picture must not exceed {MaxProfilePictureBytes / 1024} KB.");

            if (!AllowedImageMime.Contains(picture.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException(
                    "Only JPG, PNG, and WEBP images are allowed.");

            var roleFolder = role switch
            {
                "Student" => "students",
                "Teacher" or "FormTeacher" => "teachers",
                "Parent" => "parents",
                _ => "staff",
            };

            var extension = picture.ContentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };

            var key = $"{ProfilePictureFolder}/{roleFolder}/{userId}/{Guid.NewGuid():N}{extension}";

            await using var stream = picture.OpenReadStream();

            var url = await _files.UploadFileAsync(
                ProfilePictureBucket, key, stream, picture.ContentType);

            return url;
        }
        #endregion

        #region Helpers - ID Generation
        private async Task<string> GenerateAdmissionNumberAsync()
        {
            var year = DateTime.UtcNow.Year;

            var lastThisYear = await _context.Students
                .Where(p => p.AdmissionNumber.StartsWith($"DIBA/{year}/"))
                .OrderByDescending(p => p.AdmissionNumber)
                .Select(p => p.AdmissionNumber)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastThisYear))
            {
                var tail = lastThisYear.Split('/').Last();
                if (int.TryParse(tail, out var n)) nextSeq = n + 1;
            }

            return $"DIBA/{year}/{nextSeq:D3}";
        }

        private async Task<string> GenerateStaffIdAsync()
        {
            var lastStaff = await _context.Teachers
                .OrderByDescending(t => t.StaffId)
                .Select(t => t.StaffId)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastStaff))
            {
                var tail = lastStaff.Split('/').Last();
                if (int.TryParse(tail, out var n)) nextSeq = n + 1;
            }

            return $"DIBA/STF/{nextSeq:D3}";
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
            const string lower = "abcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%&*";

            var rng = new Random();
            string Pick(string chars, int count) =>
                new string(Enumerable.Range(0, count).Select(_ => chars[rng.Next(chars.Length)]).ToArray());

            return Pick(upper, 2) + Pick(lower, 4) + Pick(digits, 3) +
                   Pick(symbols, 1) + Pick(upper + lower + digits, 2);
        }
        #endregion

        #region Helpers - JWT
        private string GenerateJwtToken(
            ApplicationUser user, IList<string> roles, string? primaryRoleOverride = null)
        {
            var jwtKey = _config["Jwt:Key"] ?? "THIS_IS_A_VERY_LONG_SECRET_KEY_THAT_IS_AT_LEAST_32_CHARACTERS_LONG_FOR_HMAC_SHA256";
            var issuer = _config["Jwt:Issuer"] ?? "DIBA_School_Portal";
            var audience = _config["Jwt:Audience"] ?? "DIBA_School_Portal_Clients";

            var primaryRole = primaryRoleOverride ?? user.PrimaryRole ?? roles.FirstOrDefault() ?? "";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(ClaimTypes.GivenName, user.FirstName ?? ""),
                new(ClaimTypes.Surname, user.LastName ?? ""),
                new("primaryRole", primaryRole),
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (user.ClassId.HasValue)
                claims.Add(new Claim("classId", user.ClassId.Value.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}