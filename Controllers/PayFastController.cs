using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t12Project.Data;
using t12Project.Services;
using t12Project.Services.PayFast;

namespace t12Project.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PayFastController : ControllerBase
{
    private readonly PayFastService _payFastService;
    private readonly PaymentService _paymentService;
    private readonly NotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayFastController> _logger;

    public PayFastController(
        PayFastService payFastService,
        PaymentService paymentService,
        NotificationService notificationService,
        ApplicationDbContext context,
        ILogger<PayFastController> logger)
    {
        _payFastService = payFastService;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// PayFast ITN (Instant Transaction Notification) webhook endpoint
    /// </summary>
    [HttpPost("notify")]
    public async Task<IActionResult> PayFastNotify()
    {
        try
        {
            // Read form data from PayFast
            var formData = await Request.ReadFormAsync();

            var notification = new PayFastNotification
            {
                m_payment_id = formData["m_payment_id"].ToString(),
                pf_payment_id = formData["pf_payment_id"].ToString(),
                payment_status = formData["payment_status"].ToString(),
                item_name = formData["item_name"].ToString(),
                item_description = formData["item_description"].ToString(),
                amount_gross = formData["amount_gross"].ToString(),
                amount_fee = formData["amount_fee"].ToString(),
                amount_net = formData["amount_net"].ToString(),
                custom_str1 = formData["custom_str1"].ToString(),
                custom_str2 = formData["custom_str2"].ToString(),
                custom_str3 = formData["custom_str3"].ToString(),
                name_first = formData["name_first"].ToString(),
                name_last = formData["name_last"].ToString(),
                email_address = formData["email_address"].ToString(),
                merchant_id = formData["merchant_id"].ToString()
            };

            var signature = formData["signature"].ToString();

            _logger.LogInformation("Received PayFast notification for payment {PaymentId}, status: {Status}",
                notification.m_payment_id, notification.payment_status);

            // Validate notification
            var isValid = await _payFastService.ValidateNotificationAsync(notification, signature);
            if (!isValid)
            {
                _logger.LogWarning("Invalid PayFast notification received");
                return BadRequest("Invalid notification");
            }

            // Process payment
            var success = await _payFastService.ProcessPaymentNotificationAsync(notification);
            if (!success)
            {
                return BadRequest("Failed to process payment");
            }

            // Send notifications
            var payment = await _context.Payments
                .Include(p => p.Load)
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == Guid.Parse(notification.m_payment_id));

            if (payment != null && notification.IsComplete)
            {
                // Notify customer
                await _notificationService.SendNotificationAsync(
                    payment.CustomerId,
                    "Payment Successful",
                    $"Your payment of R{notification.amount_gross} has been received and is being held securely. Funds will be released to the driver after delivery confirmation.",
                    "Payment",
                    payment.LoadId,
                    "Load"
                );

                // Notify driver if assigned
                if (payment.Load?.AssignedDriverId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        payment.Load.AssignedDriverId,
                        "Load Payment Confirmed",
                        $"Payment of R{notification.amount_gross} has been confirmed for {payment.Load.Title}. You'll receive R{payment.DriverPayout:F2} upon delivery completion.",
                        "Payment",
                        payment.LoadId,
                        "Load"
                    );
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayFast notification");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Return URL after successful payment
    /// </summary>
    [HttpGet("return")]
    public IActionResult PaymentReturn([FromQuery] string m_payment_id)
    {
        _logger.LogInformation("Payment return for {PaymentId}", m_payment_id);

        // Redirect to customer dashboard with success message
        return Redirect($"/dashboard?payment=success&id={m_payment_id}");
    }

    /// <summary>
    /// Cancel URL when payment is cancelled
    /// </summary>
    [HttpGet("cancel")]
    public IActionResult PaymentCancel([FromQuery] string m_payment_id)
    {
        _logger.LogWarning("Payment cancelled for {PaymentId}", m_payment_id);

        // Redirect to booking page with cancelled message
        return Redirect($"/dashboard?payment=cancelled&id={m_payment_id}");
    }
}
