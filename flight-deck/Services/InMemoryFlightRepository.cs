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
            AddDummyData(); // Add dummy data on creation
        }

        private void AddDummyData()
        {
            _logger.LogInformation("Generating 5 specific dummy flights timed for status changes in the next ~60 seconds...");
            _flights.Clear(); // Clear existing flights before adding new ones
            var generatedFlights = new List<Flight>();
            var now = DateTime.UtcNow; // Reference time for calculations

            // Keep definitions for generating flight details
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

            // --- Generate 5 flights timed for specific status changes ---

            // 1. ~10s: Scheduled -> Boarding (Threshold is +30 mins)
            //    Set departure time slightly less than 30 mins from now.
            var time1 = now.AddMinutes(30).AddSeconds(10);
            generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = time1, Gate = getRandomGate() });
            _logger.LogTrace("Added flight timed for Scheduled->Boarding at {Time} (in ~10s). Current Status: {Status}", time1, GetPotentialFlightStatus(time1, now));

            // 2. ~20s: Boarding -> Departed (Threshold is +10 mins)
            //    Set departure time slightly less than 10 mins from now.
            var time2 = now.AddMinutes(10).AddSeconds(20);
            generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = time2, Gate = getRandomGate() });
            _logger.LogTrace("Added flight timed for Boarding->Departed at {Time} (in ~20s). Current Status: {Status}", time2, GetPotentialFlightStatus(time2, now));

            // 3. ~30s: Departed -> Delayed (Threshold is -15 mins)
            //    Set departure time slightly less than 15 mins *ago*.
            //    At t=0, diff is just above -15m (Departed). At t=31s, diff drops below -15m (Delayed).
            var time3 = now.AddMinutes(-15).AddSeconds(30);
            generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = time3, Gate = getRandomGate() });
            _logger.LogTrace("Added flight timed for Departed->Delayed at {Time} (in ~30s). Current Status: {Status}", time3, GetPotentialFlightStatus(time3, now));

            // 4. ~40s: Delayed -> Landed (Threshold is -60 mins)
            //    Set departure time slightly less than 60 mins *ago*.
            //    At t=0, diff is just above -60m (Delayed). At t=41s, diff drops below -60m (Landed).
            var time4 = now.AddMinutes(-60).AddSeconds(40);
            generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = time4, Gate = getRandomGate() });
            _logger.LogTrace("Added flight timed for Delayed->Landed at {Time} (in ~40s). Current Status: {Status}", time4, GetPotentialFlightStatus(time4, now));

            // 5. ~50s: Another Boarding -> Departed (Threshold is +10 mins)
            //    Set departure time slightly less than 10 mins from now.
            var time5 = now.AddMinutes(10).AddSeconds(50);
            generatedFlights.Add(new Flight { Id = Guid.NewGuid(), FlightNumber = getUniqueFlightNumber(), Destination = getRandomDestination(), DepartureTime = time5, Gate = getRandomGate() });
            _logger.LogTrace("Added flight timed for Boarding->Departed at {Time} (in ~50s). Current Status: {Status}", time5, GetPotentialFlightStatus(time5, now));


            // --- No need to fill remaining flights ---

            // Shuffle the list (optional, but keeps it less predictable if order matters elsewhere)
            // generatedFlights = generatedFlights.OrderBy(f => _random.Next()).ToList(); // Can be commented out if order doesn't matter

            // Add all 5 flights to dictionary
            foreach (var flight in generatedFlights)
            {
                if (!_flights.TryAdd(flight.Id, flight))
                {
                    // Log warning if add fails, though unlikely with cleared dictionary and new Guids
                    _logger.LogWarning("Failed to add flight {FlightNumber} ({Id}) during initial dummy data load.", flight.FlightNumber, flight.Id);
                }
            }

            _logger.LogInformation("Added exactly {Count} dummy flights to in-memory repository, timed for status changes.", _flights.Count);
            LogStatusDistribution(now); // Log initial distribution
        }

        // Helper to log distribution (optional) - NO CHANGE HERE
        private void LogStatusDistribution(DateTime referenceTime)
        {
            if (!_flights.Any()) return; // Avoid division by zero or logging empty status

            var statusCounts = _flights.Values
                .Select(f => GetPotentialFlightStatus(f.DepartureTime, referenceTime))
                .GroupBy(status => status)
                .ToDictionary(g => g.Key, g => g.Count());

            _logger.LogDebug("Initial Flight Status Distribution ({Total} flights):", _flights.Count);
            // Ensure all potential statuses are logged, even if count is 0
            var allStatuses = new[] { "Scheduled", "Boarding", "Departed", "Delayed", "Landed" };
            foreach (var status in allStatuses.OrderBy(s => s))
            {
                _logger.LogDebug("- {Status}: {Count}", status, statusCounts.TryGetValue(status, out var count) ? count : 0);
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
