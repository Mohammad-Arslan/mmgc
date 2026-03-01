using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MMGC.Services;

public class TwilioSmsService : ISmsService
{
    private readonly string? _accountSid;
    private readonly string? _authToken;
    private readonly string? _fromPhoneNumber;
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly bool _isConfigured;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        // Read environment variables first, then fall back to configuration.
        // Accept both "FromPhoneNumber" and older "FromPhone" keys to avoid key-name mismatches.
        _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
            ?? configuration["Twilio:AccountSid"];

        _authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
            ?? configuration["Twilio:AuthToken"];

        _fromPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_PHONE_NUMBER")
            ?? configuration["Twilio:FromPhoneNumber"]
            ?? configuration["Twilio:FromPhone"]; // fallback to existing key in appsettings.json

        _logger = logger;

        // Determine whether we have the required configuration to use Twilio.
        _isConfigured = !string.IsNullOrWhiteSpace(_accountSid)
                        && !string.IsNullOrWhiteSpace(_authToken)
                        && !string.IsNullOrWhiteSpace(_fromPhoneNumber);

        if (_isConfigured)
        {
            // Log configuration (without exposing full credentials)
            _logger.LogInformation("Twilio SMS Service initialized. AccountSid: {AccountSidPrefix}..., FromNumber: {FromNumber}",
                _accountSid!.Substring(0, Math.Min(8, _accountSid.Length)), _fromPhoneNumber);

            // Initialize Twilio client once during construction if configured
            TwilioClient.Init(_accountSid, _authToken);
        }
        else
        {
            _logger.LogWarning("Twilio SMS Service not fully configured. TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN and TWILIO_FROM_PHONE_NUMBER (or Twilio:FromPhone/FromPhoneNumber) are required. SMS sending will be disabled until configured.");
        }
    }

    public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Twilio not configured. Skipping SMS to {PhoneNumber}.", toPhoneNumber);
            return false;
        }

        try
        {
            // Ensure Twilio client is initialized (safe no-op if already initialized)
            TwilioClient.Init(_accountSid!, _authToken!);

            // Format phone number to E.164 format if needed
            var formattedPhoneNumber = FormatPhoneNumber(toPhoneNumber);

            _logger.LogDebug("Sending SMS. From: {From}, To: {To}, MessageLength: {Length}, MessagePreview: {Preview}",
                _fromPhoneNumber, formattedPhoneNumber, message?.Length ?? 0,
                message?.Length > 50 ? message.Substring(0, 50) + "..." : message);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(formattedPhoneNumber))
            {
                From = new PhoneNumber(_fromPhoneNumber),
                Body = message
            };

            var messageResource = await Task.Run(() => MessageResource.Create(messageOptions));

            _logger.LogInformation("SMS Response. SID: {MessageSid}, To: {To}, Status: {Status}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}, Price: {Price}, PriceUnit: {PriceUnit}, Uri: {Uri}",
                messageResource.Sid, formattedPhoneNumber, messageResource.Status,
                messageResource.ErrorCode, messageResource.ErrorMessage, messageResource.Price, messageResource.PriceUnit, messageResource.Uri);

            _logger.LogDebug("Message body sent: {MessageBody}", message?.Length > 100 ? message.Substring(0, 100) + "..." : message);

            if (messageResource.ErrorCode.HasValue && messageResource.ErrorCode.Value != 0)
            {
                _logger.LogWarning("SMS has error code: {ErrorCode}, Message: {ErrorMessage}",
                    messageResource.ErrorCode, messageResource.ErrorMessage);
                return false;
            }

            if (messageResource.Status == MessageResource.StatusEnum.Queued ||
                messageResource.Status == MessageResource.StatusEnum.Sending ||
                messageResource.Status == MessageResource.StatusEnum.Sent ||
                messageResource.Status == MessageResource.StatusEnum.Delivered)
            {
                _logger.LogInformation("SMS accepted by Twilio. Status: {Status}. Note: If using a trial account, SMS will only be delivered to verified numbers.",
                    messageResource.Status);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}: {ErrorMessage}", toPhoneNumber, ex.Message);
            return false;
        }
    }

    private string FormatPhoneNumber(string phoneNumber)
    {
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (!phoneNumber.StartsWith("+"))
        {
            if (digitsOnly.StartsWith("92"))
            {
                return "+" + digitsOnly;
            }
            else if (digitsOnly.StartsWith("0"))
            {
                return "+92" + digitsOnly.Substring(1);
            }
            else if (digitsOnly.Length == 10)
            {
                return "+92" + digitsOnly;
            }
            else
            {
                return "+" + digitsOnly;
            }
        }

        return phoneNumber;
    }
}

