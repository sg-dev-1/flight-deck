using FlightDeck.Models;

namespace FlightDeck.Services
{
    public interface IFlightRepository
    {
        Task<IEnumerable<Flight>> GetAllFlightsAsync(string? destination, string? status);
        Task<Flight?> GetFlightByIdAsync(Guid id);
        Task<Flight?> GetFlightByNumberAsync(string flightNumber);
        Task<Flight> AddFlightAsync(Flight flight);
        Task<bool> DeleteFlightAsync(Guid id);
    }
}
