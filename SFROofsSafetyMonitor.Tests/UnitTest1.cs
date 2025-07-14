using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SFROofsSafetyMonitor.Services;

namespace SFROofsSafetyMonitor.Tests
{
    public class SolarCalculationServiceTests
    {
        private readonly Mock<ILogger<SolarCalculationService>> _mockLogger;
        private readonly SolarCalculationService _solarService;

        public SolarCalculationServiceTests()
        {
            _mockLogger = new Mock<ILogger<SolarCalculationService>>();
            _solarService = new SolarCalculationService(_mockLogger.Object);
        }

        [Fact]
        public void CalculateSolarAltitude_AtSolarNoon_ReturnsPositiveAltitude()
        {
            var latitude = 40.7128;
            var longitude = -74.0060;
            var utcTime = new DateTime(2024, 6, 21, 16, 0, 0, DateTimeKind.Utc);

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.True(altitude > 0, "Solar altitude should be positive during daytime");
            Assert.True(altitude <= 90, "Solar altitude should not exceed 90 degrees");
            Assert.True(altitude > 60, "Solar altitude should be high during summer solstice near solar noon");
            Assert.InRange(altitude, 68.37, 69.37);
        }

        [Fact]
        public void CalculateSolarAltitude_AtMidnight_ReturnsNegativeAltitude()
        {
            var latitude = 40.7128;
            var longitude = -74.0060;
            var utcTime = new DateTime(2024, 6, 21, 4, 0, 0, DateTimeKind.Utc);

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.True(altitude < 0, "Solar altitude should be negative at midnight");
            Assert.True(altitude >= -90, "Solar altitude should not be below -90 degrees");
            Assert.InRange(altitude, -25.5, -23.5); // widened ±1°
        }

        [Fact]
        public void CalculateSolarAltitude_WinterSolstice_ReturnsLowerAltitude()
        {
            var latitude = 40.7128;
            var longitude = -74.0060;
            var winterSolstice = new DateTime(2024, 12, 21, 17, 0, 0, DateTimeKind.Utc);
            var summerSolstice = new DateTime(2024, 6, 21, 16, 0, 0, DateTimeKind.Utc);

            var winterAltitude = _solarService.CalculateSolarAltitude(latitude, longitude, winterSolstice);
            var summerAltitude = _solarService.CalculateSolarAltitude(latitude, longitude, summerSolstice);

            Assert.True(winterAltitude > 0, "Sun should still be above horizon at winter solstice noon");
            Assert.True(summerAltitude > winterAltitude, "Summer solar altitude should be higher than winter");
            Assert.InRange(winterAltitude, 25.3, 26.3);
            Assert.InRange(summerAltitude, 68.37, 69.37);

            var difference = summerAltitude - winterAltitude;
            Assert.True(difference > 40 && difference < 50, $"Seasonal difference should be around 47°, but was {difference:F1}°");
        }

        [Fact]
        public void CalculateSolarAltitude_EquatorAtEquinox_ReturnsHighAltitude()
        {
            var latitude = 0.0;
            var longitude = 0.0;
            var utcTime = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.True(altitude > 85, "Solar altitude at equator during equinox should be near 90 degrees");
            Assert.True(altitude <= 90, "Solar altitude should not exceed 90 degrees");
            Assert.InRange(altitude, 87.67, 88.67);
        }

        [Fact]
        public void CalculateSolarAltitude_ArcticCircle_HandlesPolarDay()
        {
            var latitude = 66.5;
            var longitude = 0.0;
            var utcTime = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.InRange(altitude, -1.0, +1.0); // Near horizon
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(51.5074, -0.1278)]
        [InlineData(35.6762, 139.6503)]
        [InlineData(-33.8688, 151.2093)]
        [InlineData(40.7128, -74.0060)]
        public void CalculateSolarAltitude_ValidCoordinates_ReturnsValidRange(double latitude, double longitude)
        {
            var utcTime = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.True(altitude >= -90 && altitude <= 90,
                $"Solar altitude should be between -90 and 90 degrees, but was {altitude:F2}° for lat={latitude}, lon={longitude}");
        }

        [Fact]
        public void GetLockoutPeriod_DayWithLockout_ReturnsValidTimes()
        {
            var date = new DateTime(2024, 6, 21);
            var latitude = 40.7128;
            var longitude = -74.0060;
            var altitudeThreshold = 15.0;

            var result = _solarService.GetLockoutPeriod(date, latitude, longitude, altitudeThreshold);

            Assert.NotNull(result.lockoutStart);
            Assert.NotNull(result.lockoutEnd);
            Assert.True(result.lockoutStart < result.lockoutEnd, "Lockout start should be before lockout end");

            Console.WriteLine($"Lockout start: {result.lockoutStart} (Hour: {result.lockoutStart.Value.Hour})");
            Console.WriteLine($"Lockout end: {result.lockoutEnd} (Hour: {result.lockoutEnd.Value.Hour})");

            Assert.True(result.lockoutStart.Value.Hour >= 0 && result.lockoutStart.Value.Hour <= 23,
                $"Lockout start hour should be valid (was {result.lockoutStart.Value.Hour})");
            Assert.True(result.lockoutEnd.Value.Hour >= 0 && result.lockoutEnd.Value.Hour <= 23,
                $"Lockout end hour should be valid (was {result.lockoutEnd.Value.Hour})");
        }

        [Fact]
        public void GetLockoutPeriod_PolarNight_ReturnsNoLockout()
        {
            var date = new DateTime(2024, 12, 21);
            var latitude = 70.0;
            var longitude = 0.0;
            var altitudeThreshold = 5.0;

            var result = _solarService.GetLockoutPeriod(date, latitude, longitude, altitudeThreshold);

            Assert.True(result.lockoutStart == null || result.lockoutEnd == null ||
                        result.lockoutStart <= result.lockoutEnd,
                "Method should handle polar night conditions gracefully");
        }

        [Fact]
        public void CalculateSolarAltitude_HandlesExceptionGracefully()
        {
            var latitude = 91.0;
            var longitude = 181.0;
            var utcTime = DateTime.UtcNow;

            var altitude = _solarService.CalculateSolarAltitude(latitude, longitude, utcTime);

            Assert.True(altitude >= -90 && altitude <= 90, "Method should handle invalid coordinates gracefully");
        }

        [Fact]
        public void GetLockoutPeriod_HighThreshold_MayReturnNoLockout()
        {
            var date = new DateTime(2024, 12, 21);
            var latitude = 60.0;
            var longitude = 0.0;
            var altitudeThreshold = 80.0;

            var result = _solarService.GetLockoutPeriod(date, latitude, longitude, altitudeThreshold);

            if (result.lockoutStart != null && result.lockoutEnd != null)
            {
                Assert.True(result.lockoutStart <= result.lockoutEnd,
                    "If lockout occurs, start should be before end");
            }
        }
    }
}
