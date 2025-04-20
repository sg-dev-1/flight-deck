using FlightDeck.Models;
using Serilog;
using System;
using System.Collections.Concurrent;

namespace FlightDeck.Services
{
    public class InMemoryFlightRepository : IFlightRepository
    {
        private readonly ConcurrentDictionary<Guid, Flight> _flights = new ConcurrentDictionary<Guid, Flight>();
        private static readonly Random _random = new Random();

        public InMemoryFlightRepository()
        {
            AddDummyData(50);
        }

        private void AddDummyData(int numberOfFlights)
        {
            var now = DateTime.UtcNow;
            var destinations = new List<string> {
                "London", "Paris", "New York", "Tokyo", "Dubai", "Singapore",
                "Frankfurt", "Amsterdam", "Los Angeles", "Chicago", "Rome", "Madrid"
            };
            var airlines = new List<string> { "BA", "AF", "LH", "AA", "DL", "UA", "EK", "SQ", "IB" };

            _flights.Clear();

            for (int i = 0; i < numberOfFlights; i++)
            {
                var airlineCode = airlines[_random.Next(airlines.Count)];
                var flightNumber = $"{airlineCode}{1000 + i}"; // Ensure unique flight numbers for this set
                var destination = destinations[_random.Next(destinations.Count)];
                // Generate times around now: -2 hours to +6 hours
                var departureTime = now.AddMinutes(_random.Next(-120, 360));
                var gate = $"{(char)('A' + _random.Next(6))}{_random.Next(1, 21)}"; // Gate A1 to F20

                var flight = new Flight
                {
                    Id = Guid.NewGuid(),
                    FlightNumber = flightNumber,
                    Destination = destination,
                    DepartureTime = departureTime,
                    Gate = gate
                };

                _flights.TryAdd(flight.Id, flight);
            }
            Log.Information("Added {Count} dummy flights to in-memory repository.", _flights.Count);
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
