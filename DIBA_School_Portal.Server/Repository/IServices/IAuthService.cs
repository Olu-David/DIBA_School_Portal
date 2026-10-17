using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Contracts.FinanceDto;
using DIBA_School_Portal.Server.Contracts.StudentDTO;


namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IAuthService
    {
        #region Login
        /// <summary>
        /// Student portal login.
        /// Identifier: Admission Number (e.g. "DIBA/2026/001") + password.
        /// </summary>
        Task<ServiceResponse<LoginResponseDto>> StudentLoginAsync(
            StudentLoginRequestDto request, string ipAddress);

        /// <summary>
        /// Staff portal login.
        /// Identifier: Email + password.
        /// </summary>
        Task<ServiceResponse<LoginResponseDto>> StaffLoginAsync(
            StaffLoginRequestDto request, string ipAddress);

        /// <summary>
        /// Parent portal login.
        /// Identifier: Phone number (preferred) OR email + password.
        /// </summary>
        Task<ServiceResponse<LoginResponseDto>> ParentLoginAsync(
            ParentLoginRequestDto request, string ipAddress);
        #endregion

        #region UpdateProfileAsync and UpdateProfilePictureAsync
        Task<ServiceResponse<UserProfileDto>> UpdateProfileAsync(
    string userId, UpdateProfileDto request, string ipAddress);

        Task<ServiceResponse<string>> UpdateProfilePictureAsync(
            string userId, IFormFile picture, string ipAddress);

   #endregion

        #region Account Creation
        /// <summary>
        /// Creates a single account. Restricted to SuperAdmin, Principal, and Registrar.
        /// Auto-generates admission number (students) or staff ID (teachers).
        /// </summary>
        Task<ServiceResponse<CreateAccountResponseDto>> CreateAccountAsync(
            CreateAccountDto request, string currentUserId, string ipAddress);
        #endregion

        #region Parent Linking
        /// <summary>
        /// Links a parent to an existing student.
        /// Reuses an existing parent account if the phone number already exists;
        /// otherwise creates a new parent account.
        /// Restricted to SuperAdmin, Principal, and Registrar.
        /// </summary>
        Task<ServiceResponse<CreateAccountResponseDto>> LinkParentToStudentAsync(
            LinkParentToStudentDto request, string currentUserId, string ipAddress);
        #endregion

        #region Password Management
        /// <summary>
        /// Self-service password change. Any authenticated user.
        /// </summary>
        Task<ServiceResponse<bool>> ChangePasswordAsync(
            string userId, ChangePasswordDto request, string ipAddress);

        /// <summary>
        /// Admin-initiated password reset (no email required).
        /// Restricted to SuperAdmin, Principal, and Registrar.
        /// </summary>
        Task<ServiceResponse<bool>> AdminResetPasswordAsync(
            string currentUserId, AdminResetPasswordDto request, string ipAddress);
        #endregion

        Task<ServiceResponse<UserProfileDto>> ChangeRoleAsync(  string targetUserId, ChangeRoleDto request,string currentUserId,string ipAddress);
    }
}