using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Email notification provider.
/// Currently supports SMTP; can be extended to use SendGrid or other providers.
/// </summary>
public class EmailNotificationProvider : INotificationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public string ProviderType => "Email";

    public EmailNotificationProvider(
        IConfiguration configuration,
        ILogger<EmailNotificationProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SendAsync(
        string recipientContact,
        string message,
        string? subject = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateContact(recipientContact))
            {
                throw new ArgumentException($"Invalid email format: {recipientContact}");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty");
            }

            subject ??= "Hospital Management System Notification";

            // For now, log that email would be sent
            // In production, integrate with SendGrid, SendPulse, or SMTP server
            var messageId = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "Email prepared for {EmailAddress}, Subject: {Subject}, Message ID: {MessageId}",
                MaskEmail(recipientContact),
                subject,
                messageId);

            // TODO: Implement actual email sending via SendGrid or SMTP
            // await SendEmailViaSendGridAsync(recipientContact, subject, message, cancellationToken);

            // Simulate async operation
            await Task.Delay(100, cancellationToken);

            return messageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {EmailAddress}",
                MaskEmail(recipientContact));

            throw new ExternalServiceException("Email Service", ex.Message, ex);
        }
    }

    public bool IsEnabled()
    {
        // Email is always enabled as fallback
        // In production, check for SendGrid API key or SMTP configuration
        return true;
    }

    public bool ValidateContact(string recipientContact)
    {
        if (string.IsNullOrWhiteSpace(recipientContact))
            return false;

        try
        {
            var mailAddress = new MailAddress(recipientContact);
            return mailAddress.Address == recipientContact;
        }
        catch
        {
            return false;
        }
    }

    private static string MaskEmail(string email)
    {
        if (email.Length <= 8)
            return "****";

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "****";

        var localPart = email.Substring(0, Math.Min(2, atIndex));
        var domain = email.Substring(atIndex);

        return $"{localPart}****{domain}";
    }

    // Future: Implement SendGrid integration
    // private async Task SendEmailViaSendGridAsync(string recipientEmail, string subject, string htmlContent, CancellationToken cancellationToken)
    // {
    //     var apiKey = _configuration["SendGrid:ApiKey"];
    //     var from = _configuration["SendGrid:FromEmail"];
    //     var client = new SendGridClient(apiKey);
    //     var msg = new SendGridMessage()
    //     {
    //         From = new EmailAddress(from, "MMGC Hospital"),
    //         Subject = subject,
    //         HtmlContent = htmlContent,
    //     };
    //     msg.AddTo(new EmailAddress(recipientEmail));
    //     var response = await client.SendEmailAsync(msg, cancellationToken);
    // }
}
