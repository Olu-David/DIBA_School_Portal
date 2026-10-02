using DIBA_School_Portal.Server.Contracts;
using DIBA_School_Portal.Server.Repository.IServices;
using Resend;

public class EmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<EmailService> _logger;

    #region Constructor
    public EmailService(IResend resend, ILogger<EmailService> logger)
    {
        _resend = resend;
        _logger = logger;
    }
    #endregion

    #region EmailSendAsync
    public async Task EmailSendAsync(string toEmail, string subject, string emailContent, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            From = "noreply@DibaSchool.com.ng",
            To = { toEmail },
            Subject = subject,
            HtmlBody = emailContent
        };

        try
        {
            var response = await _resend.EmailSendAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}. Resend ID: {MessageId}", toEmail, response.Content.ToString());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email send canceled for {Recipient} due to shutdown.", toEmail);
            throw;
        }
        catch (ResendException ex)
        {
            _logger.LogError(ex, "Resend API error sending email to {Recipient}", toEmail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", toEmail);
            throw;
        }
    }
    #endregion

    #region SendBatchEmailsAsync
    public async Task<bool> SendBatchEmailsAsync(List<EmailMessageDto> emails)
    {
        try
        {
            if (emails == null || !emails.Any())
            {
                _logger.LogWarning("Email batch list is empty.");
                return false;
            }

            foreach (var emailDto in emails)
            {
                var message = new EmailMessage
                {
                    From = "noreply@DibaSchool.com.ng",
                    To = { emailDto?.To??"" },
                    Subject = emailDto?.Subject,
                    HtmlBody = emailDto?.HtmlBody
                };

                // Send individually using the native Resend client implementation
                await _resend.EmailSendAsync(message);
            }

            _logger.LogInformation("Batch emails successfully processed for {Count} recipients via Resend.", emails.Count);
            return true;
        }
        catch (ResendException ex)
        {
            _logger.LogError(ex, "Resend API error while processing batch emails.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing batch emails.");
            return false;
        }
    }
    #endregion
}
