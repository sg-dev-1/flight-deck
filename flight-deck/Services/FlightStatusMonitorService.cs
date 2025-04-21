using FlightDeck.Hubs;
using FlightDeck.IServices;
using FlightDeck.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FlightDeck.Services
{
    public class FlightStatusMonitorService : BackgroundService
    {
        private readonly ILogger<FlightStatusMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<FlightHub> _hubContext;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every 1 minute
        private readonly ConcurrentDictionary<Guid, string> _lastKnownStatuses = new ConcurrentDictionary<Guid, string>();

        public FlightStatusMonitorService(
            ILogger<FlightStatusMonitorService> logger,
            IServiceProvider serviceProvider,
            IHubContext<FlightHub> hubContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flight Status Monitor Service starting.");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Initial delay

            using PeriodicTimer timer = new PeriodicTimer(_checkInterval);

            try
            {
                // Initial population of statuses
                await CheckFlightStatusesAsync(stoppingToken);

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CheckFlightStatusesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Flight Status Monitor Service stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in Flight Status Monitor Service.");
            }
        }

        private async Task CheckFlightStatusesAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Checking flight statuses...");
            var now = DateTime.UtcNow;

            using (var scope = _serviceProvider.CreateScope())
            {
                var flightRepository = scope.ServiceProvider.GetRequiredService<IFlightRepository>();
                IEnumerable<Flight> currentFlights;
                try
                {
                    currentFlights = await flightRepository.GetAllFlightsAsync(null, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching flights in background service.");
                    return;
                }

                var currentFlightIds = new HashSet<Guid>(currentFlights.Select(f => f.Id));

                foreach (var flight in currentFlights)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    string newStatus = InMemoryFlightRepository.GetPotentialFlightStatus(flight.DepartureTime, now);

                    if (_lastKnownStatuses.TryGetValue(flight.Id, out string? lastStatus))
                    {
                        if (lastStatus != newStatus)
                        {
                            _logger.LogInformation("Status Change: Flight {Num} ({Id}): {Old} -> {New}", flight.FlightNumber, flight.Id, lastStatus, newStatus);
                            _lastKnownStatuses[flight.Id] = newStatus;
                            await SendStatusUpdateAsync(flight.Id, newStatus);
                        }
                    }
                    else
                    {
                        // Add new flight status to tracking
                        _lastKnownStatuses.TryAdd(flight.Id, newStatus);
                        _logger.LogDebug("Tracking new flight {Num} ({Id}) with status: {Status}", flight.FlightNumber, flight.Id, newStatus);
                    }
                }

                // Clean up removed flights
                var removedFlightIds = _lastKnownStatuses.Keys.Where(id => !currentFlightIds.Contains(id)).ToList();
                foreach (var removedId in removedFlightIds)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    if (_lastKnownStatuses.TryRemove(removedId, out _))
                    {
                        _logger.LogDebug("Stopped tracking status for removed flight {Id}", removedId);
                    }
                }
            }
            _logger.LogDebug("Flight status check complete.");
        }

        private async Task SendStatusUpdateAsync(Guid flightId, string newStatus)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("FlightStatusChanged", new { FlightId = flightId, NewStatus = newStatus });
                _logger.LogTrace("Sent SignalR 'FlightStatusChanged' for {Id} to {Status}", flightId, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR status update for {Id}", flightId);
            }
        }
    }
}