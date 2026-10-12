using DIBA_School_Portal.Server.Contracts;

public interface INotificationDispatchService
{
    Task<ServiceResponse<SendQueuedResponse>> SendEmailAsync(
        SendEmailRequest request, string currentUserId, string ip);

    Task<ServiceResponse<SendQueuedResponse>> SendSmsAsync(
        SendSmsRequest request, string currentUserId, string ip);
}
