using DIBA_School_Portal.Server.Configurations;
using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Entities;
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

        // ---------- Profile picture config ----------
        private const string ProfilePictureBucket = "profile-pictures";
        private const long MaxProfilePictureBytes = 200 * 1024;   // 200 KB
        private static readonly string[] AllowedImageMime =
        {
            "image/jpeg", "image/png", "image/webp"
        };

        // Who can create accounts and link parents
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
            ISupaBaseFileService files,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _config = config;
            _auditLog = auditLog;
            _files = files;
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

        #region Create Account
        public async Task<ServiceResponse<CreateAccountResponseDto>> CreateAccountAsync(
            CreateAccountDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateAccountResponseDto>();

            // ---------- 1. Authorization ----------
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

            // ---------- 2. Role-based validation ----------
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

            // ---------- 3. Uniqueness ----------
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

            // ---------- 4. Username + admission number ----------
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

            // ---------- 5. Create user ----------
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

            // ---------- 6. Role-specific records ----------
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

            // ---------- 7. Profile picture upload ----------
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

            // ---------- 8. Audit ----------
            await _auditLog.LogActivityAsync(
                currentUserId, "Account Created",
                $"Created {request.Role} '{newUser.FirstName} {newUser.LastName}' (username: {username})." +
                (pictureWarning != null ? $" Warning: {pictureWarning}" : ""),
                ipAddress);

            // ---------- 9. Response ----------
            response.Success = true;
            response.Message = pictureWarning == null
                ? "Account created successfully."
                : $"Account created, but profile picture was not saved: {pictureWarning}";
            response.StatusCode = 201;
            response.Data = new CreateAccountResponseDto
            {
                UserId = newUser.Id,
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

        #region Link Parent To Student
        public async Task<ServiceResponse<CreateAccountResponseDto>> LinkParentToStudentAsync(
            LinkParentToStudentDto request, string currentUserId, string ipAddress)
        {
            var response = new ServiceResponse<CreateAccountResponseDto>();

            // ---- Caller authorization ----
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

            // ---- Validate student ----
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

            // ---- Validate parent details ----
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

            // ---- Find or create parent ----
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

                // ---- Optional picture upload for new parent ----
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

            // ---- Check for existing link ----
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

            // ---- Create the link ----
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

            var caller = await _userManager.FindByIdAsync(currentUserId);
            if (caller == null)
            {
                response.Success = false;
                response.StatusCode = 401;
                return response;
            }

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

        #region Helper - Profile Picture Upload (Supabase)
        /// <summary>
        /// Uploads a profile picture to Supabase into a role-based folder:
        ///   profile-pictures/students/{userId}/{guid}.jpg
        ///   profile-pictures/teachers/{userId}/{guid}.jpg
        ///   profile-pictures/parents/{userId}/{guid}.jpg
        ///   profile-pictures/staff/{userId}/{guid}.jpg
        /// Returns the public URL.
        /// </summary>
        private async Task<string?> UploadProfilePictureAsync(
            string userId, string role, IFormFile picture)
        {
            if (picture == null || picture.Length == 0)
                return null;

            // ---- Validation ----
            if (picture.Length > MaxProfilePictureBytes)
                throw new InvalidOperationException(
                    $"Profile picture must not exceed {MaxProfilePictureBytes / 1024} KB.");

            if (!AllowedImageMime.Contains(picture.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException(
                    "Only JPG, PNG, and WEBP images are allowed.");

            // ---- Role-based folder ----
            var folder = role switch
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

            var key = $"{folder}/{userId}/{Guid.NewGuid():N}{extension}";

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