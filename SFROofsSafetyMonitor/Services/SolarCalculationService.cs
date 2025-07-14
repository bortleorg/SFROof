using SFROofsSafetyMonitor.Models;
using CosineKitty;

namespace SFROofsSafetyMonitor.Services;

public class SolarCalculationService
{
    private readonly ILogger<SolarCalculationService> _logger;

    public SolarCalculationService(ILogger<SolarCalculationService> logger)
    {
        _logger = logger;
    }

    public double CalculateSolarAltitude(double latitude, double longitude, DateTime utcTime)
    {
        try
        {
            // Create observer location
            var observer = new Observer(latitude, longitude, 0.0); // 0.0 elevation for now
            
            // Create time object from UTC DateTime
            var time = new AstroTime(utcTime);
            
            // Get equatorial coordinates of the Sun
            var sunEquatorial = Astronomy.Equator(Body.Sun, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
            
            // Convert to horizontal coordinates (altitude/azimuth) for the observer
            var sunHorizontal = Astronomy.Horizon(time, observer, sunEquatorial.ra, sunEquatorial.dec, Refraction.Normal);
            
            return sunHorizontal.altitude;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating solar altitude for {Lat},{Lon} at {Time}", 
                latitude, longitude, utcTime);
            return 0.0; // Default to 0 if calculation fails
        }
    }

    /// <summary>
    /// Calculate when the sun crosses a specific altitude threshold
    /// </summary>
    /// <param name="date">The date to calculate for (local time)</param>
    /// <param name="latitude">Observatory latitude in degrees</param>
    /// <param name="longitude">Observatory longitude in degrees</param>
    /// <param name="altitudeThreshold">Altitude threshold in degrees</param>
    /// <returns>Tuple of (lockout start time, lockout end time) in local time, or null if no lockout occurs</returns>
    public (DateTime? lockoutStart, DateTime? lockoutEnd) GetLockoutPeriod(DateTime date, double latitude, double longitude, double altitudeThreshold)
    {
        try
        {
            // Create observer location
            var observer = new Observer(latitude, longitude, 0.0);
            
            // Convert to UTC for calculations
            var utcDate = date.ToUniversalTime().Date;
            
            // Use Astronomy Engine's built-in search functions for more precision
            var startTime = new AstroTime(utcDate);
            var endTime = new AstroTime(utcDate.AddDays(1));
            
            DateTime? lockoutStart = null;
            DateTime? lockoutEnd = null;
            
            // Search for times when sun altitude crosses the threshold
            try
            {
                // Search for when sun rises above threshold (ascending crossing)
                var risingTime = Astronomy.SearchAltitude(Body.Sun, observer, Direction.Rise, startTime, 1.0, altitudeThreshold);
                if (risingTime != null)
                {
                    lockoutStart = risingTime.ToUtcDateTime().ToLocalTime();
                }
                
                // Search for when sun sets below threshold (descending crossing)  
                var settingTime = Astronomy.SearchAltitude(Body.Sun, observer, Direction.Set, startTime, 1.0, altitudeThreshold);
                if (settingTime != null)
                {
                    lockoutEnd = settingTime.ToUtcDateTime().ToLocalTime();
                }
                
                // If we found a setting time but no rising time, the sun might already be above threshold at start of day
                if (lockoutEnd != null && lockoutStart == null)
                {
                    var initialAltitude = CalculateSolarAltitude(latitude, longitude, utcDate);
                    if (initialAltitude > altitudeThreshold)
                    {
                        lockoutStart = date.Date; // Start of day
                    }
                }
                
                // If we found a rising time but no setting time, the sun might still be above threshold at end of day
                if (lockoutStart != null && lockoutEnd == null)
                {
                    var finalAltitude = CalculateSolarAltitude(latitude, longitude, utcDate.AddDays(1).AddSeconds(-1));
                    if (finalAltitude > altitudeThreshold)
                    {
                        lockoutEnd = date.Date.AddDays(1).AddSeconds(-1); // End of day
                    }
                }
            }
            catch (Exception searchEx)
            {
                _logger.LogWarning(searchEx, "Precise altitude search failed, falling back to sampling method");
                return GetLockoutPeriodFallback(date, latitude, longitude, altitudeThreshold);
            }
            
            return (lockoutStart, lockoutEnd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating lockout period for {Date} at {Lat},{Lon}", 
                date, latitude, longitude);
            return (null, null);
        }
    }
    
    private (DateTime? lockoutStart, DateTime? lockoutEnd) GetLockoutPeriodFallback(DateTime date, double latitude, double longitude, double altitudeThreshold)
    {
        // Fallback to the original sampling method if precise search fails
        var utcDate = date.ToUniversalTime().Date;
        DateTime? lockoutStart = null;
        DateTime? lockoutEnd = null;
        
        // Check every 10 minutes throughout the day
        for (int minutes = 0; minutes < 1440; minutes += 10)
        {
            var checkTime = utcDate.AddMinutes(minutes);
            var altitude = CalculateSolarAltitude(latitude, longitude, checkTime);
            
            if (altitude > altitudeThreshold)
            {
                if (lockoutStart == null)
                {
                    lockoutStart = checkTime.ToLocalTime();
                }
            }
            else if (lockoutStart != null && lockoutEnd == null)
            {
                lockoutEnd = checkTime.ToLocalTime();
                break;
            }
        }
        
        return (lockoutStart, lockoutEnd);
    }
}
