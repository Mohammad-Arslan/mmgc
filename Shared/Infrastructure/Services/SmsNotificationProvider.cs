using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MMGC.Services;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;
using System.Text.RegularExpressions;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// SMS notification provider using Twilio.
/// </summary>
public class SmsNotificationProvider : INotificationProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsNotificationProvider> _logger;
    private readonly ISmsService _smsService;

    public string ProviderType => "SMS";

    public SmsNotificationProvider(
        IConfiguration configuration,
        ILogger<SmsNotificationProvider> logger,
        ISmsService smsService)
    {
        _configuration = configuration;
        _logger = logger;
        _smsService = smsService;
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
                throw new ArgumentException($"Invalid phone number format: {recipientContact}");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty");
            }

            // Call SMS service to send SMS
            var success = await _smsService.SendSmsAsync(recipientContact, message);

            if (!success)
                throw new Exception("SMS service returned false");

            // Generate a message ID for tracking
            var messageId = Guid.NewGuid().ToString();

            _logger.LogInformation(
                "SMS sent successfully to {PhoneNumber}, Message ID: {MessageId}",
                MaskPhoneNumber(recipientContact),
                messageId);

            return messageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send SMS to {PhoneNumber}",
                MaskPhoneNumber(recipientContact));

            throw new ExternalServiceException("SMS Service", ex.Message, ex);
        }
    }

    public bool IsEnabled()
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        var fromPhone = _configuration["Twilio:FromPhone"];

        return !string.IsNullOrWhiteSpace(accountSid) &&
               !string.IsNullOrWhiteSpace(authToken) &&
               !string.IsNullOrWhiteSpace(fromPhone);
    }

    public bool ValidateContact(string recipientContact)
    {
        // Validate phone number format (basic validation)
        // Accepts formats: +1234567890, 1234567890, (123) 456-7890, etc.
        if (string.IsNullOrWhiteSpace(recipientContact))
            return false;

        // Simple regex: must contain at least 10 digits
        var phoneRegex = new Regex(@"^\+?1?\d{9,15}$");
        return phoneRegex.IsMatch(Regex.Replace(recipientContact, @"[^\d+]", ""));
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
            return "****";

        var lastFour = phoneNumber.Substring(phoneNumber.Length - 4);
        return $"****{lastFour}";
    }
}
