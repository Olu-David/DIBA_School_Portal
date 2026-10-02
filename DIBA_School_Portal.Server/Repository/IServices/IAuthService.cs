using DIBA_School_Portal.Server.Contracts;


namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IAuthService
    {
        // ---------- LOGIN ----------
        Task<ServiceResponse<LoginResponseDto>> StudentLoginAsync(
            StudentLoginRequestDto request, string ipAddress);

        Task<ServiceResponse<LoginResponseDto>> StaffLoginAsync(
            StaffLoginRequestDto request, string ipAddress);

        Task<ServiceResponse<LoginResponseDto>> ParentLoginAsync(
            ParentLoginRequestDto request, string ipAddress);

        // ---------- CREATE ACCOUNTS ----------
        Task<ServiceResponse<CreateAccountResponseDto>> CreateAccountAsync(
            CreateAccountDto request, string currentUserId, string ipAddress);

        Task<ServiceResponse<CreateStudentWithParentResponseDto>> CreateStudentWithParentAsync(
            CreateStudentWithParentDto request, string currentUserId, string ipAddress);

        Task<ServiceResponse<CreateAccountResponseDto>> LinkParentToStudentAsync(
            LinkParentToStudentDto request, string currentUserId, string ipAddress);

        // ---------- PASSWORD ----------
        Task<ServiceResponse<bool>> ChangePasswordAsync(
            string userId, ChangePasswordDto request, string ipAddress);

        Task<ServiceResponse<bool>> AdminResetPasswordAsync(
            string currentUserId, AdminResetPasswordDto request, string ipAddress);
    }
}