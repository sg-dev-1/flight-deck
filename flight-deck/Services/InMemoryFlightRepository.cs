using FlightDeck.IServices;
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
            AddDummyData(120); // Add dummy data on creation
        }

        private void AddDummyData(int totalNumberOfFlights)
        {
            _logger.LogInformation("Generating {Count} specific dummy flights...", totalNumberOfFlights);
            _flights.Clear();
            var generatedFlights = new List<Flight>();
            var now = DateTime.UtcNow;

            var destinations = new List<string> {
                "London", "Paris", "New York", "Tokyo", "Dubai", "Singapore",
                "Frankfurt", "Amsterdam", "Los Angeles", "Chicago", "Rome", "Madrid"
            };
            var airlines = new List<string> { "BA", "AF", "LH", "AA", "DL", "UA", "EK", "SQ", "IB" };

            int flightCounter = 0;
            Func<string> getUniqueFlightNumber = () => {
                var airlineCode = airlines[_random.Next(airlines.Count)];
                return $"{airlineCode}{1000 + flightCounter++}";
            };
            Func<string> getRandomDestination = () => destinations[_random.Next(destinations.Count)];
            Func<string> getRandomGate = () => $"{(char)('A' + _random.Next(6))}{_random.Next(1, 21)}";

            // --- Generate flights for specific statuses ---
            int countPerStatus = 20;

            // 1. Landed (Departed > 60 mins ago)
            for (int i = 0; i < countPerStatus; i++)
            {
                var departureTime = now.AddMinutes(_random.Next(-180, -61)); // e.g., 1-3 hours ago
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // 2. Delayed (Departed 15-60 mins ago)
            for (int i = 0; i < countPerStatus; i++)
            {
                var departureTime = now.AddMinutes(_random.Next(-60, -15));
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // 3. Departed (Departed 0-15 mins ago)
            for (int i = 0; i < countPerStatus; i++)
            {
                var departureTime = now.AddMinutes(_random.Next(-15, 0));
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // 4. Boarding (Departing in 11-30 mins)
            for (int i = 0; i < countPerStatus; i++)
            {
                var departureTime = now.AddMinutes(_random.Next(11, 31));
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // 5. Scheduled (Departing > 30 mins from now)
            for (int i = 0; i < countPerStatus; i++)
            {
                var departureTime = now.AddMinutes(_random.Next(31, 180)); // e.g., 31 mins to 3 hours away
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }


            // --- Generate flights near status transitions (approx 1-4 min away) ---
            int countPerTransition = 5; // 5 * 4 transitions = 20 flights

            // A. About to become Boarding (Currently Scheduled)
            for (int i = 0; i < countPerTransition; i++)
            {
                var departureTime = now.AddMinutes(30 + (_random.NextDouble() * 3 + 1)); // 31-34 mins away
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // B. About to become Departed (Currently Boarding)
            for (int i = 0; i < countPerTransition; i++)
            {
                var departureTime = now.AddMinutes(10 + (_random.NextDouble() * 3 + 1)); // 11-14 mins away
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // C. About to become Delayed (Currently Departed)
            for (int i = 0; i < countPerTransition; i++)
            {
                var departureTime = now.AddMinutes(-15 + (_random.NextDouble() * 3 + 1)); // -14 to -11 mins ago
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            // D. About to become Landed (Currently Delayed)
            for (int i = 0; i < countPerTransition; i++)
            {
                var departureTime = now.AddMinutes(-60 + (_random.NextDouble() * 3 + 1)); // -59 to -56 mins ago
                generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
            }

            int remainingFlights = totalNumberOfFlights - generatedFlights.Count;
            if (remainingFlights > 0)
            {
                _logger.LogDebug("Generating {Count} additional random flights to meet total.", remainingFlights);
                for (int i = 0; i < remainingFlights; i++)
                {
                    // Add more flights, perhaps skewing towards future departures
                    var departureTime = now.AddMinutes(_random.Next(5, 240)); // 5 mins to 4 hours away
                    generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = departureTime, Gate = getRandomGate() });
                }
            }

            // Shuffle the list before adding to dictionary for less predictable order
            generatedFlights = generatedFlights.OrderBy(f => _random.Next()).ToList();

            // Add all generated flights to the concurrent dictionary
            foreach (var flight in generatedFlights)
            {
                _flights.TryAdd(flight.Id, flight);
            }

            _logger.LogInformation("Added {Count} dummy flights ({ActualCount} actual) to in-memory repository.", totalNumberOfFlights, _flights.Count);

            // Log status distribution for verification
            LogStatusDistribution(now);
        }

        private void LogStatusDistribution(DateTime referenceTime)
        {
            var statusCounts = _flights.Values
                .Select(f => GetPotentialFlightStatus(f.DepartureTime, referenceTime))
                .GroupBy(status => status)
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogDebug("Generated Flight Status Distribution:");
            foreach (var kvp in statusCounts.OrderBy(kvp => kvp.Key))
            {
                _logger.LogDebug("- {Status}: {Count}", kvp.Key, kvp.Value);
            }
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
                var now = DateTime.UtcNow;

                query = query.Where(f => {
                    string calculatedStatus = GetPotentialFlightStatus(f.DepartureTime, now);
                    bool match = calculatedStatus.Equals(status, StringComparison.OrdinalIgnoreCase);

                    _logger.LogTrace("Flight {FlightId} ({CalculatedStatus}) Match '{StatusFilter}'? -> {MatchResult}",
                                     f.Id, calculatedStatus, status, match);
                    return match;
                });

                _logger.LogDebug("Flight count after status filter: {Count}", query.Count());
            }

            var result = query.OrderBy(f => f.DepartureTime).ToList();
            _logger.LogInformation("Returning {Count} flights after filtering and ordering.", result.Count);

            return Task.FromResult(result.AsEnumerable());
        }

        public static string GetPotentialFlightStatus(DateTime departureTime, DateTime currentTime)
        {
            TimeSpan diff = departureTime - currentTime;
            double diffMinutes = diff.TotalMinutes;
            string status = "Scheduled";

            if (diffMinutes > 30)
            {
                status = "Scheduled";
            }
            else if (diffMinutes > 10)
            {
                status = "Boarding";
            }
            else if (diffMinutes >= -60)
            {
                status = "Departed";
            }

            if (diffMinutes < -15)
            {
                status = "Delayed";
            }

            if (diffMinutes < -60)
            {
                status = "Landed";
            }

            return status;
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

        public Task<Flight?> AddFlightAsync(Flight flight)
        {
            if (flight.Id == Guid.Empty) { flight.Id = Guid.NewGuid(); }

            if (_flights.TryAdd(flight.Id, flight))
            {
                _logger.LogInformation("Flight {FlightNumber} ({Id}) added to repository.", flight.FlightNumber, flight.Id);
                return Task.FromResult<Flight?>(flight);
            }
            else
            {
                _logger.LogWarning("Failed to add flight {FlightNumber} ({Id}) to ConcurrentDictionary. This might indicate a concurrency issue or duplicate Guid if checks failed.", flight.FlightNumber, flight.Id);
                return Task.FromResult<Flight?>(null);
            }
        }

        public Task<bool> DeleteFlightAsync(Guid id)
        {
            var removed = _flights.TryRemove(id, out _);
            if (removed)
            {
                _logger.LogInformation("Flight with ID {Id} removed from repository.", id);
            }
            else
            {
                _logger.LogWarning("Attempted to remove flight with ID {Id}, but it was not found in the repository.", id);
            }
            return Task.FromResult(removed);
        }
    }
}
