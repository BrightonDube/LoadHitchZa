using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace t12Project.Models;

/// <summary>
/// Tracks real-time driver location for live tracking
/// </summary>
public class DriverLocation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string DriverId { get; set; } = string.Empty;

    [ForeignKey(nameof(DriverId))]
    public ApplicationUser? Driver { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// Heading in degrees (0-360, where 0 is North)
    /// </summary>
    public double? Heading { get; set; }

    /// <summary>
    /// Speed in km/h
    /// </summary>
    public double? SpeedKmh { get; set; }

    /// <summary>
    /// GPS accuracy in meters
    /// </summary>
    public double? AccuracyMeters { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Load being transported (if any)
    /// </summary>
    public Guid? ActiveLoadId { get; set; }

    [ForeignKey(nameof(ActiveLoadId))]
    public Load? ActiveLoad { get; set; }
}
