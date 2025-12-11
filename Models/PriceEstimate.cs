namespace t12Project.Models;

/// <summary>
/// Price calculation result
/// </summary>
public class PriceEstimate
{
    public decimal BaseFare { get; set; }
    public decimal DistanceCost { get; set; }
    public decimal WeightCost { get; set; }
    public decimal SurgeCharge { get; set; }
    public decimal SubTotal { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DriverEarnings { get; set; }

    public double DistanceKm { get; set; }
    public int WeightKg { get; set; }
    public string LoadCategory { get; set; } = string.Empty;
    public decimal SurgeMultiplier { get; set; }

    public string BreakdownText =>
        $"Base Fare: R{BaseFare:F2}\n" +
        $"Distance ({DistanceKm:F1} km × R{(DistanceKm > 0 ? DistanceCost / (decimal)DistanceKm : 0):F2}/km): R{DistanceCost:F2}\n" +
        $"Weight ({WeightKg} kg × R{(WeightKg > 0 ? WeightCost / WeightKg : 0):F2}/kg): R{WeightCost:F2}\n" +
        (SurgeCharge > 0 ? $"Surge ({SurgeMultiplier:F2}x): R{SurgeCharge:F2}\n" : "") +
        $"Subtotal: R{SubTotal:F2}\n" +
        $"Platform Fee (15%): R{PlatformFee:F2}\n" +
        $"═══════════════════════\n" +
        $"TOTAL: R{TotalPrice:F2}\n" +
        $"Driver Earns: R{DriverEarnings:F2}";
}

/// <summary>
/// Request for price calculation
/// </summary>
public class PriceCalculationRequest
{
    public double PickupLat { get; set; }
    public double PickupLng { get; set; }
    public double DeliveryLat { get; set; }
    public double DeliveryLng { get; set; }
    public string LoadCategory { get; set; } = string.Empty;
    public int WeightKg { get; set; }
}

/// <summary>
/// Load categories with pricing
/// </summary>
public static class LoadCategory
{
    public const string Electronics = "Electronics";
    public const string Furniture = "Furniture";
    public const string Food = "Food";
    public const string Construction = "Construction";
    public const string Vehicles = "Vehicles";
    public const string Chemicals = "Chemicals";
    public const string General = "General";
    public const string Fragile = "Fragile";

    public static readonly string[] All =
    {
        Electronics,
        Furniture,
        Food,
        Construction,
        Vehicles,
        Chemicals,
        General,
        Fragile
    };
}
