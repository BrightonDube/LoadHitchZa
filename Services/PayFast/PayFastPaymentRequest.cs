using System.Security.Cryptography;
using System.Text;

namespace t12Project.Services.PayFast;

/// <summary>
/// PayFast payment request
/// </summary>
public class PayFastPaymentRequest
{
    // Merchant details
    public string merchant_id { get; set; } = string.Empty;
    public string merchant_key { get; set; } = string.Empty;
    public string return_url { get; set; } = string.Empty;
    public string cancel_url { get; set; } = string.Empty;
    public string notify_url { get; set; } = string.Empty;

    // Buyer details
    public string name_first { get; set; } = string.Empty;
    public string name_last { get; set; } = string.Empty;
    public string email_address { get; set; } = string.Empty;
    public string cell_number { get; set; } = string.Empty;

    // Transaction details
    public string m_payment_id { get; set; } = string.Empty; // Unique payment ID
    public string amount { get; set; } = string.Empty;
    public string item_name { get; set; } = string.Empty;
    public string item_description { get; set; } = string.Empty;

    // Custom fields
    public string custom_str1 { get; set; } = string.Empty; // Load ID
    public string custom_str2 { get; set; } = string.Empty; // Customer ID
    public string custom_str3 { get; set; } = string.Empty; // Driver ID (if assigned)

    // Payment methods
    public string payment_method { get; set; } = "cc"; // cc = Credit Card, eft = EFT

    // Subscription (optional)
    public string subscription_type { get; set; } = "0"; // 0 = once-off

    // Email confirmations
    public string email_confirmation { get; set; } = "1";
    public string confirmation_address { get; set; } = string.Empty;

    // Signature for security
    public string signature { get; set; } = string.Empty;

    /// <summary>
    /// Generate MD5 signature for PayFast request
    /// </summary>
    public void GenerateSignature(string passphrase)
    {
        var data = new Dictionary<string, string>
        {
            { "merchant_id", merchant_id },
            { "merchant_key", merchant_key },
            { "return_url", return_url },
            { "cancel_url", cancel_url },
            { "notify_url", notify_url },
            { "name_first", name_first },
            { "name_last", name_last },
            { "email_address", email_address },
            { "cell_number", cell_number },
            { "m_payment_id", m_payment_id },
            { "amount", amount },
            { "item_name", item_name },
            { "item_description", item_description },
            { "custom_str1", custom_str1 },
            { "custom_str2", custom_str2 },
            { "custom_str3", custom_str3 },
            { "email_confirmation", email_confirmation },
            { "confirmation_address", confirmation_address },
            { "payment_method", payment_method }
        };

        // Remove empty values
        var filteredData = data.Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                               .OrderBy(kvp => kvp.Key);

        // Create parameter string
        var paramString = string.Join("&", filteredData.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        // Add passphrase if provided
        if (!string.IsNullOrEmpty(passphrase))
        {
            paramString += $"&passphrase={Uri.EscapeDataString(passphrase)}";
        }

        // Generate MD5 hash
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(paramString));
        signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Get form fields as dictionary for HTML form generation
    /// </summary>
    public Dictionary<string, string> GetFormFields()
    {
        return new Dictionary<string, string>
        {
            { "merchant_id", merchant_id },
            { "merchant_key", merchant_key },
            { "return_url", return_url },
            { "cancel_url", cancel_url },
            { "notify_url", notify_url },
            { "name_first", name_first },
            { "name_last", name_last },
            { "email_address", email_address },
            { "cell_number", cell_number },
            { "m_payment_id", m_payment_id },
            { "amount", amount },
            { "item_name", item_name },
            { "item_description", item_description },
            { "custom_str1", custom_str1 },
            { "custom_str2", custom_str2 },
            { "custom_str3", custom_str3 },
            { "email_confirmation", email_confirmation },
            { "confirmation_address", confirmation_address },
            { "payment_method", payment_method },
            { "signature", signature }
        };
    }
}
