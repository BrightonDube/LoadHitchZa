using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace t12Project.Models;

/// <summary>
/// Payment transaction for a load
/// </summary>
public class Payment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LoadId { get; set; }

    [ForeignKey(nameof(LoadId))]
    public Load? Load { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public ApplicationUser? Customer { get; set; }

    public string? DriverId { get; set; }

    [ForeignKey(nameof(DriverId))]
    public ApplicationUser? Driver { get; set; }

    /// <summary>
    /// Total amount in ZAR
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Platform fee (15% of amount)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlatformFee { get; set; }

    /// <summary>
    /// Amount driver receives after platform fee
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DriverPayout { get; set; }

    /// <summary>
    /// Payment status: Pending, Held, Released, Refunded
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Payment method: Card, EFT, Cash
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Card";

    /// <summary>
    /// External payment provider transaction ID
    /// </summary>
    [MaxLength(200)]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Last 4 digits of card used
    /// </summary>
    [MaxLength(4)]
    public string? Last4 { get; set; }

    /// <summary>
    /// Card brand: Visa, Mastercard, etc.
    /// </summary>
    [MaxLength(50)]
    public string? CardBrand { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Driver payout transaction ID
    /// </summary>
    [MaxLength(200)]
    public string? DriverPayoutTransactionId { get; set; }

    /// <summary>
    /// Refund transaction ID
    /// </summary>
    [MaxLength(200)]
    public string? RefundTransactionId { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    [MaxLength(500)]
    public string? RefundReason { get; set; }

    /// <summary>
    /// Payment failure reason
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }
}

/// <summary>
/// Payment status constants
/// </summary>
public static class PaymentStatus
{
    public const string Pending = "Pending";           // Payment initiated
    public const string Held = "Held";                 // Funds held in escrow
    public const string Released = "Released";         // Released to driver
    public const string Refunded = "Refunded";         // Refunded to customer
    public const string Failed = "Failed";             // Payment failed
}
