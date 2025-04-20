using FlightDeck.Models;
using System.Collections.Concurrent;

namespace FlightDeck.Services
{
    public class InMemoryFlightRepository : IFlightRepository
    {
        private readonly ConcurrentDictionary<Guid, Flight> _flights = new ConcurrentDictionary<Guid, Flight>();
        private static readonly Random _random = new Random();
        private readonly ILogger<InMemoryFlightRepository> _logger;

        public InMemoryFlightRepository(ILogger<InMemoryFlightRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AddDummyData(50); // Add dummy data on creation
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
                var flightNumber = $"{airlineCode}{1000 + i}";
                var destination = destinations[_random.Next(destinations.Count)];
                var departureTime = now.AddMinutes(_random.Next(-120, 360));
                var gate = $"{(char)('A' + _random.Next(6))}{_random.Next(1, 21)}";

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
            _logger.LogInformation("Added {Count} dummy flights to in-memory repository.", _flights.Count);
        }

        public Task<IEnumerable<Flight>> GetAllFlightsAsync(string? destination, string? status)
        {
            _logger.LogInformation("GetAllFlightsAsync called. Destination filter: '{Destination}', Status filter: '{Status}'", destination ?? "None", status ?? "None");

            IEnumerable<Flight> query = _flights.Values;
            _logger.LogDebug("Initial flight count: {Count}", query.Count());

            if (!string.IsNullOrWhiteSpace(destination))
            {
                query = query.Where(f => f.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
                _logger.LogDebug("Flight count after destination filter: {Count}", query.Count());
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                _logger.LogDebug("Applying status filter for: '{Status}'", status);
                foreach (var flight in query)
                {
                    _logger.LogTrace("Checking Flight {FlightId} - Departure: {DepartureTime} - Calculated Status: {CalculatedStatus}",
                                    flight.Id, flight.DepartureTime, flight.Status);
                }

                query = query.Where(f => {
                    bool match = f.Status.Equals(status, StringComparison.OrdinalIgnoreCase);
                    _logger.LogTrace("Flight {FlightId} ({CalculatedStatus}) Match '{StatusFilter}'? -> {MatchResult}",
                                     f.Id, f.Status, status, match);
                    return match;
                });

                _logger.LogDebug("Flight count after status filter: {Count}", query.Count());
            }

            var result = query.OrderBy(f => f.DepartureTime).ToList();
            _logger.LogInformation("Returning {Count} flights after filtering and ordering.", result.Count);

            return Task.FromResult(result.AsEnumerable());
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
            if (flight.Id == Guid.Empty) { flight.Id = Guid.NewGuid(); }
            if (_flights.TryAdd(flight.Id, flight)) { return Task.FromResult(flight); }
            else { throw new InvalidOperationException($"Failed to add flight. A flight with ID {flight.Id} might already exist."); }
        }

        public Task<bool> DeleteFlightAsync(Guid id)
        {
            return Task.FromResult(_flights.TryRemove(id, out _));
        }
    }
}
