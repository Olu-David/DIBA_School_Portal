using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
using DIBA_School_Portal.Server.Repository.IServices;
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
        private readonly ILogger<AuthService> _logger;

        // Who can create accounts
        private static readonly string[] AccountCreators =
            { "SuperAdmin", "Principal", "Registrar" };

        // Assignable roles (SuperAdmin cannot be created via this endpoint)
        private static readonly string[] AssignableRoles =
        {
            "Principal", "Headmaster", "Headmistress", "VicePrincipal",
            "SchoolAdmin", "Accountant", "Registrar",
            "Teacher", "FormTeacher",
            "Student", "Parent"
        };

        // Portal access maps
        private static readonly string[] StudentPortalRoles = { "Student" };
        private static readonly string[] ParentPortalRoles = { "Parent" };
        private static readonly string[] StaffPortalRoles =
        {
            "SuperAdmin", "Principal", "Headmaster", "Headmistress",
            "VicePrincipal", "SchoolAdmin", "Accountant", "Registrar",
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
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _config = config;
            _auditLog = auditLog;
            _logger = logger;
        }
        #endregion

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

            // Block students and parents from staff portal
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

            // Load children
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

            // Registrar can't create Principal or Registrar
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

            // Validation per role
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

            // Uniqueness
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

            // Username + admission number
            string? admissionNumber = null;
            string username;

            if (isStudent)
            {
                admissionNumber = await GenerateAdmissionNumberAsync();
                username = admissionNumber;
            }
            else if (isParent)
            {
                username = phone!;  // parent logs in with phone
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

            // Role-specific records
            if (isStudent)
            {
                _context.Students.Add(new Student
                {
                    UserId = newUser.Id,
                    AdmissionNumber = admissionNumber!,
                    FirstName = newUser.FirstName!,
                    LastName = newUser.LastName!,
                    SchoolClassId = request.SchoolClassId!.Value,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else if (request.Role == "Teacher" || request.Role == "FormTeacher")
            {
                var staffId = await GenerateStaffIdAsync();
                _context.Teachers.Add(new Teacher
                {
                    UserId = newUser.Id,
                    StaffId = staffId,
                    FirstName = newUser.FirstName!,
                    LastName = newUser.LastName!,
                    DateEmployed = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "Account Created",
                $"Created {request.Role} '{newUser.FirstName} {newUser.LastName}' (username: {username}).",
                ipAddress);

            response.Success = true;
            response.Message = "Account created successfully.";
            response.StatusCode = 201;
            response.Data = new CreateAccountResponseDto
            {
                UserId = newUser.Id,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber ?? "",
                AdmissionNumber = admissionNumber,
                TemporaryPassword = tempPassword,
                Role = request.Role,
            };
            return response;
        }
        #endregion

        #region Create Student With Parent (Combined Admission)
        public async Task<ServiceResponse<CreateStudentWithParentResponseDto>> CreateStudentWithParentAsync(
            CreateStudentWithParentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateStudentWithParentResponseDto>();

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
                response.Message = "You do not have permission to admit students.";
                response.StatusCode = 403;
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

            // Start a transaction — student + parent must both succeed
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ---------- 1. Student ----------
                    var admissionNumber = await GenerateAdmissionNumberAsync();
                    var studentTempPassword = GenerateTemporaryPassword();

                    var studentUser = new ApplicationUser
                    {
                        UserName = admissionNumber,
                        Email = null,
                        EmailConfirmed = false,
                        FirstName = request.StudentFirstName.Trim(),
                        LastName = request.StudentLastName.Trim(),
                        PrimaryRole = "Student",
                        ClassId = request.SchoolClassId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    var studentResult = await _userManager.CreateAsync(studentUser, studentTempPassword);
                    if (!studentResult.Succeeded)
                    {
                        response.Success = false;
                        response.Message = "Failed to create student: " +
                            string.Join(", ", studentResult.Errors.Select(e => e.Description));
                        response.StatusCode = 400;
                        await transaction.RollbackAsync();
                        return response;
                    }

                    await _userManager.AddToRoleAsync(studentUser, "Student");

                    var studentProfile = new Student
                    {
                        UserId = studentUser.Id,
                        AdmissionNumber = admissionNumber,
                        FirstName = studentUser.FirstName!,
                        LastName = studentUser.LastName!,
                        SchoolClassId = request.SchoolClassId,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.Students.Add(studentProfile);
                    await _context.SaveChangesAsync();

                    // ---------- 2. Parent (reuse if exists) ----------
                    var parent = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == parentPhone);

                    bool parentWasCreated = false;
                    string? parentTempPassword = null;
                    string parentUserId;

                    if (parent != null)
                    {
                        parentUserId = parent.Id;

                        // Ensure parent role is assigned
                        if (!await _userManager.IsInRoleAsync(parent, "Parent"))
                            await _userManager.AddToRoleAsync(parent, "Parent");
                    }
                    else
                    {
                        parentTempPassword = GenerateTemporaryPassword();

                        parent = new ApplicationUser
                        {
                            UserName = parentPhone,
                            Email = string.IsNullOrWhiteSpace(request.ParentEmail) ? null : request.ParentEmail.Trim(),
                            EmailConfirmed = !string.IsNullOrWhiteSpace(request.ParentEmail),
                            PhoneNumber = parentPhone,
                            FirstName = request.ParentFirstName.Trim(),
                            LastName = request.ParentLastName.Trim(),
                            PrimaryRole = "Parent",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                        };

                        var parentResult = await _userManager.CreateAsync(parent, parentTempPassword);
                        if (!parentResult.Succeeded)
                        {
                            response.Success = false;
                            response.Message = "Failed to create parent: " +
                                string.Join(", ", parentResult.Errors.Select(e => e.Description));
                            response.StatusCode = 400;
                            await transaction.RollbackAsync();
                            return response;
                        }

                        await _userManager.AddToRoleAsync(parent, "Parent");
                        parentWasCreated = true;
                        parentUserId = parent.Id;
                    }

                    // ---------- 3. Link parent ↔ student ----------
                    var alreadyLinked = await _context.ParentStudents
                        .AnyAsync(ps => ps.ParentUserId == parentUserId
                                     && ps.StudentProfileId == studentProfile.Id);

                    if (!alreadyLinked)
                    {
                        _context.ParentStudents.Add(new ParentStudent
                        {
                            ParentUserId = parentUserId,
                            StudentProfileId = studentProfile.Id,
                            Relationship = request.ParentRelationship,
                            IsPrimaryContact = request.ParentIsPrimaryContact,
                            CreatedAt = DateTime.UtcNow,
                        });
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    await _auditLog.LogActivityAsync(
                        currentUserId, "Student Admitted",
                        $"Admitted {studentUser.FirstName} {studentUser.LastName} ({admissionNumber}) " +
                        $"and linked parent {parent.FirstName} {parent.LastName}.",
                        ipAddress);

                    response.Success = true;
                    response.Message = "Student and parent created successfully.";
                    response.StatusCode = 201;
                    response.Data = new CreateStudentWithParentResponseDto
                    {
                        StudentUserId = studentUser.Id,
                        AdmissionNumber = admissionNumber,
                        StudentTempPassword = studentTempPassword,
                        ParentUserId = parentUserId,
                        ParentLogin = parentPhone,
                        ParentTempPassword = parentTempPassword,
                        ParentWasCreated = parentWasCreated,
                    };
                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error admitting student with parent.");
                    response.Success = false;
                    response.Message = $"An error occurred: {ex.Message}";
                    response.StatusCode = 500;
                    return response;
                }
            });
        }
        #endregion

        #region Link Parent To Existing Student
        public async Task<ServiceResponse<CreateAccountResponseDto>> LinkParentToStudentAsync(
            LinkParentToStudentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateAccountResponseDto>();

            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null) { response.Success = false; response.StatusCode = 401; return response; }

            var callerRoles = await _userManager.GetRolesAsync(caller);
            if (!callerRoles.Any(r => AccountCreators.Contains(r)))
            {
                response.Success = false;
                response.Message = "Not authorized.";
                response.StatusCode = 403;
                return response;
            }

            var student = await _context.Students
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

            var parent = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == parentPhone);

            bool parentWasCreated = false;
            string? tempPassword = null;

            if (parent == null)
            {
                tempPassword = GenerateTemporaryPassword();
                parent = new ApplicationUser
                {
                    UserName = parentPhone,
                    Email = string.IsNullOrWhiteSpace(request.ParentEmail) ? null : request.ParentEmail.Trim(),
                    EmailConfirmed = !string.IsNullOrWhiteSpace(request.ParentEmail),
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
                response.Message = "This parent is already linked to this student.";
                response.StatusCode = 409;
                return response;
            }

            _context.ParentStudents.Add(new ParentStudent
            {
                ParentUserId = parent.Id,
                StudentProfileId = request.StudentProfileId,
                Relationship = request.Relationship,
                IsPrimaryContact = request.IsPrimaryContact,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            await _auditLog.LogActivityAsync(
                currentUserId, "Parent Linked",
                $"Linked parent '{parent.FirstName} {parent.LastName}' ({parentPhone}) " +
                $"to student {student.AdmissionNumber}.",
                ipAddress);

            response.Success = true;
            response.Message = parentWasCreated
                ? "Parent created and linked."
                : "Parent linked.";
            response.StatusCode = 201;
            response.Data = new CreateAccountResponseDto
            {
                UserId = parent.Id,
                Email = parent.Email,
                PhoneNumber = parent.PhoneNumber ?? "",
                TemporaryPassword = tempPassword ?? string.Empty,
                Role = "Parent",
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

            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null) { response.Success = false; response.StatusCode = 401; return response; }

            var callerRoles = await _userManager.GetRolesAsync(caller);
            if (!callerRoles.Any(r => AccountCreators.Contains(r)))
            {
                response.Success = false;
                response.Message = "Not authorized to reset passwords.";
                response.StatusCode = 403;
                return response;
            }

            var target = await _userManager.FindByIdAsync(request.TargetUserId);
            if (target == null)
            {
                response.Success = false;
                response.Message = "Target user not found.";
                response.StatusCode = 404;
                return response;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(target);
            var result = await _userManager.ResetPasswordAsync(target, token, request.NewPassword);

            if (!result.Succeeded)
            {
                response.Success = false;
                response.Message = string.Join(", ", result.Errors.Select(e => e.Description));
                response.StatusCode = 400;
                return response;
            }

            await _userManager.UpdateSecurityStampAsync(target);

            await _auditLog.LogActivityAsync(
                currentUserId, "Password Reset",
                $"Reset password for {target.FirstName} {target.LastName} ({target.UserName}).",
                ipAddress);

            response.Success = true;
            response.Message = "Password reset successfully.";
            response.StatusCode = 200;
            response.Data = true;
            return response;
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}