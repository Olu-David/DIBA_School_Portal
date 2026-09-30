using DIBA_School_Portal.Server.Contracts;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IEmailService
    {
        Task EmailSendAsync(string toEmail, string subject, string emailContent, CancellationToken cancellationToken = default);
        Task<bool> SendBatchEmailsAsync(List<EmailMessageDto> emails);
    }
}