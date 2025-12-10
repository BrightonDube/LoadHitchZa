using System.ComponentModel.DataAnnotations;

namespace t12Project.Models;

public class Booking
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoadId { get; set; }
    public Load? Load { get; set; }
    [Required]
    public string DriverId { get; set; } = default!;
    public ApplicationUser? Driver { get; set; }
    [MaxLength(40)]
    public string Status { get; set; } = "Requested"; // Requested, Accepted, Rejected, Cancelled
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RespondedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
