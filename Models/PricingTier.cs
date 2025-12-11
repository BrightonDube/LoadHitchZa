using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace t12Project.Models;

/// <summary>
/// Pricing configuration for different load categories
/// </summary>
public class PricingTier
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string LoadCategory { get; set; } = string.Empty; // Electronics, Furniture, Food, etc.

    /// <summary>
    /// Base fare in ZAR (South African Rand)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BaseFare { get; set; }

    /// <summary>
    /// Price per kilometer in ZAR
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePerKm { get; set; }

    /// <summary>
    /// Price per kilogram in ZAR
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePerKg { get; set; }

    /// <summary>
    /// Minimum weight in kg for this tier
    /// </summary>
    public int MinWeightKg { get; set; }

    /// <summary>
    /// Maximum weight in kg for this tier (null = unlimited)
    /// </summary>
    public int? MaxWeightKg { get; set; }

    /// <summary>
    /// Surge multiplier during peak hours (1.0 = no surge)
    /// </summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal SurgeMultiplier { get; set; } = 1.0m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
