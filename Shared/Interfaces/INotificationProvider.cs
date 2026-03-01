namespace MMGC.Shared.Interfaces;

/// <summary>
/// Base contract for notification providers.
/// Implements strategy pattern to support multiple delivery channels (SMS, Email, WhatsApp).
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Gets the type/name of this notification provider.
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Sends a notification through this provider's channel.
    /// </summary>
    /// <param name="recipientContact">Phone number (SMS) or email address (Email).</param>
    /// <param name="message">Message content to send.</param>
    /// <param name="subject">Optional subject line (for email).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>External service message ID for tracking.</returns>
    /// <exception cref="ExternalServiceException">Thrown if delivery fails.</exception>
    Task<string> SendAsync(string recipientContact, string message, string? subject = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if this provider is currently enabled/configured.
    /// </summary>
    /// <returns>True if provider is ready to send messages.</returns>
    bool IsEnabled();

    /// <summary>
    /// Validates the recipient contact format for this provider.
    /// </summary>
    /// <param name="recipientContact">Contact to validate.</param>
    /// <returns>True if contact format is valid.</returns>
    bool ValidateContact(string recipientContact);
}
