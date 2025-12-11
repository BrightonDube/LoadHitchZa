using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace t12Project.Models;

public class Load
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Status { get; set; } = "Available"; // Available, Accepted, PickedUp, InTransit, Delivered, Completed, Cancelled

    // Driver assignment
    public string? AssignedDriverId { get; set; }
    public ApplicationUser? AssignedDriver { get; set; }

    [MaxLength(256)]
    public string PickupLocation { get; set; } = string.Empty;

    [MaxLength(256)]
    public string DropoffLocation { get; set; } = string.Empty;

    // GPS Coordinates for map display
    public decimal PickupLatitude { get; set; }
    public decimal PickupLongitude { get; set; }
    public decimal DropoffLatitude { get; set; }
    public decimal DropoffLongitude { get; set; }

    public DateTimeOffset PickupDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Weight in kilograms (NOT pounds)
    /// </summary>
    public int WeightKg { get; set; }

    /// <summary>
    /// Cargo description (e.g., fragile boxes, liquids, flatbed load)
    /// </summary>
    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Load category: Electronics, Furniture, Food, Construction, Vehicles, Chemicals, General, Fragile
    /// </summary>
    [MaxLength(60)]
    public string CargoType { get; set; } = "General";

    /// <summary>
    /// Calculated road distance in kilometers
    /// </summary>
    public double? DistanceKm { get; set; }

    /// <summary>
    /// Customer's price offer in ZAR (South African Rand)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CustomerOfferPrice { get; set; }

    /// <summary>
    /// Calculated base price (before customer adjustment)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? CalculatedPrice { get; set; }

    /// <summary>
    /// Final agreed price in ZAR
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? FinalPrice { get; set; }

    /// <summary>
    /// Driver earnings after platform fee (15%)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? DriverEarnings { get; set; }

    /// <summary>
    /// Associated payment
    /// </summary>
    public Guid? PaymentId { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment? Payment { get; set; }

    [Required]
    public string CustomerId { get; set; } = default!;

    public ApplicationUser? Customer { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Lifecycle timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Estimated Time of Arrival (in minutes from current time)
    public int? EstimatedTimeOfArrivalMinutes { get; set; }

    // Delivery sequence for drivers managing multiple loads
    public int? DeliverySequence { get; set; }
}
