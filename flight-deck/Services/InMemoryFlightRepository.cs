using FlightDeck.Models;
using System.Collections.Concurrent;

namespace FlightDeck.Services
{
    public class InMemoryFlightRepository : IFlightRepository
    {
        private readonly ConcurrentDictionary<Guid, Flight> _flights = new ConcurrentDictionary<Guid, Flight>();

        public InMemoryFlightRepository()
        {
            AddDummyData();
        }

        private void AddDummyData()
        {
            var now = DateTime.UtcNow;
            var initialFlights = new List<Flight>
            {
                new Flight { Id = Guid.NewGuid(), FlightNumber = "BA2490", Destination = "London", DepartureTime = now.AddHours(2), Gate = "A1" },
                new Flight { Id = Guid.NewGuid(), FlightNumber = "AF123", Destination = "Paris", DepartureTime = now.AddMinutes(20), Gate = "B2" }, // Should be Boarding soon
                new Flight { Id = Guid.NewGuid(), FlightNumber = "LH987", Destination = "Frankfurt", DepartureTime = now.AddMinutes(-15), Gate = "C3" }, // Should be Departed
                new Flight { Id = Guid.NewGuid(), FlightNumber = "IB543", Destination = "Madrid", DepartureTime = now.AddHours(1), Gate = "D4" }
            };

            foreach (var flight in initialFlights)
            {
                _flights.TryAdd(flight.Id, flight);
            }
        }

        public Task<IEnumerable<Flight>> GetAllFlightsAsync(string? destination, string? status)
        {
            IEnumerable<Flight> result = _flights.Values;

            if (!string.IsNullOrWhiteSpace(destination))
            {
                result = result.Where(f => f.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                result = result.Where(f => f.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(result.OrderBy(f => f.DepartureTime).ToList().AsEnumerable());
        }

        public Task<Flight?> GetFlightByIdAsync(Guid id)
        {
            _flights.TryGetValue(id, out var flight);
            return Task.FromResult(flight);
        }

        public Task<Flight?> GetFlightByNumberAsync(string flightNumber)
        {
            var flight = _flights.Values
                                 .FirstOrDefault(f => f.FlightNumber.Equals(flightNumber, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(flight);
        }

        public Task<Flight> AddFlightAsync(Flight flight)
        {
            if (flight.Id == Guid.Empty)
            {
                flight.Id = Guid.NewGuid();
            }

            if (_flights.TryAdd(flight.Id, flight))
            {
                return Task.FromResult(flight);
            }
            else
            {
                throw new InvalidOperationException($"Failed to add flight. A flight with ID {flight.Id} might already exist.");
            }
        }

        public Task<bool> DeleteFlightAsync(Guid id)
        {
            return Task.FromResult(_flights.TryRemove(id, out _));
        }
    }
}
