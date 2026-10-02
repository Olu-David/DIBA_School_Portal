using DIBA_School_Portal.Server.Contracts;


namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IParentService
    {
        /// <summary>
        /// All children linked to the logged-in parent.
        /// </summary>
        Task<ServiceResponse<List<ParentChildSummaryDto>>> GetMyChildrenAsync(
            string parentUserId);

        /// <summary>
        /// Full dashboard for a single child (fees, results, enrollments).
        /// </summary>
        Task<ServiceResponse<ChildDashboardDto>> GetChildDashboardAsync(
            string parentUserId, Guid studentProfileId);

        /// <summary>
        /// Enrollment history for a child.
        /// </summary>
        Task<ServiceResponse<List<ChildEnrollmentDto>>> GetChildEnrollmentsAsync(
            string parentUserId, Guid studentProfileId);

        /// <summary>
        /// Fee breakdown for a child in a term (delegates to IPaymentService).
        /// </summary>
        Task<ServiceResponse<StudentBillResponseDto>> GetChildBillAsync(
            string parentUserId, Guid studentProfileId, Guid termId);

        /// <summary>
        /// Payment history for a child.
        /// </summary>
        Task<ServiceResponse<List<StudentPaymentDto>>> GetChildPaymentsAsync(
            string parentUserId, Guid studentProfileId);

        /// <summary>
        /// Report card for a child in a term (delegates to ITermSubjectResultService).
        /// </summary>
        Task<ServiceResponse<StudentReportCardDto>> GetChildReportCardAsync(
            string parentUserId, Guid studentProfileId, Guid termId, Guid academicSessionId);
    }
}