using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using t12Project.Data;
using t12Project.Models;

namespace t12Project.Services;

public class PricingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PricingService> logger)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Calculate price for a load
    /// </summary>
    public async Task<PriceEstimate> CalculatePriceAsync(PriceCalculationRequest request)
    {
        // Get pricing tier for category and weight
        var tier = await GetPricingTierAsync(request.LoadCategory, request.WeightKg);

        if (tier == null)
        {
            _logger.LogWarning("No pricing tier found for {Category}, using default General tier", request.LoadCategory);
            tier = await GetPricingTierAsync(LoadCategory.General, request.WeightKg);

            if (tier == null)
            {
                throw new InvalidOperationException("No default pricing tier configured. Please seed pricing data.");
            }
        }

        // Calculate distance using Mapbox Directions API
        var distanceKm = await CalculateDistanceAsync(
            (decimal)request.PickupLat,
            (decimal)request.PickupLng,
            (decimal)request.DeliveryLat,
            (decimal)request.DeliveryLng
        );

        // Calculate price components
        var baseFare = tier.BaseFare;
        var distanceCost = (decimal)distanceKm * tier.PricePerKm;
        var weightCost = request.WeightKg * tier.PricePerKg;

        // Check for surge pricing (peak hours: 7-9 AM, 4-7 PM on weekdays)
        var surgeMultiplier = GetSurgeMultiplier();
        var subTotal = baseFare + distanceCost + weightCost;
        var surgeCharge = surgeMultiplier > 1.0m ? (subTotal * (surgeMultiplier - 1.0m)) : 0;

        subTotal += surgeCharge;

        // Platform fee: 15%
        var platformFee = subTotal * 0.15m;
        var totalPrice = subTotal;
        var driverEarnings = subTotal - platformFee;

        return new PriceEstimate
        {
            BaseFare = baseFare,
            DistanceCost = distanceCost,
            WeightCost = weightCost,
            SurgeCharge = surgeCharge,
            SubTotal = subTotal,
            PlatformFee = platformFee,
            TotalPrice = totalPrice,
            DriverEarnings = driverEarnings,
            DistanceKm = distanceKm,
            WeightKg = request.WeightKg,
            LoadCategory = request.LoadCategory,
            SurgeMultiplier = surgeMultiplier
        };
    }

    /// <summary>
    /// Get applicable pricing tier for category and weight
    /// </summary>
    private async Task<PricingTier?> GetPricingTierAsync(string category, int weightKg)
    {
        return await _context.PricingTiers
            .Where(t => t.LoadCategory == category)
            .Where(t => weightKg >= t.MinWeightKg && (t.MaxWeightKg == 0 || weightKg <= t.MaxWeightKg))
            .OrderBy(t => t.MinWeightKg)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Calculate road distance using Mapbox Directions API
    /// </summary>
    private async Task<double> CalculateDistanceAsync(
        decimal pickupLat,
        decimal pickupLng,
        decimal deliveryLat,
        decimal deliveryLng)
    {
        var mapboxToken = _configuration["Mapbox:AccessToken"];

        if (string.IsNullOrEmpty(mapboxToken))
        {
            _logger.LogWarning("Mapbox token not configured, falling back to straight-line distance");
            return CalculateStraightLineDistance(pickupLat, pickupLng, deliveryLat, deliveryLng);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var coordinates = $"{pickupLng},{pickupLat};{deliveryLng},{deliveryLat}";
            var url = $"https://api.mapbox.com/directions/v5/mapbox/driving/{coordinates}?access_token={mapboxToken}&geometries=geojson";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mapbox API error: {StatusCode}", response.StatusCode);
                return CalculateStraightLineDistance(pickupLat, pickupLng, deliveryLat, deliveryLng);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(json);

            // Distance is in meters, convert to kilometers
            var distanceMeters = result.RootElement
                .GetProperty("routes")[0]
                .GetProperty("distance")
                .GetDouble();

            return distanceMeters / 1000.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance with Mapbox, falling back to straight-line");
            return CalculateStraightLineDistance(pickupLat, pickupLng, deliveryLat, deliveryLng);
        }
    }

    /// <summary>
    /// Calculate straight-line distance using Haversine formula (fallback)
    /// </summary>
    private double CalculateStraightLineDistance(
        decimal lat1,
        decimal lon1,
        decimal lat2,
        decimal lon2)
    {
        const double R = 6371; // Earth radius in kilometers

        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        // Add 30% buffer since straight-line is shorter than road distance
        return R * c * 1.3;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Get surge multiplier based on current time
    /// Peak hours: 7-9 AM, 4-7 PM on weekdays
    /// </summary>
    private decimal GetSurgeMultiplier()
    {
        var now = DateTime.Now;

        // Weekend - no surge
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            return 1.0m;

        var hour = now.Hour;

        // Morning rush: 7-9 AM (1.3x)
        if (hour >= 7 && hour < 9)
            return 1.3m;

        // Evening rush: 4-7 PM (1.5x)
        if (hour >= 16 && hour < 19)
            return 1.5m;

        return 1.0m;
    }

    /// <summary>
    /// Seed default pricing tiers
    /// </summary>
    public async Task SeedPricingTiersAsync()
    {
        if (await _context.PricingTiers.AnyAsync())
        {
            _logger.LogInformation("Pricing tiers already seeded");
            return;
        }

        var tiers = new[]
        {
            new PricingTier
            {
                LoadCategory = LoadCategory.Electronics,
                BaseFare = 150.00m,
                PricePerKm = 8.50m,
                PricePerKg = 2.00m,
                MinWeightKg = 0,
                MaxWeightKg = 500,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Furniture,
                BaseFare = 200.00m,
                PricePerKm = 10.00m,
                PricePerKg = 1.50m,
                MinWeightKg = 0,
                MaxWeightKg = 2000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Food,
                BaseFare = 100.00m,
                PricePerKm = 7.00m,
                PricePerKg = 2.50m,
                MinWeightKg = 0,
                MaxWeightKg = 1000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Construction,
                BaseFare = 250.00m,
                PricePerKm = 12.00m,
                PricePerKg = 1.00m,
                MinWeightKg = 0,
                MaxWeightKg = 5000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Vehicles,
                BaseFare = 500.00m,
                PricePerKm = 15.00m,
                PricePerKg = 0.50m,
                MinWeightKg = 1000,
                MaxWeightKg = 10000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Chemicals,
                BaseFare = 300.00m,
                PricePerKm = 14.00m,
                PricePerKg = 3.00m,
                MinWeightKg = 0,
                MaxWeightKg = 2000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.General,
                BaseFare = 120.00m,
                PricePerKm = 8.00m,
                PricePerKg = 1.50m,
                MinWeightKg = 0,
                MaxWeightKg = 10000,
                SurgeMultiplier = 1.0m
            },
            new PricingTier
            {
                LoadCategory = LoadCategory.Fragile,
                BaseFare = 180.00m,
                PricePerKm = 9.50m,
                PricePerKg = 2.20m,
                MinWeightKg = 0,
                MaxWeightKg = 800,
                SurgeMultiplier = 1.0m
            }
        };

        _context.PricingTiers.AddRange(tiers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} pricing tiers", tiers.Length);
    }
}
