using Microsoft.AspNetCore.Mvc;
using FlightDeck.Models;
using FlightDeck.Models.DTOs;
using FlightDeck.Services;
using Microsoft.AspNetCore.SignalR;
using FlightDeck.Hubs;

namespace FlightDeck.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightRepository _flightRepository;
        private readonly ILogger<FlightsController> _logger;
        private readonly IHubContext<FlightHub> _hubContext;

        public FlightsController(
            IFlightRepository flightRepository,
            ILogger<FlightsController> logger,
            IHubContext<FlightHub> hubContext)
        {
            _flightRepository = flightRepository ?? throw new ArgumentNullException(nameof(flightRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Flight>>> GetFlights(
            [FromQuery] string? destination,
            [FromQuery] string? status)
        {
            _logger.LogInformation("Getting flights with filter Destination={Destination}, Status={Status}", destination, status);
            try
            {
                var flights = await _flightRepository.GetAllFlightsAsync(destination, status);
                return Ok(flights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting flights.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Flight>> GetFlight(Guid id)
        {
            _logger.LogInformation("Getting flight with ID: {Id}", id);
            try
            {
                var flight = await _flightRepository.GetFlightByIdAsync(id);
                if (flight == null) { return NotFound(); }
                return Ok(flight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting flight with ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Flight>> AddFlight([FromBody] CreateFlightRequest request)
        {
            _logger.LogInformation("Attempting to add flight: {FlightNumber}", request.FlightNumber);

            // --- Pre-checks remain the same ---
            if (!ModelState.IsValid) { return BadRequest(ModelState); }
            if (request.DepartureTime <= DateTime.UtcNow) { ModelState.AddModelError(nameof(request.DepartureTime), "Departure time must be in the future."); return BadRequest(ModelState); }
            var existingFlight = await _flightRepository.GetFlightByNumberAsync(request.FlightNumber);
            if (existingFlight != null) { ModelState.AddModelError(nameof(request.FlightNumber), $"Flight number {request.FlightNumber} already exists."); return Conflict(ModelState); }
            // --- End of Pre-checks ---

            try
            {
                var newFlight = new Flight
                {
                    Id = Guid.NewGuid(),
                    FlightNumber = request.FlightNumber,
                    Destination = request.Destination,
                    DepartureTime = request.DepartureTime,
                    Gate = request.Gate
                };

                var addedFlight = await _flightRepository.AddFlightAsync(newFlight);

                if (addedFlight == null)
                {
                    _logger.LogError("Repository failed to add flight {FlightNumber} unexpectedly after controller checks passed.", request.FlightNumber);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected internal error occurred while saving the flight.");
                }

                _logger.LogInformation("Flight added successfully with ID: {Id}", addedFlight.Id);

                await _hubContext.Clients.All.SendAsync("FlightAdded", addedFlight);
                _logger.LogInformation("Sent SignalR 'FlightAdded' message for ID: {Id}", addedFlight.Id);

                return CreatedAtAction(nameof(GetFlight), new { id = addedFlight.Id }, addedFlight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected exception occurred while processing AddFlight request for: {FlightNumber}", request.FlightNumber);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteFlight(Guid id)
        {
            _logger.LogInformation("Attempting to delete flight with ID: {Id}", id);
            try
            {
                var flightToDelete = await _flightRepository.GetFlightByIdAsync(id);

                if (flightToDelete == null)
                {
                    _logger.LogWarning("Delete flight failed: Flight with ID {Id} not found.", id);
                    return NotFound();
                }

                var success = await _flightRepository.DeleteFlightAsync(id);

                if (!success)
                {
                    _logger.LogError("Delete flight failed unexpectedly after confirming existence for ID: {Id}", id);
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the flight from the repository.");
                }

                _logger.LogInformation("Flight with ID: {Id} ({FlightNumber}) deleted successfully.", id, flightToDelete.FlightNumber);

                await _hubContext.Clients.All.SendAsync("FlightDeleted", flightToDelete);
                _logger.LogInformation("Sent SignalR 'FlightDeleted' message for ID: {Id} ({FlightNumber})", id, flightToDelete.FlightNumber);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during delete operation for flight ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while processing your delete request.");
            }
        }
    }
}
