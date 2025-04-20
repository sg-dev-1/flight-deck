using System.ComponentModel.DataAnnotations;

namespace FlightDeck.Models.DTOs
{
    public class CreateFlightRequest
    {
        [Required(ErrorMessage = "Flight number is required.")]
        [StringLength(10, ErrorMessage = "Flight number cannot exceed 10 characters.")]
        public string FlightNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Destination is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Departure time is required.")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "Gate is required.")]
        [StringLength(10, ErrorMessage = "Gate cannot exceed 10 characters.")]
        public string Gate { get; set; } = string.Empty;
    }
}
