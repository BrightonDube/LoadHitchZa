namespace t12Project.Services.PayFast;

/// <summary>
/// PayFast configuration settings
/// </summary>
public class PayFastSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string Passphrase { get; set; } = string.Empty;
    public string ProcessUrl { get; set; } = "https://sandbox.payfast.co.za/eng/process";
    public string ValidateUrl { get; set; } = "https://sandbox.payfast.co.za/eng/query/validate";
    public bool UseSandbox { get; set; } = true;

    public void UseProduction()
    {
        UseSandbox = false;
        ProcessUrl = "https://www.payfast.co.za/eng/process";
        ValidateUrl = "https://www.payfast.co.za/eng/query/validate";
    }
}
