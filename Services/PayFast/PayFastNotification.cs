namespace t12Project.Services.PayFast;

/// <summary>
/// PayFast ITN (Instant Transaction Notification) response
/// </summary>
public class PayFastNotification
{
    // Merchant details
    public string m_payment_id { get; set; } = string.Empty;
    public string pf_payment_id { get; set; } = string.Empty;

    // Transaction details
    public string payment_status { get; set; } = string.Empty;
    public string item_name { get; set; } = string.Empty;
    public string item_description { get; set; } = string.Empty;
    public string amount_gross { get; set; } = string.Empty;
    public string amount_fee { get; set; } = string.Empty;
    public string amount_net { get; set; } = string.Empty;

    // Custom fields
    public string custom_str1 { get; set; } = string.Empty; // Load ID
    public string custom_str2 { get; set; } = string.Empty; // Customer ID
    public string custom_str3 { get; set; } = string.Empty; // Driver ID

    // Buyer details
    public string name_first { get; set; } = string.Empty;
    public string name_last { get; set; } = string.Empty;
    public string email_address { get; set; } = string.Empty;

    // Card details
    public string merchant_id { get; set; } = string.Empty;
    public string signature { get; set; } = string.Empty;

    /// <summary>
    /// Payment was successful
    /// </summary>
    public bool IsComplete => payment_status == "COMPLETE";

    /// <summary>
    /// Payment was cancelled
    /// </summary>
    public bool IsCancelled => payment_status == "CANCELLED";

    /// <summary>
    /// Payment failed
    /// </summary>
    public bool IsFailed => payment_status == "FAILED";
}

/// <summary>
/// PayFast payment status constants
/// </summary>
public static class PayFastStatus
{
    public const string Complete = "COMPLETE";
    public const string Failed = "FAILED";
    public const string Pending = "PENDING";
    public const string Cancelled = "CANCELLED";
}
