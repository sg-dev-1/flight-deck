using System.ComponentModel.DataAnnotations;

namespace FlightDeck.Models
{
    public class Flight
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(10)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        [StringLength(10)]
        public string Gate { get; set; } = string.Empty;

        public string Status => CalculateFlightStatus(DepartureTime);

        private static string CalculateFlightStatus(DateTime departureTime)
        {
            var now = DateTime.UtcNow;
            TimeSpan diff = departureTime - now;
            double diffMinutes = diff.TotalMinutes;

            if (diffMinutes > 30) { return "Scheduled"; }
            if (diffMinutes > 10) { return "Boarding"; }
            if (diffMinutes >= -60) { return "Departed"; }

            return "Scheduled";
        }
    }
}
