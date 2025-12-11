using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using t12Project.Data;
using t12Project.Models;

namespace t12Project.Services.PayFast;

/// <summary>
/// PayFast payment gateway integration service
/// </summary>
public class PayFastService
{
    private readonly PayFastSettings _settings;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayFastService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public PayFastService(
        IOptions<PayFastSettings> settings,
        ApplicationDbContext context,
        ILogger<PayFastService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _settings = settings.Value;
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Create PayFast payment request for a load
    /// </summary>
    public PayFastPaymentRequest CreatePaymentRequest(
        Payment payment,
        Load load,
        ApplicationUser customer,
        string returnUrl,
        string cancelUrl,
        string notifyUrl)
    {
        var request = new PayFastPaymentRequest
        {
            merchant_id = _settings.MerchantId,
            merchant_key = _settings.MerchantKey,
            return_url = returnUrl,
            cancel_url = cancelUrl,
            notify_url = notifyUrl,

            // Customer details
            name_first = customer.FullName.Split(' ').FirstOrDefault() ?? "Customer",
            name_last = customer.FullName.Split(' ').LastOrDefault() ?? "",
            email_address = customer.Email ?? "",
            cell_number = customer.PhoneNumber ?? "",

            // Transaction details
            m_payment_id = payment.Id.ToString(),
            amount = payment.Amount.ToString("F2"),
            item_name = $"LoadHitch - {load.Title}",
            item_description = $"{load.Description} ({load.WeightKg}kg, {load.DistanceKm:F1}km)",

            // Custom fields for tracking
            custom_str1 = load.Id.ToString(),
            custom_str2 = customer.Id,
            custom_str3 = load.AssignedDriverId ?? "",

            // Email confirmation
            email_confirmation = "1",
            confirmation_address = customer.Email ?? "",

            payment_method = "cc" // Credit card
        };

        // Generate signature
        request.GenerateSignature(_settings.Passphrase);

        _logger.LogInformation("Created PayFast payment request for Payment {PaymentId}, Amount: R{Amount}",
            payment.Id, payment.Amount);

        return request;
    }

    /// <summary>
    /// Validate PayFast ITN (Instant Transaction Notification)
    /// </summary>
    public async Task<bool> ValidateNotificationAsync(PayFastNotification notification, string signature)
    {
        try
        {
            // Step 1: Verify signature
            if (!VerifySignature(notification, signature))
            {
                _logger.LogWarning("PayFast notification signature verification failed");
                return false;
            }

            // Step 2: Verify payment amounts match
            var payment = await _context.Payments.FindAsync(Guid.Parse(notification.m_payment_id));
            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found", notification.m_payment_id);
                return false;
            }

            var expectedAmount = payment.Amount.ToString("F2");
            if (notification.amount_gross != expectedAmount)
            {
                _logger.LogWarning("Payment amount mismatch. Expected: {Expected}, Received: {Received}",
                    expectedAmount, notification.amount_gross);
                return false;
            }

            // Step 3: Verify with PayFast server
            var isValid = await VerifyWithPayFastServerAsync(notification);
            if (!isValid)
            {
                _logger.LogWarning("PayFast server validation failed");
                return false;
            }

            _logger.LogInformation("PayFast notification validated successfully for Payment {PaymentId}",
                notification.m_payment_id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayFast notification");
            return false;
        }
    }

    /// <summary>
    /// Verify signature from PayFast notification
    /// </summary>
    private bool VerifySignature(PayFastNotification notification, string receivedSignature)
    {
        var data = new Dictionary<string, string>
        {
            { "m_payment_id", notification.m_payment_id },
            { "pf_payment_id", notification.pf_payment_id },
            { "payment_status", notification.payment_status },
            { "item_name", notification.item_name },
            { "item_description", notification.item_description },
            { "amount_gross", notification.amount_gross },
            { "amount_fee", notification.amount_fee },
            { "amount_net", notification.amount_net },
            { "custom_str1", notification.custom_str1 },
            { "custom_str2", notification.custom_str2 },
            { "custom_str3", notification.custom_str3 },
            { "name_first", notification.name_first },
            { "name_last", notification.name_last },
            { "email_address", notification.email_address },
            { "merchant_id", notification.merchant_id }
        };

        // Remove empty values and sort
        var filteredData = data.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                               .OrderBy(kvp => kvp.Key);

        // Create parameter string
        var paramString = string.Join("&", filteredData.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        // Add passphrase
        if (!string.IsNullOrEmpty(_settings.Passphrase))
        {
            paramString += $"&passphrase={Uri.EscapeDataString(_settings.Passphrase)}";
        }

        // Generate MD5 hash
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(paramString));
        var calculatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return calculatedSignature == receivedSignature.ToLower();
    }

    /// <summary>
    /// Verify notification with PayFast server
    /// </summary>
    private async Task<bool> VerifyWithPayFastServerAsync(PayFastNotification notification)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            var formData = new Dictionary<string, string>
            {
                { "m_payment_id", notification.m_payment_id },
                { "pf_payment_id", notification.pf_payment_id },
                { "payment_status", notification.payment_status },
                { "item_name", notification.item_name },
                { "amount_gross", notification.amount_gross }
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await httpClient.PostAsync(_settings.ValidateUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayFast validation request failed with status {StatusCode}",
                    response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadAsStringAsync();
            return result.Contains("VALID");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying with PayFast server");
            return false;
        }
    }

    /// <summary>
    /// Process successful payment notification
    /// </summary>
    public async Task<bool> ProcessPaymentNotificationAsync(PayFastNotification notification)
    {
        try
        {
            var paymentId = Guid.Parse(notification.m_payment_id);
            var payment = await _context.Payments.FindAsync(paymentId);

            if (payment == null)
            {
                _logger.LogError("Payment {PaymentId} not found", paymentId);
                return false;
            }

            if (notification.IsComplete)
            {
                payment.Status = PaymentStatus.Held; // Hold in escrow
                payment.PaidAt = DateTimeOffset.UtcNow;
                payment.TransactionId = notification.pf_payment_id;

                // Store card info (PayFast doesn't provide card details in ITN)
                // Card info would need to be collected separately if needed

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment {PaymentId} completed and held in escrow. Amount: R{Amount}",
                    paymentId, notification.amount_gross);

                return true;
            }
            else if (notification.IsFailed || notification.IsCancelled)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = $"Payment {notification.payment_status}";

                await _context.SaveChangesAsync();

                _logger.LogWarning("Payment {PaymentId} failed/cancelled. Status: {Status}",
                    paymentId, notification.payment_status);

                return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayFast payment notification");
            return false;
        }
    }

    /// <summary>
    /// Get PayFast payment form HTML
    /// </summary>
    public string GeneratePaymentFormHtml(PayFastPaymentRequest request)
    {
        var formFields = request.GetFormFields();
        var html = new StringBuilder();

        html.AppendLine($"<form action=\"{_settings.ProcessUrl}\" method=\"post\" id=\"payfast-form\">");

        foreach (var field in formFields.Where(f => !string.IsNullOrEmpty(f.Value)))
        {
            html.AppendLine($"  <input type=\"hidden\" name=\"{field.Key}\" value=\"{field.Value}\" />");
        }

        html.AppendLine("  <button type=\"submit\" class=\"btn btn-primary\">Pay with PayFast</button>");
        html.AppendLine("</form>");
        html.AppendLine("<script>document.getElementById('payfast-form').submit();</script>");

        return html.ToString();
    }
}
