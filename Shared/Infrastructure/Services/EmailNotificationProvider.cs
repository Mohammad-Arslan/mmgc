using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;
using System.Net;
using System.Net.Mail;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Email notification provider using Gmail SMTP.
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

            var messageId = await SendEmailViaSmtpAsync(recipientContact, subject, message, cancellationToken);

            _logger.LogInformation(
                "Email sent to {EmailAddress}, Subject: {Subject}, Message ID: {MessageId}",
                MaskEmail(recipientContact),
                subject,
                messageId);

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
        var host = _configuration["Smtp:Host"];
        var fromEmail = _configuration["Smtp:FromEmail"];
        var password = _configuration["Smtp:Password"];
        return !string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(fromEmail) && !string.IsNullOrWhiteSpace(password);
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

    private async Task<string> SendEmailViaSmtpAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var port = _configuration.GetValue<int>("Smtp:Port", 587);
        var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl", true);
        var fromEmail = _configuration["Smtp:FromEmail"];
        var fromName = _configuration["Smtp:FromName"] ?? "MMGC Hospital";
        var password = _configuration["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMTP configuration is incomplete. Check Smtp:Host, Smtp:FromEmail, and Smtp:Password in appsettings.json.");
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(fromEmail, password)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = body.Contains("<html", StringComparison.OrdinalIgnoreCase) || body.Contains("<p>", StringComparison.OrdinalIgnoreCase)
        };
        mailMessage.To.Add(recipientEmail);

        await client.SendMailAsync(mailMessage, cancellationToken);

        return Guid.NewGuid().ToString();
    }
}
