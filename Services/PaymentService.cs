using Microsoft.EntityFrameworkCore;
using t12Project.Data;
using t12Project.Models;
using t12Project.Services.PayFast;

namespace t12Project.Services;

public class PaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly NotificationService _notificationService;
    private readonly PayFastService _payFastService;

    public PaymentService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        NotificationService notificationService,
        PayFastService payFastService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService;
        _payFastService = payFastService;
    }

    /// <summary>
    /// Initiate payment for a load
    /// </summary>
    public async Task<Payment> InitiatePaymentAsync(Guid loadId, string customerId, decimal amount)
    {
        var load = await _context.Loads.FindAsync(loadId);
        if (load == null)
            throw new ArgumentException("Load not found", nameof(loadId));

        var payment = new Payment
        {
            LoadId = loadId,
            CustomerId = customerId,
            Amount = amount,
            PlatformFee = amount * 0.15m, // 15% platform fee
            DriverPayout = amount * 0.85m, // Driver gets 85%
            Status = PaymentStatus.Pending,
            PaymentMethod = "Card", // Default to card
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment {PaymentId} initiated for load {LoadId}, amount: R{Amount}",
            payment.Id, loadId, amount);

        return payment;
    }

    /// <summary>
    /// Create PayFast payment request (returns payment URL for redirect)
    /// </summary>
    public async Task<PayFastPaymentRequest?> CreatePayFastPaymentAsync(Guid paymentId, string baseUrl)
    {
        var payment = await _context.Payments
            .Include(p => p.Load)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found", nameof(paymentId));

        if (payment.Load == null || payment.Customer == null)
        {
            _logger.LogError("Payment {PaymentId} missing Load or Customer", paymentId);
            return null;
        }

        try
        {
            var returnUrl = $"{baseUrl}/api/payfast/return";
            var cancelUrl = $"{baseUrl}/api/payfast/cancel";
            var notifyUrl = $"{baseUrl}/api/payfast/notify";

            var request = _payFastService.CreatePaymentRequest(
                payment,
                payment.Load,
                payment.Customer,
                returnUrl,
                cancelUrl,
                notifyUrl
            );

            _logger.LogInformation("Created PayFast payment request for {PaymentId}", paymentId);
            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayFast payment request for {PaymentId}", paymentId);
            return null;
        }
    }

    /// <summary>
    /// Process card payment and hold funds in escrow (legacy method for backward compatibility)
    /// </summary>
    public async Task<bool> ProcessCardPaymentAsync(Guid paymentId, CardPaymentDetails cardDetails)
    {
        var payment = await _context.Payments
            .Include(p => p.Load)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found", nameof(paymentId));

        try
        {
            // TODO: Integrate with payment gateway (PayFast or Stripe)
            // For now, simulate successful payment
            var gatewayResponse = await SimulatePaymentGatewayAsync(payment.Amount, cardDetails);

            if (gatewayResponse.Success)
            {
                payment.Status = PaymentStatus.Held; // Funds held in escrow
                payment.PaidAt = DateTimeOffset.UtcNow;
                payment.Last4 = cardDetails.CardNumber.Substring(cardDetails.CardNumber.Length - 4);
                payment.CardBrand = DetectCardBrand(cardDetails.CardNumber);
                payment.TransactionId = gatewayResponse.TransactionId;

                await _context.SaveChangesAsync();

                // Notify customer
                await _notificationService.SendNotificationAsync(
                    payment.CustomerId,
                    "Payment Successful",
                    $"Your payment of R{payment.Amount:F2} is being held securely. Funds will be released to the driver after successful delivery.",
                    "Payment"
                );

                _logger.LogInformation("Payment {PaymentId} processed successfully, funds held in escrow", paymentId);
                return true;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = gatewayResponse.ErrorMessage;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Payment {PaymentId} failed: {Reason}", paymentId, gatewayResponse.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
            return false;
        }
    }

    /// <summary>
    /// Release payment to driver after successful delivery
    /// </summary>
    public async Task<bool> ReleasePaymentToDriverAsync(Guid paymentId, string driverId)
    {
        var payment = await _context.Payments
            .Include(p => p.Load)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found", nameof(paymentId));

        if (payment.Status != PaymentStatus.Held)
        {
            _logger.LogWarning("Cannot release payment {PaymentId} with status {Status}",
                paymentId, payment.Status);
            return false;
        }

        try
        {
            // TODO: Integrate with payment gateway to transfer funds to driver
            // For now, simulate successful transfer
            var transferResponse = await SimulateDriverPayoutAsync(payment.DriverPayout, driverId);

            if (transferResponse.Success)
            {
                payment.Status = PaymentStatus.Released;
                payment.DriverId = driverId;
                payment.ReleasedAt = DateTimeOffset.UtcNow;
                payment.DriverPayoutTransactionId = transferResponse.TransactionId;

                await _context.SaveChangesAsync();

                // Notify driver
                await _notificationService.SendNotificationAsync(
                    driverId,
                    "Payment Received",
                    $"You've received R{payment.DriverPayout:F2} for completing the delivery. Funds have been transferred to your account.",
                    "Payment"
                );

                // Notify customer
                await _notificationService.SendNotificationAsync(
                    payment.CustomerId,
                    "Payment Released",
                    $"Payment of R{payment.Amount:F2} has been released to the driver after successful delivery.",
                    "Payment"
                );

                _logger.LogInformation("Payment {PaymentId} released to driver {DriverId}, amount: R{Amount}",
                    paymentId, driverId, payment.DriverPayout);

                return true;
            }
            else
            {
                _logger.LogError("Failed to transfer funds to driver for payment {PaymentId}: {Reason}",
                    paymentId, transferResponse.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing payment {PaymentId} to driver", paymentId);
            return false;
        }
    }

    /// <summary>
    /// Refund payment to customer
    /// </summary>
    public async Task<bool> RefundPaymentAsync(Guid paymentId, string reason)
    {
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new ArgumentException("Payment not found", nameof(paymentId));

        if (payment.Status != PaymentStatus.Held)
        {
            _logger.LogWarning("Cannot refund payment {PaymentId} with status {Status}",
                paymentId, payment.Status);
            return false;
        }

        try
        {
            // TODO: Integrate with payment gateway to refund
            var refundResponse = await SimulateRefundAsync(payment.Amount, payment.TransactionId);

            if (refundResponse.Success)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAt = DateTimeOffset.UtcNow;
                payment.RefundReason = reason;
                payment.RefundTransactionId = refundResponse.TransactionId;

                await _context.SaveChangesAsync();

                // Notify customer
                await _notificationService.SendNotificationAsync(
                    payment.CustomerId,
                    "Refund Processed",
                    $"Your payment of R{payment.Amount:F2} has been refunded. Reason: {reason}",
                    "Payment"
                );

                _logger.LogInformation("Payment {PaymentId} refunded, amount: R{Amount}, reason: {Reason}",
                    paymentId, payment.Amount, reason);

                return true;
            }
            else
            {
                _logger.LogError("Failed to refund payment {PaymentId}: {Reason}",
                    paymentId, refundResponse.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
            return false;
        }
    }

    /// <summary>
    /// Get payment for a load
    /// </summary>
    public async Task<Payment?> GetPaymentForLoadAsync(Guid loadId)
    {
        return await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Driver)
            .FirstOrDefaultAsync(p => p.LoadId == loadId);
    }

    /// <summary>
    /// Get payment history for customer
    /// </summary>
    public async Task<List<Payment>> GetCustomerPaymentsAsync(string customerId)
    {
        return await _context.Payments
            .Include(p => p.Load)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get driver earnings
    /// </summary>
    public async Task<List<Payment>> GetDriverEarningsAsync(string driverId)
    {
        return await _context.Payments
            .Include(p => p.Load)
            .Where(p => p.DriverId == driverId && p.Status == PaymentStatus.Released)
            .OrderByDescending(p => p.ReleasedAt)
            .ToListAsync();
    }

    // SIMULATION METHODS (Replace with actual payment gateway integration)

    private async Task<PaymentGatewayResponse> SimulatePaymentGatewayAsync(decimal amount, CardPaymentDetails cardDetails)
    {
        await Task.Delay(500); // Simulate API call

        // Validate card (basic simulation)
        if (cardDetails.CardNumber.Length < 13 || cardDetails.Cvv.Length < 3)
        {
            return new PaymentGatewayResponse
            {
                Success = false,
                ErrorMessage = "Invalid card details"
            };
        }

        return new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}"
        };
    }

    private async Task<PaymentGatewayResponse> SimulateDriverPayoutAsync(decimal amount, string driverId)
    {
        await Task.Delay(300); // Simulate API call

        return new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = $"PAYOUT-{Guid.NewGuid().ToString("N")[..12].ToUpper()}"
        };
    }

    private async Task<PaymentGatewayResponse> SimulateRefundAsync(decimal amount, string originalTransactionId)
    {
        await Task.Delay(400); // Simulate API call

        return new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = $"REFUND-{Guid.NewGuid().ToString("N")[..12].ToUpper()}"
        };
    }

    private string DetectCardBrand(string cardNumber)
    {
        if (cardNumber.StartsWith("4")) return "Visa";
        if (cardNumber.StartsWith("5")) return "Mastercard";
        if (cardNumber.StartsWith("3")) return "Amex";
        return "Unknown";
    }
}

// Supporting classes

public class CardPaymentDetails
{
    public string CardNumber { get; set; } = string.Empty;
    public string CardholderName { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
}

public class PaymentGatewayResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
