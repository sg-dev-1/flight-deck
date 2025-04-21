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
    }
}
